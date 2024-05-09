namespace SubtleEngineering.Analyzers.Decorators
{
    using System;

    [AttributeUsage(AttributeTargets.GenericParameter)]
    public class RestrictedTypeConstraintAttribute : Attribute
    {
        public RestrictedTypeConstraintAttribute(Type disallowedType, bool disallowDerived = false)
        {
            DisallowedType = disallowedType;
            DisallowDerived = disallowDerived;
        }

        public Type DisallowedType { get; }

        public bool DisallowDerived { get; }
    }
}
