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
        create: (symbol, attributeData, _) => new EndpointGroupDefinition(symbol, attributeData));

    private readonly string _equalityKey;

    public EndpointGroupDefinition(INamedTypeSymbol symbol, AttributeData attributeData)
    {
        Name = symbol.Name;
        ClassType = new TypeDefinition(symbol);
        IsConditionallyMapped = symbol.IsConditionallyMapped();
        IsConfigurable = symbol.IsConfigurableGroupEndpoint();
        Prefix = attributeData.ConstructorArguments.FirstOrDefault().Value as string ?? "/";

        // Capture the parent reference as a fully-qualified-name string at transform time. It is
        // keyed identically to a group's own ClassType.FullName so the hierarchy resolves by name
        // (no Roslyn symbol retained on the cached model, and the link survives across incremental
        // compilations). The hierarchy itself is computed in a transient structure during output.
        ParentGroupName = ResolveParentGroupName(attributeData);

        _equalityKey =
            $"{ClassType.FullName}|{Prefix}|{IsConfigurable}|{IsConditionallyMapped}|{ParentGroupName}";
    }

    protected override string EqualityKey => _equalityKey;

    private static string ResolveParentGroupName(AttributeData attributeData)
    {
        foreach (var namedArgument in attributeData.NamedArguments)
        {
            if (namedArgument.Key == "ParentGroup" &&
                namedArgument.Value.Value is INamedTypeSymbol parentGroupSymbol)
            {
                return new TypeDefinition(parentGroupSymbol).FullName;
            }
        }

        return null;
    }

    /// <summary>The group type's simple (unqualified) name, e.g. <c>ApiGroup</c>.</summary>
    public string Name { get; }

    public string Prefix { get; }

    public TypeDefinition ClassType { get; }

    public override string FullName => ClassType.FullName;

    /// <summary>
    /// The fully-qualified name of this group's parent group (the <c>ParentGroup = typeof(...)</c>
    /// argument), or <c>null</c>. Resolved against other groups by name in
    /// <see cref="MinimalEndpoints.CodeGeneration.Groups.GroupHierarchy"/>.
    /// </summary>
    public string ParentGroupName { get; }

    public bool IsConfigurable { get; }

    public bool IsConditionallyMapped { get; }

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
}
