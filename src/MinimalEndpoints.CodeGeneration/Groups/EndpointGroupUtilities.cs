using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Groups.Models;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Groups;

internal static class EndpointGroupUtilities
{
    public static Dictionary<INamedTypeSymbol, EndpointGroupDefinition> FillHierarchyAndDetectCycles(
        this IEnumerable<EndpointGroupDefinition> groups)
    {
        var groupsBySymbol = new Dictionary<INamedTypeSymbol, EndpointGroupDefinition>(SymbolEqualityComparer.Default);

        foreach (var group in groups)
        {
            groupsBySymbol.Add(group.Symbol, group);
        }

        foreach (var group in groupsBySymbol.Values)
        {
            group.ParentGroup = GetParentGroupDefinition(group, groupsBySymbol);
        }

        DetectAndBreakCycles(groupsBySymbol.Values);

        return groupsBySymbol;
    }

    public static bool IsConfigurableGroupEndpoint(this INamedTypeSymbol symbol)
    {
        return symbol.HasInterface(WellKnownTypes.RootNamespace, WellKnownTypes.ConfigurableGroupTypeName);
    }

    public static bool IsMapGroupAttribute(this INamedTypeSymbol symbol)
    {
        return symbol.Name == WellKnownTypes.Annotations.MapGroupAttributeName &&
               symbol.ContainingNamespace.ToDisplayString() == WellKnownTypes.Annotations.Namespace;
    }

    private static void DetectAndBreakCycles(IEnumerable<EndpointGroupDefinition> groups)
    {
        foreach (var group in groups)
        {
            var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var cycle = new List<EndpointGroupDefinition.CycleNode>(capacity: 1);
            EndpointGroupDefinition prev = null;
            var current = group;
            var index = 0;

            while (current != null)
            {
                cycle.Add(new EndpointGroupDefinition.CycleNode() { Index = index++, Symbol = current.Symbol });

                if (visited.Add(current.Symbol))
                {
                    prev = current;
                    current = current.ParentGroup;
                }
                else
                {
                    // Cycle detected
                    prev!.ParentGroup = null;
                    group.Cycles.Add(cycle);
                    break;
                }
            }
        }
    }

    private static EndpointGroupDefinition GetParentGroupDefinition(
        EndpointGroupDefinition group,
        Dictionary<INamedTypeSymbol, EndpointGroupDefinition> groupsBySymbol
    )
    {
        foreach (var namedArgument in group.AttributeData.NamedArguments)
        {
            if (namedArgument.Key != "ParentGroup" || namedArgument.Value.Value is not INamedTypeSymbol parentGroupSymbol)
            {
                continue;
            }

            if (groupsBySymbol.TryGetValue(parentGroupSymbol, out var parentGroup))
            {
                return parentGroup;
            }

            break;
        }

        return null;
    }
}
