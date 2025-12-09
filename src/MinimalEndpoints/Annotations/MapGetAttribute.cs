using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

public sealed class MapGetAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Get.Method];

    public MapGetAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
