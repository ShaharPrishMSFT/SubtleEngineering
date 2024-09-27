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
    using Microsoft.CodeAnalysis.Formatting;

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
            var editorBrowsableNamespace = SyntaxFactory.ParseName(namespaceName);

            // Check for existing using directives at the compilation unit level
            if (root is CompilationUnitSyntax compilationUnit)
            {
                var hasUsingDirective = compilationUnit.Usings.Any(u => u.Name.ToString() == namespaceName);
                if (hasUsingDirective)
                {
                    // The using directive already exists at the compilation unit level
                    return root;
                }

                // Check if there are any existing using directives at the compilation unit level
                if (compilationUnit.Usings.Any())
                {
                    // Add the using directive after the existing usings at the compilation unit level
                    var newUsing = SyntaxFactory.UsingDirective(editorBrowsableNamespace);
                    var newUsings = compilationUnit.Usings.Add(newUsing);
                    var newCompilationUnit = compilationUnit.WithUsings(newUsings)
                        .WithAdditionalAnnotations(Formatter.Annotation);

                    return newCompilationUnit;
                }

                // No usings at the compilation unit level, check inside namespace declarations
                var firstNamespace = compilationUnit.Members.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
                if (firstNamespace != null)
                {
                    var newFirstNamespace = AddUsingToNamespace(firstNamespace, namespaceName);
                    var newRoot = root.ReplaceNode(firstNamespace, newFirstNamespace);
                    return newRoot;
                }

                // No namespaces, add using directive at the compilation unit level after any leading trivia (e.g., comments)
                var leadingTrivia = compilationUnit.GetLeadingTrivia();
                compilationUnit = compilationUnit.WithoutLeadingTrivia();

                var newUsingWhenNoNamespace = SyntaxFactory.UsingDirective(editorBrowsableNamespace);
                compilationUnit = compilationUnit.AddUsings(newUsingWhenNoNamespace)
                                                 .WithAdditionalAnnotations(Formatter.Annotation);

                return compilationUnit;
            }

            return root;
        }

        private NamespaceDeclarationSyntax AddUsingToNamespace(NamespaceDeclarationSyntax namespaceDeclaration, string namespaceName)
        {
            var editorBrowsableNamespace = SyntaxFactory.ParseName(namespaceName);

            var hasUsingDirective = namespaceDeclaration.Usings.Any(u => u.Name.ToString() == namespaceName);
            if (hasUsingDirective)
            {
                // The using directive already exists inside the namespace
                return namespaceDeclaration;
            }

            // Create the new using directive
            var newUsing = SyntaxFactory.UsingDirective(editorBrowsableNamespace);

            // Add the new using directive to the namespace's using directives
            // Place it after existing using directives, or at the top if there are none
            var newUsings = namespaceDeclaration.Usings.Add(newUsing);

            // Update the namespace declaration with the new usings
            var newNamespaceDeclaration = namespaceDeclaration.WithUsings(newUsings)
                .WithAdditionalAnnotations(Formatter.Annotation);

            return newNamespaceDeclaration;
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
