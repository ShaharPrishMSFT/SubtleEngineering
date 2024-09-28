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
    public class ExhaustiveEnumSwitchCodeFixTests
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
                            switch (value.ForceExhaustive())
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
                            switch (value.ForceExhaustive())
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
                    .WithLocation(13, 21) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "B, C, (default or _)"),
            };

            var test = CreateTest(code, fixedCode, expected);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchStatement_MissingEnumCases_ShouldAddCases_ButOnlyByValue()
        {
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
                namespace TestNamespace
                {
                    enum MyEnum { A, B, C, D = B, E = C, F = 0, G = 1 }

                    class TestClass
                    {
                        void TestMethod()
                        {
                            var value = MyEnum.A;
                            switch (value.ForceExhaustive())
                            {
                                case MyEnum.A:
                                    break;
                                default:
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
                    enum MyEnum { A, B, C, D = B, E = C, F = 0, G = 1 }
                
                    class TestClass
                    {
                        void TestMethod()
                        {
                            var value = MyEnum.A;
                            switch (value.ForceExhaustive())
                            {
                                case MyEnum.A:
                                    break;
                                case MyEnum.B:
                                    throw new NotImplementedException();
                                case MyEnum.C:
                                    throw new NotImplementedException();
                                default:
                                    break;
                            }
                        }
                    }
                }
                """;

            var expected = new List<DiagnosticResult>
            {
                VerifyCf.Diagnostic(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault)
                    .WithLocation(13, 21) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "(B or D or G), (C or E)"),
            };

            var test = CreateTest(code, fixedCode, expected);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchStatement_MissingEnumCases_ShouldAddCases_ButOnlyByValue_WithMissingDefault()
        {
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
                namespace TestNamespace
                {
                    enum MyEnum { A, B, C, D = B, E = C, F = 0, G = 1 }

                    class TestClass
                    {
                        void TestMethod()
                        {
                            var value = MyEnum.A;
                            switch (value.ForceExhaustive())
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
                    enum MyEnum { A, B, C, D = B, E = C, F = 0, G = 1 }
                
                    class TestClass
                    {
                        void TestMethod()
                        {
                            var value = MyEnum.A;
                            switch (value.ForceExhaustive())
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
                    .WithLocation(13, 21) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "(B or D or G), (C or E), (default or _)"),
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
                            return value.ForceExhaustive() switch
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
                            return value.ForceExhaustive() switch
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
                    .WithLocation(13, 20) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "B, C, (default or _)"),
            };

            var test = CreateTest(code, fixedCode, expected);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_MissingEnumCases_ButMatchingValues_ShouldAddCases()
        {
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
                namespace TestNamespace
                {
                    enum MyEnum { A, B, C, D = B, E = C, F = 0, G = 1 }
                
                    class TestClass
                    {
                        int TestMethod()
                        {
                            var value = MyEnum.A;
                            return value.ForceExhaustive() switch
                            {
                                MyEnum.A => 1,
                                _ => 2
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
                    enum MyEnum { A, B, C, D = B, E = C, F = 0, G = 1 }
                
                    class TestClass
                    {
                        int TestMethod()
                        {
                            var value = MyEnum.A;
                            return value.ForceExhaustive() switch
                            {
                                MyEnum.A => 1,
                                MyEnum.B => throw new NotImplementedException(),
                                MyEnum.C => throw new NotImplementedException(),
                                _ => 2
                            };
                        }
                    }
                }
                """;

            var expected = new List<DiagnosticResult>
            {
                VerifyCf.Diagnostic(DiagnosticsDetails.ExhaustiveEnumSwitch.SwitchNeedsToCheckAllEnumValuesAndDefault)
                    .WithLocation(13, 20) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "(B or D or G), (C or E)"),
            };

            var test = CreateTest(code, fixedCode, expected);
            await test.RunAsync();
        }

        [Fact]
        public async Task SwitchExpression_MissingEnumCases_ButMatchingValues_ShouldAddCases_MissingDefault()
        {
            const string code = """
                using System;
                using SubtleEngineering.Analyzers.Decorators;
                
                namespace TestNamespace
                {
                    enum MyEnum { A, B, C, D = B, E = C, F = 0, G = 1 }
                
                    class TestClass
                    {
                        int TestMethod()
                        {
                            var value = MyEnum.A;
                            return value.ForceExhaustive() switch
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
                    enum MyEnum { A, B, C, D = B, E = C, F = 0, G = 1 }
                
                    class TestClass
                    {
                        int TestMethod()
                        {
                            var value = MyEnum.A;
                            return value.ForceExhaustive() switch
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
                    .WithLocation(13, 20) // Adjust the span to the location of Exhaustive() invocation
                    .WithArguments("MyEnum", "(B or D or G), (C or E), (default or _)"),
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
                            switch (value.ForceExhaustive())
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
                            switch (value.ForceExhaustive())
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
                    .WithLocation(13, 21) // Adjust the span to the location of Exhaustive() invocation
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
