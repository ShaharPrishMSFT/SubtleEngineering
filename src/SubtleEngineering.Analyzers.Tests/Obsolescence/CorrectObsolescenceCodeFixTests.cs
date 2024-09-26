namespace SubtleEngineering.Analyzers.Tests.Obsolescence;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using SubtleEngineering.Analyzers.Obsolescence;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System;
using VerifyCf = CSharpCodeFixVerifier<Analyzers.Obsolescence.CorrectObsolescenceAnalyzer, Analyzers.Obsolescence.CorrectObsolescenceCodeFix>;

public class CorrectObsolescenceCodeFixTests
{
    [Fact]
    public async Task ObsoleteMethodWithoutEditorBrowsable_ShouldAddAttribute()
    {
        const string code = """
            using System;
            
            class TestClass
            {
                [Obsolete]
                public void {|#0:ObsoleteMethod|}()
                {
                }
            }
            """;

        const string fixedCode = """
            using System;
            using System.ComponentModel;

            class TestClass
            {
                [Obsolete]
                [EditorBrowsable(EditorBrowsableState.Never)]
                public void ObsoleteMethod()
                {
                }
            }
            """
        ;

        var expectedDiagnostics = new List<DiagnosticResult>
        {
            VerifyCf.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
                .WithLocation(0)
                .WithArguments("ObsoleteMethod")
        };

        var sut = CreateSut(code, fixedCode, expectedDiagnostics);

        sut.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllInDocumentCheck; // Ensures only the specific fix is applied
        sut.TestBehaviors |= TestBehaviors.SkipSuppressionCheck; // If suppression is involved

        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteClassWithoutEditorBrowsable_ShouldAddAttribute()
    {
        const string code = """
            using System;
            
            [Obsolete]
            public class {|#0:ObsoleteClass|}
            {
            }
            """;

        const string fixedCode = """
            using System;
            using System.ComponentModel;
            
            [Obsolete]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public class ObsoleteClass
            {
            }
            """;

        var expectedDiagnostics = new List<DiagnosticResult>
        {
            VerifyCf.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
                .WithLocation(0)
                .WithArguments("ObsoleteClass")
        };

        var sut = CreateSut(code, fixedCode, expectedDiagnostics);
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoletePropertyWithoutEditorBrowsable_ShouldAddAttribute()
    {
        const string code = """
            using System;
            
            class TestClass
            {
                [Obsolete]
                public int {|#0:ObsoleteProperty|} { get; set; }
            }
            """;

        const string fixedCode = """
            using System;
            using System.ComponentModel;

            class TestClass
            {
                [Obsolete]
                [EditorBrowsable(EditorBrowsableState.Never)]
                public int ObsoleteProperty { get; set; }
            }
            """;

        var expectedDiagnostics = new List<DiagnosticResult>
        {
            VerifyCf.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
                .WithLocation(0)
                .WithArguments("ObsoleteProperty")
        };

        var sut = CreateSut(code, fixedCode, expectedDiagnostics);
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteEventWithoutEditorBrowsable_ShouldAddAttribute()
    {
        const string code = """
            using System;
            
            class TestClass
            {
                [Obsolete]
                public event EventHandler {|#0:ObsoleteEvent|};
            }
            """;

        const string fixedCode = """
            using System;
            using System.ComponentModel;

            class TestClass
            {
                [Obsolete]
                [EditorBrowsable(EditorBrowsableState.Never)]
                public event EventHandler ObsoleteEvent;
            }
            """;

        var expectedDiagnostics = new List<DiagnosticResult>
        {
            VerifyCf.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
                .WithLocation(0)
                .WithArguments("ObsoleteEvent")
        };

        var sut = CreateSut(code, fixedCode, expectedDiagnostics);
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteFieldWithoutEditorBrowsable_ShouldAddAttribute()
    {
        const string code = """
            using System;
            using System.ComponentModel;
            
            class TestClass
            {
                [Obsolete]
                public int {|#0:ObsoleteField|};
            }
            """;

        const string fixedCode = """
            using System;
            using System.ComponentModel;

            class TestClass
            {
                [Obsolete]
                [EditorBrowsable(EditorBrowsableState.Never)]
                public int ObsoleteField;
            }
            """;

        var expectedDiagnostics = new List<DiagnosticResult>
        {
            VerifyCf.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
                .WithLocation(0)
                .WithArguments("ObsoleteField")
        };

        var sut = CreateSut(code, fixedCode, expectedDiagnostics);
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteDelegateWithoutEditorBrowsable_ShouldAddAttribute()
    {
        const string code = """
            using System;
            using System.ComponentModel;
            
            [Obsolete]
            public delegate void {|#0:ObsoleteDelegate|}();
            """;

        const string fixedCode = """
            using System;
            using System.ComponentModel;

            [Obsolete]
            [EditorBrowsable(EditorBrowsableState.Never)]
            public delegate void ObsoleteDelegate();
            """;

        var expectedDiagnostics = new List<DiagnosticResult>
        {
            VerifyCf.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
                .WithLocation(0)
                .WithArguments("ObsoleteDelegate")
        };

        var sut = CreateSut(code, fixedCode, expectedDiagnostics);
        await sut.RunAsync();
    }

    private VerifyCf.Test CreateSut(string code, string fixedCode, List<DiagnosticResult> expected)
    {
        var test = new VerifyCf.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState =
            {
                AdditionalReferences =
                {
                    //MetadataReference.CreateFromFile(typeof(ObsoleteAttribute).Assembly.Location),
                    //MetadataReference.CreateFromFile(typeof(System.ComponentModel.EditorBrowsableAttribute).Assembly.Location),
                },
            },
        };

        test.ExpectedDiagnostics.AddRange(expected);

        return test;
    }
}
