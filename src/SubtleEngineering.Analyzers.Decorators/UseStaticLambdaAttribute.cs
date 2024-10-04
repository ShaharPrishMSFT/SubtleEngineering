namespace SubtleEngineering.Analyzers.Decorators
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class UseStaticLambdaAttribute : Attribute
    {
        public UseStaticLambdaAttribute()
        {
        }

        public UseStaticLambdaAttribute(string reasoning)
        {
            Reasoning = reasoning;
        }

        public string Reasoning { get; }
    }
}
