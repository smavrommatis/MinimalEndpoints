using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class MapPutAttribute : MapMethodsBaseAttribute
{
    private static readonly IEnumerable<string> s_supportedMethods = new[] { HttpMethod.Put.Method };

    public MapPutAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
