using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Endpoints;

internal static class MapMethodsUtilities
{
    // Plain read-only dictionary rather than FrozenDictionary: FrozenDictionary needs
    // System.Collections.Immutable 8.0+, which the lowered Roslyn floor (4.8.0) does not bring,
    // and analyzers must not bundle their own BCL. The lookup is tiny, so there is no perf concern.
    private static readonly IReadOnlyDictionary<string, (string Method, string EndpointBuilderMethodName)>
        s_mapMethodAttributes =
            new Dictionary<string, (string Method, string EndpointBuilderMethodName)>()
            {
                { WellKnownTypes.Annotations.MapGetAttributeName, ("GET", "MapGet") },
                { WellKnownTypes.Annotations.MapPostAttributeName, ("POST", "MapPost") },
                { WellKnownTypes.Annotations.MapPutAttributeName, ("PUT", "MapPut") },
                { WellKnownTypes.Annotations.MapDeleteAttributeName, ("DELETE", "MapDelete") },
                { WellKnownTypes.Annotations.MapPatchAttributeName, ("PATCH", "MapPatch") },
                // IEndpointRouteBuilder has no MapHead extension, so HEAD is emitted via
                // MapMethods(pattern, ["HEAD"], Handler) rather than a dedicated builder call.
                { WellKnownTypes.Annotations.MapHeadAttributeName, ("HEAD", "MapMethods") }
            };

    public static MapMethodsAttributeDefinition GetMapMethodAttributeDefinition(this AttributeData attributeData)
    {
        // A mid-typing / malformed attribute (e.g. '[MapGet]' before its pattern is typed) still
        // resolves its AttributeClass, so the factory predicate passes — but it has no valid
        // constructor to bind to. Reading its constructor arguments would throw and crash the
        // generator (CS8785) / analyzer (AD0001). Decline it here instead; the compiler already
        // reports the incomplete attribute, and callers tolerate a null result.
        if (attributeData.AttributeConstructor is null)
        {
            return null;
        }

        return WellKnownTypes.Annotations.MapMethodsAttributeName.Equals(attributeData.AttributeClass!.Name)
            ? GetAttributeDataForMultipleMethods(attributeData)
            : GetAttributeDataForSingleMethod(attributeData);
    }

    public static bool IsMapMethodsAttribute(this INamedTypeSymbol type)
    {
        return (s_mapMethodAttributes.ContainsKey(type.Name) ||
                WellKnownTypes.Annotations.MapMethodsAttributeName.Equals(type.Name))
               && type.ContainingNamespace.ToDisplayString() == WellKnownTypes.Annotations.Namespace;
    }

    private static MapMethodsAttributeDefinition GetAttributeDataForSingleMethod(AttributeData attributeData)
    {
        // Single-method ctor is (string pattern, ServiceLifetime lifetime). Guard against an
        // error-state attribute whose arguments are short or in error (the lifetime defaults to
        // a constant when omitted, so a well-formed attribute always has both arguments).
        var args = attributeData.ConstructorArguments;
        if (args.Length < 2 ||
            args[0].Kind == TypedConstantKind.Error ||
            args[1].Kind == TypedConstantKind.Error ||
            args[1].Value is not int lifetimeValue)
        {
            return null;
        }

        var pattern = args[0].Value as string;
        var lifetime = (ServiceLifetime)lifetimeValue;

        var attributeDefinition = s_mapMethodAttributes[attributeData.AttributeClass!.Name];

        return GetMapMethodsAttributeDefinitionInternal(
            attributeData: attributeData,
            pattern: pattern,
            lifetime: lifetime,
            endpointBuilderMethodName: attributeDefinition.EndpointBuilderMethodName,
            methods: [attributeDefinition.Method]
        );
    }

    private static MapMethodsAttributeDefinition GetAttributeDataForMultipleMethods(AttributeData attributeData)
    {
        // MapMethods ctor is (string pattern, string[] methods, ServiceLifetime lifetime). Guard
        // against a short/error-state attribute: a missing or null methods array (e.g.
        // '[MapMethods("/x")]' or '[MapMethods("/x", null)]') leaves a default ImmutableArray
        // whose enumeration throws, and a missing lifetime would index out of bounds.
        var args = attributeData.ConstructorArguments;
        if (args.Length < 3 ||
            args[0].Kind == TypedConstantKind.Error ||
            args[1].Kind != TypedConstantKind.Array ||
            args[1].Values.IsDefault ||
            args[2].Kind == TypedConstantKind.Error ||
            args[2].Value is not int lifetimeValue)
        {
            return null;
        }

        var pattern = args[0].Value as string;
        var methods = args[1].Values
            .Select(v => v.Value as string)
            .Where(s => s != null)
            .ToArray();
        var lifetime = (ServiceLifetime)lifetimeValue;

        return GetMapMethodsAttributeDefinitionInternal(
            attributeData: attributeData,
            pattern: pattern,
            lifetime: lifetime,
            "MapMethods",
            methods: methods
        );
    }

    private static MapMethodsAttributeDefinition GetMapMethodsAttributeDefinitionInternal(
        AttributeData attributeData,
        string pattern,
        ServiceLifetime lifetime,
        string endpointBuilderMethodName,
        string[] methods
    )
    {
        string entryPoint = null;
        string serviceName = null;
        string groupTypeName = null;

        foreach (var namedArg in attributeData.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "EntryPoint" when namedArg.Value.Value is string entry:
                    entryPoint = entry;
                    break;
                case "ServiceType" when namedArg.Value.Value is INamedTypeSymbol serviceType:
                    serviceName = serviceType.ToDisplayString();
                    break;
                // Capture the group reference as a fully-qualified-name string (via the same
                // TypeDefinition path the group uses for its own identity), not the symbol — the
                // symbol would root the source compilation in the cached model and would not
                // match the cached group's symbol across incremental compilations.
                case "Group" when namedArg.Value.Value is INamedTypeSymbol group:
                    groupTypeName = new TypeDefinition(group).FullName;
                    break;
            }
        }

        return new MapMethodsAttributeDefinition
        {
            Pattern = pattern,
            EndpointBuilderMethodName = endpointBuilderMethodName,
            Methods = methods,
            Lifetime = lifetime,
            EntryPoint = entryPoint,
            ServiceName = serviceName,
            GroupTypeName = groupTypeName
        };
    }
}
