using SubtleEngineering.Analyzers.RelativeImport;

namespace SubtleEngineering.Analyzers.Tests.RelativeImport;

using VerifyCf = CSharpCodeFixVerifier<RelativeImportAnalyzer, RelativeImportCodeFix>;
using SubtleEngineering.Analyzers.Decorators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

public class RelativeImportCodeFixTests
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

        const string fixedCode = """
            namespace A.B.C
            {
            }

            namespace A
            {
                using A.B.C;
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCf.Diagnostic(
                RelativeImportAnalyzer.Rules.Find(DiagnosticIds.RelativeImport.DoNotUseRelativeImportUsingStatements))
                    .WithLocation(7, 5)
                    .WithArguments("B.C", "A.B.C"),
            ];
        var sut = CreateSut(code, fixedCode, expected);
        await sut.RunAsync();

    }

    private VerifyCf.Test CreateSut(string code, string fixedCode, List<DiagnosticResult> expected)
    {
        var test = new VerifyCf.Test()
        {
            TestCode = code,
            FixedCode = fixedCode,
            ReferenceAssemblies = TestHelpers.Net80,
            TestState =
            {
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(RequireUsingAttribute).Assembly.Location),
                    //MetadataReference.CreateFromFile(typeof(IAsyncDisposable).Assembly.Location),
                },
            },

        };

        test.ExpectedDiagnostics.AddRange(expected);

        return test;
    }
}