namespace SubtleEngineering.Analyzers.Decorators
{
    using System;
    using System.Threading.Tasks;

    public static class SubtleEngineeringExtensions
    {
        public static T ExcludeFromUsing<T>(this T disposable)
            where T : IDisposable
        {
            // No operation, serves as a marker for the analyzer.
            return disposable;
        }

        public static T ExcludeFromUsingAsync<T>(this T disposable)
            where T : IAsyncDisposable
        {
            // No operation, serves as a marker for the analyzer.
            return disposable;
        }

        public static TEnum ForceExhaustive<TEnum>(this TEnum t)
            where TEnum : Enum
            => t;
    }
}
