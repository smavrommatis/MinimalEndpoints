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
        create: (symbol, attributeData) =>
        {
            var mapMethodsAttributeInfo = attributeData.GetMapMethodAttributeDefinition();

            if (mapMethodsAttributeInfo == null)
            {
                return null;
            }

            var entryPoint = symbol.FindEntryPointMethod(mapMethodsAttributeInfo.EntryPoint);

            return entryPoint == null
                ? null
                : Create(symbol, entryPoint, mapMethodsAttributeInfo);
        }
    );

    private EndpointDefinition()
    {
    }

    public TypeDefinition ClassType { get; private set; }

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
                    DefaultValue = p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() : null,
                    Attributes = p.GetAttributes()
                        .Select(AttributeDefinition.FromAttributeData)
                        .Where(attr => attr != null)
                        .ToList()
                }).ToDictionary(x => x.Name)
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

        var parameters = string.Join(";", EntryPoint.Parameters.Values.Select(p =>
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
