using System.Text;
using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.Models;

/// <summary>
/// Represents a type definition that stores the fully qualified name and can render
/// it either as fully qualified or simplified based on available using directives.
/// </summary>
internal class TypeDefinition
{
    private readonly string _fullName;

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
    /// </summary>
    /// <remarks>
    /// A pure function of <see cref="_fullName"/> and the usings set. It used to memoize results
    /// in a dictionary keyed by an XOR of the usings' hash codes with no set-equality check, which
    /// (a) is a latent correctness landmine — a hash collision between two different usings sets
    /// would return a name simplified against the wrong set — and (b) mutated a cached pipeline
    /// object during output. There is exactly one usings set per generated file, so the cache only
    /// ever added overhead; it was removed.
    /// </remarks>
    /// <param name="availableUsings">Set of namespace strings that are available via using directives</param>
    /// <returns>Simplified type name (e.g., "Task&lt;int&gt;" instead of "System.Threading.Tasks.Task&lt;int&gt;")</returns>
    public string ToDisplayString(HashSet<string> availableUsings) => SimplifyTypeName(_fullName, availableUsings);

    /// <summary>
    /// Returns the fully qualified type name.
    /// </summary>
    public override string ToString() => _fullName;

    private static string BuildFullTypeName(ITypeSymbol symbol)
    {
        // Handle special/built-in types that have a C# keyword alias (or DateTime).
        if (symbol.SpecialType != SpecialType.None)
        {
            var specialName = GetSpecialTypeName(symbol.SpecialType);
            if (specialName != null)
            {
                return specialName;
            }
            // Other special types (IDisposable, non-generic IEnumerable, IntPtr, Enum, Array, …)
            // have no keyword: fall through to normal qualified-name rendering instead of
            // collapsing to "object", which produced non-compiling handler signatures.
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

        // Handle all named types (generic or not, nested or not) including the full
        // containing-type chain so nested types like Outer<T>.Inner<U> render correctly.
        if (symbol is INamedTypeSymbol namedType)
        {
            return BuildNamedTypeName(namedType);
        }

        // Fallback for type parameters and any other symbol kind.
        return symbol.Name;
    }

    private static string BuildTupleTypeName(INamedTypeSymbol tupleType)
    {
        var elements = tupleType.TupleElements;
        if (elements.IsDefaultOrEmpty)
        {
            // Fallback to regular named-type representation
            return BuildNamedTypeName(tupleType);
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

    private static string BuildNamedTypeName(INamedTypeSymbol named)
    {
        // Qualifier: the containing type (recursively, preserving ITS own generic arguments) when
        // this is a nested type, otherwise the namespace. Walking ContainingType is what makes
        // Outer<T>.Inner<U> render in full instead of losing the container.
        string qualifier;
        if (named.ContainingType != null)
        {
            qualifier = BuildFullTypeName(named.ContainingType);
        }
        else
        {
            var namespaceName = named.ContainingNamespace?.ToDisplayString();
            qualifier = string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>"
                ? null
                : namespaceName;
        }

        var typeArgs = named.TypeArguments;
        var self = typeArgs.Length == 0
            ? named.Name
            : $"{named.Name}<{string.Join(", ", typeArgs.Select(BuildFullTypeName))}>";

        return qualifier == null ? self : $"{qualifier}.{self}";
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
        SpecialType.System_DateTime => "System.DateTime",
        // No C# keyword: return null so the caller renders the fully-qualified name instead of "object".
        _ => null
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

        // Find the '>' that MATCHES the first '<' by depth-tracking, rather than assuming it is the
        // final character. The latter corrupts a nested type on a CLOSED generic outer (e.g.
        // "Outer<int>.Inner" or "Dictionary<string, int>.Enumerator"), whose '>' is followed by a
        // ".Nested" suffix — slicing to the end re-parsed that suffix as part of the type arguments
        // and emitted invalid C# ("Outer<int>.Inne>").
        var closeBracket = FindMatchingCloseBracket(genericTypeName, openBracket);

        var basePart = genericTypeName.Substring(0, openBracket);
        var genericPart = genericTypeName.Substring(openBracket + 1, closeBracket - openBracket - 1);
        var suffix = closeBracket + 1 < genericTypeName.Length
            ? genericTypeName.Substring(closeBracket + 1)
            : string.Empty;

        // Simplify the base type
        var simplifiedBase = SimplifyRegularType(basePart, availableUsings);

        // Parse and simplify type arguments
        var typeArgs = ParseGenericArguments(genericPart);
        var simplifiedArgs = typeArgs.Select(arg => SimplifyTypeName(arg.Trim(), availableUsings)).ToList();

        // Recurse into any trailing nested-type segment (".Inner", ".Inner<U>", …) so it is simplified
        // with the same rules; the leading '.' is preserved verbatim.
        var simplifiedSuffix = suffix.Length > 0 && suffix[0] == '.'
            ? "." + SimplifyTypeName(suffix.Substring(1), availableUsings)
            : suffix;

        return $"{simplifiedBase}<{string.Join(", ", simplifiedArgs)}>{simplifiedSuffix}";
    }

    /// <summary>
    /// Returns the index of the <c>&gt;</c> that closes the <c>&lt;</c> at <paramref name="openIndex"/>,
    /// accounting for nested generics. Falls back to the last character for malformed input (the
    /// previous always-last-char assumption), so a well-formed open generic is unaffected.
    /// </summary>
    private static int FindMatchingCloseBracket(string text, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < text.Length; i++)
        {
            if (text[i] == '<')
            {
                depth++;
            }
            else if (text[i] == '>')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return text.Length - 1;
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
