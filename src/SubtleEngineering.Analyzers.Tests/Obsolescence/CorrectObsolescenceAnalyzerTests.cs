using SubtleEngineering.Analyzers.Obsolescence;

namespace SubtleEngineering.Analyzers.Tests.Obsolescence;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using SubtleEngineering.Analyzers.Obsolescence;
using Xunit;
using VerifyAn = CSharpAnalyzerVerifier<CorrectObsolescenceAnalyzer>;

public class CorrectObsolescenceAnalyzerTests
{
    [Fact]
    public async Task ObsoleteMethodWithoutEditorBrowsable_ShouldReportDiagnostic()
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

        var expected = VerifyAn.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
            .WithLocation(0)
            .WithArguments("ObsoleteMethod");

        var sut = CreateSut(code, new List<DiagnosticResult> { expected });
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteMethodWithEditorBrowsable_ShouldNotReportDiagnostic()
    {
        const string code = """
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
        """;

        var sut = CreateSut(code, new List<DiagnosticResult>());
        await sut.RunAsync();
    }

    [Fact]
    public async Task NonObsoleteMethod_ShouldNotReportDiagnostic()
    {
        const string code = """
        class TestClass
        {
            public void NormalMethod()
            {
            }
        }
        """;

        var sut = CreateSut(code, new List<DiagnosticResult>());
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteClassWithoutEditorBrowsable_ShouldReportDiagnostic()
    {
        const string code = """
        using System;

        [Obsolete]
        public class {|#0:ObsoleteClass|}
        {
        }
        """;

        var expected = VerifyAn.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
            .WithLocation(0)
            .WithArguments("ObsoleteClass");

        var sut = CreateSut(code, new List<DiagnosticResult> { expected });
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteClassWithEditorBrowsable_ShouldNotReportDiagnostic()
    {
        const string code = """
        using System;
        using System.ComponentModel;

        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class ObsoleteClass
        {
        }
        """;

        var sut = CreateSut(code, new List<DiagnosticResult>());
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteClassWithEditorBrowsableBeforeObs_ShouldNotReportDiagnostic()
    {
        const string code = """
        using System;
        using System.ComponentModel;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public class ObsoleteClass
        {
        }
        """;

        var sut = CreateSut(code, new List<DiagnosticResult>());
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteClassWithEditorSameLine_ShouldNotReportDiagnostic()
    {
        const string code = """
        using System;
        using System.ComponentModel;

        [Obsolete][EditorBrowsable(EditorBrowsableState.Never)]
        public class ObsoleteClass
        {
        }
        """;

        var sut = CreateSut(code, new List<DiagnosticResult>());
        await sut.RunAsync();
    }


    [Fact]
    public async Task ObsoletePropertyWithoutEditorBrowsable_ShouldReportDiagnostic()
    {
        const string code = """
        using System;

        class TestClass
        {
            [Obsolete]
            public int {|#0:ObsoleteProperty|} { get; set; }
        }
        """;

        var expected = VerifyAn.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
            .WithLocation(0)
            .WithArguments("ObsoleteProperty");

        var sut = CreateSut(code, new List<DiagnosticResult> { expected });
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteEventWithoutEditorBrowsable_ShouldReportDiagnostic()
    {
        const string code = """
        using System;

        class TestClass
        {
            [Obsolete]
            public event EventHandler {|#0:ObsoleteEvent|};
        }
        """;

        var expected = VerifyAn.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
            .WithLocation(0)
            .WithArguments("ObsoleteEvent");

        var sut = CreateSut(code, new List<DiagnosticResult> { expected });
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteFieldWithoutEditorBrowsable_ShouldReportDiagnostic()
    {
        const string code = """
        using System;

        class TestClass
        {
            [Obsolete]
            public int {|#0:ObsoleteField|};
        }
        """;

        var expected = VerifyAn.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
            .WithLocation(0)
            .WithArguments("ObsoleteField");

        var sut = CreateSut(code, new List<DiagnosticResult> { expected });
        await sut.RunAsync();
    }

    [Fact]
    public async Task ObsoleteDelegateWithoutEditorBrowsable_ShouldReportDiagnostic()
    {
        const string code = """
        using System;

        [Obsolete]
        public delegate void {|#0:ObsoleteDelegate|}();
        """;

        var expected = VerifyAn.Diagnostic(CorrectObsolescenceAnalyzer.Rules[0])
            .WithLocation(0)
            .WithArguments("ObsoleteDelegate");

        var sut = CreateSut(code, new List<DiagnosticResult> { expected });
        await sut.RunAsync();
    }

    private VerifyAn.Test CreateSut(string code, List<DiagnosticResult> expected)
    {
        var test = new VerifyAn.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState =
        {
            Sources = { code },
            AdditionalReferences =
            {
                //MetadataReference.CreateFromFile(typeof(ObsoleteAttribute).Assembly.Location),
                //MetadataReference.CreateFromFile(typeof(System.ComponentModel.EditorBrowsableAttribute).Assembly.Location),
            },
        }
        };

        test.ExpectedDiagnostics.AddRange(expected);

        return test;
    }
}