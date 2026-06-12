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

    /// <summary>
    /// The fully-qualified name of the endpoint's group (the <c>Group = typeof(...)</c> argument),
    /// or <c>null</c>. Captured as a string at transform time — never a Roslyn symbol — so the
    /// endpoint→group link resolves by name and stays valid across incremental compilations.
    /// </summary>
    public string GroupTypeName { get; set; }
}
