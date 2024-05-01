namespace SubtleEngineering.Analyzers.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

internal record ParsedLocations(string ActualCode)
{
    public List<ParsedLocation> Positions { get; } = new();
}
