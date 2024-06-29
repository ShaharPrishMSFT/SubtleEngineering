namespace SubtleEngineering.Analyzers.ExhaustiveInitialization
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis;
    using SubtleEngineering.Analyzers.Decorators;
    using System.Linq;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExhaustiveInitializationAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1030 = 0;
        private const int SE1031 = 1;
        private const int SE1032 = 2;
        private const int SE1033 = 3;

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticsDetails.ExhaustiveInitialization.TypeInitializationIsNonExhaustiveId,
                "Type initialization is non-exhaustive.",
                "Type '{0}' initialization is non-exhaustive.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId,
                "All properties on this type must be set to 'required'",
                "Type '{0}' has a property {1} that's not marked as 'required'.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticsDetails.ExhaustiveInitialization.OnlyOneConstructorAllowedId,
                "Marking a type as ExhaustiveInitialization means a type can only have one explicitly declared constructor. This type has more.",
                "Type '{0}' has more than one constructor.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticsDetails.ExhaustiveInitialization.NotAllowedOnTypeId,
                "This type is not compatible with the ExhaustiveInitialization attribute.",
                "Type '{0}' cannot have the ExhaustiveInitialization attribute applied to it because {1}.",
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
                var constructors = namedTypeSymbol
                    .Constructors
                    .Where(x => x.DeclaredAccessibility != Accessibility.Private && !x.IsImplicitlyDeclared);

                if (constructors.Count() != 1)
                {
                    var diagnostic = Diagnostic.Create(Rules[SE1032], namedTypeSymbol.Locations[0], namedTypeSymbol.ToDisplayString());
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
                else if (constructorSyntax == null && !namedTypeSymbol.IsRecord)
                {
                    var typeDiagnostic = Diagnostic.Create(Rules[SE1033], namedTypeSymbol.Locations[0], namedTypeSymbol.ToDisplayString(), DiagnosticsDetails.ExhaustiveInitialization.PrimaryCtorOnNonRecordReason);
                    context.ReportDiagnostic(typeDiagnostic);
                }
                else if (namedTypeSymbol.IsRecord)
                {
                    potentiallyBad.RemoveAll(x => HasMatchingParameterName(constructor, x));
                }

                bool emittedTypeError = false;
                foreach (var property in potentiallyBad)
                {
                    if (property.DeclaringSyntaxReferences.Length > 0)
                    {
                        var syntaxReference = property.DeclaringSyntaxReferences[0];
                        var propertySyntax = syntaxReference.GetSyntax(context.CancellationToken);

                        if (!property.IsRequired)
                        {
                            if (!emittedTypeError)
                            {
                                emittedTypeError = true;
                                var typeDiagnostic = Diagnostic.Create(Rules[SE1030], namedTypeSymbol.Locations[0], namedTypeSymbol.ToDisplayString());
                                context.ReportDiagnostic(typeDiagnostic);
                            }

                            var diagnostic = Diagnostic.Create(Rules[SE1031], property.Locations[0], namedTypeSymbol.ToDisplayString(), property.Name);
                            context.ReportDiagnostic(diagnostic);
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
