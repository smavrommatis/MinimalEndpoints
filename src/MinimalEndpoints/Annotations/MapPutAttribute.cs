using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

public sealed class MapPutAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Put.Method];

    public MapPutAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
