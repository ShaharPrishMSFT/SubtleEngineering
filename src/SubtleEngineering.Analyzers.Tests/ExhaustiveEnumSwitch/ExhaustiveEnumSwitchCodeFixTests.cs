using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using SubtleEngineering.Analyzers.Decorators;
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
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
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
                """;

            const string fixedCode = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
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
                                default:
                                    throw new InvalidOperationException();
                            }
                        }
                    }
                }
                """;

            var expected = new List<DiagnosticResult>
            {
                VerifyCf.Diagnostic(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault)
                    .WithSpan(13, 21, 13, 39) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "B, C, (default or _)"),
            };

            var test = CreateTest(code, fixedCode, expected);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_MissingEnumCases_ShouldAddCases()
        {
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
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
                """;

            const string fixedCode = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
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
                                _ => throw new InvalidOperationException()
                            };
                        }
                    }
                }
                """;

            var expected = new List<DiagnosticResult>
            {
                VerifyCf.Diagnostic(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault)
                    .WithSpan(13, 20, 13, 38) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "B, C, (default or _)"),
            };

            var test = CreateTest(code, fixedCode, expected);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchStatement_MissingDefault_ShouldAddDefaultCase()
        {
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
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
                """;

            const string fixedCode = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
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
                                    throw new InvalidOperationException();
                            }
                        }
                    }
                }
                """;

            var expected = new List<DiagnosticResult>
            {
                VerifyCf.Diagnostic(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault)
                    .WithSpan(13, 21, 13, 39) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "(default or _)"),
            };

            var test = CreateTest(code, fixedCode, expected);
            await test.RunAsync();
        }

        private VerifyCf.Test CreateTest(string code, string fixedCode, List<DiagnosticResult> expected)
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
                        MetadataReference.CreateFromFile(typeof(RequireUsingAttribute).Assembly.Location),
                    },
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);

            return test;
        }
    }
}
