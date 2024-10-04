using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using SubtleEngineering.Analyzers;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;

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

        // Register an operation action to analyze lambda assignments
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
    }

    private static void AnalyzeMember(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        var attribute = GetUseStaticLambdaAttribute(symbol);
        if (attribute == null)
        {
            return;
        }

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
                ReportIfNonStaticLambda(context, symbol, diagnosticDescriptor, initializer, reasonArg);
            }
            else if (syntax is PropertyDeclarationSyntax propertyDeclaration)
            {
                var initializer = propertyDeclaration.Initializer?.Value;
                ReportIfNonStaticLambda(context, symbol, diagnosticDescriptor, initializer, reasonArg);
            }
        }
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context)
    {
        var assignmentOperation = (IAssignmentOperation)context.Operation;
        ISymbol symbol = assignmentOperation.Target switch
        {
            IPropertyReferenceOperation propertyReference => (ISymbol)propertyReference.Property,
            IFieldReferenceOperation fieldReference => (ISymbol)fieldReference.Field,
            _ => null
        };

        if (symbol == null)
        {
            return;
        }

        var attribute = GetUseStaticLambdaAttribute(symbol);
        if (attribute != null)
        {
            AnalyzeLambdaAssignment(context, assignmentOperation.Value, symbol, attribute);
        }
    }

    private static void AnalyzeLambdaAssignment(OperationAnalysisContext context, IOperation value, ISymbol symbol, AttributeData attribute)
    {
        if (value is IAnonymousFunctionOperation lambdaOperation)
        {
            if (!lambdaOperation.Symbol.IsStatic)
            {
                var reasonArg = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value?.ToString() : null;
                var diagnosticDescriptor = reasonArg != null ? Rules[SE1061] : Rules[SE1060];
                var diagnostic = Diagnostic.Create(diagnosticDescriptor, symbol.Locations[0], symbol.Name, reasonArg);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static AttributeData GetUseStaticLambdaAttribute(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "SubtleEngineering.Analyzers.Decorators.UseStaticLambdaAttribute")
            {
                return attribute;
            }
        }
        return null;
    }

    private static void ReportIfNonStaticLambda(SymbolAnalysisContext context, ISymbol symbol, DiagnosticDescriptor diagnosticDescriptor, ExpressionSyntax initializer, string reasonArg)
    {
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