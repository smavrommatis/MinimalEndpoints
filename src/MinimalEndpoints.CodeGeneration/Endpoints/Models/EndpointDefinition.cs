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
            // Pass the endpoint symbol so the generator degrades a ServiceType the endpoint is not
            // assignable to down to concrete registration (avoiding CS0311); the analyzer omits the symbol,
            // so it still reports MINEP012.
            var mapMethodsAttributeInfo = attributeData.GetMapMethodAttributeDefinition(scope, symbol);

            if (mapMethodsAttributeInfo == null)
            {
                return null;
            }

            // Decline a malformed attribute rather than emit a broken mapping: a null route pattern
            // (which EscapeStringContent would otherwise turn into the empty route "") or an empty
            // HTTP-method set (a non-routable [MapMethods]). The analyzer reports MINEP015.
            if (mapMethodsAttributeInfo.Pattern is null ||
                mapMethodsAttributeInfo.Methods is null or { Length: 0 })
            {
                return null;
            }

            var entryPoint = symbol.FindEntryPointMethod(mapMethodsAttributeInfo.EntryPoint);

            if (entryPoint == null)
            {
                return null;
            }

            // ASP.NET cannot model-bind a by-ref (ref/out/in) or pointer parameter, and the generated
            // handler delegate cannot reproduce the modifier — emitting it produces CS1620/CS0214 (or, for
            // `in`, silently wrong by-value binding). Decline the endpoint instead; the analyzer reports
            // MINEP011. Mirrors the EntryPointSurfaceIsPublic guard below.
            if (HasUnsupportedParameterModifier(entryPoint))
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

    /// <summary>
    /// True when any entry-point parameter is by-reference (ref/out/in) or a pointer. Such parameters
    /// cannot be bound by ASP.NET Core or carried through the generated handler delegate, so the endpoint
    /// is declined here (the analyzer reports MINEP011) rather than emitting non-compiling code.
    /// </summary>
    private static bool HasUnsupportedParameterModifier(IMethodSymbol entryPoint)
    {
        foreach (var parameter in entryPoint.Parameters)
        {
            if (parameter.RefKind != RefKind.None || parameter.Type is IPointerTypeSymbol)
            {
                return true;
            }
        }

        return false;
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
                    RefKind = p.RefKind,
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
            $"{p.Name}:{p.Type.FullName}:{p.Nullable}:{p.RefKind}:{p.DefaultValue}:" +
            string.Join("+", p.Attributes.Select(a => a.ToDisplayString(s_emptyUsings)))));

        return
            $"{ClassType.FullName}|{attr.Pattern}|{attr.EndpointBuilderMethodName}|" +
            $"{(attr.Methods is null ? "" : string.Join(",", attr.Methods))}|{attr.Lifetime}|" +
            $"{attr.EntryPoint}|{attr.ServiceName}|{attr.GroupTypeName}|" +
            $"{EntryPoint.Name}|{EntryPoint.ReturnType.FullName}|" +
            $"{parameters}|{IsConfigurable}|{IsConditionallyMapped}";
    }
}
