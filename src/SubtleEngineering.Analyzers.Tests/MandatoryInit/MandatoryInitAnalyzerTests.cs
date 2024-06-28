using SubtleEngineering.Analyzers.MandatoryInit;

namespace SubtleEngineering.Analyzers.Tests.MandatoryInit;

using VerifyAn = CSharpAnalyzerVerifier<MandatoryInitAnalyzer>;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using SubtleEngineering.Analyzers.Decorators;

public class MandatoryInitAnalyzerTests
{
    [Fact]
    public async Task SimpleClassWithProperty()
    {
        const string code = """
            namespace A
            {
                class B
                {
                    public int MyInt { get; set; } = 1;
                }
            }
            """;

        var expected =
            VerifyAn.Diagnostic(
                MandatoryInitAnalyzer.Rules.Find(DiagnosticIds.MandatoryInit.TypeHasDefaultInitializers))
                    .WithLocation(7, 5)
                    .WithArguments("B.B", "MyInt");
        var sut = CreateSut(code, [expected]);
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
