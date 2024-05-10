namespace SubtleEngineering.Analyzers.Decorators
{
    using System;

    [AttributeUsage(AttributeTargets.GenericParameter, AllowMultiple = true, Inherited = true)]
    public class RestrictedTypeAttribute : Attribute
    {
        public RestrictedTypeAttribute(Type restrictedType, bool restrictDerived = true)
        {
            RestrictedType = restrictedType;
            RestrictDerived = restrictDerived;
        }

        public string GroupId { get; set; }

        public Type RestrictedType { get; }

        public bool RestrictDerived { get; }
    }
}
