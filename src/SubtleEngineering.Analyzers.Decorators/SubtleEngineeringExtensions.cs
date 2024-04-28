namespace SubtleEngineering.Analyzers.Decorators
{
    using System;
    using System.Collections.Generic;
    using System.Text;

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
    }
}
