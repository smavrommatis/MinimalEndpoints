using System.Text;
using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Tests.Models;

public class CSharpFileMethodBuilderTests
{
    [Fact]
    public void Constructor_StoresAllParameters()
    {
        // Act
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "string param");
        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        Assert.Contains("public static void TestMethod(string param)", result);
    }

    [Fact]
    public void AddEmptyLine_AddsBlankLine()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "");

        // Act
        builder.AddLine("Console.WriteLine(\"Before\");");
        builder.AddEmptyLine();
        builder.AddLine("Console.WriteLine(\"After\");");

        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();
        var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        // Assert
        var beforeIndex = Array.FindIndex(lines, l => l.Contains("Before"));
        var afterIndex = Array.FindIndex(lines, l => l.Contains("After"));

        Assert.True(afterIndex - beforeIndex == 2, "Should have exactly one blank line between statements");
        Assert.True(string.IsNullOrWhiteSpace(lines[beforeIndex + 1]), "Line between should be empty");
    }

    [Fact]
    public void AddLine_AddsSimpleLine()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "");

        // Act
        builder.AddLine("Console.WriteLine(\"Test\");");
        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        Assert.Contains("Console.WriteLine(\"Test\");", result);
    }

    [Fact]
    public void AddLine_WithAdditionalIndentation_AddsExtraIndent()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "");

        // Act
        builder.AddLine("if (true)", 0);
        builder.AddLine("{", 0);
        builder.AddLine("Console.WriteLine(\"Test\");", 1);
        builder.AddLine("}", 0);

        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var ifLine = Array.Find(lines, l => l.Contains("if (true)"));
        var consoleLine = Array.Find(lines, l => l.Contains("Console.WriteLine"));

        Assert.NotNull(ifLine);
        Assert.NotNull(consoleLine);

        var ifIndent = ifLine.Length - ifLine.TrimStart().Length;
        var consoleIndent = consoleLine.Length - consoleLine.TrimStart().Length;

        Assert.True(consoleIndent > ifIndent, "Inner line should have more indentation");
    }

    [Fact]
    public void AddLine_MultipleLines_PreservesOrder()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "");

        // Act
        builder.AddLine("// Line 1");
        builder.AddLine("// Line 2");
        builder.AddLine("// Line 3");

        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        var line1Index = result.IndexOf("// Line 1", StringComparison.Ordinal);
        var line2Index = result.IndexOf("// Line 2", StringComparison.Ordinal);
        var line3Index = result.IndexOf("// Line 3", StringComparison.Ordinal);

        Assert.True(line1Index < line2Index, "Line 1 should come before Line 2");
        Assert.True(line2Index < line3Index, "Line 2 should come before Line 3");
    }

    [Fact]
    public void Build_GeneratesMethodSignature()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "Task<int>", "GetValueAsync", "int id, string name");

        // Act
        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        Assert.Contains("public static Task<int> GetValueAsync(int id, string name)", result);
    }

    [Fact]
    public void Build_GeneratesMethodBody()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "");
        builder.AddLine("var x = 1;");
        builder.AddLine("return x;");

        // Act
        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        Assert.Contains("var x = 1;", result);
        Assert.Contains("return x;", result);
        Assert.Contains("{", result);
        Assert.Contains("}", result);
    }

    [Fact]
    public void Build_WithEmptyBody_GeneratesEmptyMethod()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "");

        // Act
        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        Assert.Contains("public static void TestMethod()", result);
        Assert.Contains("{", result);
        Assert.Contains("}", result);
    }

    [Fact]
    public void Build_WithDifferentIndentLevel_AdjustsIndentation()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public", "void", "TestMethod", "");
        builder.AddLine("Console.WriteLine(\"Test\");");

        // Act - Build with indent level 2
        var sb = new StringBuilder();
        builder.Build(sb, indentLevel: 2);
        var result = sb.ToString();
        var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        // Assert
        var methodLine = Array.Find(lines, l => l.Contains("void TestMethod"));
        Assert.NotNull(methodLine);

        var indent = methodLine.Length - methodLine.TrimStart().Length;
        Assert.True(indent >= 8, "Should have at least 2 levels of indentation (8 spaces)");
    }

    [Fact]
    public void Build_WithDifferentModifiers_GeneratesCorrectly()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("private async", "Task", "ProcessAsync", "");
        builder.AddLine("await Task.Delay(100);");

        // Act
        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        Assert.Contains("private async Task ProcessAsync()", result);
    }

    [Fact]
    public void AddLine_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "");

        // Act
        var result = builder
            .AddLine("var x = 1;")
            .AddLine("var y = 2;")
            .AddLine("return x + y;");

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void AddEmptyLine_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "TestMethod", "");

        // Act
        var result = builder
            .AddLine("var x = 1;")
            .AddEmptyLine()
            .AddLine("var y = 2;");

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Build_WithComplexBody_GeneratesProperIndentation()
    {
        // Arrange
        var builder = new CSharpFileMethodBuilder("public static", "void", "ComplexMethod", "");
        builder.AddLine("if (condition)", 0);
        builder.AddLine("{", 0);
        builder.AddLine("foreach (var item in items)", 1);
        builder.AddLine("{", 1);
        builder.AddLine("Process(item);", 2);
        builder.AddLine("}", 1);
        builder.AddLine("}", 0);

        // Act
        var sb = new StringBuilder();
        builder.Build(sb);
        var result = sb.ToString();

        // Assert
        Assert.Contains("if (condition)", result);
        Assert.Contains("foreach (var item in items)", result);
        Assert.Contains("Process(item);", result);
    }
}

