namespace SubtleEngineering.Analyzers.Decorators
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class ExhaustiveInitializationAttribute : Attribute
    {
        public ExhaustiveInitializationAttribute()
        {
        }
    }
}
