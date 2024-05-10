namespace SubtleEngineering.Analyzers.Decorators
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public class TaggedRestrictedTypeAttribute : Attribute
    {
        public TaggedRestrictedTypeAttribute(string tag, Type disallowedType, bool disallowDerived = false)
        {
            Tag = tag;
            DisallowedType = disallowedType;
            DisallowDerived = disallowDerived;
        }

        public string Tag { get; set; }

        public Type DisallowedType { get; }

        public bool DisallowDerived { get; }
    }
}
