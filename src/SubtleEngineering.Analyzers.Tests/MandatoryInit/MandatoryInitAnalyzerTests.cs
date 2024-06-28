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
    public async Task LegalElementsWork()
    {
        const string code = """
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                class Class_SingleRequired
                {
                    required public int MyInt { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                class Class_WithJustGetter
                {
                    public int MyInt { get; }
                }
            
                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                class Class_Empty
                {
                }

                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                class Class_TwoRequiredOneRefType
                {
                    required public int MyInt { get; set; }

                    required public string MyString { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                struct Struct_TwoRequired
                {
                    required public int MyInt { get; set; }
            
                    required public string MyString { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                struct Struct_Empty
                {
                }

                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                record Record_WithProp
                {
                    required public string MyString { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                record Record_Empty
                {
                }
            
                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                record Record_WithCtorAndProp(int MyInt)
                {
                    required public string MyString { get; set; }
                }
            
                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                class Class_WithCtorAndThisAssignment
                {
                    public Class_WithCtorAndThisAssignment(int myInt)
                    {
                        this.MyInt = myInt;
                    }
                        
                    required public string MyString { get; set; }

                    public int MyInt { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                class Class_WithCtorAndImplicitThisAssignment
                {
                    public Class_WithCtorAndImplicitThisAssignment(int myInt)
                    {
                        MyInt = myInt;
                    }
                        
                    required public string MyString { get; set; }
            
                    public int MyInt { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                struct Struct_WithCtorAndImplicitThisAssignment
                {
                    public Struct_WithCtorAndImplicitThisAssignment(int myInt)
                    {
                        MyInt = myInt;
                    }
                        
                    required public string MyString { get; set; }
            
                    public int MyInt { get; set; }
                }
            }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();   
    }
    [Fact]
    public async Task SimpleClassWithProperty()
    {
        const string code = """
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                class AllPropsB
                {
                    public AllPropsB(int myInt)
                    {
                        // Do nothing
                    }

                    public int MyInt { get; set; }
                }
            }
            """;

        var expected =
            VerifyAn.Diagnostic(
                MandatoryInitAnalyzer.Rules.Find(DiagnosticIds.MandatoryInit.PropertyIsMissingRequired))
                    .WithLocation(11, 20)
                    .WithArguments("A.AllPropsB", "MyInt");
        var sut = CreateSut(code, [expected]);
        await sut.RunAsync();
    }

    [Fact]
    public async Task RecordFailsWithTwoRequired()
    {
        const string code = """
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.MandatoryInit]
                record AllPropsB(string MyString)
                {
                    public int MyInt { get; set; }
                    public string MyString { get; set; }
                }
            }
            """;

        List<DiagnosticResult> expected =
        [
            VerifyAn.Diagnostic(
                MandatoryInitAnalyzer.Rules.Find(DiagnosticIds.MandatoryInit.PropertyIsMissingRequired))
                    .WithLocation(4, 11)
                    .WithArguments("A.AllPropsB", "MyInt"),
            VerifyAn.Diagnostic(
                MandatoryInitAnalyzer.Rules.Find(DiagnosticIds.MandatoryInit.PropertyIsMissingRequired))
                    .WithLocation(5, 15)
                    .WithArguments("A.AllPropsB", "MyInt"),
        ];

        var sut = CreateSut(code, expected);
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
