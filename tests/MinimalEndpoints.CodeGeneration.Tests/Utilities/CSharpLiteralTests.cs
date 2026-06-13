using System.Globalization;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Tests.Utilities;

/// <summary>
/// Direct unit tests for <see cref="CSharpLiteral"/> — the escaping/literal-formatting helper that
/// keeps user route patterns, verbs, and default values from breaking the generated file's syntax.
/// </summary>
public class CSharpLiteralTests
{
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("with\"quote", "with\\\"quote")]
    [InlineData("back\\slash", "back\\\\slash")]
    [InlineData("line\nbreak", "line\\nbreak")]
    [InlineData("tab\there", "tab\\there")]
    [InlineData("carriage\rreturn", "carriage\\rreturn")]
    [InlineData("it's fine", "it's fine")] // a single quote needs no escaping inside a "..." literal
    public void EscapeStringContent_EscapesSpecials(string input, string expected)
    {
        Assert.Equal(expected, CSharpLiteral.EscapeStringContent(input));
    }

    [Fact]
    public void EscapeStringContent_ControlCharacter_EmitsUnicodeEscape()
    {
        // U+0007 (bell) has a dedicated \a escape; U+0001 has none and must become \uXXXX.
        Assert.Equal("\\u0001", CSharpLiteral.EscapeStringContent(""));
        Assert.Equal("\\a", CSharpLiteral.EscapeStringContent("\a"));
    }

    [Fact]
    public void EscapeStringContent_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CSharpLiteral.EscapeStringContent(null));
        Assert.Equal(string.Empty, CSharpLiteral.EscapeStringContent(string.Empty));
    }

    [Fact]
    public void EscapeCharContent_EscapesSingleQuoteAndBackslash()
    {
        Assert.Equal("\\'", CSharpLiteral.EscapeCharContent('\''));
        Assert.Equal("\\\\", CSharpLiteral.EscapeCharContent('\\'));
        Assert.Equal("\"", CSharpLiteral.EscapeCharContent('"')); // a double quote needs no escaping in '...'
        Assert.Equal("a", CSharpLiteral.EscapeCharContent('a'));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.1f)]
    [InlineData(1.5f)]
    [InlineData(-3.25f)]
    [InlineData(float.Epsilon)]
    [InlineData(float.MaxValue)]
    [InlineData(0.123456789f)]
    public void FormatSingle_IsRoundTrippable_WithFloatSuffix(float value)
    {
        var literal = CSharpLiteral.FormatSingle(value);

        Assert.EndsWith("f", literal);
        var parsed = float.Parse(literal.Substring(0, literal.Length - 1), CultureInfo.InvariantCulture);
        Assert.Equal(value, parsed);
    }

    [Fact]
    public void FormatSingle_NonFinite_EmitsNamedConstants()
    {
        Assert.Equal("float.NaN", CSharpLiteral.FormatSingle(float.NaN));
        Assert.Equal("float.PositiveInfinity", CSharpLiteral.FormatSingle(float.PositiveInfinity));
        Assert.Equal("float.NegativeInfinity", CSharpLiteral.FormatSingle(float.NegativeInfinity));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(0.1d)]
    [InlineData(-3.25d)]
    [InlineData(double.Epsilon)]
    [InlineData(double.MaxValue)]
    [InlineData(0.1234567890123456d)]
    public void FormatDouble_IsRoundTrippable_WithDoubleSuffix(double value)
    {
        var literal = CSharpLiteral.FormatDouble(value);

        Assert.EndsWith("d", literal);
        var parsed = double.Parse(literal.Substring(0, literal.Length - 1), CultureInfo.InvariantCulture);
        Assert.Equal(value, parsed);
    }

    [Fact]
    public void FormatDouble_NonFinite_EmitsNamedConstants()
    {
        Assert.Equal("double.NaN", CSharpLiteral.FormatDouble(double.NaN));
        Assert.Equal("double.PositiveInfinity", CSharpLiteral.FormatDouble(double.PositiveInfinity));
        Assert.Equal("double.NegativeInfinity", CSharpLiteral.FormatDouble(double.NegativeInfinity));
    }
}
