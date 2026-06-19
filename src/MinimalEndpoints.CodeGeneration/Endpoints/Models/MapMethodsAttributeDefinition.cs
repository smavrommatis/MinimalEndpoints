using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Endpoints.Models;

// Properties are init-only: this definition is held on the cached EndpointDefinition model, whose
// equality key is computed once at construction. A post-construction mutation would silently desync
// that key from the array the emitter reads, so immutability is part of the incremental-cache contract.
internal class MapMethodsAttributeDefinition
{
    public string Pattern { get; init; }

    public string EndpointBuilderMethodName { get; init; }

    public string[] Methods { get; init; }

    public ServiceLifetime Lifetime { get; init; }


    public string EntryPoint { get; init; }

    public string ServiceName { get; init; }

    /// <summary>
    /// The fully-qualified name of the endpoint's group (the <c>Group = typeof(...)</c> argument),
    /// or <c>null</c>. Captured as a string at transform time — never a Roslyn symbol — so the
    /// endpoint→group link resolves by name and stays valid across incremental compilations.
    /// </summary>
    public string GroupTypeName { get; init; }
}
