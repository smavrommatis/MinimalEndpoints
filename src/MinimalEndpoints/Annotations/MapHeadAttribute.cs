using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

public sealed class MapHeadAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Head.Method];

    public MapHeadAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
