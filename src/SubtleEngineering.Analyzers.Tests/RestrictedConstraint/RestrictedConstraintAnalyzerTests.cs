
namespace SubtleEngineering.Analyzers.Tests.RestrictedConstraint;
using System.Collections.Generic;
using SubtleEngineering.Analyzers.RestrictedConstraint;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using SubtleEngineering.Analyzers.Decorators;
using VerifyCS = CSharpAnalyzerVerifier<Analyzers.RestrictedConstraint.RestrictedConstraintAnalyzer>;

public class RestrictedConstraintAnalyzerTests
{
    [Fact]
    public async Task TestRestrictedClassInstantiationConstraint()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public static void DoIt()
                {
                    var x = new TClass<int>();
                }

                public class TClass<[RestrictedType(typeof(int))] T>
                {
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(7, 9)
                    .WithArguments("T", "TClass"),
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(7, 17)
                    .WithArguments("T", "TClass"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestGenericGetThatDoesNotIncludeType()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public static string Get<[RestrictedType(typeof(int))] T>(T t) => t.ToString();

                public static void DoIt()
                {
                    Get(8);
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(9, 9)
                    .WithArguments("T", "Get"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestGenericMethodThatHasType()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public static string Get<[RestrictedType(typeof(int))] T>(T t) => t.ToString();

                public static void DoIt()
                {
                    Get<int>(8);
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(9, 9)
                    .WithArguments("T", "Get"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task PassGenericDelegate()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public static void DoIt()
                {
                    TakeDelegate(SomeIntMethod);
                }

                public static void SomeIntMethod(int i)
                {
                }

                public static void TakeDelegate(MyDelegate<int> d) {}
            
                public delegate void MyDelegate<[RestrictedType(typeof(int))] T>(T t);
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(7, 22)
                    .WithArguments("T", "MyDelegate"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestConversionToType()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public static void DoIt()
                {
                    var d = (IDerived<int>)null;
                    var a = (IBase<int>)d;
                }

                public interface IBase<[RestrictedType(typeof(int))] out T>;
                
                public interface IDerived<out T> : IBase<T>;
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(8, 17)
                    .WithArguments("T", "IBase"),
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(8, 9)
                    .WithArguments("T", "IBase"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestDeclarationOfVariable()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public static void DoIt()
                {
                    IBase<int> b;
                }
                public interface IBase<[RestrictedType(typeof(int))] out T>;
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(7, 9)
                    .WithArguments("T", "IBase"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestFieldThatUsesRestrictedConstraints()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public TClass<int> _field;
            }

            public class TClass<[RestrictedType(typeof(int))] T>
            {
            }
            """;

        List<DiagnosticResult> expected = [

            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(5, 24)
                    .WithArguments("T", "_field"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestPropertyWithRestrictedConstraint()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public TClass<int> Property { get; set; }

                public class TClass<[RestrictedType(typeof(int))] T>
                {
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(5, 24)
                    .WithArguments("T", "Property"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestMultipleConstraints()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;

            public class MyClass
            {
                public TClass<int> Property1 { get; set; }

                public TClass<int> Property2 { get; set; }
            
                public class TClass<[RestrictedType(typeof(int)), RestrictedType(typeof(string))] T>
                {
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(5, 24)
                    .WithArguments("T", "Property1"),
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(7, 24)
                    .WithArguments("T", "Property2"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Theory]
    [InlineData("Interface1", "Interface1", true, true)]
    [InlineData("Interface1", "Interface1", false, true)]
    [InlineData("Interface1", "Interface2", true, true)]
    [InlineData("Interface1", "Interface2", false, false)]
    [InlineData("Interface1", "Class1", true, true)]
    [InlineData("Interface1", "Class1", false, false)]
    [InlineData("Interface1", "Class2", true, true)]
    [InlineData("Interface1", "Class2", false, false)]
    public async Task TestDerivedDisallowed(string disallowType, string askedType, bool inherit, bool error)
    {
        var code = $$"""
            using SubtleEngineering.Analyzers.Decorators;

            public interface Interface1;

            public interface Interface2 : Interface1;

            public class Class1 : Interface1 ;

            public class Class2 : Class1, Interface2;

            public class MyClass
            {
                
                public static string Get<[RestrictedType(typeof({{disallowType}}), {{(inherit ? "true" : "false")}})] T>(T t) => t.ToString();

                public static void DoIt()
                {
                    Get<{{askedType}}>(null);
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(18, 9)
                    .WithArguments("T", "Get"),
            ];
        var sut = CreateSut(code, error ? expected : []);
        await sut.RunAsync();
    }

    [Theory]
    [InlineData("Interface1<int>", "Interface1<int>", false, true)]
    [InlineData("Interface1<>", "Interface1<int>", false, true)]
    [InlineData("Interface1<>", "Class1<int>", false, false)]
    [InlineData("Interface1<>", "Class1<int>", true, true)]
    public async Task TestGenericTypeRestrictions(string disallowType, string askedType, bool inherit, bool error)
    {
        var code = $$"""
            using SubtleEngineering.Analyzers.Decorators;

            public interface Interface1<T>;

            public class Class1<T> : Interface1<T>;

            public class MyClass
            {
                public static string Get<[RestrictedType(typeof({{disallowType}}), {{(inherit ? "true" : "false")}})] T>(T t) => t.ToString();

                public static void DoIt()
                {
                    Get<{{askedType}}>(null);
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RestrictedConstraintAnalyzer.Rules.Find(DiagnosticIds.RestrictedConstraint.Used))
                    .WithLocation(13, 9)
                    .WithArguments("T", "Get"),
            ];
        var sut = CreateSut(code, error ? expected : []);
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
