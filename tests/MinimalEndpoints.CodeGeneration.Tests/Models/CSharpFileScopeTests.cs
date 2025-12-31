using MinimalEndpoints.CodeGeneration.Models;

namespace MinimalEndpoints.CodeGeneration.Tests.Models;

public class CSharpFileScopeTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Act
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");
        var result = scope.Build();

        // Assert
        Assert.Contains("namespace MyNamespace;", result);
        Assert.Contains("public static partial class MyClass", result);
    }

    [Fact]
    public void WithHeader_SetsHeader()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");
        var header = "// This is a header";

        // Act
        scope.WithHeader(header);
        var result = scope.Build();

        // Assert
        Assert.StartsWith("// This is a header", result);
    }

    [Fact]
    public void WithHeader_WithoutHeader_DoesNotIncludeHeader()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        var result = scope.Build();

        // Assert
        Assert.DoesNotContain("//", result);
    }

    [Fact]
    public void AddUsing_AddsSingleUsing()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        scope.AddUsing("System");
        var usings = scope.GetAvailableUsings();

        // Assert
        Assert.Contains("System", usings);
    }

    [Fact]
    public void AddUsing_RemovesTrailingSemicolon()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        scope.AddUsing("System;");
        var usings = scope.GetAvailableUsings();

        // Assert
        Assert.Contains("System", usings);
        Assert.DoesNotContain("System;", usings);
    }

    [Fact]
    public void AddUsing_PreventsDuplicates()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        scope.AddUsing("System");
        scope.AddUsing("System");
        var usings = scope.GetAvailableUsings();

        // Assert
        Assert.Single(usings);
    }

    [Fact]
    public void AddUsing_OrdersUsingsAlphabetically()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        scope.AddUsing("System.Linq");
        scope.AddUsing("System");
        scope.AddUsing("System.Collections");
        var result = scope.Build();

        // Assert
        var systemIndex = result.IndexOf("using System;", StringComparison.Ordinal);
        var collectionsIndex = result.IndexOf("using System.Collections;", StringComparison.Ordinal);
        var linqIndex = result.IndexOf("using System.Linq;", StringComparison.Ordinal);

        Assert.True(systemIndex < collectionsIndex, "System should come before System.Collections");
        Assert.True(collectionsIndex < linqIndex, "System.Collections should come before System.Linq");
    }

    [Fact]
    public void AddClassAttribute_AddsSingleAttribute()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        scope.AddClassAttribute("[GeneratedCode(\"Test\", \"1.0\")]");
        var result = scope.Build();

        // Assert
        Assert.Contains("[GeneratedCode(\"Test\", \"1.0\")]", result);
    }

    [Fact]
    public void AddClassAttribute_AddsMultipleAttributes()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        scope.AddClassAttribute("[GeneratedCode(\"Test\", \"1.0\")]");
        scope.AddClassAttribute("[Obsolete]");
        var result = scope.Build();

        // Assert
        Assert.Contains("[GeneratedCode(\"Test\", \"1.0\")]", result);
        Assert.Contains("[Obsolete]", result);
    }

    [Fact]
    public void AddMethod_AddsNewMethod()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        var method = scope.AddMethod("public static", "void", "TestMethod", "");
        method.AddLine("Console.WriteLine(\"Test\");");
        var result = scope.Build();

        // Assert
        Assert.Contains("public static void TestMethod()", result);
        Assert.Contains("Console.WriteLine(\"Test\");", result);
    }

    [Fact]
    public void AddMethod_WithDuplicateName_ReturnsSameMethod()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        var method1 = scope.AddMethod("public static", "void", "TestMethod", "");
        method1.AddLine("Console.WriteLine(\"First\");");

        var method2 = scope.AddMethod("public static", "void", "TestMethod", "");
        method2.AddLine("Console.WriteLine(\"Second\");");

        var result = scope.Build();

        // Assert
        Assert.Same(method1, method2);
        Assert.Contains("Console.WriteLine(\"First\");", result);
        Assert.Contains("Console.WriteLine(\"Second\");", result);
    }

    [Fact]
    public void Build_WithEmptyClass_GeneratesValidCode()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        var result = scope.Build();

        // Assert
        Assert.Contains("namespace MyNamespace;", result);
        Assert.Contains("public static partial class MyClass", result);
        Assert.Contains("{", result);
        Assert.Contains("}", result);
    }

    [Fact]
    public void Build_WithAllComponents_GeneratesCompleteFile()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        scope.WithHeader("// Auto-generated");
        scope.AddUsing("System");
        scope.AddUsing("System.Linq");
        scope.AddClassAttribute("[GeneratedCode(\"Test\", \"1.0\")]");

        var method = scope.AddMethod("public static", "void", "Method1", "");
        method.AddLine("Console.WriteLine(\"Hello\");");

        var result = scope.Build();

        // Assert
        Assert.Contains("// Auto-generated", result);
        Assert.Contains("using System;", result);
        Assert.Contains("using System.Linq;", result);
        Assert.Contains("[GeneratedCode(\"Test\", \"1.0\")]", result);
        Assert.Contains("namespace MyNamespace;", result);
        Assert.Contains("public static partial class MyClass", result);
        Assert.Contains("public static void Method1()", result);
        Assert.Contains("Console.WriteLine(\"Hello\");", result);
    }

    [Fact]
    public void Build_WithMultipleMethods_SeparatesWithBlankLine()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");

        // Act
        scope.AddMethod("public static", "void", "Method1", "").AddLine("// Method 1");
        scope.AddMethod("public static", "void", "Method2", "").AddLine("// Method 2");

        var result = scope.Build();
        var lines = result.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

        // Assert
        var method1EndIndex = -1;
        var method2StartIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("// Method 1"))
            {
                // Find closing brace of method1
                for (int j = i; j < lines.Length; j++)
                {
                    if (lines[j].Trim() == "}")
                    {
                        method1EndIndex = j;
                        break;
                    }
                }
            }
            if (lines[i].Contains("// Method 2"))
            {
                // Find start of method2
                for (int j = i; j >= 0; j--)
                {
                    if (lines[j].Contains("void Method2()"))
                    {
                        method2StartIndex = j;
                        break;
                    }
                }
            }
        }

        Assert.True(method1EndIndex > -1, "Method1 end not found");
        Assert.True(method2StartIndex > -1, "Method2 start not found");
        Assert.True(method2StartIndex - method1EndIndex >= 2, "Should have blank line between methods");
    }

    [Fact]
    public void GetAvailableUsings_ReturnsUsingsHashSet()
    {
        // Arrange
        var scope = new CSharpFileScope("public static", "MyNamespace", "MyClass");
        scope.AddUsing("System");
        scope.AddUsing("System.Linq");

        // Act
        var usings = scope.GetAvailableUsings();

        // Assert
        Assert.IsType<HashSet<string>>(usings);
        Assert.Equal(2, usings.Count);
    }
}

