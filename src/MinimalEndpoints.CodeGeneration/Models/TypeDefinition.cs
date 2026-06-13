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

    /// <summary>
    /// The format used to render the fully-qualified name at construction time, while the
    /// <see cref="ITypeSymbol"/> is still available. Roslyn's own renderer is authoritative for
    /// arrays (rank order), tuples (named and unnamed), pointers, nullable reference and value
    /// types, and nested generics — eliminating the class of bugs that came from hand-building the
    /// name string. <c>UseSpecialTypes</c> yields C# keyword aliases (int, string, object, …);
    /// <c>IncludeNullableReferenceTypeModifier</c> preserves the <c>?</c> on nullable reference
    /// types (which the namespace-simplification step below relies on but never produces itself).
    /// </summary>
    private static readonly SymbolDisplayFormat FullyQualifiedFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static string BuildFullTypeName(ITypeSymbol symbol) => symbol.ToDisplayString(FullyQualifiedFormat);

    private static string SimplifyTypeName(string fullTypeName, HashSet<string> availableUsings)
    {
        // Handle tuple types (int id, string name)
        if (fullTypeName.StartsWith("(") && fullTypeName.EndsWith(")"))
        {
            return SimplifyTupleType(fullTypeName, availableUsings);
        }

        // Handle nullable types (value or reference): peel the trailing '?' and recurse.
        if (fullTypeName.EndsWith("?"))
        {
            var underlyingType = fullTypeName.Substring(0, fullTypeName.Length - 1);
            return SimplifyTypeName(underlyingType, availableUsings) + "?";
        }

        var angleIndex = fullTypeName.IndexOf('<');
        var bracketIndex = fullTypeName.IndexOf('[');

        // A generic type's '<' always precedes any top-level array '[' suffix, so when both are
        // present and '<' comes first this is a generic. SimplifyGenericType depth-tracks the
        // matching '>' and recurses into both the type arguments and any trailing array suffix —
        // testing '[' first sliced into an interior array argument like List<int[,]> (emitting
        // invalid C# such as "List<in>[,]>").
        if (angleIndex >= 0 && (bracketIndex < 0 || angleIndex < bracketIndex))
        {
            return SimplifyGenericType(fullTypeName, availableUsings);
        }

        // Handle array types (the '[' is the start of the array-rank suffix on a non-generic element)
        if (bracketIndex >= 0)
        {
            var elementType = fullTypeName.Substring(0, bracketIndex);
            var arrayPart = fullTypeName.Substring(bracketIndex);
            return SimplifyTypeName(elementType, availableUsings) + arrayPart;
        }

        // Handle regular types (try to remove namespace if it's in usings)
        return SimplifyRegularType(fullTypeName, availableUsings);
    }

    private static string SimplifyTupleType(string tupleTypeName, HashSet<string> availableUsings)
    {
        // Remove outer parentheses
        var innerContent = tupleTypeName.Substring(1, tupleTypeName.Length - 2);

        // Parse tuple elements (considering nested tuples, generics, and arrays)
        var elements = ParseTupleElements(innerContent);

        // Simplify each element
        var simplifiedElements = elements.Select(element =>
        {
            var trimmed = element.Trim();

            // A tuple element is "Type" or "Type name". The element NAME (if any) is a trailing
            // identifier separated from the type by a space at bracket-depth 0 — the only such space
            // in a well-formed element. Splitting on the FIRST space instead landed inside a
            // multi-argument generic's "<a, b>" and corrupted both named and unnamed elements.
            var nameSeparator = IndexOfTopLevelSpace(trimmed);
            if (nameSeparator > 0)
            {
                var simplifiedType = SimplifyTypeName(trimmed.Substring(0, nameSeparator), availableUsings);
                return $"{simplifiedType} {trimmed.Substring(nameSeparator + 1)}";
            }

            // No name: just "Type"
            return SimplifyTypeName(trimmed, availableUsings);
        }).ToList();

        return $"({string.Join(", ", simplifiedElements)})";
    }

    /// <summary>
    /// Index of the first space at bracket-depth 0 (outside <c>&lt;&gt;</c>, <c>()</c>, and
    /// <c>[]</c>), or -1. A tuple element's only top-level space separates its type from its
    /// optional name; spaces inside a generic argument list or a nested tuple are nested and ignored.
    /// </summary>
    private static int IndexOfTopLevelSpace(string text)
    {
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '<':
                case '(':
                case '[':
                    depth++;
                    break;
                case '>':
                case ')':
                case ']':
                    depth--;
                    break;
                case ' ' when depth == 0:
                    return i;
            }
        }

        return -1;
    }

    private static List<string> ParseTupleElements(string tupleContent)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var depth = 0; // Track nesting depth (parentheses, angle brackets, and square brackets)

        foreach (var ch in tupleContent)
        {
            switch (ch)
            {
                case '(':
                case '<':
                case '[':
                    depth++;
                    current.Append(ch);
                    break;
                case ')':
                case '>':
                case ']':
                    depth--;
                    current.Append(ch);
                    break;
                case ',' when depth == 0:
                    // Top-level comma - split here
                    result.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(ch);
                    break;
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
        // with the same rules; an array-rank suffix ("[]", "[,]") is preserved verbatim.
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
        var depth = 0; // Track nesting depth (angle brackets, parentheses, and square brackets)

        foreach (var ch in genericArgs)
        {
            switch (ch)
            {
                case '<':
                case '(':
                case '[':
                    depth++;
                    current.Append(ch);
                    break;
                case '>':
                case ')':
                case ']':
                    depth--;
                    current.Append(ch);
                    break;
                case ',' when depth == 0:
                    result.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(ch);
                    break;
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
