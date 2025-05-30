﻿namespace SubtleEngineering.Analyzers.RequireUsing
{
    using System;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using System.Linq;
    using System.Collections.Immutable;
    using SubtleEngineering.Analyzers.Decorators;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RequireUsingAnalyzer : DiagnosticAnalyzer
    {
        private const int SE1000 = 0;
        private const int SE1001 = 1;
        private const int SE1002 = 2;

        public static readonly ImmutableArray<DiagnosticDescriptor> Rules = ImmutableArray.Create(
            new DiagnosticDescriptor(
                DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId,
                "Type must be instantiated within a using statement",
                "Type or method '{0}' must only be instantiated or called within a using statement or using declaration. Use the ExcludeFromUsing and ExcludeFromUsingAsync extension method on the IDisposable to mark that you will take care of disposing of it.",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticsDetails.RequireUsing.TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposableId,
                $"Types decorated with the {nameof(RequireUsingAttribute)} attribute must inherit from IDisposable or IDisposableAsync",
                "Type '{0}' must support IDisposable or IDisposableAsync to apply the attribute to it",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true),
            new DiagnosticDescriptor(
                DiagnosticsDetails.RequireUsing.MethodsReturnsDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposableId,
                $"Methods decorated with the {nameof(RequireUsingAttribute)} attribute must have a return value tha inherits from IDisposable or IDisposableAsync",
                "Method '{0}' must have a return type that supports IDisposable or IDisposableAsync to apply the attribute to it",
                "Usage",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true)
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Rules;


        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMisuseOfAttributeClass, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMisuseOfAttributeMethod, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMisuseOfAttributeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);

            if (methodSymbol == null)
            {
                return;
            }

            if (HasAttribute<RequireUsingAttribute>(methodSymbol))
            {
                var returnType = methodSymbol.ReturnType;

                // Check if the return type is a ref struct
                if (returnType.IsRefLikeType)
                {
                    // Check for public Dispose() or DisposeAsync() methods
                    if (HasPublicDisposeMethod(returnType))
                    {
                        return; // No diagnostic needed since the ref struct can be disposed via public Dispose methods
                    }
                }
                else if (!IsOrInheritingFromDisposable(returnType))
                {
                    // If the method's return type does not implement the required interfaces, report a diagnostic
                    var diagnostic = Diagnostic.Create(Rules[SE1002], methodDeclaration.ReturnType.GetLocation(), methodSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void AnalyzeMisuseOfAttributeClass(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            if (classSymbol == null)
            {
                return;
            }

            if (HasAttribute<RequireUsingAttribute>(classSymbol))
            {
                // Check if the class is a ref struct
                if (classSymbol.IsRefLikeType)
                {
                    // Check for public Dispose() or DisposeAsync() methods
                    if (HasPublicDisposeMethod(classSymbol))
                    {
                        return; // No diagnostic needed since the ref struct can be disposed via public Dispose methods
                    }
                }
                else if (!IsOrInheritingFromDisposable(classSymbol))
                {
                    // If the class does not implement the required interfaces, report a diagnostic
                    var diagnostic = Diagnostic.Create(Rules[SE1001], classDeclaration.Identifier.GetLocation(), classSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool HasPublicDisposeMethod(ITypeSymbol typeSymbol)
        {
            // Check if the type has a public Dispose() or DisposeAsync() method
            return typeSymbol.GetMembers().OfType<IMethodSymbol>().Any(m =>
                (m.Name == "Dispose" || m.Name == "DisposeAsync") &&
                m.DeclaredAccessibility == Accessibility.Public &&
                m.Parameters.IsEmpty);
        }
        private static bool IsOrInheritingFromDisposable(ITypeSymbol classSymbol)
        {
            if (classSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                // If it's a ValueTask<T>, check if T implements IDisposable/IAsyncDisposable
                if (namedType.ConstructedFrom.Name == "ValueTask")
                {
                    var typeArg = namedType.TypeArguments[0];
                    return typeArg.AllInterfaces.Append(typeArg).Any(i => i.IsOfType<IDisposable>() || i.FuzzyIsTypeOf("IAsyncDisposable"));
                }
            }
            return classSymbol.AllInterfaces.Append(classSymbol).Any(i => i.IsOfType<IDisposable>() || i.FuzzyIsTypeOf("IAsyncDisposable"));
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

            if (HasAttribute<RequireUsingAttribute>(typeSymbol))
            {
                // Verify 'using' context and report diagnostics as needed
                if (IsExcludedFromUsing(objectCreation, context))
                {
                    return;
                }

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

            if (HasAttribute<RequireUsingAttribute>(methodSymbol))
            {
                if (IsExcludedFromUsing(invocationExpression, context))
                {
                    return;
                }

                if (IsInsideExpressionLambda(invocationExpression, context))
                {
                    return;
                }

                // Verify 'using' context and report diagnostics as needed
                CheckUsingContext(methodSymbol.Name, invocationExpression, context);
            }
        }

        private static bool IsExcludedFromUsing(ExpressionSyntax usageExpression, SyntaxNodeAnalysisContext context)
        {
            var parent = usageExpression.Parent;

            // Check if the parent is an argument to a method invocation
            if (parent is MemberAccessExpressionSyntax memberAccess)
            {
                if (parent.Parent is InvocationExpressionSyntax invocation)
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
                var diagnostic = Diagnostic.Create(Rules[SE1000], node.GetLocation(), symbolUsed);
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

        private static bool IsInsideExpressionLambda(SyntaxNode node, SyntaxNodeAnalysisContext context)
        {
            var current = node;
            while (current != null)
            {
                if (current is LambdaExpressionSyntax lambda && lambda.Parent is ArgumentSyntax arg)
                {
                    ITypeSymbol symbolInfo = Helpers.GetParameterTypeForArgument(arg, context.SemanticModel);
                    if (symbolInfo.TypeKind == TypeKind.Class &&
                        symbolInfo.Name == "Expression")
                    {
                        return true;
                    }
                }
                current = current.Parent;
            }
            return false;
        }
    }
}
