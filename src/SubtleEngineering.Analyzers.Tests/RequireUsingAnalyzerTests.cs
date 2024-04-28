namespace SubtleEngineering.Analyzers.Tests;

using VerifyCS = CSharpAnalyzerVerifier<RequireUsingAnalyzer>;
using SubtleEngineering.Analyzers.Decorators;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;

public class RequireUsingAnalyzerTests
{
    [Fact]
    public async Task TestMisuseOfRequireAttribute()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            [RequireUsing]
            public class MyClass
            {
            }
            """;

        var expected = VerifyCS.Diagnostic(
            RequireUsingAnalyzer.Rules.Find(DiagnosticIds.TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable))
                .WithLocation(3, 14)
                .WithArguments("MyClass");
        var sut = CreateSut(code, [expected]);
        await sut.RunAsync();
    }

    private VerifyCS.Test CreateSut(string code, List<DiagnosticResult> expected)
    {
        var test = new VerifyCS.Test()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestState =
            {
                Sources = { code },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(RequireUsingAttribute).Assembly.Location),
                },
            }
        };

        test.ExpectedDiagnostics.AddRange(expected);

        return test;
    }
}