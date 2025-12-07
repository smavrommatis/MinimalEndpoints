using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class MapMethodsBaseAttribute : Attribute
{
    public string Pattern { get; }

    public IEnumerable<string> Methods { get; }

    public ServiceLifetime Lifetime { get; }

    public string? GroupPrefix { get; set; }

    public string? EntryPoint { get; set; }

    public Type? ServiceType { get; set; }

    protected MapMethodsBaseAttribute(
        [StringSyntax("Route")] string pattern,
        IEnumerable<string> methods,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        Pattern = pattern;
        Methods = methods;
        Lifetime = lifetime;
    }
}
