namespace SubtleEngineering.Analyzers
{
    public class DiagnosticsDetails
    {
        public class RequireUsing
        {
            public const string TypeMustBeInstantiatedWithinAUsingStatementId = "SE1000";
            public const string TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposableId = "SE1001";
            public const string MethodsReturnsDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposableId = "SE1002";
        }

        public class RelativeImport
        {
            public const string DoNotUseRelativeImportUsingStatementsId = "SE1010";
        }

        public class RestrictedConstraint
        {
            public const string UsedId = "SE1020";
            public const string MisusedOnAssemblyId = "SE1021";
            public const string MisusedOnGenericParameterId = "SE1022";
        }

        public class ExhaustiveInitialization
        {
            public const string TypeInitializationIsNonExhaustiveId = "SE1030";
            public const string PropertyIsMissingRequiredId = "SE1031";
            public const string OnlyOneConstructorAllowedId = "SE1032";
            public const string NotAllowedOnTypeId = "SE1033";

            public const string PrimaryCtorOnNonRecordReason = "primary constructors on classes and structs cannot be analyzed for assignment to non-required properties. Use a regular constructor if you need to initialize properties that are not set as required.";
        }
    }
}
