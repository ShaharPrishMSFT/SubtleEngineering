namespace SubtleEngineering.Analyzers.RelativeImport
{
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Collections.Immutable;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RelativeImportAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1010 = 0;

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticsDetails.RelativeImport.DoNotUseRelativeImportUsingStatementsId,
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
            var fullyQualified = symbol.Symbol.ToDisplayString(Helpers.FullyQualifiedNamespaceFormat);
            if (node.Alias == null && fullyQualified != node.Name.ToString() && fullyQualified.EndsWith(node.Name.ToString()))
            {
                var diagnostic = Diagnostic.Create(Rules[SE1010], node.GetLocation(), node.Name, fullyQualified);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
