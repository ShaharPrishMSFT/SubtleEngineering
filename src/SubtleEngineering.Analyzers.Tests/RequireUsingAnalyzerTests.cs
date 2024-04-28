namespace SubtleEngineering.Analyzers.Tests;

using RoslynTestKit;
using SubtleEngineering.Analyzers.Decorators;

public class RequireUsingAnalyzerTests
{
    [Fact]
    public void TestMisuseOfRequireAttribute()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            [RequireUsing]
            public class [|MyClass|]
            {
            }
            """;
        var sut = CreateSut();

        sut.HasDiagnostic(code, DiagnosticIds.TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable);
    }

    public AnalyzerTestFixture CreateSut()
    {
        var fixture = RoslynFixtureFactory.Create<RequireUsingAnalyzer>(
            new AnalyzerTestFixtureConfig()
            {
                References = [
                    ReferenceSource.FromType<RequireUsingAttribute>(),
                    // ReferenceSource.FromType<Attribute>(),
                    ],
            });

        return fixture;
    }
}