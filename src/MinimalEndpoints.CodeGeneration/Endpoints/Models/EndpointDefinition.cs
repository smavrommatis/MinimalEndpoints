using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Models;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Endpoints.Models;

internal sealed class EndpointDefinition : SymbolDefinition
{
    public static readonly SymbolDefinitionFactory Factory = new(
        predicate: attributeData => attributeData?.AttributeClass != null &&
                                    attributeData.AttributeClass.IsMapMethodsAttribute(),
        create: (symbol, attributeData, scope) =>
        {
            var mapMethodsAttributeInfo = attributeData.GetMapMethodAttributeDefinition(scope);

            if (mapMethodsAttributeInfo == null)
            {
                return null;
            }

            var entryPoint = symbol.FindEntryPointMethod(mapMethodsAttributeInfo.EntryPoint);

            if (entryPoint == null)
            {
                return null;
            }

            // Cross-assembly: the host emits the handler delegate by name, so every type in the
            // handler signature must be public in the referenced assembly. If not, skip the endpoint
            // rather than emit host code that fails to compile (CS0122). (ServiceType is handled
            // separately in GetMapMethodAttributeDefinition — it degrades to concrete registration.)
            if (scope == AccessibilityScope.External && !EntryPointSurfaceIsPublic(entryPoint))
            {
                return null;
            }

            return Create(symbol, entryPoint, mapMethodsAttributeInfo);
        }
    );

    // Every type the host emits by name from the handler signature must be public in the referenced
    // assembly. A public entry point's parameter/return TYPES are already guaranteed public by the C#
    // compiler (CS0050/CS0051), so those checks are defensive; a parameter ATTRIBUTE type is NOT so
    // constrained and would be emitted onto the generated handler parameter and fail with CS0122.
    private static bool EntryPointSurfaceIsPublic(IMethodSymbol entryPoint)
    {
        if (!SymbolDefinitionFactory.IsPubliclyAccessible(entryPoint.ReturnType))
        {
            return false;
        }

        foreach (var parameter in entryPoint.Parameters)
        {
            if (!SymbolDefinitionFactory.IsPubliclyAccessible(parameter.Type))
            {
                return false;
            }

            foreach (var attribute in parameter.GetAttributes())
            {
                if (attribute.AttributeClass is { } attributeClass &&
                    !SymbolDefinitionFactory.IsPubliclyAccessible(attributeClass))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private EndpointDefinition()
    {
    }

    public TypeDefinition ClassType { get; private set; }

    public override string FullName => ClassType.FullName;

    public bool IsConfigurable { get; private set; }

    public bool IsConditionallyMapped { get; private set; }

    public string MappingEndpointMethodName
    {
        get
        {
            if (field == null)
            {
                // Use StringBuilder for better performance than chained Replace
                var sb = new StringBuilder("Map__", ClassType.FullName.Length + 10);
                foreach (var ch in ClassType.FullName)
                {
                    sb.Append(ch is '.' or '+' ? '_' : ch);
                }

                field = sb.ToString();
            }

            return field;
        }
    }

    public MapMethodsAttributeDefinition MapMethodsAttribute { get; private set; }

    public MethodInfo EntryPoint { get; private set; }


    public static EndpointDefinition Create(INamedTypeSymbol symbol, IMethodSymbol entryPoint,
        MapMethodsAttributeDefinition mapMethodsAttribute)
    {
        var isConfigurable = symbol.IsConfigurableEndpoint();
        var isConditionallyMapped = symbol.IsConditionallyMapped();

        var definition = new EndpointDefinition()
        {
            ClassType = new TypeDefinition(symbol),
            MapMethodsAttribute = mapMethodsAttribute,
            EntryPoint = new MethodInfo()
            {
                Name = entryPoint.Name,
                ReturnType = new TypeDefinition(entryPoint.ReturnType),
                Parameters = entryPoint.Parameters.Select(p => new ParameterInfo
                {
                    Name = p.Name,
                    Type = new TypeDefinition(p.Type),
                    Nullable = p.NullableAnnotation == NullableAnnotation.Annotated,
                    DefaultValue = p.FormatDefaultValueLiteral(),
                    Attributes = p.GetAttributes()
                        .Select(AttributeDefinition.FromAttributeData)
                        .Where(attr => attr != null)
                        .ToList()
                }).ToImmutableArray()
            },
            IsConfigurable = isConfigurable,
            IsConditionallyMapped = isConditionallyMapped
        };

        definition._equalityKey = definition.BuildEqualityKey();
        return definition;
    }

    private static readonly HashSet<string> s_emptyUsings = new();

    private string _equalityKey;

    protected override string EqualityKey => _equalityKey;

    private string BuildEqualityKey()
    {
        var attr = MapMethodsAttribute;

        var parameters = string.Join(";", EntryPoint.Parameters.Select(p =>
            $"{p.Name}:{p.Type.FullName}:{p.Nullable}:{p.DefaultValue}:" +
            string.Join("+", p.Attributes.Select(a => a.ToDisplayString(s_emptyUsings)))));

        return
            $"{ClassType.FullName}|{attr.Pattern}|{attr.EndpointBuilderMethodName}|" +
            $"{(attr.Methods is null ? "" : string.Join(",", attr.Methods))}|{attr.Lifetime}|" +
            $"{attr.EntryPoint}|{attr.ServiceName}|{attr.GroupTypeName}|" +
            $"{EntryPoint.Name}|{EntryPoint.ReturnType.FullName}|" +
            $"{parameters}|{IsConfigurable}|{IsConditionallyMapped}";
    }
}
