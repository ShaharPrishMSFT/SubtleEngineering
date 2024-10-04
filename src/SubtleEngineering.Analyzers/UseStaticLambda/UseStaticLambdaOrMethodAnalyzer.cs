using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using SubtleEngineering.Analyzers;
using System.Collections.Immutable;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseStaticLambdaOrMethodAnalyzer : DiagnosticAnalyzer
{
    private const int SE1060 = 0;
    private const int SE1061 = 1;

    public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
        new DiagnosticDescriptor(
            DiagnosticsDetails.UseStaticLambdaOrMethod.UseStaticNoReasoning,
            "Property, field or argument should be a static lambda",
            "Argument, property or field '{0}' should be declared as static.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true),
        new DiagnosticDescriptor(
            DiagnosticsDetails.UseStaticLambdaOrMethod.UseStaticWithReason,
            "Property, field or argument should be a static lambda",
            "Argument, property or field '{0}' should be declared as static. Reasoning: {1}",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true)
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register a symbol action to analyze fields, properties, and method parameters
        context.RegisterSymbolAction(AnalyzeMember, SymbolKind.Field, SymbolKind.Property, SymbolKind.Parameter);
    }

    private static void AnalyzeMember(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        var attributes = symbol.GetAttributes();

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() == "SubtleEngineering.Analyzers.Decorators.UseStaticLambdaAttribute")
            {
                if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.Type.TypeKind == TypeKind.Delegate)
                {
                    AnalyzeLambda(fieldSymbol, context, attribute);
                }
                else if (symbol is IPropertySymbol propertySymbol && propertySymbol.Type.TypeKind == TypeKind.Delegate)
                {
                    AnalyzeLambda(propertySymbol, context, attribute);
                }
                else if (symbol is IParameterSymbol parameterSymbol && parameterSymbol.Type.TypeKind == TypeKind.Delegate)
                {
                    AnalyzeLambda(parameterSymbol, context, attribute);
                }

                return;
            }
        }
    }

    private static void AnalyzeLambda(ISymbol symbol, SymbolAnalysisContext context, AttributeData attribute)
    {
        var reasonArg = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value?.ToString() : null;
        var diagnosticDescriptor = reasonArg != null ? Rules[SE1061] : Rules[SE1060];

        // Check if the lambda assigned is static
        if (symbol.DeclaringSyntaxReferences.Length > 0)
        {
            var syntax = symbol.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);

            if (syntax is VariableDeclaratorSyntax variableDeclarator)
            {
                var initializer = variableDeclarator.Initializer?.Value;
                if (initializer is SimpleLambdaExpressionSyntax lambdaExpression && !lambdaExpression.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    var diagnostic = Diagnostic.Create(diagnosticDescriptor, symbol.Locations[0], symbol.Name, reasonArg);
                    context.ReportDiagnostic(diagnostic);
                }
                else if (initializer is ParenthesizedLambdaExpressionSyntax parenthesizedLambda && !parenthesizedLambda.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    var diagnostic = Diagnostic.Create(diagnosticDescriptor, symbol.Locations[0], symbol.Name, reasonArg);
                    context.ReportDiagnostic(diagnostic);
                }
            }
            else if (syntax is PropertyDeclarationSyntax propertyDeclaration)
            {
                var initializer = propertyDeclaration.Initializer?.Value;
                if (initializer is SimpleLambdaExpressionSyntax lambdaExpression && !lambdaExpression.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    var diagnostic = Diagnostic.Create(diagnosticDescriptor, symbol.Locations[0], symbol.Name, reasonArg);
                    context.ReportDiagnostic(diagnostic);
                }
                else if (initializer is ParenthesizedLambdaExpressionSyntax parenthesizedLambda && !parenthesizedLambda.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    var diagnostic = Diagnostic.Create(diagnosticDescriptor, symbol.Locations[0], symbol.Name, reasonArg);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}