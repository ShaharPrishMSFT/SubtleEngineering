namespace SubtleEngineering.Analyzers.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

internal static class TestHelpers
{
    private const string OpeningMarker = "[|";
    private const string ClosingMarker = "|]";

    private static readonly Lazy<ReferenceAssemblies> _lazyNet80 =
        new Lazy<ReferenceAssemblies>(() =>
        {
            return new ReferenceAssemblies(
                "net8.0",
                new PackageIdentity(
                    "Microsoft.NETCore.App.Ref",
                    "8.0.0"),
                Path.Combine("ref", "net8.0"));
        });

    public static ReferenceAssemblies Net80 => _lazyNet80.Value;

    public static ParsedLocations ParseCodeLocations(string code)
    {
        var locations = new List<ParsedLocation>();
        var lines = code.Split(Environment.NewLine).ToList();
        var sb = new StringBuilder();

        for (int i = 0; i < lines.Count; i++)
        {
            var parts = lines[i].Split(OpeningMarker);
            if (parts.Length == 1)
            {
                continue;
            }

            sb.Clear();
            int column = 0;
            for (int partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                if (partIndex == 0)
                {
                    sb.Append(parts[partIndex]);
                    continue;
                }

                var part = parts[partIndex];
                int closingIndex = part.IndexOf(ClosingMarker);
                if (closingIndex > -1)
                {
                    column += part.Length - ClosingMarker.Length;
                    var closingParts = part[0..closingIndex];
                    sb.Append(closingParts);
                    locations.Add(new ParsedLocation(new LinePosition(i + 1, column + 1)));
                }
                else
                {
                    throw new InvalidOperationException("Unbalanced markers");
                }
            }

            lines[i] = sb.ToString();
        }

        var result = new ParsedLocations(string.Join(Environment.NewLine, lines));

        result.Positions.AddRange(locations);
        return result;
    }


}
