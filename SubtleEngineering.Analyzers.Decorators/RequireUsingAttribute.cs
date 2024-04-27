namespace SubtleEngineering.Analyzers.Decorators
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class RequireUsingAttribute : Attribute
    {
    }
}
