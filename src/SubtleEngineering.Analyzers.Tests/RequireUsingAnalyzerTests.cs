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
                [RequireUsing]
                public static int Create() => 0;
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                RequireUsingAnalyzer.Rules.Find(DiagnosticIds.TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable))
                    .WithLocation(3, 14)
                    .WithArguments("MyClass"),
            VerifyCS.Diagnostic(
                RequireUsingAnalyzer.Rules.Find(DiagnosticIds.MethodsReturnsDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable))
                    .WithLocation(6, 19)
                   .WithArguments("Create"),
            ];
        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestCorrectUsageOfRequireAttribute()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            using System.Threading.Tasks;

            [RequireUsing]
            public class MyClass : IDisposable
            {
                public void Dispose() { }

                [RequireUsing]
                public static MyClass Create() => null;

                [RequireUsing]
                public static IDisposable CreateDisposable() => null;
            }

            [RequireUsing]
            public class MyClassAsync : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => ValueTask.CompletedTask;

                [RequireUsing]
                public static MyClassAsync Create() => null;
            
                [RequireUsing]
                public static IAsyncDisposable CreateDisposable() => null;
                        }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestSimpleBadCreationCase()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            [RequireUsing]
            public class MyClass : IDisposable
            {
                public void Dispose()
                {
                }
            }

            public class Program
            {
                public static void Main()
                {
                    var myClass = new MyClass();
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(
            RequireUsingAnalyzer.Rules.Find(DiagnosticIds.TypeMustBeInstantiatedWithinAUsingStatement))
                .WithLocation(16, 23)
                .WithArguments("MyClass");
        var sut = CreateSut(code, [expected]);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestSimpleBadCreationCaseAsync()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System.Threading.Tasks;
            using System;

            [RequireUsing]
            public class MyClass : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            }

            public class Program
            {
                public static void Main()
                {
                    var myClass = new MyClass();
                }
            }
            """;

        var expected = VerifyCS.Diagnostic(
            RequireUsingAnalyzer.Rules.Find(DiagnosticIds.TypeMustBeInstantiatedWithinAUsingStatement))
                .WithLocation(15, 23)
                .WithArguments("MyClass");
        var sut = CreateSut(code, [expected]);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestSimpleBadCallCase()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            public class MyClass : IDisposable
            {
                public void Dispose()
                {
                }
            }

            public class Program
            {
                public static void Main()
                {
                    var myClass = Create();
                }

            [RequireUsing]
            public static MyClass Create() => null;
            }
            """;

        var expected = VerifyCS.Diagnostic(
            RequireUsingAnalyzer.Rules.Find(DiagnosticIds.TypeMustBeInstantiatedWithinAUsingStatement))
                .WithLocation(15, 23)
                .WithArguments("Create");
        var sut = CreateSut(code, [expected]);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestSuppressionMethodOnCreation()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            [RequireUsing]
            public class MyClass : IDisposable
            {
                public void Dispose()
                {
                }
            }

            public class Program
            {
                public static void Main()
                {
                    var myClass = new MyClass().ExcludeFromUsing();
                }
            }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();
    }

    [Fact(Skip = "AsyncDisposable not working for some reason")]
    public async Task TestSuppressionMethodOnCreationAsync()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            using System.Threading.Tasks;

            [RequireUsing]
            public class MyClass : IAsyncDisposable
            {
                public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            }

            public class Program
            {
                public static void Main()
                {
                    var myClass = new MyClass().ExcludeFromUsingAsync();
                }
            }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestSuppressionMethodOnInvocation()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            public class MyClass : IDisposable
            {
                public void Dispose()
                {
                }
            }

            public class Program
            {
                public static void Main()
                {
                    var myClass = Create(3).ExcludeFromUsing();
                }

            [RequireUsing]
            public static MyClass Create(int i) => new MyClass();
            }
            """;

        var sut = CreateSut(code, []);
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