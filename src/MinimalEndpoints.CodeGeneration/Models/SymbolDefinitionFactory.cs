using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration.Models;

internal class SymbolDefinitionFactory
{
    private static readonly ImmutableArray<SymbolDefinitionFactory> s_factories =
    [
        EndpointDefinition.Factory,
        EndpointGroupDefinition.Factory
    ];

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
        var attributeFactoryPair = symbol.GetAttributes()
            .Select(attributeData => new
            {
                AttributeData = attributeData,
                Factories = s_factories.Where(factory => factory.Predicate(attributeData)).ToArray()
            })
            .SingleOrDefault(x => x.Factories.Length == 1);

        return attributeFactoryPair?.Factories[0].Create(symbol, attributeFactoryPair.AttributeData);
    }
}
