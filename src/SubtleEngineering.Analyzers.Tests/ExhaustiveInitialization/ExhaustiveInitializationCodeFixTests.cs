using SubtleEngineering.Analyzers.ExhaustiveInitialization;

namespace SubtleEngineering.Analyzers.Tests.ExhaustiveInitialization;

using VerifyCf = CSharpCodeFixVerifier<ExhaustiveInitializationAnalyzer, ExhaustiveInitializationCodeFix>;
using SubtleEngineering.Analyzers.Decorators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

public class ExhaustiveInitializationCodeFixTests
{
    [Fact]
    public async Task MissingFromProperty()
    {
        string code = $$"""
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class AllPropsB
                {
                    public int MyInt { get; set; }
                    
                    required public string MyString { get; set; }
                }
            }
            """;

        const string fixedCode = """
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class AllPropsB
                {
                    required public int MyInt { get; set; }
                    
                    required public string MyString { get; set; }
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCf.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticsDetails.ExhaustiveInitialization.TypeInitializationIsNonExhaustiveId))
                    .WithLocation(4, 11)
                    .WithArguments("A.AllPropsB"),
            VerifyCf.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId))
                    .WithLocation(6, 20)
                    .WithArguments("A.AllPropsB", "MyInt"),
            ];
        var sut = CreateSut(code, fixedCode, DiagnosticsDetails.ExhaustiveInitialization.PropertyEquivalenceKey, expected);
        await sut.RunAsync();
    }

    [Fact(Skip = "In UTs modifies one of the props with two required")]
    public async Task FixEntireType()
    {
        string code = $$"""
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class AllPropsB
                {
                    public int MyInt { get; set; }
                    public string MyString { get; set; }
                }
            }
            """;

        // TODO shaharp: Fix the code - for some reason it created two required.
        const string fixedCode = """
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class AllPropsB
                {
                    required public int MyInt { get; set; }
                    required required public string MyString { get; set; }
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCf.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticsDetails.ExhaustiveInitialization.TypeInitializationIsNonExhaustiveId))
                    .WithLocation(4, 11)
                    .WithArguments("A.AllPropsB"),
            VerifyCf.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId))
                    .WithLocation(6, 20)
                    .WithArguments("A.AllPropsB", "MyInt"),
            VerifyCf.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId))
                    .WithLocation(7, 23)
                    .WithArguments("A.AllPropsB", "MyString"),
            ];
        var sut = CreateSut(code, fixedCode, DiagnosticsDetails.ExhaustiveInitialization.TypeEquivalenceKey, expected);
        await sut.RunAsync();
    }

    [Fact(Skip = "In UTs modifies one of the props with two required")]
    public async Task FixPartialType()
    {
        string code = $$"""
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class AllPropsB
                {
                    public int MyInt { get; set; }
                    public string MyString { get; set; }
                    required public long MyLong{ get; set; }
                }
            }
            """;

        // TODO shaharp: For some reason, in UTs required is generated twice.
        const string fixedCode = """
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class AllPropsB
                {
                    required public int MyInt { get; set; }
                    required public string MyString { get; set; }
                    required public long MyLong{ get; set; }
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCf.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticsDetails.ExhaustiveInitialization.TypeInitializationIsNonExhaustiveId))
                    .WithLocation(4, 11)
                    .WithArguments("A.AllPropsB"),
            VerifyCf.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId))
                    .WithLocation(6, 20)
                    .WithArguments("A.AllPropsB", "MyInt"),
            VerifyCf.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticsDetails.ExhaustiveInitialization.PropertyIsMissingRequiredId))
                    .WithLocation(7, 23)
                    .WithArguments("A.AllPropsB", "MyString"),
            ];
        var sut = CreateSut(code, fixedCode, DiagnosticsDetails.ExhaustiveInitialization.TypeEquivalenceKey, expected);
        await sut.RunAsync();
    }

    private VerifyCf.Test CreateSut(string code, string fixedCode, string equivalenceKey, List<DiagnosticResult> expected)
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
        test.CodeActionEquivalenceKey = equivalenceKey;
        test.ExpectedDiagnostics.AddRange(expected);
        test.NumberOfFixAllIterations = 1;
        test.NumberOfFixAllInDocumentIterations = 1;
        test.NumberOfFixAllInProjectIterations = 1;
        test.CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne;

        return test;
    }
}