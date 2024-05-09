namespace SubtleEngineering.Analyzers.NegativeConstraint
{
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Collections.Immutable;
    using SubtleEngineering.Analyzers.Decorators;
    using System.Linq;
    using System;
    using Microsoft.CodeAnalysis.Operations;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NegativeConstraintAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1020 = 0;

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticIds.NegativeConstraintUsed,
                "Negative constraint for Generic",
                "The generic parameter {0} on element {1} is of a disallowed type.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true)
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(
                AnalyzeGenericUsage,
                OperationKind.ObjectCreation,
                OperationKind.Invocation,
                OperationKind.DelegateCreation,
                OperationKind.SimpleAssignment,
                OperationKind.VariableDeclaration,
                OperationKind.PropertyReference,
                OperationKind.Conversion,
                OperationKind.FieldReference);
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field, SymbolKind.Property);
            // context.RegisterSyntaxNodeAction(AnalyzeGenericName, SyntaxKind.GenericName);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var field = context.Symbol as IFieldSymbol;
            switch (context.Symbol)
            {
                case IFieldSymbol fieldSymbol:
                    AnalyzeTypeArguments(context, fieldSymbol.Type, fieldSymbol.Name);
                    break;
                case IPropertySymbol propertySymbol:
                    AnalyzeTypeArguments(context, propertySymbol.Type, propertySymbol.Name);
                    break;
            }
        }

        private void AnalyzeGenericUsage(OperationAnalysisContext context)
        {
            IOperation operation = context.Operation;

            // Grouping cases based on commonality
            switch (operation)
            {
                case IInvocationOperation invocation:
                    AnalyzeTypeArguments(context.ReportDiagnostic, context.Operation.Syntax.GetLocation(), invocation.TargetMethod.Name, invocation.TargetMethod.TypeParameters, invocation.TargetMethod.TypeArguments);
                    break;

                case IObjectCreationOperation objectCreation:
                    AnalyzeTypeArguments(context, objectCreation.Type);
                    break;

                case IDelegateCreationOperation delegateCreation:
                    AnalyzeTypeArguments(context, delegateCreation.Type);
                    break;

                case IConversionOperation conversion:
                    AnalyzeTypeArguments(context, conversion.Type);
                    break;

                case IVariableDeclarationOperation variableDeclaration:
                    foreach (var declarator in variableDeclaration.Declarators)
                    {
                        AnalyzeTypeArguments(context, declarator.Symbol.Type);
                    }
                    break;
                case ISimpleAssignmentOperation simpleAssignment:
                    if (simpleAssignment.Target is IFieldReferenceOperation fieldReference)
                    {
                        AnalyzeTypeArguments(context, fieldReference.Type);
                    }
                    break;
            }
        }

        private void AnalyzeTypeArguments(SymbolAnalysisContext context, ITypeSymbol typeSymbol, string elementName)
        {
            if (typeSymbol == null)
            {
                return;
            }

            if (typeSymbol is INamedTypeSymbol namedType &&  namedType.IsGenericType)
            {
                AnalyzeTypeArguments(context.ReportDiagnostic, context.Symbol.Locations[0], elementName, namedType.TypeParameters, namedType.TypeArguments);
            }
        }

        private void AnalyzeTypeArguments(OperationAnalysisContext context, ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return;
            }

            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                AnalyzeTypeArguments(context.ReportDiagnostic, context.Operation.Syntax.GetLocation(), typeSymbol.Name, namedType.TypeParameters, namedType.TypeArguments);
            }
        }

        private void AnalyzeTypeArguments(Action<Diagnostic> reportDiagnostic, Location location, string elementName, ImmutableArray<ITypeParameterSymbol> typeParameters, ImmutableArray<ITypeSymbol> typeArguments)
        {
            for (int i = 0; i < typeParameters.Length; i++)
            {
                var typeArgument = typeArguments[i];
                var typeParameter = typeParameters[i];
                VerifyGenericParameter(typeParameter, typeArgument, reportDiagnostic, location, elementName);
            }
        }

        private void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
        {
            var genericName = (GenericNameSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            // Get the method symbol
            var symbolInfo = semanticModel.GetSymbolInfo(genericName);
            var nameSymbol = symbolInfo.Symbol as INamedTypeSymbol;

            if (nameSymbol == null)
            {
                return;
            }

            // Iterate through each type parameter to check for the NegativeTypeConstraintAttribute
            for (int i = 0; i < nameSymbol.TypeParameters.Length; i++)
            {
                var typeParameter = nameSymbol.TypeParameters[i];
                var providedType = nameSymbol.TypeArguments[i];
                VerifyGenericParameter(typeParameter, providedType, context.ReportDiagnostic, genericName.GetLocation(), nameSymbol.Name);
            }
        }

        private static void VerifyGenericParameter(ITypeParameterSymbol typeParameter, ITypeSymbol providedType, Action<Diagnostic> reportDiagnostic, Location location, string symbolName)
        {
            var attributeData = GetNegativeTypeConstraintAttribute(typeParameter);
            if (attributeData == null)
            {
                return;
            }

            var ctorArgs = attributeData.ConstructorArguments;
            if (ctorArgs.Length != 2)
            {
                return;
            }

            var disallowedType = ctorArgs[0].Value as INamedTypeSymbol;
            var disallowDerived = (bool)ctorArgs[1].Value;

            if (TypeIsDisallowed(providedType, disallowedType, disallowDerived))
            {
                var diagnostic = Diagnostic.Create(
                    Rules[SE1020],
                    location,
                    typeParameter.ToDisplayString(),
                    symbolName);

                reportDiagnostic(diagnostic);
            }
        }

        private static AttributeData GetNegativeTypeConstraintAttribute(ITypeParameterSymbol typeParameter)
        {
            foreach (var attribute in typeParameter.GetAttributes())
            {
                if (attribute.AttributeClass.IsAssignableTo<NegativeTypeConstraintAttribute>())
                {
                    return attribute;
                }
            }

            return null;
        }

        private static bool TypeIsDisallowed(ITypeSymbol providedType, INamedTypeSymbol disallowedType, bool disallowDerived)
        {
            if (SymbolEqualityComparer.Default.Equals(providedType, disallowedType))
            {
                return true;
            }

            if (disallowDerived && InheritsFrom(providedType, disallowedType))
            {
                return true;
            }

            return false;
        }

        private static bool InheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            while (type != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type.BaseType, baseType))
                {
                    return true;
                }

                if (type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, baseType)))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }
    }
}
