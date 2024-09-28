using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCf = SubtleEngineering.Analyzers.Tests.CSharpCodeFixVerifier<
    SubtleEngineering.Analyzers.ExhaustiveEnumSwitch.ExhaustiveEnumSwitchAnalyzer,
    SubtleEngineering.Analyzers.ExhaustiveEnumSwitch.AddMissingEnumCasesCodeFixProvider>;

namespace SubtleEngineering.Analyzers.Tests.ExhaustiveEnumSwitch
{
    public class AddMissingEnumCasesCodeFixTests
    {
        [Fact]
        public async Task SwitchStatement_MissingEnumCases_ShouldAddCases()
        {
            const string code = @"
using System;

namespace TestNamespace
{
    enum MyEnum { A, B, C }

    class TestClass
    {
        void TestMethod()
        {
            var value = MyEnum.A;
            switch (value.Exhaustive())
            {
                case MyEnum.A:
                    break;
            }
        }
    }
}
";

            const string fixedCode = @"
using System;

namespace TestNamespace
{
    enum MyEnum { A, B, C }

    class TestClass
    {
        void TestMethod()
        {
            var value = MyEnum.A;
            switch (value.Exhaustive())
            {
                case MyEnum.A:
                    break;
                case MyEnum.B:
                    throw new NotImplementedException();
                case MyEnum.C:
                    throw new NotImplementedException();
            }
        }
    }
}
";

            var expectedDiagnostics = new List<DiagnosticResult>
            {
                VerifyCf.Diagnostic(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault)
                    .WithSpan(13, 21, 13, 39) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "B, C, (default or _)"),
            };

            var test = CreateTest(code, fixedCode, expectedDiagnostics);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_MissingEnumCases_ShouldAddCases()
        {
            const string code = @"
using System;

namespace TestNamespace
{
    enum MyEnum { A, B, C }

    class TestClass
    {
        int TestMethod()
        {
            var value = MyEnum.A;
            return value.Exhaustive() switch
            {
                MyEnum.A => 1,
            };
        }
    }
}
";

            const string fixedCode = @"
using System;

namespace TestNamespace
{
    enum MyEnum { A, B, C }

    class TestClass
    {
        int TestMethod()
        {
            var value = MyEnum.A;
            return value.Exhaustive() switch
            {
                MyEnum.A => 1,
                MyEnum.B => throw new NotImplementedException(),
                MyEnum.C => throw new NotImplementedException(),
            };
        }
    }
}
";

            var expectedDiagnostics = new List<DiagnosticResult>
            {
                VerifyCf.Diagnostic(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault)
                    .WithSpan(13, 20, 13, 38) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "B, C, (default or _)"),
            };

            var test = CreateTest(code, fixedCode, expectedDiagnostics);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchStatement_WithDiscard_ShouldNotAddCases()
        {
            const string code = @"
using System;

namespace TestNamespace
{
    enum MyEnum { A, B }

    class TestClass
    {
        void TestMethod()
        {
            var value = MyEnum.A;
            switch (value.Exhaustive())
            {
                case MyEnum.A:
                    break;
                case _:
                    break;
            }
        }
    }
}
";

            // Since a discard pattern (_) is present, no code fix should be applied.
            var fixedCode = code;

            var expectedDiagnostics = new List<DiagnosticResult>(); // No diagnostics expected.

            var test = CreateTest(code, fixedCode, expectedDiagnostics);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_WithAllCases_ShouldNotAddCases()
        {
            const string code = @"
using System;

namespace TestNamespace
{
    enum MyEnum { A, B }

    class TestClass
    {
        int TestMethod()
        {
            var value = MyEnum.A;
            return value.Exhaustive() switch
            {
                MyEnum.A => 1,
                MyEnum.B => 2,
            };
        }
    }
}
";

            // All enum cases are covered, so no code fix should be applied.
            var fixedCode = code;

            var expectedDiagnostics = new List<DiagnosticResult>(); // No diagnostics expected.

            var test = CreateTest(code, fixedCode, expectedDiagnostics);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchStatement_MissingDefault_ShouldAddDefaultCase()
        {
            const string code = @"
using System;

namespace TestNamespace
{
    enum MyEnum { A, B }

    class TestClass
    {
        void TestMethod()
        {
            var value = MyEnum.A;
            switch (value.Exhaustive())
            {
                case MyEnum.A:
                    break;
                case MyEnum.B:
                    break;
            }
        }
    }
}
";

            const string fixedCode = @"
using System;

namespace TestNamespace
{
    enum MyEnum { A, B }

    class TestClass
    {
        void TestMethod()
        {
            var value = MyEnum.A;
            switch (value.Exhaustive())
            {
                case MyEnum.A:
                    break;
                case MyEnum.B:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
";

            var expectedDiagnostics = new List<DiagnosticResult>
            {
                VerifyCf.Diagnostic(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault)
                    .WithSpan(13, 21, 13, 39) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "(default or _)"),
            };

            var test = CreateTest(code, fixedCode, expectedDiagnostics);
            await test.RunAsync();
        }

        private VerifyCf.Test CreateTest(string code, string fixedCode, List<DiagnosticResult> expectedDiagnostics)
        {
            var test = new VerifyCf.Test()
            {
                TestCode = code,
                FixedCode = fixedCode,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
                TestState =
                {
                    AdditionalReferences =
                    {
                        // Add any additional references your analyzer or code fix requires
                    },
                },
            };

            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);

            return test;
        }
    }
}
