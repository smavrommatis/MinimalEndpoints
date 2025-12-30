using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Endpoints.Models;

internal class MapMethodsAttributeDefinition
{
    public string Pattern { get; set; }

    public string EndpointBuilderMethodName { get; set; }

    public string[] Methods { get; set; }

    public ServiceLifetime Lifetime { get; set; }


    public string EntryPoint { get; set; }

    public string ServiceName { get; set; }

    public INamedTypeSymbol GroupType { get; set; }
}
