namespace SubtleEngineering.Analyzers.ExhaustiveEnumSwitch
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;
    using SubtleEngineering.Analyzers;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddMissingEnumCasesCodeFixProvider)), Shared]
    public class AddMissingEnumCasesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault);

        public override FixAllProvider GetFixAllProvider()
        {
            // Support batch fixing
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the node at the diagnostic location.
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnosticSpan);

            // Find the switch statement or expression node.
            var switchNode = node.FirstAncestorOrSelf<SwitchStatementSyntax>() as SyntaxNode
                             ?? node.FirstAncestorOrSelf<SwitchExpressionSyntax>();

            if (switchNode == null)
                return;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Make switch exhaustive",
                    createChangedDocument: c => AddMissingEnumCasesAsync(context.Document, switchNode, c),
                    equivalenceKey: "AddMissingEnumCases"),
                diagnostic);
        }

        private async Task<Document> AddMissingEnumCasesAsync(Document document, SyntaxNode switchNode, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (switchNode is SwitchStatementSyntax switchStatement)
            {
                // Handle switch statements.
                var switchExpression = switchStatement.Expression;

                // Get the type of the expression.
                var typeInfo = semanticModel.GetTypeInfo(switchExpression, cancellationToken);
                var enumType = typeInfo.ConvertedType as INamedTypeSymbol;

                if (enumType == null || enumType.TypeKind != TypeKind.Enum)
                    return document;

                // Get all enum members.
                var enumMembers = enumType.GetMembers().OfType<IFieldSymbol>()
                    .Where(f => f.HasConstantValue && !f.IsImplicitlyDeclared).ToList();

                // Collect existing labels.
                var existingLabels = new HashSet<object>();
                bool hasDiscard = false;
                foreach (var section in switchStatement.Sections)
                {
                    foreach (var label in section.Labels)
                    {
                        if (label is CaseSwitchLabelSyntax caseLabel)
                        {
                            var constantValue = semanticModel.GetConstantValue(caseLabel.Value, cancellationToken);
                            if (constantValue.HasValue)
                            {
                                existingLabels.Add(constantValue.Value);
                            }
                        }
                        else if (label is CasePatternSwitchLabelSyntax patternLabel)
                        {
                            CollectConstantsFromPattern(patternLabel.Pattern, existingLabels, semanticModel, cancellationToken, ref hasDiscard);
                        }
                        else if (label is DefaultSwitchLabelSyntax)
                        {
                            hasDiscard = true;
                        }
                    }
                }

                // Find missing enum members.
                var missingMembers = enumMembers
                    .Where(em => !existingLabels.Contains(em.ConstantValue))
                    .OrderBy(x => x.Name)
                    .DistinctBy(x => x.ConstantValue)
                    .ToList();

                if (!missingMembers.Any() && hasDiscard)
                    return document;

                // Create new switch sections for missing enum members.
                var newSections = missingMembers.Select(em =>
                {
                    var label = SyntaxFactory.CaseSwitchLabel(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(enumType.Name),
                        SyntaxFactory.IdentifierName(em.Name)));

                    var throwStatement = SyntaxFactory.ThrowStatement(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("NotImplementedException"))
                        .WithArgumentList(SyntaxFactory.ArgumentList()));

                    var section = SyntaxFactory.SwitchSection(
                        SyntaxFactory.List<SwitchLabelSyntax>(new[] { label }),
                        SyntaxFactory.List<StatementSyntax>(new[] { throwStatement }));

                    return section;
                }).ToList();

                // Separate existing sections into those before and after the default section.
                var sections = switchStatement.Sections;
                var sectionsBeforeDefault = sections.TakeWhile(s => !s.Labels.Any(SyntaxKind.DefaultSwitchLabel)).ToList();
                var defaultSection = sections.FirstOrDefault(s => s.Labels.Any(SyntaxKind.DefaultSwitchLabel));
                var sectionsAfterDefault = sections.SkipWhile(s => !s.Labels.Any(SyntaxKind.DefaultSwitchLabel)).Skip(1).ToList();

                // Insert the new sections before the default section if it exists.
                if (defaultSection != null)
                {
                    sectionsBeforeDefault.AddRange(newSections);
                    sectionsBeforeDefault.Add(defaultSection);
                    sectionsBeforeDefault.AddRange(sectionsAfterDefault);
                }
                else
                {
                    // If there's no default case and `hasDiscard` is false, add a default section.
                    if (!hasDiscard)
                    {
                        var defaultLabel = SyntaxFactory.DefaultSwitchLabel();
                        var defaultThrowStatement = SyntaxFactory.ThrowStatement(
                            SyntaxFactory.ObjectCreationExpression(
                                SyntaxFactory.IdentifierName("InvalidOperationException"))
                            .WithArgumentList(SyntaxFactory.ArgumentList()));

                        var defaultSectionToAdd = SyntaxFactory.SwitchSection(
                            SyntaxFactory.List<SwitchLabelSyntax>(new[] { defaultLabel }),
                            SyntaxFactory.List<StatementSyntax>(new[] { defaultThrowStatement }));

                        newSections.Add(defaultSectionToAdd);
                    }

                    // Add the new sections.
                    sectionsBeforeDefault.AddRange(newSections);
                }

                // Create the updated switch statement with the reordered sections.
                var updatedSwitch = switchStatement.WithSections(SyntaxFactory.List(sectionsBeforeDefault));

                var newRoot = root.ReplaceNode(switchStatement, updatedSwitch);

                return document.WithSyntaxRoot(newRoot);
            }
            else if (switchNode is SwitchExpressionSyntax switchExpression)
            {
                // Handle switch expressions.
                var switchValue = switchExpression.GoverningExpression;

                var typeInfo = semanticModel.GetTypeInfo(switchValue, cancellationToken);
                var enumType = typeInfo.ConvertedType as INamedTypeSymbol;

                if (enumType == null || enumType.TypeKind != TypeKind.Enum)
                    return document;

                // Get all enum members.
                var enumMembers = enumType.GetMembers().OfType<IFieldSymbol>()
                    .Where(f => f.HasConstantValue && !f.IsImplicitlyDeclared).ToList();

                // Collect existing patterns.
                var existingValues = new HashSet<object>();
                bool hasDiscard = false;

                foreach (var arm in switchExpression.Arms)
                {
                    CollectConstantsFromPattern(arm.Pattern, existingValues, semanticModel, cancellationToken, ref hasDiscard);
                }

                // Find missing enum members.
                var missingMembers = enumMembers
                    .Where(em => !existingValues.Contains(em.ConstantValue))
                    .OrderBy(x => x.Name)
                    .DistinctBy(x => x.ConstantValue)
                    .ToList();

                if (!missingMembers.Any())
                    return document;

                // Create new arms for missing enum members.
                var newArms = missingMembers.Select(em =>
                {
                    var pattern = SyntaxFactory.ConstantPattern(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(enumType.Name),
                        SyntaxFactory.IdentifierName(em.Name)));

                    var throwExpression = SyntaxFactory.ThrowExpression(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.IdentifierName("NotImplementedException"))
                        .WithArgumentList(SyntaxFactory.ArgumentList()));

                    var arm = SyntaxFactory.SwitchExpressionArm(pattern, throwExpression);

                    return arm;
                }).ToList();

                // Separate existing arms into those before and after the default (discard) arm.
                var existingArms = switchExpression.Arms;
                var armsBeforeDefault = existingArms.TakeWhile(a => !(a.Pattern is DiscardPatternSyntax)).ToList();
                var defaultArm = existingArms.FirstOrDefault(a => a.Pattern is DiscardPatternSyntax);
                var armsAfterDefault = existingArms.SkipWhile(a => !(a.Pattern is DiscardPatternSyntax)).Skip(1).ToList();

                // Insert new arms before the default arm if it exists.
                if (defaultArm != null)
                {
                    armsBeforeDefault.AddRange(newArms);
                    armsBeforeDefault.Add(defaultArm);
                    armsBeforeDefault.AddRange(armsAfterDefault);
                }
                else
                {
                    // If there's no default arm and `hasDiscard` is false, add a default arm.
                    if (!hasDiscard)
                    {
                        var defaultArmToAdd = SyntaxFactory.SwitchExpressionArm(
                            SyntaxFactory.DiscardPattern(),
                            SyntaxFactory.ThrowExpression(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.IdentifierName("InvalidOperationException"))
                                .WithArgumentList(SyntaxFactory.ArgumentList())));

                        newArms.Add(defaultArmToAdd);
                    }

                    // Add the new arms.
                    armsBeforeDefault.AddRange(newArms);
                }

                // Create the updated switch expression with the reordered arms.
                var updatedSwitch = switchExpression.WithArms(SyntaxFactory.SeparatedList(armsBeforeDefault));

                var newRoot = root.ReplaceNode(switchExpression, updatedSwitch);

                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }

        private void CollectConstantsFromPattern(PatternSyntax pattern, HashSet<object> existingValues, SemanticModel semanticModel, CancellationToken cancellationToken, ref bool hasDiscard)
        {
            if (pattern == null)
                return;

            switch (pattern)
            {
                case ConstantPatternSyntax constantPattern:
                    var constantValue = semanticModel.GetConstantValue(constantPattern.Expression, cancellationToken);
                    if (constantValue.HasValue)
                    {
                        existingValues.Add(constantValue.Value);
                    }
                    break;

                case BinaryPatternSyntax binaryPattern when binaryPattern.IsKind(SyntaxKind.OrPattern):
                    CollectConstantsFromPattern(binaryPattern.Left, existingValues, semanticModel, cancellationToken, ref hasDiscard);
                    CollectConstantsFromPattern(binaryPattern.Right, existingValues, semanticModel, cancellationToken, ref hasDiscard);
                    break;

                case DiscardPatternSyntax _:
                    hasDiscard = true;
                    break;

                default:
                    // Unsupported patterns are ignored for code fix purposes.
                    break;
            }
        }
    }
}