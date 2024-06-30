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
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExhaustiveInitializationCodeFix)), Shared]
    public class ExhaustiveInitializationCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
            DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId,
            DiagnosticsDetails.ExhaustiveInitialization.TypeInitializationIsNonExhaustiveId,
            DiagnosticsDetails.ExhaustiveInitialization.PrimaryDefaultConstructorValuesNotAllowedId);

        public override FixAllProvider GetFixAllProvider()
        {
            // Support batch fixing
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics.Where(x => x.Id.StartsWith("SE")))
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
                            equivalenceKey: DiagnosticsDetails.ExhaustiveInitialization.TypeEquivalenceKey),
                        diagnostic);
                }
                else if (diagnostic.Id == DiagnosticsDetails.ExhaustiveInitialization.PrimaryDefaultConstructorValuesNotAllowedId)
                {
                    var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                    var paramSyntax = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<ParameterSyntax>();

                    // Register a code action that will invoke the fix.
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Make type exhaustive",
                            createChangedDocument: c => FixParameter(context, paramSyntax, diagnostic, c),
                            equivalenceKey: DiagnosticsDetails.ExhaustiveInitialization.ParameterEquivalenceKey),
                        diagnostic);
                }
            }
        }

        private async Task<Document> FixParameter(CodeFixContext context, ParameterSyntax paramSyntax, Diagnostic diagnostic, CancellationToken ct)
        {
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);

            // Add the 'required' modifier to the member declaration
            var newParamSyntax = GetFixedParameter(paramSyntax);

            var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(paramSyntax, newParamSyntax);

            return document.WithSyntaxRoot(newRoot);
        }

        private ParameterSyntax GetFixedParameter(ParameterSyntax paramSyntax)
        {
            return paramSyntax.WithDefault(null);
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
            if (memberDeclarationSyntax.Modifiers.Any(x => x.IsKind(SyntaxKind.RequiredKeyword)))
            {
                return memberDeclarationSyntax;
            }

            var requiredModifier = SyntaxFactory.Token(SyntaxKind.RequiredKeyword);
            var newModifiers = memberDeclarationSyntax.Modifiers.Insert(0, requiredModifier);

            var newMemberDeclarationSyntax = memberDeclarationSyntax.WithModifiers(newModifiers);
            return newMemberDeclarationSyntax;
        }

        private async Task<Document> FixEntireType(CodeFixContext context, TypeDeclarationSyntax typeSyntax, Diagnostic diagnostic, CancellationToken ct)
        {
            var document = context.Document;
            var editor = await DocumentEditor.CreateAsync(document, ct);

            // Find all relevant properties.
            var propertiesToFix = diagnostic
                .Properties
                .Where(x => x.Key.StartsWith(DiagnosticsDetails.ExhaustiveInitialization.BadPropertyPrefix))
                .Select(x => x.Value)
                .ToArray();

            foreach (var member in typeSyntax.Members.OfType<PropertyDeclarationSyntax>().Where(x => propertiesToFix.Contains(x.Identifier.Text)))
            {
                // Add the 'required' modifier to the member declaration
                var newMember = GetFixedProperty(member);
                editor.ReplaceNode(member, newMember);
            }

            // Find all relevant properties.
            var parametersToFix = diagnostic
                .Properties
                .Where(x => x.Key.StartsWith(DiagnosticsDetails.ExhaustiveInitialization.BadParameterPrefix))
                .Select(x => x.Value)
                .ToArray();

            if (typeSyntax is RecordDeclarationSyntax recordDeclaration)
            {
                var constructorParameters = recordDeclaration
                    .ParameterList
                    .Parameters
                    .ToArray() ?? Array.Empty<ParameterSyntax>();

                foreach (var prop in constructorParameters)
                {
                    var newMember = GetFixedParameter(prop);
                    editor.ReplaceNode(prop, newMember);
                }
            }
            var newRoot = editor.GetChangedRoot();
            var newDocument = document.WithSyntaxRoot(newRoot);

            newRoot = await newDocument.GetSyntaxRootAsync(ct);
            var props = newRoot.DescendantNodesAndSelf().OfType<PropertyDeclarationSyntax>().ToArray();
            return newDocument;
        }
    }
}
