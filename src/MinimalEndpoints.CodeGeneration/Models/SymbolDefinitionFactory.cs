using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration.Models;

internal class SymbolDefinitionFactory
{
    public Func<AttributeData, bool> Predicate { get; }
    public Func<INamedTypeSymbol, AttributeData, SymbolDefinition> Create { get; }

    public SymbolDefinitionFactory(
        Func<AttributeData, bool> predicate,
        Func<INamedTypeSymbol, AttributeData, SymbolDefinition> create
    )
    {
        Predicate = predicate;
        Create = create;
    }

    public static SymbolDefinition TryCreateSymbol(INamedTypeSymbol symbol)
    {
        var classification = Classify(symbol);

        // Only an unambiguous single endpoint OR single group attribute yields a definition.
        // Anything else — multiple endpoint attributes, an endpoint + a group attribute, a
        // duplicated group attribute, or no recognized attribute — is left for the analyzers to
        // diagnose. Discovery must never throw here, or it crashes the whole generator (CS8785)
        // and drops every generated mapping, not just the offending class.
        if (classification.EndpointAttributes.Length == 1 && classification.GroupAttributes.Length == 0)
        {
            return EndpointDefinition.Factory.Create(symbol, classification.EndpointAttributes[0]);
        }

        if (classification.GroupAttributes.Length == 1 && classification.EndpointAttributes.Length == 0)
        {
            return EndpointGroupDefinition.Factory.Create(symbol, classification.GroupAttributes[0]);
        }

        return null;
    }

    /// <summary>
    /// Partitions a type's attributes into endpoint-mapping attributes (MapGet, MapPost, …) and
    /// group attributes (MapGroup). Shared by <see cref="TryCreateSymbol"/> and the analyzers so
    /// the ambiguous endpoint + group case (MINEP007) can be detected explicitly rather than by
    /// catching the exception a predicate-form <c>SingleOrDefault</c> would throw.
    /// </summary>
    public static SymbolClassification Classify(INamedTypeSymbol symbol)
    {
        var endpointAttributes = ImmutableArray.CreateBuilder<AttributeData>();
        var groupAttributes = ImmutableArray.CreateBuilder<AttributeData>();

        foreach (var attributeData in symbol.GetAttributes())
        {
            if (EndpointDefinition.Factory.Predicate(attributeData))
            {
                endpointAttributes.Add(attributeData);
            }
            else if (EndpointGroupDefinition.Factory.Predicate(attributeData))
            {
                groupAttributes.Add(attributeData);
            }
        }

        return new SymbolClassification(endpointAttributes.ToImmutable(), groupAttributes.ToImmutable());
    }
}

/// <summary>
/// The endpoint- and group-mapping attributes found on a single type, as classified by
/// <see cref="SymbolDefinitionFactory.Classify"/>.
/// </summary>
internal readonly struct SymbolClassification
{
    public SymbolClassification(
        ImmutableArray<AttributeData> endpointAttributes,
        ImmutableArray<AttributeData> groupAttributes)
    {
        EndpointAttributes = endpointAttributes;
        GroupAttributes = groupAttributes;
    }

    public ImmutableArray<AttributeData> EndpointAttributes { get; }

    public ImmutableArray<AttributeData> GroupAttributes { get; }

    /// <summary>
    /// True when the type carries at least one endpoint attribute AND at least one group
    /// attribute — the invalid combination reported as MINEP007.
    /// </summary>
    public bool IsEndpointAndGroup => EndpointAttributes.Length > 0 && GroupAttributes.Length > 0;
}
