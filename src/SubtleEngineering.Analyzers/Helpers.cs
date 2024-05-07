namespace SubtleEngineering.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.FlowAnalysis;

    public static class Helpers
    {
        public static readonly SymbolDisplayFormat FullyQualifiedNamespaceFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted);

        public static bool IsOfType(this ITypeSymbol symbol, string fullName)
            => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).EndsWith(fullName);

        public static bool IsOfType(this ITypeSymbol symbol, Type type)
            => symbol.IsOfType(type.FullName);

        public static bool IsOfType<T>(this ITypeSymbol symbol)
            => symbol.IsOfType(typeof(T));


        public static bool FuzzyIsTypeOf(this ITypeSymbol symbol, string typeName)
            => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).EndsWith($".{typeName}");

        public static DiagnosticDescriptor Find(this ImmutableArray<DiagnosticDescriptor> rules, string id)
            => rules.Single(r => r.Id == id);

        public static bool IsAssignableTo<T>(this ITypeSymbol symbol)
            => symbol.IsAssignableTo(typeof(T));

        public static bool IsAssignableTo(this ITypeSymbol symbol, Type type)
            => symbol.IsAssignableTo(type.FullName);

        public static bool IsAssignableTo(this ITypeSymbol symbol, string soughtFullName)
        {
            // Go through all base types and interfaces of each base type and see if it matches the type
            var current = symbol;
            while (current != null)
            {
                if (current.IsOfType(soughtFullName))
                {
                    return true;
                }

                if (current.AllInterfaces.Any(x => x.IsOfType(soughtFullName)))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        public static ITypeSymbol GetParameterTypeForArgument(ArgumentSyntax argument, SemanticModel model)
        {
            // Get the invocation expression or object creation from the parent of the ArgumentSyntax
            var parentExpression = argument.Parent?.Parent as ExpressionSyntax;
            if (parentExpression == null)
            {
                return null;
            }

            // Get the symbol for the method or constructor being called
            var symbolInfo = model.GetSymbolInfo(parentExpression);

            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                // Find the position of the argument in the argument list
                var argumentList = argument.Parent as ArgumentListSyntax;
                if (argumentList != null)
                {
                    int argumentIndex = argumentList.Arguments.IndexOf(argument);

                    // Retrieve the parameter type if the argument index is valid
                    if (argumentIndex >= 0 && argumentIndex < methodSymbol.Parameters.Length)
                    {
                        return methodSymbol.Parameters[argumentIndex].Type;
                    }
                }
            }

            return null;
        }
    }
}
