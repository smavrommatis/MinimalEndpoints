using System.Collections.Frozen;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.Analyzers.Models;

namespace MinimalEndpoints.Analyzers.Utilities;

internal static class MapMethodsUtilities
{
    private static readonly FrozenDictionary<string, (string Method, string EndpointBuilderMethodName)> MapMethodAttributes = new Dictionary<string, (string Method, string EndpointBuilderMethodName)>()
    {
        { WellKnownTypes.Annotations.MapGetAttributeName, ("GET", "MapGet") },
        { WellKnownTypes.Annotations.MapPostAttributeName, ("POST", "MapPost") },
        { WellKnownTypes.Annotations.MapPutAttributeName, ("PUT", "MapPut") },
        { WellKnownTypes.Annotations.MapDeleteAttributeName, ("DELETE", "MapDelete") },
        { WellKnownTypes.Annotations.MapPatchAttributeName, ("PATCH", "MapPatch") },
        { WellKnownTypes.Annotations.MapHeadAttributeName, ("HEAD", "MapHead") }
    }.ToFrozenDictionary();

    public static AttributeData[] GetMapMethodAttributes(this INamedTypeSymbol symbol)
    {
        // Use array instead of LINQ for better performance
        var attributes = symbol.GetAttributes();
        var result = new List<AttributeData>(attributes.Length);

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass != null && attribute.AttributeClass.IsMapMethodsAttribute())
            {
                result.Add(attribute);
            }
        }

        return result.ToArray();
    }

    public static MapMethodsAttributeDefinition GetMapMethodAttributeDefinition(this AttributeData attributeData)
    {
        return WellKnownTypes.Annotations.MapMethodsAttributeName.Equals(attributeData.AttributeClass!.Name)
            ? GetAttributeDataForMultipleMethods(attributeData)
            : GetAttributeDataForSingleMethod(attributeData);
    }

    public static MapMethodsAttributeDefinition GetMapMethodAttributeDefinition(this INamedTypeSymbol symbol)
    {
        var attributeData = symbol.GetAttributes()
            .SingleOrDefault(attribute =>
                attribute.AttributeClass != null
                && attribute.AttributeClass.IsMapMethodsAttribute()
            );

        return attributeData?.GetMapMethodAttributeDefinition();
    }

    private static MapMethodsAttributeDefinition GetAttributeDataForSingleMethod(AttributeData attributeData)
    {
        var pattern = attributeData.ConstructorArguments[0].Value as string;
        var lifetime = (ServiceLifetime)attributeData.ConstructorArguments[1].Value!;

        var attributeDefinition = MapMethodAttributes[attributeData.AttributeClass!.Name];

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
        var pattern = attributeData.ConstructorArguments[0].Value as string;
        var methods = attributeData.ConstructorArguments[1].Values
            .Select(v => v.Value as string)
            .Where(s => s != null)
            .ToArray();
        var lifetime = (ServiceLifetime)attributeData.ConstructorArguments[2].Value!;

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
        INamedTypeSymbol groupType = null;

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
                case "Group" when namedArg.Value.Value is INamedTypeSymbol group:
                    groupType = group;
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
            GroupType = groupType
        };
    }

    private static bool IsMapMethodsAttribute(this INamedTypeSymbol type)
    {
        return (MapMethodAttributes.ContainsKey(type.Name) ||
                WellKnownTypes.Annotations.MapMethodsAttributeName.Equals(type.Name))
            && type.ContainingNamespace.ToDisplayString() == WellKnownTypes.Annotations.Namespace;
    }
}
