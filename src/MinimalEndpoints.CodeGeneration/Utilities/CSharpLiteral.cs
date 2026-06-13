using System.Globalization;
using System.Text;

namespace MinimalEndpoints.CodeGeneration.Utilities;

/// <summary>
/// Produces valid C# literal text for emission into generated source. Centralizes string and
/// character escaping so route patterns, group prefixes, HTTP-method strings, and attribute
/// argument values can never break the generated file's syntax (CS1009/CS1010/CS1056) when a
/// user value contains a quote, backslash, or control character.
/// </summary>
internal static class CSharpLiteral
{
    /// <summary>
    /// Escapes the inner content of a double-quoted string literal (no surrounding quotes).
    /// </summary>
    public static string EscapeStringContent(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            sb.Append(Escape(c, isCharLiteral: false));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a single character for emission inside a single-quoted char literal
    /// (no surrounding quotes).
    /// </summary>
    public static string EscapeCharContent(char value) => Escape(value, isCharLiteral: true);

    /// <summary>
    /// Renders a <see cref="float"/> as valid C# literal text. Non-finite values (NaN, ±Infinity)
    /// have no literal form — their <c>ToString</c> yields the bare words "NaN"/"Infinity", which
    /// with the <c>f</c> suffix produce uncompilable identifiers (CS0103) — so they are emitted as
    /// the named <c>float</c> constants instead. Finite values use the "G9" specifier, the shortest
    /// guaranteed round-trippable form for <see cref="float"/> on every host runtime (the "R"
    /// specifier is documented as NOT reliably round-trippable for <see cref="float"/> on .NET
    /// Framework, where the generator runs inside the VS compiler).
    /// </summary>
    public static string FormatSingle(float value)
    {
        if (float.IsNaN(value)) return "float.NaN";
        if (float.IsPositiveInfinity(value)) return "float.PositiveInfinity";
        if (float.IsNegativeInfinity(value)) return "float.NegativeInfinity";
        return value.ToString("G9", CultureInfo.InvariantCulture) + "f";
    }

    /// <summary>
    /// Renders a <see cref="double"/> as valid C# literal text. See <see cref="FormatSingle"/> for
    /// the non-finite rationale; "G17" is the round-trippable specifier for <see cref="double"/>.
    /// </summary>
    public static string FormatDouble(double value)
    {
        if (double.IsNaN(value)) return "double.NaN";
        if (double.IsPositiveInfinity(value)) return "double.PositiveInfinity";
        if (double.IsNegativeInfinity(value)) return "double.NegativeInfinity";
        return value.ToString("G17", CultureInfo.InvariantCulture) + "d";
    }

    private static string Escape(char c, bool isCharLiteral)
    {
        switch (c)
        {
            case '\\': return "\\\\";
            case '\0': return "\\0";
            case '\a': return "\\a";
            case '\b': return "\\b";
            case '\f': return "\\f";
            case '\n': return "\\n";
            case '\r': return "\\r";
            case '\t': return "\\t";
            case '\v': return "\\v";
            // A quote only needs escaping inside its own literal kind.
            case '"': return isCharLiteral ? "\"" : "\\\"";
            case '\'': return isCharLiteral ? "\\'" : "'";
            default:
                // Remaining control characters cannot appear verbatim in a literal — emit \uXXXX.
                return char.IsControl(c)
                    ? "\\u" + ((int)c).ToString("X4", CultureInfo.InvariantCulture)
                    : c.ToString();
        }
    }
}
