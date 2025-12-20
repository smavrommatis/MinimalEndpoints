using System.Text;
using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.Analyzers.Models;

/// <summary>
/// Represents a type definition that stores the fully qualified name and can render
/// it either as fully qualified or simplified based on available using directives.
/// </summary>
public class TypeDefinition
{
    private readonly string _fullName;
    private readonly Dictionary<int, string> _displayStringCache = new();

    /// <summary>
    /// The fully qualified type name (e.g., "System.Threading.Tasks.Task&lt;int&gt;")
    /// </summary>
    public string FullName => _fullName;

    public TypeDefinition(ITypeSymbol symbol)
    {
        _fullName = BuildFullTypeName(symbol);
    }

    /// <summary>
    /// Returns the type name simplified based on the provided using directives.
    /// If a namespace is in the usings set, it will be omitted from the type name.
    /// Results are cached for performance.
    /// </summary>
    /// <param name="availableUsings">Set of namespace strings that are available via using directives</param>
    /// <returns>Simplified type name (e.g., "Task&lt;int&gt;" instead of "System.Threading.Tasks.Task&lt;int&gt;")</returns>
    public string ToDisplayString(HashSet<string> availableUsings)
    {
        // Create a cache key based on the usings set
        var cacheKey = ComputeUsingsHashCode(availableUsings);

        if (_displayStringCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var result = SimplifyTypeName(_fullName, availableUsings);
        _displayStringCache[cacheKey] = result;
        return result;
    }

    private static int ComputeUsingsHashCode(HashSet<string> usings)
    {
        // Use XOR of hash codes for a simple, order-independent hash
        var hash = 0;
        foreach (var u in usings)
        {
            hash ^= u.GetHashCode();
        }
        return hash;
    }

    /// <summary>
    /// Returns the fully qualified type name.
    /// </summary>
    public override string ToString() => _fullName;

    private static string BuildFullTypeName(ITypeSymbol symbol)
    {
        // Handle special/built-in types
        if (symbol.SpecialType != SpecialType.None)
        {
            return GetSpecialTypeName(symbol.SpecialType);
        }

        // Handle tuple types
        if (symbol is INamedTypeSymbol { IsTupleType: true } tupleType)
        {
            return BuildTupleTypeName(tupleType);
        }

        // Handle array types
        if (symbol is IArrayTypeSymbol arrayType)
        {
            var elementTypeName = BuildFullTypeName(arrayType.ElementType);
            var rankSpecifier = new string(',', arrayType.Rank - 1);
            return $"{elementTypeName}[{rankSpecifier}]";
        }

        // Handle nullable value types (e.g., int?)
        if (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            symbol is INamedTypeSymbol nullableType)
        {
            var underlyingTypeName = BuildFullTypeName(nullableType.TypeArguments[0]);
            return $"{underlyingTypeName}?";
        }

        // Handle generic types
        if (symbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return BuildGenericTypeName(namedType);
        }

        // Handle regular types (including nested types)
        var namespaceName = symbol.ContainingNamespace?.ToDisplayString();
        var typeName = BuildNestedTypeName(symbol);

        return string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>"
            ? typeName
            : $"{namespaceName}.{typeName}";
    }

    private static string BuildTupleTypeName(INamedTypeSymbol tupleType)
    {
        var elements = tupleType.TupleElements;
        if (elements.IsDefaultOrEmpty)
        {
            // Fallback to regular generic type representation
            return BuildGenericTypeName(tupleType);
        }

        var elementTypes = elements
            .Select(e =>
            {
                var typeName = BuildFullTypeName(e.Type);
                // Include element name if it has one (e.g., (int id, string name))
                return string.IsNullOrEmpty(e.Name) || e.Name.StartsWith("Item")
                    ? typeName
                    : $"{typeName} {e.Name}";
            })
            .ToList();

        return $"({string.Join(", ", elementTypes)})";
    }

    private static string BuildGenericTypeName(INamedTypeSymbol namedType)
    {
        var namespaceName = namedType.ContainingNamespace?.ToDisplayString();
        var baseName = namedType.Name;

        // Build full base name with namespace
        var baseFullName = string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>"
            ? baseName
            : $"{namespaceName}.{baseName}";

        // Build generic arguments
        var typeArgs = namedType.TypeArguments;
        if (typeArgs.Length == 0)
        {
            return baseFullName;
        }

        var typeArgNames = typeArgs.Select(BuildFullTypeName).ToList();
        return $"{baseFullName}<{string.Join(", ", typeArgNames)}>";
    }

    private static string BuildNestedTypeName(ITypeSymbol symbol)
    {
        var parts = new List<string>();
        var current = symbol;

        while (current != null && current.ContainingType != null)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingType;
        }

        if (current != null)
        {
            parts.Insert(0, current.Name);
        }

        return string.Join(".", parts);
    }

    private static string GetSpecialTypeName(SpecialType specialType) => specialType switch
    {
        SpecialType.System_Object => "object",
        SpecialType.System_Void => "void",
        SpecialType.System_Boolean => "bool",
        SpecialType.System_Char => "char",
        SpecialType.System_SByte => "sbyte",
        SpecialType.System_Byte => "byte",
        SpecialType.System_Int16 => "short",
        SpecialType.System_UInt16 => "ushort",
        SpecialType.System_Int32 => "int",
        SpecialType.System_UInt32 => "uint",
        SpecialType.System_Int64 => "long",
        SpecialType.System_UInt64 => "ulong",
        SpecialType.System_Decimal => "decimal",
        SpecialType.System_Single => "float",
        SpecialType.System_Double => "double",
        SpecialType.System_String => "string",
        _ => "object"
    };

