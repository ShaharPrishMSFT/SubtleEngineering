namespace SubtleEngineering.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Data;
    using System.Linq;
    using System.Collections.Immutable;
    using SubtleEngineering.Analyzers.Decorators;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RelativeImportAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1010 = 0;

        private static readonly SymbolDisplayFormat NamespaceFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted);

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticIds.DoNotUseRelativeImportUsingStatements,
                "Do not use relative namespaces in using directives - this can cause hidden bugs when copying code or during refactors.",
                "Namespace '{0}' is a relative and represents {1} - use the fully qualified name instead.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true)
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        }

        private void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
        {
            var node = (UsingDirectiveSyntax)context.Node;
            var symbol = context.SemanticModel.GetSymbolInfo(node.Name);
            var fullyQualified = symbol.Symbol.ToDisplayString(NamespaceFormat);
            if (node.Alias == null && fullyQualified != node.Name.ToString() && fullyQualified.EndsWith(node.Name.ToString()))
            {
                var diagnostic = Diagnostic.Create(Rules[SE1010], node.GetLocation(), node.Name, fullyQualified);
                context.ReportDiagnostic(diagnostic);
            }

        }
    }
}
