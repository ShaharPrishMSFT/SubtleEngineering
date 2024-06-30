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
        private const int SE1034 = 4;

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
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticsDetails.ExhaustiveInitialization.PrimaryDefaultConstructorValuesNotAllowedId,
                "Record with default values in primary constructor",
                "Record type '{0}' constructor parameter/property {1} has a default initialization value",
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
            bool needToEmitTypeWarning = false;
            List<IPropertySymbol> potentiallyBadProperties = new List<IPropertySymbol>();
            List<IParameterSymbol> badParameters = new List<IParameterSymbol>();
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            if ((namedTypeSymbol.TypeKind == TypeKind.Class || namedTypeSymbol.TypeKind == TypeKind.Struct) &&
                namedTypeSymbol.HasAttribute<ExhaustiveInitializationAttribute>())
            {
                var allNonePrivateProperties = namedTypeSymbol
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(x => x.DeclaredAccessibility != Accessibility.Private && x.SetMethod != null && !x.IsStatic && !x.HasAttribute<ExcludeFromExhaustiveAnalysisAttribute>());

                potentiallyBadProperties = allNonePrivateProperties.Where(x => !x.IsRequired).ToList();

                if (potentiallyBadProperties.Count == 0)
                {
                    return;
                }

                // Find accessible, non-clone constructors.
                var constructors = namedTypeSymbol
                    .Constructors
                    .Where(x => x.DeclaredAccessibility != Accessibility.Private && !x.IsImplicitlyDeclared && !x.HasAttribute<ExcludeFromExhaustiveAnalysisAttribute>());

                if (constructors.Count() > 1)
                {
                    var diagnostic = Diagnostic.Create(Rules[SE1032], namedTypeSymbol.Locations[0], namedTypeSymbol.ToDisplayString());
                    ReportDiagnostic(diagnostic);
                    return;
                }

                var constructor = constructors.SingleOrDefault();
                ConstructorDeclarationSyntax constructorSyntax = null;
                if (constructor != null)
                {

                    constructorSyntax = constructor
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
                            .Select(x => potentiallyBadProperties.FirstOrDefault(prop => x.IsPropertyIdentifier(prop)))
                            .Where(x => x != null)
                            .ToList();


                        potentiallyBadProperties.RemoveAll(x => assignedProperties.Contains(x, SymbolEqualityComparer.Default));
                    }
                    else if (constructorSyntax == null && !namedTypeSymbol.IsRecord)
                    {
                        var typeDiagnostic = Diagnostic.Create(Rules[SE1033], namedTypeSymbol.Locations[0], namedTypeSymbol.ToDisplayString(), DiagnosticsDetails.ExhaustiveInitialization.PrimaryCtorOnNonRecordReason);
                        ReportDiagnostic(typeDiagnostic);
                    }
                    else if (namedTypeSymbol.IsRecord)
                    {
                        potentiallyBadProperties.RemoveAll(x => HasMatchingParameterName(constructor, x));
                    }
                }

                foreach (var property in potentiallyBadProperties)
                {
                    if (property.DeclaringSyntaxReferences.Length > 0)
                    {
                        var syntaxReference = property.DeclaringSyntaxReferences[0];
                        var propertySyntax = syntaxReference.GetSyntax(context.CancellationToken);

                        if (!property.IsRequired)
                        {
                            var diagnostic = Diagnostic.Create(Rules[SE1031], property.Locations[0], namedTypeSymbol.ToDisplayString(), property.Name);
                            ReportDiagnostic(diagnostic);
                        }
                    }
                }

                // If this is a record, check for default values in the primary constructor
                if (namedTypeSymbol.IsRecord && constructor != null && constructorSyntax == null)
                {
                    var defaultValues = constructor
                        .Parameters
                        .Where(x => x.HasExplicitDefaultValue)
                        .ToList();

                    foreach (var param in defaultValues)
                    {
                        var syntaxReference = param.DeclaringSyntaxReferences[0];
                        var syntax = syntaxReference.GetSyntax();
                        var diagnostic = Diagnostic.Create(Rules[SE1034], syntax.GetLocation(), namedTypeSymbol.ToDisplayString(), param.Name);
                        badParameters.Add(param);
                        ReportDiagnostic(diagnostic);
                    }
                }
            }

            if (needToEmitTypeWarning)
            {
                var props = potentiallyBadProperties
                    .Select(x => (DiagnosticsDetails.ExhaustiveInitialization.BadPropertyPrefix, x.Name))
                    .Concat(badParameters.Select(x => (DiagnosticsDetails.ExhaustiveInitialization.BadParameterPrefix, x.Name)))
                    .ToImmutableDictionary(x => $"{x.Item1}_{x.Item2}", x => x.Item2);

                var typeDiagnostic = Diagnostic.Create(Rules[SE1030], namedTypeSymbol.Locations[0], props, namedTypeSymbol.ToDisplayString());
                context.ReportDiagnostic(typeDiagnostic);
            }

            void ReportDiagnostic(Diagnostic diagnostic)
            {
                context.ReportDiagnostic(diagnostic);
                needToEmitTypeWarning = true;
            }
        }

        bool HasMatchingParameterName(IMethodSymbol methodSymbol, IPropertySymbol propertySymbol)
        {
            return methodSymbol.Parameters.Any(p => p.Name == propertySymbol.Name);
        }
    }
}
