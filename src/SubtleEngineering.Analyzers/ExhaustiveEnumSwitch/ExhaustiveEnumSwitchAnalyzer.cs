﻿namespace SubtleEngineering.Analyzers.ExhaustiveEnumSwitch
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Operations;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExhaustiveEnumSwitchAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1050 = 0;
        private const int SE1051 = 1;
        private const int SE1052 = 2;

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault,
                "Switch expression or statement must check for all declared enum values",
                "Enum type '{0}' is being used with the Exhaustive() extension method - that means it is expected that the switch check all values (including default). Missing values are: {1}",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticsDetails.ExhaustiveEnumSwitch.ExhaustiveExtensionMayOnlyBeUsedWithSwitch,
                $"The Exhaustive() extension method is used incorrectly",
                "The Exhaustive() call on the '{0}' enum may only be used when it's being tested with a switch statement or expression.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchContainsUnsupportedPatterns,
                "Switch contains unsupported patterns for exhaustive enum checking",
                "Switch statement or expression contains patterns that cannot be exhaustively analyzed. Only constant patterns and 'or' patterns of constants are supported.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true)
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;

        public override void Initialize(AnalysisContext context)
        {
            // Enable concurrent execution and configure generated code analysis.
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register an operation action to analyze invocation expressions.
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;

            // Check if the method being called is the 'Exhaustive' extension method.
            if (!IsExhaustive(invocation))
            {
                return;
            }

            // Get the enum type being passed to the 'Exhaustive' method.
            var enumType = invocation.Arguments.FirstOrDefault()?.Value?.Type as INamedTypeSymbol;

            if (enumType == null || enumType.TypeKind != TypeKind.Enum)
            {
                return;
            }

            // Check if the result of 'Exhaustive' is used in a switch statement or expression.
            var parentOperation = invocation.Parent;

            // Flag to indicate if Exhaustive() is used in a switch.
            bool isUsedInSwitch = false;

            while (parentOperation != null)
            {
                if (parentOperation.Kind == OperationKind.Tuple)
                {
                    // Report diagnostic SE1051 if 'Exhaustive' is used within a tuple.
                    var diagnostic = Diagnostic.Create(Rules[SE1051], invocation.Syntax.GetLocation(), enumType.Name);
                    context.ReportDiagnostic(diagnostic);
                    return;
                }
                else if (parentOperation.Kind == OperationKind.Switch || parentOperation.Kind == OperationKind.SwitchExpression)
                {
                    // Check if the switch is directly using the result of 'Exhaustive()' invocation.
                    if (IsOperationUsingExhaustiveValue(parentOperation, invocation))
                    {
                        isUsedInSwitch = true;
                        break;
                    }
                    else
                    {
                        // If the switch is not directly using the 'Exhaustive()' result, and it's used within a tuple,
                        // we consider it a misuse.
                        var diagnostic = Diagnostic.Create(Rules[SE1051], invocation.Syntax.GetLocation(), enumType.Name);
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }

                parentOperation = parentOperation.Parent;
            }

            if (!isUsedInSwitch)
            {
                // Report diagnostic SE1051 if 'Exhaustive()' is not used with a switch.
                var diagnostic = Diagnostic.Create(Rules[SE1051], invocation.Syntax.GetLocation(), enumType.Name);
                context.ReportDiagnostic(diagnostic);
                return;
            }

            // Get all declared enum values.
            var enumMembers = enumType
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue && !f.IsImplicitlyDeclared)
                .ToLookup(x => x.ConstantValue, x => x);

            var matchedValues = new HashSet<object>();
            bool hasDefaultLabel = false;
            bool containsUnsupportedPatterns = false;

            if (parentOperation is ISwitchOperation switchOperation)
            {
                // Analyze switch statement cases.
                foreach (var section in switchOperation.Cases)
                {
                    foreach (var clause in section.Clauses)
                    {
                        switch (clause)
                        {
                            case ISingleValueCaseClauseOperation singleValueClause:
                                var constantValue = singleValueClause.Value.ConstantValue;
                                if (constantValue.HasValue)
                                {
                                    matchedValues.Add(constantValue.Value);
                                }
                                else
                                {
                                    containsUnsupportedPatterns = true;
                                }
                                break;

                            case IPatternCaseClauseOperation patternClause:
                                CollectConstantsFromPattern(patternClause.Pattern, matchedValues, ref hasDefaultLabel, ref containsUnsupportedPatterns);
                                break;

                            case IDefaultCaseClauseOperation _:
                                hasDefaultLabel = true;
                                break;

                            default:
                                containsUnsupportedPatterns = true;
                                break;
                        }
                    }
                }
            }
            else if (parentOperation is ISwitchExpressionOperation switchExpressionOperation)
            {
                // Analyze switch expression arms.
                foreach (var arm in switchExpressionOperation.Arms)
                {
                    var pattern = arm.Pattern;
                    CollectConstantsFromPattern(pattern, matchedValues, ref hasDefaultLabel, ref containsUnsupportedPatterns);
                }
            }

            if (containsUnsupportedPatterns)
            {
                // Report diagnostic SE1052 if unsupported patterns are found.
                var diagnostic = Diagnostic.Create(Rules[SE1052], invocation.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
                return;
            }

            var dictionary = enumMembers.ToDictionary(x => x.Key, x => x.ToArray());

            // Remove matched values.
            foreach (var matchedValue in matchedValues)
            {
                dictionary.Remove(matchedValue);
            }

            if (dictionary.Any() || !hasDefaultLabel)
            {
                const string MissingDefault = "(default or _)";
                var missingValuesGrouped = dictionary
                    .Values
                    .OrderBy(x => x[0].Name)
                    .Select(x => x.Length == 1 ? x[0].Name : $"({string.Join(" or ", x.Select(f => f.Name))})");

                if (!hasDefaultLabel)
                {
                    missingValuesGrouped = missingValuesGrouped.Concat(new[] { MissingDefault });
                }

                var missing = string.Join(", ", missingValuesGrouped);

                // Report diagnostic SE1050 if not all enum values are covered.
                var diagnostic = Diagnostic.Create(Rules[SE1050], invocation.Syntax.GetLocation(), enumType.Name, missing);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsExhaustive(IInvocationOperation invocation)
        {
            var methodSymbol = invocation.TargetMethod;

            if (methodSymbol.Name != "ForceExhaustive" || !methodSymbol.IsExtensionMethod)
            {
                return false;
            }

            // Ensure the containing type and namespace match the desired class.
            var containingType = methodSymbol.ContainingType;
            var containingNamespace = containingType.ContainingNamespace;

            if (containingType.Name != "SubtleEngineeringExtensions" ||
                containingNamespace.ToDisplayString() != "SubtleEngineering.Analyzers.Decorators")
            {
                return false;
            }

            // Ensure the method is the 'Exhaustive<T>' where T : Enum.
            if (methodSymbol.TypeParameters.Length != 1 ||
                !methodSymbol.TypeParameters[0].ConstraintTypes.Any(ct => ct.SpecialType == SpecialType.System_Enum))
            {
                return false;
            }

            return true;
        }

        private bool IsOperationUsingExhaustiveValue(IOperation operation, IInvocationOperation exhaustiveInvocation)
        {
            if (operation == null)
                return false;

            if (operation == exhaustiveInvocation)
                return true;

            foreach (var child in operation.ChildOperations)
            {
                if (IsOperationUsingExhaustiveValue(child, exhaustiveInvocation))
                {
                    return true;
                }
            }

            return false;
        }

        private void CollectConstantsFromPattern(IPatternOperation pattern, HashSet<object> matchedValues, ref bool hasDefaultLabel, ref bool containsUnsupportedPatterns)
        {
            if (pattern == null)
                return;

            switch (pattern)
            {
                case IConstantPatternOperation constantPattern:
                    var constantValue = constantPattern.Value.ConstantValue;
                    if (constantValue.HasValue)
                    {
                        matchedValues.Add(constantValue.Value);
                    }
                    else
                    {
                        containsUnsupportedPatterns = true;
                    }
                    break;

                case IBinaryPatternOperation binaryPattern when binaryPattern.OperatorKind == BinaryOperatorKind.Or:
                    CollectConstantsFromPattern(binaryPattern.LeftPattern, matchedValues, ref hasDefaultLabel, ref containsUnsupportedPatterns);
                    CollectConstantsFromPattern(binaryPattern.RightPattern, matchedValues, ref hasDefaultLabel, ref containsUnsupportedPatterns);
                    break;

                case IDiscardPatternOperation _:
                    hasDefaultLabel = true;
                    break;

                default:
                    containsUnsupportedPatterns = true;
                    break;
            }
        }
    }
}
