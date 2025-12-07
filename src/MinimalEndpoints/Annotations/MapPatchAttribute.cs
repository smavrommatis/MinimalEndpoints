using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MapPatchAttribute : MapMethodsBaseAttribute
{
    private static readonly IEnumerable<string> s_supportedMethods = new[] { HttpMethod.Patch.Method };

    public MapPatchAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
