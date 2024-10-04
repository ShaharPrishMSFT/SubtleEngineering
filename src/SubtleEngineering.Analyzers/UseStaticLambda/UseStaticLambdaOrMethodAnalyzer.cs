using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using SubtleEngineering.Analyzers;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;
using System.Linq;
using System;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseStaticLambdaOrMethodAnalyzer : DiagnosticAnalyzer
{
    private const int SE1060 = 0;
    private const int SE1061 = 1;

    public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
        new DiagnosticDescriptor(
            "SE1060",
            "Property, field or parameter should use a static lambda or method",
            "The member '{0}' should be assigned a static lambda or method.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true),
        new DiagnosticDescriptor(
            "SE1061",
            "Property, field or parameter should use a static lambda or method",
            "The member '{0}' should be assigned a static lambda or method. Reason: {1}",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true)
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;

    public override void Initialize(AnalysisContext context)
    {
        // Configure analysis settings
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register actions to analyze assignments and initializations
        context.RegisterOperationAction(AnalyzeAssignmentOrInitializer, OperationKind.SimpleAssignment, OperationKind.VariableInitializer, OperationKind.ParameterInitializer);

        // Register action to analyze property declarations with initializers
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);

        // Register action to analyze method invocations
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private void AnalyzeAssignmentOrInitializer(OperationAnalysisContext context)
    {
        IOperation operation = context.Operation;
        ISymbol targetSymbol = null;
        IOperation valueOperation = null;

        // Determine the target symbol and the value being assigned
        switch (operation)
        {
            case ISimpleAssignmentOperation assignmentOperation:
                targetSymbol = GetTargetSymbol(assignmentOperation.Target);
                valueOperation = assignmentOperation.Value;
                break;

            case IVariableInitializerOperation variableInitializer:
                if (variableInitializer.Parent is IVariableDeclaratorOperation declarator)
                {
                    targetSymbol = declarator.Symbol;
                    valueOperation = variableInitializer.Value;
                }
                break;

            case IParameterInitializerOperation parameterInitializer:
                targetSymbol = parameterInitializer.Parameter;
                valueOperation = parameterInitializer.Value;
                break;
        }

        if (targetSymbol == null || valueOperation == null)
            return;

        // Check if the target symbol has the UseStaticLambdaAttribute
        if (!HasUseStaticLambdaAttribute(targetSymbol))
            return;

        // Analyze the value being assigned
        AnalyzeValueOperation(context.ReportDiagnostic, valueOperation, targetSymbol);
    }

    private void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        // Check if the property has an initializer
        var initializer = propertyDeclaration.Initializer;
        if (initializer == null)
            return;

        // Get the symbol for the property
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);
        if (propertySymbol == null)
            return;

        // Check if the property has the UseStaticLambdaAttribute
        if (!HasUseStaticLambdaAttribute(propertySymbol))
            return;

        // Get the operation for the initializer value
        var valueOperation = context.SemanticModel.GetOperation(initializer.Value, context.CancellationToken);
        if (valueOperation == null)
            return;

        // Create an OperationAnalysisContext to pass to AnalyzeValueOperation
        AnalyzeValueOperation(context.ReportDiagnostic, valueOperation, propertySymbol);
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        foreach (var argument in invocation.Arguments)
        {
            var parameter = argument.Parameter;
            if (parameter == null)
                continue;

            // Check if the parameter has the UseStaticLambdaAttribute
            if (!HasUseStaticLambdaAttribute(parameter))
                continue;

            var valueOperation = argument.Value;
            if (valueOperation == null)
                continue;

            // Analyze the argument value
            AnalyzeValueOperation(context.ReportDiagnostic, valueOperation, parameter);
        }
    }

    private ISymbol GetTargetSymbol(IOperation target)
    {
        return target switch
        {
            IFieldReferenceOperation fieldRef => fieldRef.Field,
            IPropertyReferenceOperation propRef => propRef.Property,
            ILocalReferenceOperation localRef => localRef.Local,
            IParameterReferenceOperation paramRef => paramRef.Parameter,
            IVariableDeclaratorOperation varDecl => varDecl.Symbol,
            _ => null
        };
    }

    private void AnalyzeValueOperation(Action<Diagnostic> report, IOperation valueOperation, ISymbol targetSymbol)
    {
        switch (valueOperation)
        {
            case IAnonymousFunctionOperation anonymousFunction:
                if (!anonymousFunction.Symbol.IsStatic)
                    ReportDiagnostic(report, targetSymbol, valueOperation.Syntax.GetLocation());
                break;

            case IDelegateCreationOperation delegateCreation:
                AnalyzeValueOperation(report, delegateCreation.Target, targetSymbol);
                break;

            case IMethodReferenceOperation methodReference:
                if (!methodReference.Method.IsStatic)
                    ReportDiagnostic(report, targetSymbol, valueOperation.Syntax.GetLocation());
                break;

            case IParenthesizedOperation parenthesizedOperation:
                AnalyzeValueOperation(report, parenthesizedOperation.Operand, targetSymbol);
                break;

            case IConversionOperation conversionOperation:
                AnalyzeValueOperation(report, conversionOperation.Operand, targetSymbol);
                break;

            case ILiteralOperation literalOperation:
                // Ignore null literals
                break;

            default:
                // Handle other cases if necessary
                break;
        }
    }

    private bool HasUseStaticLambdaAttribute(ISymbol symbol)
    {
        return symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "UseStaticLambdaAttribute");
    }

    private void ReportDiagnostic(Action<Diagnostic> report, ISymbol targetSymbol, Location location)
    {
        var attributeData = targetSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "SubtleEngineering.Analyzers.Decorators.UseStaticLambdaAttribute");
        var reasoning = attributeData?.ConstructorArguments.FirstOrDefault().Value?.ToString();

        int ruleIndex = string.IsNullOrEmpty(reasoning) ? SE1060 : SE1061;
        var rule = Rules[ruleIndex];

        var diagnostic = string.IsNullOrEmpty(reasoning)
            ? Diagnostic.Create(rule, location, targetSymbol.Name)
            : Diagnostic.Create(rule, location, targetSymbol.Name, reasoning);

        report(diagnostic);
    }
}