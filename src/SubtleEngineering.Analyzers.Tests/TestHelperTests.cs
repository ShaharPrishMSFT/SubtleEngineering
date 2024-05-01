namespace SubtleEngineering.Analyzers.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TestHelperTests
{
    private const string Br = "\r\n";

    [Theory(Skip = "Not used yet")]
    [InlineData($"a[|b|]", new string[] { "1,2" })]
    [InlineData($"var x = 1; // [|var y = 2;|]", new string[] { "1,11" })]
    [InlineData($"var x = 1; // [|var y = 2;|]{Br}[|var x = 1|]; // var y = 2;", new string[] { "1,11", "2,25" })]
    [InlineData($"[|var x = 1|]; // [|var y = 2;|]{Br}[|var x = 1|]; // var y = 2;", new string[] { "1,1", "1,10", "2,24" })]
    [InlineData("", new string[] {  })]
    [InlineData($"var x = 1; // var y = 2;", new string[] {  })]
    public void LocationParsingWorks(string code, string[] expectedStrings)
    {
        var parsed = TestHelpers.ParseCodeLocations(code);
        var actual = parsed.Positions.Select(p => (p.Position.Line, p.Position.Character)).ToList();
        var expected = expectedStrings.Select(s => s.Split(",").Select(s => s.Trim()).ToArray()).Select(s => (int.Parse(s[0]), int.Parse(s[1]))).ToList();
        Assert.Equal(expected, actual);

        string expectedCode = code.Replace("[|", "").Replace("|]", "");
        Assert.Equal(expectedCode, parsed.ActualCode);
    }
}
