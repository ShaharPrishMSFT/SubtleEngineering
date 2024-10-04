namespace SubtleEngineering.Analyzers.Tests.UseStaticLambda;

using VerifyCS = CSharpAnalyzerVerifier<UseStaticLambdaOrMethodAnalyzer>;
using SubtleEngineering.Analyzers.Decorators;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;

public class UseStaticLambdaOrMethodAnalyzerTests
{
    [Fact]
    public async Task TestNonStaticLambdaUsage()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                [UseStaticLambda("Ensure performance optimization")]
                public Func<int> MyLambda { get; set; }
            }

            public class Program
            {
                public void Main()
                {
                    var c = new MyClass();
                    c.MyLambda = () => 42;
                }
            }
            """;

        List<DiagnosticResult> expected = new List<DiagnosticResult>
        {
            VerifyCS.Diagnostic(
                UseStaticLambdaOrMethodAnalyzer.Rules[1])
                    .WithLocation(14, 22)
                    .WithArguments("MyLambda", "Ensure performance optimization")
        };
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestNonStaticLambdaParameterUsage()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                public static void DoIt(
                    [UseStaticLambda]
                    Func<int> lambda)
                {
                    lambda();
                }
            }

            public class Program
            {
                public void Main()
                {
                    MyClass.DoIt(() => 42);
                }
            }
            """;

        List<DiagnosticResult> expected = new List<DiagnosticResult>
        {
            VerifyCS.Diagnostic(
                UseStaticLambdaOrMethodAnalyzer.Rules[0])
                    .WithLocation(17, 22)
                    .WithArguments("lambda")
        };
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestNonStaticLambdaUsageWhenInitializing()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                [UseStaticLambda("Ensure performance optimization")]
                public Func<int> MyLambda { get; set; } = () => 42;
            }

            public class Program
            {
                public void Main()
                {
                    var c = new MyClass();
                }
            }
            """;

        List<DiagnosticResult> expected = new List<DiagnosticResult>
        {
            VerifyCS.Diagnostic(
                UseStaticLambdaOrMethodAnalyzer.Rules[1])
                    .WithLocation(6, 47)
                    .WithArguments("MyLambda", "Ensure performance optimization")
        };
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestStaticLambdaUsage_NoDiagnostic()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                [UseStaticLambda("Ensure performance optimization")]
                public Func<int> MyLambda { get; set; }
            }
            
            public class Program
            {
                public void Main()
                {
                    var c = new MyClass();
                    c.MyLambda = static () => 42;
                }
            }
            """;

        List<DiagnosticResult> expected = new List<DiagnosticResult>();
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestStaticLambdaUsage_WithField()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                [UseStaticLambda]
                public Func<int> MyLambdaField;
            }
            
            public class Program
            {
                public void Main()
                {
                    var c = new MyClass();
                    c.MyLambdaField = () => 42;
                }
            }
            """;

        List<DiagnosticResult> expected = new List<DiagnosticResult>
        {
            VerifyCS.Diagnostic(
                UseStaticLambdaOrMethodAnalyzer.Rules[0])
                    .WithLocation(14, 27)
                    .WithArguments("MyLambdaField")
        };
        var sut = CreateSut(code, expected); await sut.RunAsync();
    }

    [Fact]
    public async Task TestStaticLambdaUsage_WithImplicitDelegate()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                [UseStaticLambda]
                public Func<int> MyLambdaField;
            }
            
            public class Program
            {
                public void Main()
                {
                    var c = new MyClass();
                    c.MyLambdaField = MyFunc;
                }

                public int MyFunc() => 42;
            }
            """;

        List<DiagnosticResult> expected = new List<DiagnosticResult>
        {
            VerifyCS.Diagnostic(
                UseStaticLambdaOrMethodAnalyzer.Rules[0])
                    .WithLocation(14, 27)
                    .WithArguments("MyLambdaField")
        };
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestStaticLambdaUsage_WithImplicitStaticDelegate()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                [UseStaticLambda]
                public Func<int> MyLambdaField;
            }
            
            public class Program
            {
                public void Main()
                {
                    var c = new MyClass();
                    c.MyLambdaField = MyFunc;
                }

                public static int MyFunc() => 42;
            }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestStaticLambdaUsage_WithField_WhenStatic()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                [UseStaticLambda("Ensure performance optimization")]
                public Func<int> MyLambdaField;
            }
            
            public class Program
            {
                public void Main()
                {
                    var c = new MyClass();
                    c.MyLambdaField = static () => 42;
                }
            }
            """;

        List<DiagnosticResult> expected = new List<DiagnosticResult>();
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestStaticLambdaUsage_WithFieldInitializer()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            public class MyClass
            {
                [UseStaticLambda]
                public Func<int> MyLambdaField = () => 42;
            }
            
            public class Program
            {
                public void Main()
                {
                    var c = new MyClass();
                    c.MyLambdaField = () => 42;
                }
            }
            """;

        List<DiagnosticResult> expected = new List<DiagnosticResult>
        {
            VerifyCS.Diagnostic(
                UseStaticLambdaOrMethodAnalyzer.Rules[0])
                    .WithLocation(14, 27)
                    .WithArguments("MyLambdaField")
        };
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    private VerifyCS.Test CreateSut(string code, List<DiagnosticResult> expected)
    {
        var test = new VerifyCS.Test()
        {
            ReferenceAssemblies = TestHelpers.Net80,
            TestState =
            {
                Sources = { code },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(UseStaticLambdaAttribute).Assembly.Location),
                },
            }
        };

        test.ExpectedDiagnostics.AddRange(expected);

        return test;
    }
}