namespace SubtleEngineering.Analyzers
{
    public class DiagnosticIds
    {
        public const string TypeMustBeInstantiatedWithinAUsingStatement = "SE1000";
        public const string TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable = "SE1001";
        public const string MethodsReturnsDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable = "SE1002";

        public const string DoNotUseRelativeImportUsingStatements = "SE1010";

        public const string RestrictedConstraintUsed = "SE1020";
        public const string RestrictedConstraintMisusedOnAssembly = "SE1021";
        public const string RestrictedConstraintMisusedOnGenericParameter = "SE1022";
    }
}
