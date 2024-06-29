namespace SubtleEngineering.Analyzers.Decorators
{
    using System;

    [AttributeUsage(AttributeTargets.Constructor| AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class ExcludeFromExhaustiveAnalysisAttribute : Attribute
    {
        public ExcludeFromExhaustiveAnalysisAttribute()
        {
        }
    }
}
