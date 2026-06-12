using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Groups;

internal static class EndpointGroupUtilities
{
    public static bool IsConfigurableGroupEndpoint(this INamedTypeSymbol symbol)
    {
        return symbol.HasInterface(WellKnownTypes.RootNamespace, WellKnownTypes.ConfigurableGroupTypeName);
    }

    public static bool IsMapGroupAttribute(this INamedTypeSymbol symbol)
    {
        return symbol.Name == WellKnownTypes.Annotations.MapGroupAttributeName &&
               symbol.ContainingNamespace.ToDisplayString() == WellKnownTypes.Annotations.Namespace;
    }
}
