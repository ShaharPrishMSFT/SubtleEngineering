namespace SubtleEngineering.Analyzers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.CodeAnalysis;

    public static class Helpers
    {
        public static bool IsOfType(this ITypeSymbol symbol, Type type)
            => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == type.FullName;

        public static bool IsOfType<T>(this ITypeSymbol symbol)
            => symbol.IsOfType(typeof(T));


        public static bool FuzzyIsTypeOf(this ITypeSymbol symbol, string typeName)
            => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).EndsWith($".{typeName}");
    }
}
