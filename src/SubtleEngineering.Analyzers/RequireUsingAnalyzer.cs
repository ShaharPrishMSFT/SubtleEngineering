namespace SubtleEngineering.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Data;
    using System.Linq;
    using System.Collections.Immutable;
    using SubtleEngineering.Analyzers.Decorators;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RequireUsingAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1000 = 0;
        private const int SE1001 = 1;

        private static readonly ImmutableArray<DiagnosticDescriptor> Rule = ImmutableArray.Create(
            new DiagnosticDescriptor(
                "SE1000",
                "Type must be instantiated within a using statement",
                "Type or method '{0}' must only be instantiated or called within a using statement or using declaration",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                "SE1001",
                $"Types decorated with the {nameof(RequireUsingAttribute)} attribute must inherit from IDisposable or IDisposableAsync",
                "Type '{0}' must support IDisposable or IDisposableAsync to apply the attribute to it",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true)
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rule;


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMisuseOfAttribute, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeMisuseOfAttribute(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            // Check if the class is decorated with the RequireUsingAttribute
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            if (classSymbol == null)
            {
                return;
            }

            if (!HasAttribute<RequireUsingAttribute>(classSymbol))
                return;

            // Check if the class implements IDisposable or IAsyncDisposable
            var implementsIDisposable = classSymbol.AllInterfaces.Any(i => i.IsOfType<IDisposable>() || i.FuzzyIsTypeOf("IAsyncDisposable"));

            if (!implementsIDisposable)
            {
                // If the class does not implement the required interfaces, report a diagnostic
                var diagnostic = Diagnostic.Create(Rule[SE1001], classDeclaration.Identifier.GetLocation(), classSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
            // Get the type symbol and check for the attribute
            var typeSymbol = context.SemanticModel.GetTypeInfo(objectCreation).Type;

            if (typeSymbol == null)
            {
                return;
            }

            if (IsExcludedFromUsing(objectCreation, context))
            {
                return;
            }

            if (HasAttribute<RequireUsingAttribute>(typeSymbol))
            {
                // Verify 'using' context and report diagnostics as needed
                CheckUsingContext(typeSymbol.Name, objectCreation, context);
            }
        }

        // Method to analyze method invocations
        private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetSymbolInfo(invocationExpression).Symbol as IMethodSymbol;

            if (methodSymbol == null)
            {
                return;
            }

            //if (IsExcludedFromUsing(invocationExpression, context))
            //{
            //    return;
            //}

            if (HasAttribute<RequireUsingAttribute>(methodSymbol))
            {
                // Verify 'using' context and report diagnostics as needed
                CheckUsingContext(methodSymbol.Name, invocationExpression, context);
            }
        }

        private static bool IsExcludedFromUsing(ExpressionSyntax objectCreation, SyntaxNodeAnalysisContext context)
        {
            var parent = objectCreation.Parent;

            // Check if the parent is an argument to a method invocation
            if (parent is ArgumentSyntax argument && argument.Parent is ArgumentListSyntax argumentList)
            {
                if (argumentList.Parent is InvocationExpressionSyntax invocation)
                {
                    var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;

                    // Get the attributes to see if it's method that 
                    if ((methodSymbol?.Name == nameof(SubtleEngineeringExtensions.ExcludeFromUsing) || methodSymbol?.Name == nameof(SubtleEngineeringExtensions.ExcludeFromUsingAsync)) &&
                        methodSymbol.ContainingType.Name == nameof(SubtleEngineeringExtensions))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasAttribute(ISymbol typeSymbol, string name)
            => typeSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == name);

        private static bool HasAttribute<T>(ISymbol typeSymbol)
            where T : Attribute
            => typeSymbol.GetAttributes().Any(attr => attr.AttributeClass?.Name == typeof(T).Name);

        // Common method to check 'using' context and report diagnostics
        private static void CheckUsingContext(string symbolUsed, SyntaxNode node, SyntaxNodeAnalysisContext context)
        {
            // Logic to determine if node is within a 'using' statement or declaration
            if (!IsInUsingContext(node))
            {
                var diagnostic = Diagnostic.Create(Rule[SE1000], node.GetLocation(), symbolUsed);
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Utility to check if the given node is within a 'using' context
        private static bool IsInUsingContext(SyntaxNode node)
        {
            // Traverse the syntax tree upwards and check for 'using' contexts
            for (var current = node.Parent; current != null; current = current.Parent)
            {
                // Check for using statement
                if (current is UsingStatementSyntax usingStatement)
                {
                    // Check if the node is part of the expression that is being disposed
                    if (usingStatement.Declaration != null &&
                        usingStatement.Declaration.Variables.Any(v => v.Initializer?.Value == node))
                    {
                        return true;
                    }
                    else if (usingStatement.Expression != null && usingStatement.Expression == node)
                    {
                        return true;
                    }
                }
                // Check for using declaration (C# 8.0+)
                else if (current is LocalDeclarationStatementSyntax localDeclaration &&
                         localDeclaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                {
                    // Check if the node is part of the variables being declared and initialized
                    if (localDeclaration.Declaration.Variables.Any(v => v.Initializer?.Value == node))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
