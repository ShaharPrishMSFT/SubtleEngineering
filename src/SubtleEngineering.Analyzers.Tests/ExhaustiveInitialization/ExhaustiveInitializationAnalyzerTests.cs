﻿
namespace SubtleEngineering.Analyzers.Tests.ExhaustiveInitialization;

using SubtleEngineering.Analyzers.ExhaustiveInitialization;
using SubtleEngineering.Analyzers.Tests;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using SubtleEngineering.Analyzers.Decorators;
using VerifyAn = CSharpAnalyzerVerifier<Analyzers.ExhaustiveInitialization.ExhaustiveInitializationAnalyzer>;

public class ExhaustiveInitializationAnalyzerTests
{
    [Fact]
    public async Task LegalElementsWork()
    {
        const string code = """
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class Class_SingleRequired
                {
                    required public int MyInt { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class Class_WithJustGetter
                {
                    public int MyInt { get; }
                }
            
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class Class_Empty
                {
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class Class_TwoRequiredOneRefType
                {
                    required public int MyInt { get; set; }

                    required public string MyString { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                struct Struct_TwoRequired
                {
                    required public int MyInt { get; set; }
            
                    required public string MyString { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                struct Struct_Empty
                {
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                record Record_WithProp
                {
                    required public string MyString { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                record Record_Empty
                {
                }
            
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                record Record_WithCtorAndProp(int MyInt)
                {
                    required public string MyString { get; set; }
                }
            
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class Class_WithCtorAndThisAssignment
                {
                    public Class_WithCtorAndThisAssignment(int myInt)
                    {
                        this.MyInt = myInt;
                    }
                        
                    required public string MyString { get; set; }

                    public int MyInt { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                class Class_WithCtorAndImplicitThisAssignment
                {
                    public Class_WithCtorAndImplicitThisAssignment(int myInt)
                    {
                        MyInt = myInt;
                    }
                        
                    required public string MyString { get; set; }
            
                    public int MyInt { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                struct Struct_WithCtorAndImplicitThisAssignment
                {
                    public Struct_WithCtorAndImplicitThisAssignment(int myInt)
                    {
                        MyInt = myInt;
                    }
                        
                    required public string MyString { get; set; }
            
                    public int MyInt { get; set; }
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                record Record_WithExhaustiveCtor(int MyInt, string MyString)
                {
                }

                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                record Record_WithExhaustiveCtorAndProp(int MyInt, string MyString)
                {
                    public string MyString { get; set; }
                }
            }
            """;

        var sut = CreateSut(code, []);
        await sut.RunAsync();   
    }

    [Theory]
    [InlineData("record")]
    [InlineData("record struct")]
    [InlineData("struct")]
    [InlineData("class")]
    public async Task SimpleTypeWithProperty(string type)
    {
        string code = $$"""
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                {{type}} AllPropsB
                {
                    public AllPropsB(int myInt)
                    {
                        var x = MyInt + myInt;
                    }

                    public int MyInt { get; set; }
                }
            }
            """;

        List<DiagnosticResult> expected =
        [
            VerifyAn.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticIds.ExhaustiveInitialization.TypeInitializationIsNonExhaustive))
                    .WithLocation(4, 14 + type.Length - "{{type}}".Length)
                    .WithArguments("A.AllPropsB"),
            VerifyAn.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticIds.ExhaustiveInitialization.PropertyIsMissingRequired))
                    .WithLocation(11, 20)
                    .WithArguments("A.AllPropsB", "MyInt"),
        ];

        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Theory]
    [InlineData("record")]
    [InlineData("record struct")]
    public async Task RecordFailsWithTwoRequired(string type)
    {
        string code = $$"""
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                {{type}} AllPropsB(string MyString)
                {
                    public int MyInt { get; set; }
                    public string MyString { get; set; }
                }
            }
            """;

        List<DiagnosticResult> expected =
        [
            VerifyAn.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticIds.ExhaustiveInitialization.TypeInitializationIsNonExhaustive))
                    .WithLocation(4, 14 + type.Length - "{{type}}".Length)
                    .WithArguments("A.AllPropsB"),
            VerifyAn.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticIds.ExhaustiveInitialization.PropertyIsMissingRequired))
                    .WithLocation(6, 20)
                    .WithArguments("A.AllPropsB", "MyInt"),
        ];

        var sut = CreateSut(code, expected);
        await sut.RunAsync();
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public async Task ClassicPrimaryConstructorUsage(string type)
    {
        string code = $$"""
            namespace A
            {
                [SubtleEngineering.Analyzers.Decorators.ExhaustiveInitialization]
                {{type}} AllPropsB(string s)
                {
                    public int MyInt { get; set; }
                    public string MyString { get; set; } = s;
                }
            }
            """;

        List<DiagnosticResult> expected =
        [
            VerifyAn.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticIds.ExhaustiveInitialization.TypeInitializationIsNonExhaustive))
                    .WithLocation(4, 14 + type.Length - "{{type}}".Length)
                    .WithArguments("A.AllPropsB"),
            VerifyAn.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticIds.ExhaustiveInitialization.PropertyIsMissingRequired))
                    .WithLocation(6, 20)
                    .WithArguments("A.AllPropsB", "MyInt"),
            VerifyAn.Diagnostic(
                ExhaustiveInitializationAnalyzer.Rules.Find(DiagnosticIds.ExhaustiveInitialization.PropertyIsMissingRequired))
                    .WithLocation(7, 23)
                    .WithArguments("A.AllPropsB", "MyString"),
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
