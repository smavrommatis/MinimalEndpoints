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
            SpecialType.System_Single => ((float)value).ToString("R", CultureInfo.InvariantCulture) + "f",
            SpecialType.System_Double => ((double)value).ToString("R", CultureInfo.InvariantCulture) + "d",
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
        var publicMethods = symbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => !x.IsStatic)
            .Where(x => x.DeclaredAccessibility == Accessibility.Public);

        if (string.IsNullOrEmpty(preferredMethodName))
        {
            publicMethods = publicMethods
                .Where(x => x.Name is DefaultEntryPointMethodName or DefaultAsyncMethodName)
                .OrderByDescending(x => x.Name == DefaultAsyncMethodName) // Prefer async method if both exist
                .ThenByDescending(x => x.Name.EndsWith("Async"));
        }
        else
        {
            publicMethods = publicMethods
                .Where(x=> x.Name == preferredMethodName);
        }

        return publicMethods.FirstOrDefault();
    }
}
