﻿namespace SubtleEngineering.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using SubtleEngineering.Analyzers.Decorators;

    public static class Helpers
    {
        public static readonly SymbolDisplayFormat FullyQualifiedNamespaceFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted);

        public static readonly SymbolDisplayFormat FullyQualifiedClrTypeName = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);
        public static bool IsOfType(this ITypeSymbol symbol, string fullName)
            => symbol.ToDisplayString(FullyQualifiedClrTypeName).EndsWith(fullName);

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

        public static IEnumerable<AttributeData> GetAttributesOfType<T>(this ImmutableArray<AttributeData> attributes)
            => attributes.Where(a => a.AttributeClass.IsOfType<T>());

        public static bool HasAttribute<T>(this ISymbol symbol)
            => symbol.GetAttributes().GetAttributesOfType<T>().Any();
            
        public static string GetContainingNodeName(this INamedTypeSymbol symbol)
        {
            var containingSymbol = symbol.ContainingSymbol;

            // If the containing symbol is a method, get its name
            if (containingSymbol is IMethodSymbol methodSymbol)
            {
                return methodSymbol.Name;
            }

            // If the containing symbol is a named type, get its name
            if (containingSymbol is INamedTypeSymbol containingTypeSymbol)
            {
                return containingTypeSymbol.Name;
            }

            return "[Unknown]";
        }

        public static bool IsOfTypeOrGeneric(this ITypeSymbol symbol, INamedTypeSymbol ofType)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol, ofType))
            {
                return true;
            }

            if (ofType.IsUnboundGenericType && symbol is INamedTypeSymbol namedTypeSymbol)
            {
                return SymbolEqualityComparer.Default.Equals(namedTypeSymbol.ConstructUnboundGenericType(), ofType);
            }

            return false;
        }

        public static bool IsPropertyIdentifier(this IdentifierNameSyntax identifierName, IPropertySymbol propertySymbol)
        {
            var isMatchingName = identifierName.Identifier.Text == propertySymbol.Name;

            var isMemberAccessWithThis = identifierName.Parent is MemberAccessExpressionSyntax memberAccess &&
                                         memberAccess.Expression is ThisExpressionSyntax;

            var isDirectPropertyAccess = identifierName.Parent is AssignmentExpressionSyntax assignExpr &&
                                         assignExpr.Left == identifierName;

            return isMatchingName && (isMemberAccessWithThis || isDirectPropertyAccess);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            return source.Distinct(new DistinctByComparer<TSource, TKey>(keySelector));
        }

        private class DistinctByComparer<TSource, TKey> : IEqualityComparer<TSource>
        {
            private readonly Func<TSource, TKey> _keySelector;

            public DistinctByComparer(Func<TSource, TKey> keySelector)
            {
                _keySelector = keySelector;
            }

            public bool Equals(TSource x, TSource y)
            {
                return EqualityComparer<TKey>.Default.Equals(_keySelector(x), _keySelector(y));
            }

            public int GetHashCode(TSource obj)
            {
                return _keySelector(obj)?.GetHashCode() ?? 0;
            }
        }
    }
}
