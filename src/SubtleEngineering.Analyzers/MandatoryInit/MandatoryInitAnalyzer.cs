namespace SubtleEngineering.Analyzers.MandatoryInit
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Text;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis;
    using System.Data;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MandatoryInitAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1030 = 0;

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticIds.MandatoryInit.TypeHasDefaultInitializers,
                "This type should not have default initializations",
                "Type '{0}' has a property {1} that's initialized with a default value.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true)
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            if (namedTypeSymbol.Name.StartsWith("AllProps") && (namedTypeSymbol.TypeKind == TypeKind.Class || namedTypeSymbol.TypeKind == TypeKind.Struct))
            {
                foreach (var member in namedTypeSymbol.GetMembers())
                {
                    if (member is IPropertySymbol property)
                    {
                        if (property.DeclaringSyntaxReferences.Length > 0)
                        {
                            var syntaxReference = property.DeclaringSyntaxReferences[0];
                            var propertySyntax = syntaxReference.GetSyntax(context.CancellationToken);

                            if (propertySyntax is PropertyDeclarationSyntax propertyDeclaration && propertyDeclaration.Initializer != null)
                            {
                                var diagnostic = Diagnostic.Create(Rules[SE1030], namedTypeSymbol.Locations[0], namedTypeSymbol.Name);
                                context.ReportDiagnostic(diagnostic);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
