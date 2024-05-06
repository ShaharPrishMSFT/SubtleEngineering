namespace SubtleEngineering.Analyzers.Decorators
{
    using System;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = true, Inherited = true)]
    public class NegativeTypeConstraintAttribute : Attribute
    {
        public NegativeTypeConstraintAttribute(Type disallowedType, bool disallowDerived = false)
        {
            DisallowedType = disallowedType;
            DisallowDerived = disallowDerived;
        }

        public Type DisallowedType { get; }

        public bool DisallowDerived { get; }
    }
}
