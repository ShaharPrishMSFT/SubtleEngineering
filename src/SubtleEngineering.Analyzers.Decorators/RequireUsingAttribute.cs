namespace SubtleEngineering.Analyzers.Decorators
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class RequireUsingAttribute : Attribute
    {
    }
}
