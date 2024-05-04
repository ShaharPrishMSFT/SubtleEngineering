namespace SubtleEngineering.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class Helpers
    {
        public static bool IsOfType(this ITypeSymbol symbol, Type type)
            => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).EndsWith(type.FullName);

        public static bool IsOfType<T>(this ITypeSymbol symbol)
            => symbol.IsOfType(typeof(T));


        public static bool FuzzyIsTypeOf(this ITypeSymbol symbol, string typeName)
            => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).EndsWith($".{typeName}");

        public static DiagnosticDescriptor Find(this ImmutableArray<DiagnosticDescriptor> rules, string id)
            => rules.Single(r => r.Id == id);

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