    private static string SimplifyTypeName(string fullTypeName, HashSet<string> availableUsings)
    {
        // Handle tuple types (int id, string name)
        if (fullTypeName.StartsWith("(") && fullTypeName.EndsWith(")"))
        {
            return SimplifyTupleType(fullTypeName, availableUsings);
        }

        // Handle nullable types
        if (fullTypeName.EndsWith("?"))
        {
            var underlyingType = fullTypeName.Substring(0, fullTypeName.Length - 1);
            return SimplifyTypeName(underlyingType, availableUsings) + "?";
        }

        // Handle array types
        if (fullTypeName.Contains("["))
        {
            var bracketIndex = fullTypeName.IndexOf('[');
            var elementType = fullTypeName.Substring(0, bracketIndex);
            var arrayPart = fullTypeName.Substring(bracketIndex);
            return SimplifyTypeName(elementType, availableUsings) + arrayPart;
        }

        // Handle generic types
        if (fullTypeName.Contains("<"))
        {
            return SimplifyGenericType(fullTypeName, availableUsings);
        }

        // Handle regular types (try to remove namespace if it's in usings)
        return SimplifyRegularType(fullTypeName, availableUsings);
    }

    private static string SimplifyTupleType(string tupleTypeName, HashSet<string> availableUsings)
    {
        // Remove outer parentheses
        var innerContent = tupleTypeName.Substring(1, tupleTypeName.Length - 2);

        // Parse tuple elements (considering nested tuples and generics)
        var elements = ParseTupleElements(innerContent);

        // Simplify each element
        var simplifiedElements = elements.Select(element =>
        {
            // Check if element has a name (e.g., "int id" or "string name")
            var parts = element.Trim().Split(new[] { ' ' }, 2);
            if (parts.Length == 2)
            {
                // Has name: "Type name"
                var simplifiedType = SimplifyTypeName(parts[0], availableUsings);
                return $"{simplifiedType} {parts[1]}";
            }
            else
            {
                // No name: just "Type"
                return SimplifyTypeName(element.Trim(), availableUsings);
            }
        }).ToList();

        return $"({string.Join(", ", simplifiedElements)})";
    }

    private static List<string> ParseTupleElements(string tupleContent)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var depth = 0; // Track nesting depth (parentheses and angle brackets)

        foreach (var ch in tupleContent)
        {
            if (ch == '(' || ch == '<')
            {
                depth++;
                current.Append(ch);
            }
            else if (ch == ')' || ch == '>')
            {
                depth--;
                current.Append(ch);
            }
            else if (ch == ',' && depth == 0)
            {
                // Top-level comma - split here
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    private static string SimplifyGenericType(string genericTypeName, HashSet<string> availableUsings)
    {
        var openBracket = genericTypeName.IndexOf('<');
        var basePart = genericTypeName.Substring(0, openBracket);
        var genericPart = genericTypeName.Substring(openBracket + 1, genericTypeName.Length - openBracket - 2);

        // Simplify the base type
        var simplifiedBase = SimplifyRegularType(basePart, availableUsings);

        // Parse and simplify type arguments
        var typeArgs = ParseGenericArguments(genericPart);
        var simplifiedArgs = typeArgs.Select(arg => SimplifyTypeName(arg.Trim(), availableUsings)).ToList();

        return $"{simplifiedBase}<{string.Join(", ", simplifiedArgs)}>";
    }

    private static string SimplifyRegularType(string typeName, HashSet<string> availableUsings)
    {
        // Built-in types don't have namespaces to remove
        if (IsBuiltInType(typeName))
        {
            return typeName;
        }

        // Try to find a namespace that matches and is in usings
        var lastDotIndex = typeName.LastIndexOf('.');
        if (lastDotIndex > 0)
        {
            var namespacePart = typeName.Substring(0, lastDotIndex);
            var typePart = typeName.Substring(lastDotIndex + 1);

            // Check if this namespace is available
            if (availableUsings.Contains(namespacePart))
            {
                return typePart;
            }
        }

        // Return as-is if namespace not in usings
        return typeName;
    }

    private static List<string> ParseGenericArguments(string genericArgs)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var depth = 0;

        foreach (var ch in genericArgs)
        {
            if (ch == '<')
            {
                depth++;
                current.Append(ch);
            }
            else if (ch == '>')
            {
                depth--;
                current.Append(ch);
            }
            else if (ch == ',' && depth == 0)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    private static bool IsBuiltInType(string typeName) => typeName switch
    {
        "object" or "void" or "bool" or "char" or "sbyte" or "byte" or "short" or "ushort" or
            "int" or "uint" or "long" or "ulong" or "decimal" or "float" or "double" or "string" => true,
        _ => false
    };

    public override bool Equals(object obj)
    {
        if (obj is TypeDefinition other)
        {
            return _fullName == other._fullName;
        }

        return false;
    }

    public override int GetHashCode() => _fullName.GetHashCode();
}
