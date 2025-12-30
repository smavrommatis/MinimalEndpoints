using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Endpoints;

internal static class EndpointUtilities
{
    public const string DefaultEntryPointMethodName = "Handle";
    public const string DefaultAsyncMethodName = "HandleAsync";

    public static bool IsConfigurableEndpoint(this INamedTypeSymbol symbol)
    {
        return symbol.HasInterface(WellKnownTypes.RootNamespace, WellKnownTypes.ConfigurableEndpointTypeName);
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
                .Where(x => x.Name is DefaultEntryPointMethodName or DefaultAsyncMethodName)
                .OrderByDescending(x => x.Name == DefaultAsyncMethodName) // Prefer async method if both exist
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
