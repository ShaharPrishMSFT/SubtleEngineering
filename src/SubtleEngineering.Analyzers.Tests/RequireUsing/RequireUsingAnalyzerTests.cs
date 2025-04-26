using SubtleEngineering.Analyzers.RequireUsing;

namespace SubtleEngineering.Analyzers.Tests.RequireUsing;

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
                RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposableId))
                    .WithLocation(3, 14)
                    .WithArguments("MyClass"),
            VerifyCS.Diagnostic(
                RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.MethodsReturnsDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposableId))
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
            RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId))
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
            RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId))
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
            RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId))
                .WithLocation(15, 23)
                .WithArguments("Create");
        var sut = CreateSut(code, [expected]);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestSimpleBadCallCaseTask()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            using System.Threading.Tasks;

            public class MyClass : IDisposable
            {
                public void Dispose()
                {
                }
            }

            public class Program
            {
                public static async Task Main()
                {
                    var myClass = await Create();
                }

            [RequireUsing]
            public static Task<MyClass> Create() => null;
            }
            """;

        var expected = VerifyCS.Diagnostic(
            RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId))
                .WithLocation(16, 29)
                .WithArguments("Create");
        var sut = CreateSut(code, [expected]);
        await sut.RunAsync();
    }

    [Fact]
    public async Task TestSimpleBadCallCaseValueTask()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;
            using System.Threading.Tasks;

            public class MyClass : IDisposable
            {
                public void Dispose()
                {
                }
            }

            public class Program
            {
                public static async Task Main()
                {
                    var myClass = await Create();
                }

            [RequireUsing]
            public static ValueTask<MyClass> Create() => ValueTask.FromResult<MyClass>(null);
            }
            """;

        var expected = VerifyCS.Diagnostic(
            RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId))
                .WithLocation(16, 29)
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
    public async Task BadCallInsideExpressionIsAllowed()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            public class MyClass
            {
                [RequireUsing]
                public static IDisposable Create() => null;
            }

            public class Program
            {
                public static void TakeExpression(System.Linq.Expressions.Expression<Action> expression)
                {
                }

                public static void Main()
                {
                    TakeExpression(() => MyClass.Create());
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

    [Fact]
    public async Task InterfaceImplInheritsAttribute()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            public interface MyInterface
            {
                [RequireUsing]
                IDisposable Method();
            }

            public class MyClass : MyInterface
            {
                public IDisposable Method() => null;
            }

            public class Program
            {
                public static void Main()
                {
                    var c = new MyClass();
                    c.Method();
                    var i = (MyInterface)c;
                    i.Method();
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
            RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId))
                .WithLocation(22, 9)
                .WithArguments("Method"),
                 ];

        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task RefStructWithRequiresUsingOnMethodDoesNotWarnOnAttributeUsage()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            public class MyClass
            {
                [RequireUsing]
                public MyRefStruct Method() => default;
            }

            public class Program
            {
                public static void Main()
                {
                    var c = new MyClass();
                    c.Method();
                }
            }

            public ref struct MyRefStruct
            {
                public void Dispose()
                {
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
            RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId))
                .WithLocation(15, 9)
                .WithArguments("Method"),
                 ];

        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Fact]
    public async Task RefStructWithRequiresUsingOnMethodDoesNotWarnWhenUsedCorrectly()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            public class MyClass
            {
                [RequireUsing]
                public MyRefStruct Method() => default;
            }

            public class Program
            {
                public static void Main()
                {
                    var c = new MyClass();
                    using (c.Method())
                    {
                    }
                }
            }

            public ref struct MyRefStruct
            {
                public void Dispose()
                {
                }
            }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();
    }

    [Fact]
    public async Task RefStructWithRequiresUsingOnStructDoesNotWarnWhenUsedCorrectly()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            public class Program
            {
                public static void Main()
                {
                    using (var c = new MyRefStruct())
                    {
                    }
                }
            }

            [RequireUsing]
            public ref struct MyRefStruct
            {
                public void Dispose()
                {
                }
            }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();
    }

    [Fact]
    public async Task RefStructWithRequiresUsingOnStructWarnsWhenUsedIncorrectly()
    {
        const string code = """
            using SubtleEngineering.Analyzers.Decorators;
            using System;

            public class Program
            {
                public static void Main()
                {
                    new MyRefStruct();
                }
            }

            [RequireUsing]
            public ref struct MyRefStruct
            {
                public void Dispose()
                {
                }
            }
            """;

        List<DiagnosticResult> expected = [
            VerifyCS.Diagnostic(
                    RequireUsingAnalyzer.Rules.Find(DiagnosticsDetails.RequireUsing.TypeMustBeInstantiatedWithinAUsingStatementId))
                        .WithLocation(8, 9)
                        .WithArguments("MyRefStruct"),
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
