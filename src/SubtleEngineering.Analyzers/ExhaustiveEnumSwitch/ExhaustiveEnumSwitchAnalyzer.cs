namespace SubtleEngineering.Analyzers.ExhaustiveEnumSwitch
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Operations;
    using SubtleEngineering.Analyzers.Decorators;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExhaustiveEnumSwitchAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1050 = 0;
        private const int SE1051 = 1;

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
            var methodSymbol = invocation.TargetMethod;

            if (methodSymbol.Name != "Exhaustive" || !methodSymbol.IsExtensionMethod)
            {
                return;
            }

            // Ensure the method is the 'Exhaustive<T>' where T : Enum.
            if (methodSymbol.TypeParameters.Length != 1 ||
                !methodSymbol.TypeParameters[0].ConstraintTypes.Any(ct => ct.SpecialType == SpecialType.System_Enum))
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

            while (parentOperation != null &&
                   parentOperation.Kind != OperationKind.Switch &&
                   parentOperation.Kind != OperationKind.SwitchExpression)
            {
                parentOperation = parentOperation.Parent;
            }

            if (parentOperation == null)
            {
                // Report diagnostic SE1051 if 'Exhaustive' is not used with a switch.
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

            if (parentOperation is ISwitchOperation switchOperation)
            {
                // Analyze switch statement cases.
                foreach (var section in switchOperation.Cases)
                {
                    foreach (var clause in section.Clauses)
                    {
                        if (clause is ISingleValueCaseClauseOperation singleValueClause)
                        {
                            var constantValue = singleValueClause.Value.ConstantValue;
                            if (constantValue.HasValue)
                            {
                                matchedValues.Add(constantValue.Value);
                            }
                        }
                        else if (clause is IDefaultCaseClauseOperation)
                        {
                            hasDefaultLabel = true;
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

                    if (pattern is IConstantPatternOperation constantPattern)
                    {
                        var constantValue = constantPattern.Value.ConstantValue;
                        if (constantValue.HasValue)
                        {
                            matchedValues.Add(constantValue.Value);
                        }
                    }
                    else if (pattern is IDeclarationPatternOperation)
                    {
                        // Handle declaration patterns if necessary.
                    }
                    else if (pattern is IDiscardPatternOperation)
                    {
                        hasDefaultLabel = true;
                    }
                }
            }

            var dictionary = enumMembers.ToDictionary(x => x.Key, x => x.ToArray());
            // Determine if any enum values are missing from the switch.
            foreach (var matchedValue in matchedValues)
            {
                dictionary.Remove(matchedValue);
            }

            if (dictionary.Any() || !hasDefaultLabel)
            {
                const string MissingDefault = "(default or _)";
                var missingValuesGrouped  = dictionary
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
    }
}
