using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Utilities;

internal static class NamedTypeSymbolExtensions
{
    public static bool IsConditionallyMapped(this INamedTypeSymbol symbol)
    {
        return symbol.AllInterfaces.Any(i =>
            i.Name == WellKnownTypes.ConditionalMappingTypeName &&
            i.ContainingNamespace.ToDisplayString() == WellKnownTypes.RootNamespace);
    }

    public static bool HasInterface(this INamedTypeSymbol symbol, string @namespace, string interfaceName)
    {
        return symbol.AllInterfaces.Any(i =>
            i.Name == interfaceName &&
            i.ContainingNamespace.ToDisplayString() == @namespace);
    }
}
