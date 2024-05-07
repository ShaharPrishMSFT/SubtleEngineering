namespace SubtleEngineering.Analyzers.NegativeConstraint
{
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Collections.Immutable;
    using SubtleEngineering.Analyzers.Decorators;

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
            context.RegisterSyntaxNodeAction(AnalyzeMethodInvocation, SyntaxKind.GenericName);
        }

        private void AnalyzeMethodInvocation(SyntaxNodeAnalysisContext context)
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
                var attributeData = GetNegativeTypeConstraintAttribute(typeParameter);

                if (attributeData == null)
                    continue;

                var disallowedType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                var disallowDerived = (bool)attributeData.ConstructorArguments[1].Value;
                var providedType = nameSymbol.TypeArguments[i];

                if (TypeIsDisallowed(providedType, disallowedType, disallowDerived))
                {
                    var diagnostic = Diagnostic.Create(
                        Rules[SE1020],
                        genericName.GetLocation(),
                        typeParameter.ToDisplayString(),
                        nameSymbol.Name);

                    context.ReportDiagnostic(diagnostic);
                }
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
                return true;

            if (disallowDerived && InheritsFrom(providedType, disallowedType))
                return true;

            return false;
        }

        private static bool InheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
        {
            while (type != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type.BaseType, baseType))
                    return true;

                type = type.BaseType;
            }

            return false;
        }
    }
}
