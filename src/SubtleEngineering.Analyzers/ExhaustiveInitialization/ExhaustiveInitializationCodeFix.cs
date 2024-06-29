namespace SubtleEngineering.Analyzers.ExhaustiveInitialization
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
    using System.Linq;
    using System;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExhaustiveInitializationCodeFix)), Shared]
    public class ExhaustiveInitializationCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId, DiagnosticsDetails.ExhaustiveInitialization.TypeInitializationIsNonExhaustiveId);

        public override FixAllProvider GetFixAllProvider()
        {
            // Support batch fixing
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics.Where(x => x.Id == DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId || x.Id == DiagnosticsDetails.ExhaustiveInitialization.TypeInitializationIsNonExhaustiveId))
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                if (diagnostic.Id == DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId)
                {
                    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                    var foundNode = root.FindNode(diagnosticSpan);
                    var memberDeclarationSyntax = foundNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();

                    // Register a code action that will invoke the fix.
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Make property required",
                            createChangedDocument: c => FixProperty(context, memberDeclarationSyntax, diagnostic, c),
                            equivalenceKey: DiagnosticsDetails.ExhaustiveInitialization.PropertyEquivalenceKey),
                        diagnostic);
                }
                else if (diagnostic.Id == DiagnosticsDetails.ExhaustiveInitialization.TypeInitializationIsNonExhaustiveId)
                {
                    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                    var typeSyntax = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<TypeDeclarationSyntax>();

                    // Register a code action that will invoke the fix.
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Make all properties required",
                            createChangedDocument: c => FixEntireType(context, typeSyntax, diagnostic, c),
                            equivalenceKey: DiagnosticsDetails.ExhaustiveInitialization.PropertyEquivalenceKey),
                        diagnostic);
                }
            }
        }

        private async Task<Document> FixProperty(CodeFixContext context, MemberDeclarationSyntax memberDeclarationSyntax, Diagnostic diagnostic, CancellationToken ct)
        {
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);

            // Add the 'required' modifier to the member declaration
            var newMemberDeclarationSyntax = GetFixedProperty(memberDeclarationSyntax);

            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(memberDeclarationSyntax, newMemberDeclarationSyntax);

            return document.WithSyntaxRoot(newRoot);
        }

        private static MemberDeclarationSyntax GetFixedProperty(MemberDeclarationSyntax memberDeclarationSyntax)
        {
            var requiredModifier = SyntaxFactory.Token(SyntaxKind.RequiredKeyword);
            var newModifiers = memberDeclarationSyntax.Modifiers.Insert(0, requiredModifier);

            var newMemberDeclarationSyntax = memberDeclarationSyntax.WithModifiers(newModifiers);
            return newMemberDeclarationSyntax;
        }

        private async Task<Document> FixEntireType(CodeFixContext context, TypeDeclarationSyntax typeSyntax, Diagnostic diagnostic, CancellationToken ct)
        {
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);

            // Find all relevant properties.
            var propertiesToFix = diagnostic
                .Properties
                .Where(x => x.Key.StartsWith(DiagnosticsDetails.ExhaustiveInitialization.BadPropertyPrefix))
                .Select(x => x.Value)
                .ToArray();

            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            var newRoot = root;
            foreach (var member in typeSyntax.Members.OfType<PropertyDeclarationSyntax>().Where(x => propertiesToFix.Contains(x.Identifier.Text)))
            {
                // Add the 'required' modifier to the member declaration
                var newMember = GetFixedProperty(member);
                newRoot = newRoot.ReplaceNode(member, newMember);
            }
            return document.WithSyntaxRoot(newRoot);
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
