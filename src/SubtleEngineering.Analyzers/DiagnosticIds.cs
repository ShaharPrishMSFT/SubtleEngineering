namespace SubtleEngineering.Analyzers
{
    public class DiagnosticIds
    {
        public class RequireUsing
        {
            public const string TypeMustBeInstantiatedWithinAUsingStatement = "SE1000";
            public const string TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable = "SE1001";
            public const string MethodsReturnsDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable = "SE1002";
        }

        public class RelativeImport
        {
            public const string DoNotUseRelativeImportUsingStatements = "SE1010";
        }

        public class RestrictedConstraint
        {
            public const string Used = "SE1020";
            public const string MisusedOnAssembly = "SE1021";
            public const string MisusedOnGenericParameter = "SE1022";
        }

        public class ExhaustiveInitialization
        {
            public const string PropertyIsMissingRequired = "SE1030";
            public const string OnlyOneConstructorAllowedWhenMissingRequired = "SE1031";
        }
    }
}
