using Microsoft.CodeAnalysis;
using System.Globalization;

namespace MinimalEndpoints.Analyzers.Models;

/// <summary>
/// Represents an attribute definition with its type, constructor arguments, and named arguments (properties).
/// </summary>
internal sealed class AttributeDefinition
{
    public TypeDefinition AttributeType { get; }
    public List<AttributeArgument> ConstructorArguments { get; }
    public List<AttributeNamedArgument> NamedArguments { get; }

    public AttributeDefinition(
        TypeDefinition attributeType,
        List<AttributeArgument> constructorArguments,
        List<AttributeNamedArgument> namedArguments)
    {
        AttributeType = attributeType;
        ConstructorArguments = constructorArguments;
        NamedArguments = namedArguments;
    }

    /// <summary>
    /// Creates an AttributeDefinition from Roslyn's AttributeData
    /// </summary>
    public static AttributeDefinition FromAttributeData(AttributeData attributeData)
    {
        if (attributeData.AttributeClass == null)
        {
            return null;
        }

        var attributeType = new TypeDefinition(attributeData.AttributeClass);

        // Process constructor arguments
        var constructorArgs = attributeData.ConstructorArguments
            .Select(arg => new AttributeArgument(
                arg.Type != null ? new TypeDefinition(arg.Type) : null,
                FormatArgumentValue(arg)
            ))
            .ToList();

        // Process named arguments (properties or fields)
        var namedArgs = attributeData.NamedArguments
            .Select(arg => new AttributeNamedArgument(
                arg.Key,
                arg.Value.Type != null ? new TypeDefinition(arg.Value.Type) : null,
                FormatArgumentValue(arg.Value)
            ))
            .ToList();

        return new AttributeDefinition(attributeType, constructorArgs, namedArgs);
    }

    /// <summary>
    /// Renders the attribute with available usings for simplified type names
    /// </summary>
    public string ToDisplayString(HashSet<string> availableUsings)
    {
        var attributeTypeName = AttributeType.ToDisplayString(availableUsings);

        // Remove "Attribute" suffix if present
        if (attributeTypeName.EndsWith("Attribute"))
        {
            attributeTypeName = attributeTypeName.Substring(0, attributeTypeName.Length - "Attribute".Length);
        }

        // Build arguments list
        var arguments = new List<string>();

        // Add constructor arguments
        arguments.AddRange(ConstructorArguments.Select(arg => arg.Value));

        // Add named arguments
        arguments.AddRange(NamedArguments.Select(arg => $"{arg.Name} = {arg.Value}"));

        // Build final attribute string
        if (arguments.Count > 0)
        {
            return $"[{attributeTypeName}({string.Join(", ", arguments)})]";
        }

        return $"[{attributeTypeName}]";
    }

    private static string FormatArgumentValue(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                return FormatPrimitiveValue(constant);

            case TypedConstantKind.Enum:
                return FormatEnumValue(constant);

            case TypedConstantKind.Type:
                var typeSymbol = constant.Value as ITypeSymbol;
                return typeSymbol != null ? $"typeof({new TypeDefinition(typeSymbol).FullName})" : "null";

            case TypedConstantKind.Array:
                var elements = constant.Values.Select(FormatArgumentValue);
                return $"new[] {{ {string.Join(", ", elements)} }}";

            default:
                return constant.Value?.ToString() ?? "null";
        }
    }

    private static string FormatPrimitiveValue(TypedConstant constant)
    {
        return constant.Type?.SpecialType switch
        {
            SpecialType.System_String => $"\"{EscapeString(constant.Value?.ToString() ?? "")}\"",
            SpecialType.System_Char => $"'{EscapeChar((char)(constant.Value ?? '\0'))}'",
            SpecialType.System_Boolean => constant.Value?.ToString()?.ToLowerInvariant() ?? "false",
            SpecialType.System_Single => FormatFloat(constant.Value),
            SpecialType.System_Double => FormatDouble(constant.Value),
            SpecialType.System_Decimal => FormatDecimal(constant.Value),
            _ => constant.Value?.ToString() ?? "null"
        };
    }

    private static string FormatFloat(object value)
    {
        if (value == null) return "null";
        return ((float)value).ToString("G", CultureInfo.InvariantCulture) + "f";
    }

    private static string FormatDouble(object value)
    {
        if (value == null) return "null";
        return ((double)value).ToString("G", CultureInfo.InvariantCulture) + "d";
    }

    private static string FormatDecimal(object value)
    {
        if (value == null) return "null";
        return ((decimal)value).ToString("G", CultureInfo.InvariantCulture) + "m";
    }

    private static string FormatEnumValue(TypedConstant constant)
    {
        if (constant.Type == null)
        {
            return constant.Value?.ToString() ?? "null";
        }

        var enumType = new TypeDefinition(constant.Type);
        var enumValue = constant.Value?.ToString() ?? "0";

        // Try to get the enum member name
        if (constant.Type is INamedTypeSymbol namedType)
        {
            var members = namedType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.IsConst && f.ConstantValue?.Equals(constant.Value) == true);

            var member = members.FirstOrDefault();
            if (member != null)
            {
                return $"{enumType.FullName}.{member.Name}";
            }
        }

        return $"({enumType.FullName}){enumValue}";
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static char EscapeChar(char value)
    {
        return value switch
        {
            '\'' => '\\',
            '\\' => '\\',
            '\n' => 'n',
            '\r' => 'r',
            '\t' => 't',
            _ => value
        };
    }
}

/// <summary>
/// Represents a constructor argument for an attribute
/// </summary>
internal sealed class AttributeArgument
{
    public TypeDefinition Type { get; }
    public string Value { get; }

    public AttributeArgument(TypeDefinition type, string value)
    {
        Type = type;
        Value = value;
    }
}

/// <summary>
/// Represents a named argument (property or field) for an attribute
/// </summary>
internal sealed class AttributeNamedArgument
{
    public string Name { get; }
    public TypeDefinition Type { get; }
    public string Value { get; }

    public AttributeNamedArgument(string name, TypeDefinition type, string value)
    {
        Name = name;
        Type = type;
        Value = value;
    }
}
