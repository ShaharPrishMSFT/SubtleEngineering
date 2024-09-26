namespace SubtleEngineering.Analyzers.Obsolescence
{
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CodeActions;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CorrectObsolescenceCodeFix)), Shared]
    public class CorrectObsolescenceCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticsDetails.Obsolescence.HideObsoleteElementsId);

        public override FixAllProvider GetFixAllProvider()
        {
            // Support batch fixing
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the declaration syntax node identified by the diagnostic
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnosticSpan);

            var declarationNode = node.FirstAncestorOrSelf<SyntaxNode>(n =>
                n is MethodDeclarationSyntax ||
                n is PropertyDeclarationSyntax ||
                n is FieldDeclarationSyntax ||
                n is EventDeclarationSyntax ||
                n is EventFieldDeclarationSyntax || // Added this line
                n is ClassDeclarationSyntax ||
                n is StructDeclarationSyntax ||
                n is InterfaceDeclarationSyntax ||
                n is EnumDeclarationSyntax ||
                n is DelegateDeclarationSyntax);

            if (declarationNode == null)
            {
                return;
            }

            // Register a code action that will invoke the fix
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Hide obsolete element from IntelliSense",
                    createChangedDocument: c => AddEditorBrowsableAttributeAsync(context.Document, declarationNode, c),
                    equivalenceKey: "AddEditorBrowsableAttribute"),
                diagnostic);
        }

        private async Task<Document> AddEditorBrowsableAttributeAsync(Document document, SyntaxNode declarationNode, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Track the declaration node so we can find it after modifying the root
            root = root.TrackNodes(declarationNode);

            // Add using directive for System.ComponentModel if it's not already present
            var rootWithUsing = AddUsingDirectiveIfMissing(root, "System.ComponentModel");

            // Get the updated declaration node from the new root
            var updatedDeclarationNode = rootWithUsing.GetCurrentNode(declarationNode);

            // Create the [EditorBrowsable(EditorBrowsableState.Never)] attribute
            var editorBrowsableAttribute = SyntaxFactory.Attribute(
                SyntaxFactory.ParseName("EditorBrowsable"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("EditorBrowsableState"),
                                SyntaxFactory.IdentifierName("Never"))))));

            var newAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(editorBrowsableAttribute));

            // Add the attribute to the updated declaration node
            var modifiedDeclarationNode = AddAttributeToDeclaration(updatedDeclarationNode, newAttributeList);

            // Replace the old node with the new node in the syntax tree
            var newRoot = rootWithUsing.ReplaceNode(updatedDeclarationNode, modifiedDeclarationNode);

            // Return the new document
            return document.WithSyntaxRoot(newRoot);
        }

        private SyntaxNode AddUsingDirectiveIfMissing(SyntaxNode root, string namespaceName)
        {
            if (root is CompilationUnitSyntax compilationUnit)
            {
                var hasUsingDirective = compilationUnit.Usings.Any(u => u.Name.ToString() == namespaceName);
                if (!hasUsingDirective)
                {
                    var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName));
                        //.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                    compilationUnit = compilationUnit.AddUsings(usingDirective);
                    return compilationUnit;
                }
            }
            return root;
        }

        private SyntaxNode AddAttributeToDeclaration(SyntaxNode declarationNode, AttributeListSyntax attributeList)
        {
            switch (declarationNode)
            {
                case MethodDeclarationSyntax methodDeclaration:
                    return methodDeclaration.AddAttributeLists(attributeList);
                case PropertyDeclarationSyntax propertyDeclaration:
                    return propertyDeclaration.AddAttributeLists(attributeList);
                case FieldDeclarationSyntax fieldDeclaration:
                    return fieldDeclaration.AddAttributeLists(attributeList);
                case EventDeclarationSyntax eventDeclaration:
                    return eventDeclaration.AddAttributeLists(attributeList);
                case EventFieldDeclarationSyntax eventFieldDeclaration: // Added this case
                    return eventFieldDeclaration.AddAttributeLists(attributeList);
                case ClassDeclarationSyntax classDeclaration:
                    return classDeclaration.AddAttributeLists(attributeList);
                case StructDeclarationSyntax structDeclaration:
                    return structDeclaration.AddAttributeLists(attributeList);
                case InterfaceDeclarationSyntax interfaceDeclaration:
                    return interfaceDeclaration.AddAttributeLists(attributeList);
                case EnumDeclarationSyntax enumDeclaration:
                    return enumDeclaration.AddAttributeLists(attributeList);
                case DelegateDeclarationSyntax delegateDeclaration:
                    return delegateDeclaration.AddAttributeLists(attributeList);
                default:
                    return declarationNode;
            }
        }
    }
}
