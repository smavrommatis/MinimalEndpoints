using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.Analyzers.Models;

internal class MapMethodsAttributeDefinition
{
    public string Pattern { get; set; }

    public string EndpointBuilderMethodName { get; set; }

    public string[] Methods { get; set; }

    public ServiceLifetime Lifetime { get; set; }


    public string EntryPoint { get; set; }

    public string ServiceName { get; set; }

    /// <summary>
    /// The type symbol of the group this endpoint belongs to, or null if not grouped.
    /// </summary>
    public INamedTypeSymbol GroupType { get; set; }
}
