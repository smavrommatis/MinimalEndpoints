using MinimalEndpoints.CodeGeneration.Utilities;
using System.Text;

namespace MinimalEndpoints.CodeGeneration.Tests.Utilities;

public class StringExtensionsTests
{
    #region Indent(int level) Tests

    [Fact]
    public void Indent_DefaultLevel_Returns4Spaces()
    {
        // Act
        var result = StringExtensions.Indent();

        // Assert
        Assert.Equal("    ", result);
    }

    [Fact]
    public void Indent_Level0_ReturnsEmptyString()
    {
        // Act
        var result = StringExtensions.Indent(0);

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Indent_Level2_Returns8Spaces()
    {
        // Act
        var result = StringExtensions.Indent(2);

        // Assert
        Assert.Equal("        ", result);
    }

    [Fact]
    public void Indent_Level5_Returns20Spaces()
    {
        // Act
        var result = StringExtensions.Indent(5);

        // Assert
        Assert.Equal("                    ", result);
    }

    [Fact]
    public void Indent_NegativeLevel_ReturnsEmptyString()
    {
        // Act
        var result = StringExtensions.Indent(-1);

        // Assert
        Assert.Equal("", result);
    }

    #endregion

    #region Indent(string code, int level) Tests

    [Fact]
    public void IndentString_SingleLine_AddsIndentation()
    {
        // Arrange
        var code = "public class Test";

        // Act
        var result = StringExtensions.Indent(code, 1);

        // Assert
        Assert.Equal("    public class Test", result);
    }

    [Fact]
    public void IndentString_MultipleLines_AddsIndentationToEach()
    {
        // Arrange
        var code = "public class Test\n{\n    void Method() { }\n}";

        // Act
        var result = StringExtensions.Indent(code, 1);

        // Assert
        var expected = "    public class Test\n    {\n        void Method() { }\n    }";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IndentString_EmptyString_ReturnsIndentation()
    {
        // Arrange
        var code = "";

        // Act
        var result = StringExtensions.Indent(code, 1);

        // Assert
        Assert.Equal("    ", result);
    }

    [Fact]
    public void IndentString_WithWindowsLineEndings_PreservesFormat()
    {
        // Arrange
        var code = "line1\r\nline2\r\nline3";

        // Act
        var result = StringExtensions.Indent(code, 2);

        // Assert
        Assert.Contains("        line1", result);
        Assert.Contains("        line2", result);
        Assert.Contains("        line3", result);
    }

    [Fact]
    public void IndentString_WithMacLineEndings_HandlesCorrectly()
    {
        // Arrange
        var code = "line1\rline2\rline3";

        // Act
        var result = StringExtensions.Indent(code, 1);

        // Assert
        Assert.Contains("    line1", result);
        Assert.Contains("    line2", result);
        Assert.Contains("    line3", result);
    }

    [Fact]
    public void IndentString_WithMixedLineEndings_HandlesCorrectly()
    {
        // Arrange
        var code = "line1\r\nline2\nline3\rline4";

        // Act
        var result = StringExtensions.Indent(code, 1);

        // Assert
        var lines = result.Split('\n');
        Assert.All(lines, line => Assert.StartsWith("    ", line));
    }

    [Fact]
    public void IndentString_Level0_ReturnsOriginal()
    {
        // Arrange
        var code = "public class Test";

        // Act
        var result = StringExtensions.Indent(code, 0);

        // Assert
        Assert.Equal(code, result);
    }

    [Fact]
    public void IndentString_WithTrailingNewline_PreservesIt()
    {
        // Arrange
        var code = "line1\nline2\n";

        // Act
        var result = StringExtensions.Indent(code, 1);

        // Assert
        Assert.EndsWith("\n    ", result);
    }

    #endregion

    #region AppendLineWithIndentation Tests

    [Fact]
    public void AppendLineWithIndentation_AddsIndentedLine()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        sb.AppendLineWithIndentation("test", 1);

        // Assert
        Assert.Equal("    test" + Environment.NewLine, sb.ToString());
    }

    [Fact]
    public void AppendLineWithIndentation_MultipleLines_AccumulatesCorrectly()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        sb.AppendLineWithIndentation("line1", 1);
        sb.AppendLineWithIndentation("line2", 2);
        sb.AppendLineWithIndentation("line3", 1);

        // Assert
        var result = sb.ToString();
        Assert.Contains("    line1", result);
        Assert.Contains("        line2", result);
        Assert.Contains("    line3", result);
    }

    [Fact]
    public void AppendLineWithIndentation_EmptyString_AddsOnlyIndentation()
    {
        // Arrange
        var sb = new StringBuilder();

        // Act
        sb.AppendLineWithIndentation("", 2);

        // Assert
        Assert.Equal("        " + Environment.NewLine, sb.ToString());
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Fact]
    public void Indent_VeryLargeLevel_HandlesWithoutException()
    {
        // Act
        var result = StringExtensions.Indent(100);

        // Assert
        Assert.Equal(400, result.Length);
        Assert.All(result, c => Assert.Equal(' ', c));
    }

    [Fact]
    public void IndentString_VeryLongLine_HandlesWithoutException()
    {
        // Arrange
        var code = new string('x', 10000);

        // Act
        var result = StringExtensions.Indent(code, 1);

        // Assert
        Assert.StartsWith("    ", result);
        Assert.Equal(10004, result.Length);
    }

    [Fact]
    public void IndentString_ManyLines_HandlesWithoutException()
    {
        // Arrange
        var lines = Enumerable.Range(0, 1000).Select(i => $"line{i}");
        var code = string.Join("\n", lines);

        // Act
        var result = StringExtensions.Indent(code, 1);

        // Assert
        var resultLines = result.Split('\n');
        Assert.Equal(1000, resultLines.Length);
        Assert.All(resultLines, line => Assert.StartsWith("    ", line));
    }

    #endregion
}

