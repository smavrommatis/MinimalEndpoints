using System.Collections.Generic;
using System.Linq;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Groups.Models;

namespace MinimalEndpoints.CodeGeneration;

/// <summary>
/// Resolves the per-file generated method and variable names, disambiguating the (rare) case where
/// two distinct classes sanitize to the same identifier — e.g. namespace <c>My.App</c> and
/// namespace <c>My_App</c> both yield <c>Map__My_App_Foo</c> once '.' is replaced by '_'. Open
/// generics (the other collision source) are already filtered out by the discovery gate, so the
/// only residual collisions are these dot/underscore aliases.
/// <para>
/// Non-colliding definitions keep their exact base name, so generated output is byte-identical for
/// every project that has no such collision. Colliding definitions get a short, deterministic
/// FNV-1a suffix derived from the fully-qualified name (NOT <see cref="object.GetHashCode"/>, which
/// is process-randomized and would make output non-deterministic across builds).
/// </para>
/// </summary>
internal sealed class GeneratedNames
{
    private readonly Dictionary<EndpointDefinition, string> _endpointMethodNames;
    private readonly Dictionary<EndpointGroupDefinition, string> _groupMethodNames;
    private readonly Dictionary<EndpointGroupDefinition, string> _groupVariableNames;

    private GeneratedNames(
        Dictionary<EndpointDefinition, string> endpointMethodNames,
        Dictionary<EndpointGroupDefinition, string> groupMethodNames,
        Dictionary<EndpointGroupDefinition, string> groupVariableNames)
    {
        _endpointMethodNames = endpointMethodNames;
        _groupMethodNames = groupMethodNames;
        _groupVariableNames = groupVariableNames;
    }

    public static GeneratedNames Build(
        IReadOnlyList<EndpointDefinition> endpoints,
        IReadOnlyList<EndpointGroupDefinition> groups)
    {
        var endpointMethodNames = new Dictionary<EndpointDefinition, string>();
        foreach (var collisionGroup in endpoints.GroupBy(e => e.MappingEndpointMethodName))
        {
            var collides = collisionGroup.Count() > 1;
            foreach (var endpoint in collisionGroup)
            {
                endpointMethodNames[endpoint] = collides
                    ? endpoint.MappingEndpointMethodName + "_" + StableHash(endpoint.ClassType.FullName)
                    : endpoint.MappingEndpointMethodName;
            }
        }

        var groupMethodNames = new Dictionary<EndpointGroupDefinition, string>();
        var groupVariableNames = new Dictionary<EndpointGroupDefinition, string>();
        // A group's method name and variable name both derive from the same fully-qualified name
        // (only the prefix differs), so they collide together — one suffix decision per group keeps
        // the declaration and its call site in sync.
        foreach (var collisionGroup in groups.GroupBy(g => g.MappingGroupMethodName))
        {
            var collides = collisionGroup.Count() > 1;
            foreach (var group in collisionGroup)
            {
                var suffix = collides ? "_" + StableHash(group.ClassType.FullName) : "";
                groupMethodNames[group] = group.MappingGroupMethodName + suffix;
                groupVariableNames[group] = group.VariableName + suffix;
            }
        }

        return new GeneratedNames(endpointMethodNames, groupMethodNames, groupVariableNames);
    }

    public string MethodName(EndpointDefinition endpoint) => _endpointMethodNames[endpoint];

    public string MethodName(EndpointGroupDefinition group) => _groupMethodNames[group];

    public string VariableName(EndpointGroupDefinition group) => _groupVariableNames[group];

    private static string StableHash(string value)
    {
        // FNV-1a (32-bit), rendered as 8 lowercase hex digits — deterministic across processes.
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }

        return hash.ToString("x8");
    }
}
