using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.Analyzers.Models;

internal sealed class ParameterInfo
{
    public string Name { get; set; }

    public string Type { get; set; }

    public bool Nullable { get; set; }

    public string DefaultValue { get; set; }

    public ImmutableArray<AttributeData> Attributes { get; set; } = [];
}
