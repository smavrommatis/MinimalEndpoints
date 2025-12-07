using Microsoft.CodeAnalysis;
using MinimalEndpoints.Analyzers.Utilities;

namespace MinimalEndpoints.Analyzers.Models;

internal sealed class EndpointInfo
{
    public string ClassName { get; set; }

    public string FullTypeName { get; set; }

    public string Namespace { get; set; }

    public bool IsConfigurable { get; set; }

    public string MappingEndpointMethodName => $"Map__{FullTypeName.Replace(".", "_").Replace("+", "_")}";

    public MapMethodsAttributeInfo MapMethodsAttribute { get; set; }

    public MethodInfo EntryPoint { get; set;  }

    public static EndpointInfo Create(INamedTypeSymbol symbol, IMethodSymbol entryPoint,
        MapMethodsAttributeInfo mapMethodsAttribute)
    {
        var isConfigurable = symbol.IsConfigurableEndpoint();

        return new EndpointInfo
        {
            ClassName = symbol.Name,
            FullTypeName = symbol.ToDisplayString(),
            Namespace = symbol.ContainingNamespace.ToDisplayString(),
            MapMethodsAttribute = mapMethodsAttribute,
            EntryPoint = new MethodInfo()
            {
                Name = entryPoint.Name,
                ReturnType = entryPoint.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Parameters = entryPoint.Parameters.Select(p => new ParameterInfo
                {
                    Name = p.Name,
                    Type = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Nullable = p.NullableAnnotation == NullableAnnotation.Annotated,
                    DefaultValue = p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() : null,
                    Attributes = p.GetAttributes()
                }).ToList(),
                IsAsync = entryPoint.IsAsync || entryPoint.ReturnType.Name.Contains("Task")
            },
            IsConfigurable = isConfigurable,
        };
    }
}
