namespace SubtleEngineering.Analyzers.Tests;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using FluentAssertions;

public class TestHelperTests
{
    private const string Br = "\r\n";

    [Theory(Skip = "Not used yet")]
    [InlineData($"a[|b|]", new string[] { "1,2" })]
    [InlineData($"var x = 1; // [|var y = 2;|]", new string[] { "1,11" })]
    [InlineData($"var x = 1; // [|var y = 2;|]{Br}[|var x = 1|]; // var y = 2;", new string[] { "1,11", "2,25" })]
    [InlineData($"[|var x = 1|]; // [|var y = 2;|]{Br}[|var x = 1|]; // var y = 2;", new string[] { "1,1", "1,10", "2,24" })]
    [InlineData("", new string[] { })]
    [InlineData($"var x = 1; // var y = 2;", new string[] { })]
    public void LocationParsingWorks(string code, string[] expectedStrings)
    {
        var parsed = TestHelpers.ParseCodeLocations(code);
        var actual = parsed.Positions.Select(p => (p.Position.Line, p.Position.Character)).ToList();
        var expected = expectedStrings.Select(s => s.Split(",").Select(s => s.Trim()).ToArray()).Select(s => (int.Parse(s[0]), int.Parse(s[1]))).ToList();
        Assert.Equal(expected, actual);

        string expectedCode = code.Replace("[|", "").Replace("|]", "");
        Assert.Equal(expectedCode, parsed.ActualCode);
    }

    [Theory]
    [InlineData("System.IDisposable")]
    [InlineData("TestNamespace.MyBaseClass")]
    public void IsAssignableTo_ReturnsTrue_WhenTypeIsAssignable(string typeToCheck)
    {
        // Arrange
        var sourceCode = @"
            using System;
            namespace TestNamespace
            {
                public class MyBaseClass : IDisposable
                {
                    public void Dispose() { }
                }
                public class MyDerivedClass : MyBaseClass {}
            }";

        var compilation = CreateCompilation(sourceCode);
        var typeSymbol = GetTypeSymbol(compilation, "TestNamespace.MyDerivedClass");

        // Act
        typeSymbol.IsAssignableTo(typeToCheck).Should().BeTrue();
    }

    [Fact]
    public void IsAssignableTo_ReturnsFalse_WhenTypeIsNotAssignable()
    {
        // Arrange
        var sourceCode = @"
            namespace TestNamespace
            {
                public class MyBaseClass {}
                public class MyDerivedClass : MyBaseClass {}
            }";

        var compilation = CreateCompilation(sourceCode);
        var typeSymbol = GetTypeSymbol(compilation, "TestNamespace.MyDerivedClass");
        var typeToCheck = typeof(IDisposable);

        // Act
        var result = typeSymbol.IsAssignableTo(typeToCheck);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ClrTypeNameWorks()
    {
        var sourceCode = @"
            namespace TestNamespace
            {
                public class MyClass
                {
                    public MyClass(string s, int i, MyClass c)
                    {
                    }
                }
            }";

        var compilation = CreateCompilation(sourceCode);
        var x = (INamedTypeSymbol)GetTypeSymbol(compilation, "TestNamespace.MyClass");
        var ctor = (IMethodSymbol)x.Constructors[0];
        var parameters = ctor.Parameters;

        parameters[0].Type.ToDisplayString(Helpers.FullyQualifiedClrTypeName).Should().Be("System.String");
        parameters[1].Type.ToDisplayString(Helpers.FullyQualifiedClrTypeName).Should().Be("System.Int32");
        parameters[2].Type.ToDisplayString(Helpers.FullyQualifiedClrTypeName).Should().Be("TestNamespace.MyClass");
    }

    private static Compilation CreateCompilation(string sourceCode)
    {
        var c = CSharpCompilation.Create("TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(sourceCode) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        c.GetDiagnostics().Should().BeEmpty();

        return c;
    }

    private static ITypeSymbol GetTypeSymbol(Compilation compilation, string fullyQualifiedName)
        => compilation.GetTypeByMetadataName(fullyQualifiedName) ?? throw new InvalidOperationException();
}
