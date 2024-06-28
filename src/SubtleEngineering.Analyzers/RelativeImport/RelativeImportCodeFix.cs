namespace SubtleEngineering.Analyzers.RelativeImport
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis;
    using System.Threading.Tasks;
    using System.Threading;
    using Microsoft.CodeAnalysis.CodeFixes;
    using System.Composition;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.Simplification;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RelativeImportCodeFix)), Shared]
    public class RelativeImportCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.RelativeImport.DoNotUseRelativeImportUsingStatements);

        public override FixAllProvider GetFixAllProvider()
        {
            // Support batch fixing
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the using directive identified by the diagnostic.
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var usingDirective = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<UsingDirectiveSyntax>();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use fully qualified namespace",
                    createChangedDocument: c => FixUsingDirectiveAsync(context, usingDirective, diagnostic, c),
                    equivalenceKey: "UseFullyQualifiedNamespace"),
                diagnostic);
        }

        private async Task<Document> FixUsingDirectiveAsync(CodeFixContext context, UsingDirectiveSyntax usingDirective, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetSymbolInfo(usingDirective.Name);
            var fullyQualifiedNamespace = symbol.Symbol.ToDisplayString(Helpers.FullyQualifiedNamespaceFormat);
            var qualifiedNameSyntax = SyntaxFactory.ParseName(fullyQualifiedNamespace)
                .WithTriviaFrom(usingDirective.Name)
                .WithAdditionalAnnotations(Simplifier.Annotation);

            var newUsingDirective = usingDirective.WithName(qualifiedNameSyntax);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(usingDirective, newUsingDirective);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
