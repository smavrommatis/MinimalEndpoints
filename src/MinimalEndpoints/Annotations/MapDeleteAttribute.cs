using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MapDeleteAttribute : MapMethodsBaseAttribute
{
    private static readonly IEnumerable<string> s_supportedMethods = new[] { HttpMethod.Delete.Method };

    public MapDeleteAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
