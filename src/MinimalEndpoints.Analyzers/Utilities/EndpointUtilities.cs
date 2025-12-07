using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.Analyzers.Utilities;

internal static class EndpointUtilities
{
    public static bool IsConfigurableEndpoint(this INamedTypeSymbol symbol)
    {
        return symbol.AllInterfaces.Any(i =>
            i.Name == WellKnownTypes.ConfigurableEndpointTypeName &&
            i.ContainingNamespace.ToDisplayString() == WellKnownTypes.RootNamespace);
    }

    public static IMethodSymbol FindEntryPointMethod(this INamedTypeSymbol symbol, string preferredMethodName)
    {
        var publicMethods = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => !x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public);

        if (string.IsNullOrEmpty(preferredMethodName))
        {
            publicMethods = publicMethods
                .Where(x => x.Name is "Handle" or "HandleAsync")
                .OrderByDescending(x => x.Name == "HandleAsync") // Prefer async method if both exist
                .ThenByDescending(x => x.Name.EndsWith("Async"));
        }
        else
        {
            publicMethods = publicMethods
                .Where(x=> x.Name == preferredMethodName);
        }

        return publicMethods.FirstOrDefault();
    }
}
