using SubtleEngineering.Analyzers.RelativeImport;

namespace SubtleEngineering.Analyzers.Tests.RelativeImport;

using VerifyAn = CSharpAnalyzerVerifier<RelativeImportAnalyzer>;
using SubtleEngineering.Analyzers.Decorators;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;

public class RelativeImportAnalyzerTests
{
    [Fact]
    public async Task BadRelativeNamespace()
    {
        const string code = """
            namespace A.B.C
            {
            }

            namespace A
            {
                using B.C;
            }
            """;

        var expected =
            VerifyAn.Diagnostic(
                RelativeImportAnalyzer.Rules.Find(DiagnosticIds.RelativeImport.DoNotUseRelativeImportUsingStatements))
                    .WithLocation(7, 5)
                    .WithArguments("B.C", "A.B.C");
        var sut = CreateSut(code, [expected]);
        await sut.RunAsync();

    }

    [Fact]
    public async Task NoRelativeNamespace()
    {
        const string code = """
            namespace A.B.C
            {
            }

            namespace A
            {
                using A.B.C;
            }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();
    }

    private VerifyAn.Test CreateSut(string code, List<DiagnosticResult> expected)
    {
        var test = new VerifyAn.Test()
        {
            ReferenceAssemblies = TestHelpers.Net80,
            TestState =
            {
                Sources = { code },
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(RequireUsingAttribute).Assembly.Location),
                    //MetadataReference.CreateFromFile(typeof(IAsyncDisposable).Assembly.Location),
                },
            }
        };

        test.ExpectedDiagnostics.AddRange(expected);

        return test;
    }
}