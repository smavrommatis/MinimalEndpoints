namespace MinimalEndpoints.Analyzers.Models;

internal sealed class ParameterInfo
{
    public string Name { get; set; }

    public TypeDefinition Type { get; set; }

    public bool Nullable { get; set; }

    public string DefaultValue { get; set; }

    public List<AttributeDefinition> Attributes { get; set; } = [];
}
