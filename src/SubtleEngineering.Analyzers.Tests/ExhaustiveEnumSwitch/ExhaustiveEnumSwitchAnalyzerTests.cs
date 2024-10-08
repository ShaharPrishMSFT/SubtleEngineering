﻿using FluentAssertions.Equivalency;
using Microsoft;
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
        public async Task NormalSwitchDoesNothing()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C }
                
                class TestClass
                {
                    void TestMethod()
                    {
                        var obj = new object();
                        var x = obj switch
                        {
                            int v => MyInvocation(MyEnum.A),
                            double v => MyInvocation(MyEnum.B),
                            float v => MyInvocation(MyEnum.C),
                            _ => (object)null,
                        };
                    }

                    public static object MyInvocation<T>(T o) => o;
                }
                """;

            var sut = CreateSut(code, new List<DiagnosticResult>());
            await sut.RunAsync();
        }

        [Fact]
        public async Task NestedSwitchWorks()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B }
                enum MyEnum2 { C, D }
                                
                class TestClass
                {
                    void TestMethod()
                    {
                        var e = MyEnum.A;
                        var e2 = MyEnum2.C;
                        var x = e.ForceExhaustive() switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => e2.ForceExhaustive() switch
                                {
                                    MyEnum2.C => 3,
                                    MyEnum2.D => 4,
                                    _ => 0,
                                },
                            _ => (object)null,
                        };
                    }

                    public static object MyInvocation<T>(T o) => o;
                }
                """;

            var sut = CreateSut(code, new List<DiagnosticResult>());
            await sut.RunAsync();
        }

        [Fact]
        public async Task NestedSwitchWhereTopHasForceAndMissingValuesWorks()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B }
                enum MyEnum2 { C, D }
                                
                class TestClass
                {
                    void TestMethod()
                    {
                        var e = MyEnum.A;
                        var e2 = MyEnum2.C;
                        var x = e.ForceExhaustive() switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => e2 switch
                                {
                                    MyEnum2.C => 3,
                                    MyEnum2.D => 4,
                                    _ => 0,
                                },
                            _ => (object)null,
                        };
                    }

                    public static object MyInvocation<T>(T o) => o;
                }
                """;

            var sut = CreateSut(code, new List<DiagnosticResult>());
            await sut.RunAsync();
        }

        [Fact]
        public async Task NestedSwitchWhereInnerHasForceWorks()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B }
                enum MyEnum2 { C, D }
                                
                class TestClass
                {
                    void TestMethod()
                    {
                        var e = MyEnum.A;
                        var e2 = MyEnum2.C;
                        var x = e switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => e2.ForceExhaustive() switch
                                {
                                    MyEnum2.C => 3,
                                    MyEnum2.D => 4,
                                    _ => 0,
                                },
                            _ => (object)null,
                        };
                    }

                    public static object MyInvocation<T>(T o) => o;
                }
                """;

            var sut = CreateSut(code, new List<DiagnosticResult>());
            await sut.RunAsync();
        }

        [Fact]
        public async Task NestedSwitchWhereTopHasForceWorks()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B }
                enum MyEnum2 { C, D }
                                
                class TestClass
                {
                    void TestMethod()
                    {
                        var e = MyEnum.A;
                        var e2 = MyEnum2.C;
                        var x = e.ForceExhaustive() switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => e2 switch
                                {
                                    MyEnum2.C => 3,
                                    MyEnum2.D => 4,
                                    _ => 0,
                                },
                            _ => (object)null,
                        };
                    }

                    public static object MyInvocation<T>(T o) => o;
                }
                """;

            var sut = CreateSut(code, new List<DiagnosticResult>());
            await sut.RunAsync();
        }

        [Fact]
        public async Task NestedSwitchWhereInnerHasForceAndMissingWorks()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B }
                enum MyEnum2 { C, D }
                                
                class TestClass
                {
                    void TestMethod()
                    {
                        var e = MyEnum.A;
                        var e2 = MyEnum2.C;
                        var x = e switch
                        {
                            MyEnum.A => 1,
                            MyEnum.B => e2.ForceExhaustive() switch
                                {
                                    MyEnum2.C => 3,
                                    _ => 0,
                                },
                            _ => (object)null,
                        };
                    }

                    public static object MyInvocation<T>(T o) => o;
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[0])
                .WithLocation(14, 25)
                .WithArguments("MyEnum2", "D");

            var sut = CreateSut(code, new List<DiagnosticResult>() { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task NestedSwitchWhereOuterHasForceAndMissingWorks()
        {
            const string code = """
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B }
                enum MyEnum2 { C, D }
                                
                class TestClass
                {
                    void TestMethod()
                    {
                        var e = MyEnum.A;
                        var e2 = MyEnum2.C;
                        var x = e.ForceExhaustive() switch
                        {
                            MyEnum.B => e2 switch
                                {
                                    MyEnum2.C => 3,
                                    MyEnum2.D => 4,
                                    _ => 0,
                                },
                            _ => (object)null,
                        };
                    }

                    public static object MyInvocation<T>(T o) => o;
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[0])
                .WithLocation(11, 17)
                .WithArguments("MyEnum", "A");

            var sut = CreateSut(code, new List<DiagnosticResult>() { expected });
            await sut.RunAsync();
        }

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
                        switch (value.ForceExhaustive())
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
                        switch (value.ForceExhaustive())
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
                        switch (value.ForceExhaustive())
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
                        return value.ForceExhaustive() switch
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
                        return value.ForceExhaustive() switch
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
                        return value.ForceExhaustive() switch
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
                        return value.ForceExhaustive() switch
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
                        return value.ForceExhaustive() switch
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
                        var value = MyEnum.A.ForceExhaustive();
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
        public async Task ExhaustiveUsedInUnsupportedWayInTuple_Diagnostic()
        {
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                enum MyEnum { A, B, C }
                enum MyEnum2 { A, B, C }
                
                class TestClass
                {
                    void TestMethod()
                    {
                        var value = MyEnum.A;
                        var value2 = MyEnum2.B;
                        var result = (value.ForceExhaustive(), value2.ForceExhaustive()) switch
                        {
                            (MyEnum.A, MyEnum2.B) => 1,
                            _ => 2,
                        };

                        Console.WriteLine(value);
                    }
                }
                """;

            DiagnosticResult[] expected = [
                VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[1])
                    .WithLocation(12, 23)
                    .WithArguments("MyEnum"),
                VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[1])
                    .WithLocation(12, 48)
                    .WithArguments("MyEnum2"),
            ];
            var sut = CreateSut(code, expected.ToList());
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
                        if (MyEnum.A.ForceExhaustive() == MyEnum.B)
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

        [Fact]
        public async Task SwitchWithUnsupportedPattern_ShouldReportSE1052()
        {
            var code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
                namespace TestNamespace
                {
                    enum MyEnum { Option1, Option2, Option3 }

                    class TestClass
                    {
                        void TestMethod()
                        {
                            MyEnum value = MyEnum.Option1;
                            switch (value.ForceExhaustive())
                            {
                                case MyEnum.Option1:
                                    break;
                                case MyEnum.Option2:
                                    break;
                                case var x when x > MyEnum.Option2:
                                    break;
                            }
                        }
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[2])
                .WithLocation(13, 21) // Adjust the span to the location of Exhaustive() invocation
            .WithArguments();

            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchExpressionWithUnsupportedPattern_ShouldReportSE1052()
        {
            var code = """
            using System;
            using SubtleEngineering.Analyzers.Decorators;
            
            namespace TestNamespace
            {
                enum MyEnum { Option1, Option2, Option3 }

                class TestClass
                {
                    int TestMethod()
                    {
                        MyEnum value = MyEnum.Option1;
                        return value.ForceExhaustive() switch
                        {
                            MyEnum.Option1 => 1,
                            MyEnum.Option2 => 2,
                            var x when x > MyEnum.Option2 => 3,
                        };
                    }
                }
            }
            """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[2])
                .WithLocation(13, 20) // Adjust the span to the location of Exhaustive() invocation
            .WithArguments();

            var sut = CreateSut(code, new List<DiagnosticResult> { expected });
            await sut.RunAsync();
        }

        [Fact]
        public async Task SwitchWithNegatedPattern_ShouldReportSE1052()
        {
            var code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
                namespace TestNamespace
                {
                    enum MyEnum { Option1, Option2, Option3, Option4 }

                    class TestClass
                    {
                        void TestMethod()
                        {
                            MyEnum value = MyEnum.Option1;
                            switch (value.ForceExhaustive())
                            {
                                case MyEnum.Option1:
                                    break;
                                case not MyEnum.Option1:
                                    break;
                            }
                        }
                    }
                }
                """;

            var expected = VerifyAn.Diagnostic(ExhaustiveEnumSwitchAnalyzer.Rules[2])
                .WithLocation(13, 21) // Adjust the span to the location of Exhaustive() invocation
                .WithArguments();

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
