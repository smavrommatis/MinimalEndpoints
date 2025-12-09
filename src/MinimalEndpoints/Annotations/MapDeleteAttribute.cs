using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

public sealed class MapDeleteAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Delete.Method];

    public MapDeleteAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
