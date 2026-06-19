using System.Globalization;
using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Models;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Endpoints;

internal static class EndpointUtilities
{
    public const string DefaultEntryPointMethodName = "Handle";
    public const string DefaultAsyncMethodName = "HandleAsync";

    /// <summary>
    /// Renders a parameter's optional default value as valid C# literal text (e.g. <c>1</c>,
    /// <c>"abc"</c>, <c>true</c>, <c>MyEnum.A</c>, <c>default</c>), or <c>null</c> when the
    /// parameter has no explicit default. The generated handler delegate must reproduce the
    /// default so ASP.NET Core treats the parameter as optional; a raw <c>ToString()</c> would
    /// emit <c>True</c>, unquoted strings, or culture-formatted numbers and fail to compile.
    /// </summary>
    public static string FormatDefaultValueLiteral(this IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
        {
            return null;
        }

        var value = parameter.ExplicitDefaultValue;
        var type = parameter.Type;

        if (value == null)
        {
            // `null` for reference/nullable types; `default` for non-nullable value types
            // (e.g. `CancellationToken ct = default`), which cannot be written as `null`.
            return type.IsValueType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T
                ? "default"
                : "null";
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            var enumTypeName = new TypeDefinition(enumType).FullName;
            var member = enumType.GetMembers()
                .OfType<IFieldSymbol>()
                .FirstOrDefault(f => f.IsConst && Equals(f.ConstantValue, value));

            return member != null
                ? $"{enumTypeName}.{member.Name}"
                : $"({enumTypeName}){FormatInvariant(value)}";
        }

        return type.SpecialType switch
        {
            SpecialType.System_String => $"\"{CSharpLiteral.EscapeStringContent((string)value)}\"",
            SpecialType.System_Char => $"'{CSharpLiteral.EscapeCharContent((char)value)}'",
            SpecialType.System_Boolean => (bool)value ? "true" : "false",
            SpecialType.System_Single => CSharpLiteral.FormatSingle((float)value),
            SpecialType.System_Double => CSharpLiteral.FormatDouble((double)value),
            SpecialType.System_Decimal => ((decimal)value).ToString(CultureInfo.InvariantCulture) + "m",
            SpecialType.System_Int64 => FormatInvariant(value) + "L",
            SpecialType.System_UInt64 => FormatInvariant(value) + "UL",
            SpecialType.System_UInt32 => FormatInvariant(value) + "U",
            _ => FormatInvariant(value)
        };
    }

    private static string FormatInvariant(object value) =>
        value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value.ToString();

    public static bool IsConfigurableEndpoint(this INamedTypeSymbol symbol)
    {
        return symbol.HasInterface(WellKnownTypes.RootNamespace, WellKnownTypes.ConfigurableEndpointTypeName);
    }

    public static IMethodSymbol FindEntryPointMethod(this INamedTypeSymbol symbol, string preferredMethodName)
    {
        // Search the type AND its base-class chain (declared members per type, most-derived first) so an
        // entry point inherited from a base class is found — the generated `instance.Handle(...)` call binds
        // to inherited members. Generic methods are excluded: the generated handler cannot supply type
        // arguments, so a generic `Handle<T>()` cannot be mapped (the analyzer reports MINEP010 for it).
        var publicMethods = EnumerateSelfAndBaseMethods(symbol)
            .Where(x => !x.IsStatic)
            .Where(x => !x.IsGenericMethod)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public);

        if (string.IsNullOrEmpty(preferredMethodName))
        {
            publicMethods = publicMethods
                .Where(x => x.Name is DefaultEntryPointMethodName or DefaultAsyncMethodName)
                .OrderByDescending(x => x.Name == DefaultAsyncMethodName); // Prefer HandleAsync if both exist
        }
        else
        {
            publicMethods = publicMethods
                .Where(x=> x.Name == preferredMethodName);
        }

        return publicMethods.FirstOrDefault();
    }

    /// <summary>
    /// Yields the methods declared on <paramref name="symbol"/> and each of its base types, most-derived
    /// first, so a derived class's own (or overriding) member is preferred over an inherited one of the
    /// same name. Declared-members-only per type; the loop supplies the inheritance walk that
    /// <see cref="INamedTypeSymbol.GetMembers()"/> does not.
    /// </summary>
    private static IEnumerable<IMethodSymbol> EnumerateSelfAndBaseMethods(INamedTypeSymbol symbol)
    {
        for (INamedTypeSymbol current = symbol; current != null; current = current.BaseType)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>())
            {
                yield return method;
            }
        }
    }

    /// <summary>
    /// Returns the first public, non-static candidate for the requested entry point name when EVERY such
    /// candidate is generic (so <see cref="FindEntryPointMethod"/> returned null only because the generic
    /// ones were filtered out); otherwise null. Lets the analyzer report the precise MINEP010 — naming the
    /// offending method — instead of the generic MINEP001.
    /// </summary>
    public static IMethodSymbol FindGenericOnlyEntryPointCandidate(
        this INamedTypeSymbol symbol, string preferredMethodName)
    {
        bool nameMatches(IMethodSymbol m) => string.IsNullOrEmpty(preferredMethodName)
            ? m.Name is DefaultEntryPointMethodName or DefaultAsyncMethodName
            : m.Name == preferredMethodName;

        var candidates = EnumerateSelfAndBaseMethods(symbol)
            .Where(x => !x.IsStatic && x.DeclaredAccessibility == Accessibility.Public)
            .Where(nameMatches)
            .ToList();

        return candidates.Count > 0 && candidates.All(x => x.IsGenericMethod) ? candidates[0] : null;
    }
}
