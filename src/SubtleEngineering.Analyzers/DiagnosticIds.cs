﻿namespace SubtleEngineering.Analyzers
{
    public class DiagnosticIds
    {
        public const string TypeMustBeInstantiatedWithinAUsingStatement = "SE1000";
        public const string TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable = "SE1001";
        public const string MethodsReturnsDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable = "SE1002";
    }
}
