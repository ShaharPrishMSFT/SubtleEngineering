namespace SubtleEngineering.Analyzers.Tests;
using System.Collections.Generic;

internal record ParsedLocations(string ActualCode)
{
    public List<ParsedLocation> Positions { get; } = new();
}
