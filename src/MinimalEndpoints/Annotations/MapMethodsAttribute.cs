using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

public sealed class MapMethodsAttribute : MapMethodsBaseAttribute
{
    public MapMethodsAttribute(
        [StringSyntax("Route")] string pattern,
        string[] methods,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    ) : base(pattern, methods, lifetime)
    {

    }
}
