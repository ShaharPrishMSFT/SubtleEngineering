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
                    .WithLocation(6, 22)
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