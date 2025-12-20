using System.Text;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers.Models;

internal sealed class EndpointDefinition
{
    public TypeDefinition ClassType { get; set; }

    public bool IsConfigurable { get; set; }

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
                    sb.Append(ch == '.' || ch == '+' ? '_' : ch);
                }
                field = sb.ToString();
            }
            return field;
        }
    }

    public MapMethodsAttributeDefinition MapMethodsAttribute { get; private set; }

    public MethodInfo EntryPoint { get; private set;  }


    public static EndpointDefinition Create(INamedTypeSymbol symbol, IMethodSymbol entryPoint,
        MapMethodsAttributeDefinition mapMethodsAttribute)
    {
        var isConfigurable = symbol.IsConfigurableEndpoint();

        return new EndpointDefinition
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
                }).ToDictionary(x => x.Name),
                IsAsync = entryPoint.IsAsync || entryPoint.ReturnType.Name.Contains("Task")
            },
            IsConfigurable = isConfigurable,
        };
    }
}
