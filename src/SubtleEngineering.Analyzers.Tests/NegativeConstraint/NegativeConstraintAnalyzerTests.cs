using SubtleEngineering.Analyzers.NegativeConstraint;
using SubtleEngineering.Analyzers.RequireUsing;

namespace SubtleEngineering.Analyzers.Tests.NegativeConstraint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using SubtleEngineering.Analyzers.Decorators;
using VerifyCS = CSharpAnalyzerVerifier<NegativeConstraintAnalyzer>;

public class NegativeConstraintAnalyzerTests
{
    [Fact]
    public async Task TestBasicNegativeConstraint()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                
                public static string Get<[NegativeTypeConstraint(typeof(int))] T>(T t) => t.ToString();

                public static void DoIt()
                {
                    var x = new TClass<int>();
                    Get(8);
                }

                public class TClass<[NegativeTypeConstraint(typeof(int))] T>
                {
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                NegativeConstraintAnalyzer.Rules.Find(DiagnosticIds.NegativeConstraintUsed))
                    .WithLocation(10, 21)
                    .WithArguments("T", "TClass"),
            ];
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
                    MetadataReference.CreateFromFile(typeof(RequireUsingAttribute).Assembly.Location),
                    //MetadataReference.CreateFromFile(typeof(IAsyncDisposable).Assembly.Location),
                },
            }
        };

        test.ExpectedDiagnostics.AddRange(expected);

        return test;
    }
}
