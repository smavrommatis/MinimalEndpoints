using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MapMethodsAttribute : MapMethodsBaseAttribute
{
    public MapMethodsAttribute(
        [StringSyntax("Route")] string pattern,
        IEnumerable<string> methods,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    ) : base(pattern, methods, lifetime)
    {

    }
}
