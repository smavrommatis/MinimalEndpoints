using System.Text;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Models;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Groups.Models;

internal class EndpointGroupDefinition : SymbolDefinition
{
    public static readonly SymbolDefinitionFactory Factory = new(
        predicate: attributeData => attributeData?.AttributeClass != null &&
                                    attributeData.AttributeClass.IsMapGroupAttribute(),
        create: (symbol, attributeData) => new EndpointGroupDefinition(symbol, attributeData));

    private bool? _hierarchyConditionallyMapped = null;

    public EndpointGroupDefinition(INamedTypeSymbol symbol, AttributeData attributeData) : base(symbol)
    {
        AttributeData = attributeData;
        ClassType = new TypeDefinition(symbol);
        IsConditionallyMapped = symbol.IsConditionallyMapped();
        IsConfigurable = symbol.IsConfigurableGroupEndpoint();
        Prefix = attributeData.ConstructorArguments.FirstOrDefault().Value as string ?? "/";
    }

    public AttributeData AttributeData { get; }

    public string Prefix { get; }

    public TypeDefinition ClassType { get; set; }

    public EndpointGroupDefinition ParentGroup { get; set; }

    public bool IsConfigurable { get; set; }

    public bool IsConditionallyMapped { get; set; }

    public List<List<CycleNode>> Cycles { get; } = [];

    public string VariableName
    {
        get
        {
            if (field == null)
            {
                // Use StringBuilder for better performance than chained Replace
                var sb = new StringBuilder("group_", ClassType.FullName.Length + 10);
                foreach (var ch in ClassType.FullName)
                {
                    sb.Append(ch is '.' or '+' ? '_' : ch);
                }

                field = sb.ToString();
            }

            return field;
        }
    }

    public string MappingGroupMethodName
    {
        get
        {
            if (field == null)
            {
                // Use StringBuilder for better performance than chained Replace
                var sb = new StringBuilder("MapGroup__", ClassType.FullName.Length + 10);
                foreach (var ch in ClassType.FullName)
                {
                    sb.Append(ch is '.' or '+' ? '_' : ch);
                }

                field = sb.ToString();
            }

            return field;
        }
    }

    public int Depth
    {
        get
        {
            if (field == 0)
            {
                var parentDepth = ParentGroup?.Depth ?? 0;
                field = parentDepth + 1;
            }

            return field;
        }
    }

    public bool HierarchyConditionallyMapped
    {
        get
        {
            _hierarchyConditionallyMapped ??= IsConditionallyMapped || (ParentGroup?.HierarchyConditionallyMapped ?? false);
            return _hierarchyConditionallyMapped.Value;
        }
    }

    public string FullPrefix
    {
        get
        {
            field ??= (ParentGroup?.FullPrefix ?? "") + Prefix;
            return field;
        }
    }

    internal struct CycleNode
    {
        public int Index { get; set; }

        public INamedTypeSymbol Symbol { get; set; }
    }
}
