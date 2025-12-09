using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

public sealed class MapPatchAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Patch.Method];

    public MapPatchAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
