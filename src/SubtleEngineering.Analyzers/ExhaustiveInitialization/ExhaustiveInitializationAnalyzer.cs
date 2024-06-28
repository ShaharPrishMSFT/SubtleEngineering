namespace SubtleEngineering.Analyzers.ExhaustiveInitialization
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
    using SubtleEngineering.Analyzers.Decorators;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExhaustiveInitializationAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1030 = 0;
        private const int SE1031 = 1;

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticIds.ExhaustiveInitialization.PropertyIsMissingRequired,
                "All properties on this type must be required",
                "Type '{0}' has a property {1} that's not marked as required.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticIds.ExhaustiveInitialization.OnlyOneConstructorAllowedWhenMissingRequired,
                "When some properties are not marked as required, they must be initialized from a single constructor. Having more than one constructor will allow for defaults, which is not allowed.",
                "Type '{0}' has more than one constructor.",
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
            if ((namedTypeSymbol.TypeKind == TypeKind.Class || namedTypeSymbol.TypeKind == TypeKind.Struct) &&
                GetMandatoryAttribute(namedTypeSymbol).Any())
            {
                var allNonePrivateProperties = namedTypeSymbol.GetMembers().OfType<IPropertySymbol>().Where(x => x.DeclaredAccessibility != Accessibility.Private && x.SetMethod != null && !x.IsStatic);

                var potentiallyBad = allNonePrivateProperties.Where(x => !x.IsRequired).ToList();

                if (potentiallyBad.Count == 0)
                {
                    return;
                }

                // Find accessible, non-clone constructors.
                var constructors = namedTypeSymbol.Constructors.Where(x =>
                    x.DeclaredAccessibility != Accessibility.Private &&
                    !x.IsImplicitlyDeclared /* &&
                    (x.Parameters.Length != 1 || !x.Parameters[0].Type.Equals(namedTypeSymbol, SymbolEqualityComparer.Default))*/);

                // Structs always have a default constructor
                //if (namedTypeSymbol.TypeKind == TypeKind.Struct)
                //{
                //    constructors = constructors.Where(x => x.Parameters.Count() != 0);
                //}

                if (constructors.Count() != 1)
                {
                    var diagnostic = Diagnostic.Create(Rules[SE1031], namedTypeSymbol.Locations[0], namedTypeSymbol.ToDisplayString());
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                var constructor = constructors.Single();

                var constructorSyntax = constructor
                    .DeclaringSyntaxReferences
                    .FirstOrDefault()
                    ?.GetSyntax(context.CancellationToken)
                    as ConstructorDeclarationSyntax;

                if (constructorSyntax != null)
                {
                    var assignedProperties = constructorSyntax
                        .Body
                        .DescendantNodes()
                        .OfType<AssignmentExpressionSyntax>()
                        .Select(x => x.Left.DescendantNodesAndSelf().FirstOrDefault(statement => statement is IdentifierNameSyntax) as IdentifierNameSyntax)
                        .Where(x => x != null)
                        .Select(x => potentiallyBad.FirstOrDefault(prop => x.IsPropertyIdentifier(prop)))
                        .Where(x => x != null)
                        .ToList();


                    potentiallyBad.RemoveAll(x => assignedProperties.Contains(x, SymbolEqualityComparer.Default));
                }
                else
                {
                    potentiallyBad.RemoveAll(x => HasMatchingParameterName(constructor, x));
                }

                foreach (var property in potentiallyBad)
                {
                    if (property.DeclaringSyntaxReferences.Length > 0)
                    {
                        var syntaxReference = property.DeclaringSyntaxReferences[0];
                        var propertySyntax = syntaxReference.GetSyntax(context.CancellationToken);

                        if (!property.IsRequired)
                        {
                            var diagnostic = Diagnostic.Create(Rules[SE1030], property.Locations[0], namedTypeSymbol.ToDisplayString(), property.Name);
                            context.ReportDiagnostic(diagnostic);
                            return;
                        }
                    }
                }
            }
        }

        bool HasMatchingParameterName(IMethodSymbol methodSymbol, IPropertySymbol propertySymbol)
        {
            return methodSymbol.Parameters.Any(p => p.Name == propertySymbol.Name);
        }

        private static IEnumerable<AttributeData> GetMandatoryAttribute(ISymbol typeParameter)
        {
            foreach (var attribute in typeParameter.GetAttributes())
            {
                if (attribute.AttributeClass.IsAssignableTo<ExhaustiveInitializationAttribute>())
                {
                    yield return attribute;
                }
            }
        }
    }
}
