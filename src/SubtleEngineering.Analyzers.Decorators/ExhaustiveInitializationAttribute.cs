namespace SubtleEngineering.Analyzers.Decorators
{
    using System;

    [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class ExhaustiveInitializationAttribute : Attribute
    {
        public ExhaustiveInitializationAttribute()
        {
        }
    }
}
