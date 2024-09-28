using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using SubtleEngineering.Analyzers.Decorators;
using SubtleEngineering.Analyzers.ExhaustiveEnumSwitch;
using VerifyAn = SubtleEngineering.Analyzers.Tests.CSharpAnalyzerVerifier<SubtleEngineering.Analyzers.ExhaustiveEnumSwitch.ExhaustiveEnumSwitchAnalyzer>;

namespace SubtleEngineering.Analyzers.Tests.ExhaustiveEnumSwitch
{
    public class ExhaustiveEnumSwitchAnalyzerTests
    {
        [Fact]
        public async Task SwitchStatement_AllEnumValuesCovered_NoDiagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;

                enum MyEnum { A, B, C }

                class TestClass
                {
                    void TestMethod()
                    {
                        var value = MyEnum.A;
                        switch (value.Exhaustive())
                        {
                            case MyEnum.A:
                            case MyEnum.B:
                            case MyEnum.C:
                            default:
                                break;
                        }
                    }
                }
                """;

            var sut = CreateSut(code, new List<DiagnosticResult>());
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchStatement_MissingEnumValue_Diagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C, D }

                class TestClass
                {
                    void TestMethod()
                    {
                        var value = MyEnum.A;
                        switch (value.Exhaustive())
                        {
                            case MyEnum.A:
                            case MyEnum.B:
                            default:
                                break;
                        }
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[0])
                .WithLocation(9, 17)
                .WithArguments("MyEnum", "C, D");
            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchStatement_NoDefaultCase_Diagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C }

                class TestClass
                {
                    void TestMethod()
                    {
                        var value = MyEnum.A;
                        switch (value.Exhaustive())
                        {
                            case MyEnum.A:
                            case MyEnum.B:
                            case MyEnum.C:
                                break;
                        }
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[0])
                .WithLocation(9, 17)
                .WithArguments("MyEnum", "(default or _)");
            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_AllEnumValuesCovered_NoDiagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C }

                class TestClass
                {
                    int TestMethod()
                    {
                        var value = MyEnum.A;
                        return value.Exhaustive() switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => 2,
                            MyEnum.C => 3,
                            _ => 0,
                        };
                    }
                }
                """;

            var sut = CreateSut(code, new List<DiagnosticResult>());
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_MissingEnumValue_Diagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C }

                class TestClass
                {
                    int TestMethod()
                    {
                        var value = MyEnum.A;
                        return value.Exhaustive() switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => 2,
                            _ => 0,
                        };
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[0])
                .WithLocation(9, 16)
                .WithArguments("MyEnum", "C");
            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_MissingEnumValue_WithSameValueForSome_Diagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A = 1, B = 2, C = 2, D = 3, E = 4, F = 4 }

                class TestClass
                {
                    int TestMethod()
                    {
                        var value = MyEnum.A;
                        return value.Exhaustive() switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => 2,
                            _ => 0,
                        };
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[0])
                .WithLocation(9, 16)
                .WithArguments("MyEnum", "D, (E or F)");
            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_MissingEnumValueWhenUsingOr_Diagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C, D }

                class TestClass
                {
                    int TestMethod()
                    {
                        var value = MyEnum.A;
                        return value.Exhaustive() switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B or MyEnum.D => 2,
                            _ => 0,
                        };
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[0])
                .WithLocation(9, 16)
                .WithArguments("MyEnum", "C");
            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_NoDefaultCase_Diagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C }

                class TestClass
                {
                    int TestMethod()
                    {
                        var value = MyEnum.A;
                        return value.Exhaustive() switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => 2,
                            MyEnum.C => 3,
                        };
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[0])
                .WithLocation(9, 16)
                .WithArguments("MyEnum", "(default or _)");
            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task ExhaustiveUsedWithoutSwitch_Diagnostic()
        {
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C }

                class TestClass
                {
                    void TestMethod()
                    {
                        var value = MyEnum.A.Exhaustive();
                        // Not using value in a switch
                        Console.WriteLine(value);
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[1])
                .WithLocation(9, 21)
                .WithArguments("MyEnum");
            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task ExhaustiveNotUsed_NoDiagnostic()
        {
            const string code = """
                enum MyEnum { A, B, C }

                class TestClass
                {
                    void TestMethod()
                    {
                        var value = MyEnum.A;
                        switch (value)
                        {
                            case MyEnum.A:
                                break;
                        }
                    }
                }
                """;

            var sut = CreateSut(code, new List<DiagnosticResult>());
            await sut.RunAsync();
        }

        [Fact]
        public async Task ExhaustiveCalledWithinIfStatement_Diagnostic()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B }

                class TestClass
                {
                    void TestMethod()
                    {
                        if (MyEnum.A.Exhaustive() == MyEnum.B)
                        {
                            // Do something
                        }
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[1])
                .WithLocation(8, 13)
                .WithArguments("MyEnum");
            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        private VerifyAn.Test CreateSut(string code, List<DiagnosticResult> expected)
        {
            var test = new VerifyAn.Test
            {
                ReferenceAssemblies = TestHelpers.Net80,
                TestState =
                {
                    Sources = { code },
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
