namespace SubtleEngineering.Analyzers.Tests
{
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Testing;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Testing;

    public static partial class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId)!.CompilationOptions;
                    compilationOptions = compilationOptions!
                        .WithSpecificDiagnosticOptions(
                            compilationOptions
                                .SpecificDiagnosticOptions
                                .SetItems(CSharpVerifierHelper.NullableWarnings));

                    solution = solution
                        .WithProjectCompilationOptions(projectId, compilationOptions)
                        .WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp12));

                    return solution;
                });
            }
        }
    }
}
