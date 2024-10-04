using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using SubtleEngineering.Analyzers;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Operations;
using System.Linq;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseStaticLambdaOrMethodAnalyzer : DiagnosticAnalyzer
{
    private const int SE1060 = 0;
    private const int SE1061 = 1;

    public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
        new DiagnosticDescriptor(
            DiagnosticsDetails.UseStaticLambdaOrMethod.UseStaticNoReasoning,
            "Property, field or parameter should use a static lambda or method",
            "The member '{0}' should be assigned a static lambda or method.",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true),
        new DiagnosticDescriptor(
            DiagnosticsDetails.UseStaticLambdaOrMethod.UseStaticWithReason,
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
        AnalyzeValueOperation(context, valueOperation, targetSymbol);
    }

    private ISymbol GetTargetSymbol(IOperation target)
    {
        return target switch
        {
            IFieldReferenceOperation fieldRef => fieldRef.Field,
            IPropertyReferenceOperation propRef => propRef.Property,
            ILocalReferenceOperation localRef => localRef.Local,
            IParameterReferenceOperation paramRef => paramRef.Parameter,
            _ => null
        };
    }

    private void AnalyzeValueOperation(OperationAnalysisContext context, IOperation valueOperation, ISymbol targetSymbol)
    {
        switch (valueOperation)
        {
            case IAnonymousFunctionOperation anonymousFunction:
                if (!anonymousFunction.Symbol.IsStatic)
                    ReportDiagnostic(context, targetSymbol, valueOperation.Syntax.GetLocation());
                break;

            case IDelegateCreationOperation delegateCreation:
                AnalyzeValueOperation(context, delegateCreation.Target, targetSymbol);
                break;

            case IMethodReferenceOperation methodReference:
                if (!methodReference.Method.IsStatic)
                    ReportDiagnostic(context, targetSymbol, valueOperation.Syntax.GetLocation());
                break;

            case IParenthesizedOperation parenthesizedOperation:
                AnalyzeValueOperation(context, parenthesizedOperation.Operand, targetSymbol);
                break;

            case IConversionOperation conversionOperation:
                AnalyzeValueOperation(context, conversionOperation.Operand, targetSymbol);
                break;

                // Handle other cases if necessary
        }
    }

    private bool HasUseStaticLambdaAttribute(ISymbol symbol)
    {
        return symbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == "UseStaticLambdaAttribute");
    }

    private void ReportDiagnostic(OperationAnalysisContext context, ISymbol targetSymbol, Location location)
    {
        var attributeData = targetSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "UseStaticLambdaAttribute");
        var reasoning = attributeData?.ConstructorArguments.FirstOrDefault().Value?.ToString();

        int ruleIndex = string.IsNullOrEmpty(reasoning) ? SE1060 : SE1061;
        var rule = Rules[ruleIndex];

        var diagnostic = string.IsNullOrEmpty(reasoning)
            ? Diagnostic.Create(rule, location, targetSymbol.Name)
            : Diagnostic.Create(rule, location, targetSymbol.Name, reasoning);

        context.ReportDiagnostic(diagnostic);
    }
}
