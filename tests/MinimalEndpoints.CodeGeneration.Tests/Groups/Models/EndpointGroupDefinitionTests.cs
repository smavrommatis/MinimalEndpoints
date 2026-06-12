using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Groups.Models;
using static MinimalEndpoints.Tests.Common.SymbolTestHelpers;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups.Models;

public class EndpointGroupDefinitionTests
{
    [Fact]
    public void Constructor_WithMapGroupAttribute_InitializesProperties()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = new EndpointGroupDefinition(classSymbol, attribute);

        // Assert
        Assert.Equal("/api", result.Prefix);
        Assert.NotNull(result.ClassType);
        Assert.Contains("TestGroup", result.ClassType.FullName);
        Assert.Equal("TestGroup", result.Name);
        Assert.Null(result.ParentGroupName);
    }

    [Fact]
    public void Constructor_WithParentGroup_CapturesParentGroupName()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ParentGroup
{
}

[MapGroup(""/v1"", ParentGroup = typeof(ParentGroup))]
public class ChildGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "ChildGroup");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = new EndpointGroupDefinition(classSymbol, attribute);

        // Assert — the parent is captured as a fully-qualified-name string (no symbol retained),
        // keyed identically to the parent group's own ClassType.FullName.
        Assert.Equal("TestApp.ParentGroup", result.ParentGroupName);
    }

    [Fact]
    public void Constructor_WithEmptyPrefix_KeepsEmptyString()
    {
        // An explicit empty prefix is preserved as "". The constructor's `?? "/"` fallback only
        // fires when the prefix argument is absent/non-string, which valid C# cannot express
        // (MapGroupAttribute requires a string prefix), so it is not exercised here.
        var code = @"
namespace TestApp;

[MapGroup("""")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = new EndpointGroupDefinition(classSymbol, attribute);

        // Assert
        Assert.Equal("", result.Prefix);
    }

    [Fact]
    public void Factory_Predicate_WithMapGroupAttribute_ReturnsTrue()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointGroupDefinition.Factory.Predicate(attribute);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Factory_Predicate_WithNonMapGroupAttribute_ReturnsFalse()
    {
        // Arrange
        var code = @"
using System;

namespace TestApp;

[Obsolete]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointGroupDefinition.Factory.Predicate(attribute);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Factory_Create_CreatesGroupDefinition()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();

        // Act
        var result = EndpointGroupDefinition.Factory.Create(classSymbol, attribute);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<EndpointGroupDefinition>(result);
    }

    [Fact]
    public void VariableName_GeneratesCorrectName()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act
        var result = group.VariableName;

        // Assert
        Assert.Equal("group_TestApp_TestGroup", result);
    }

    [Fact]
    public void VariableName_WithDotsInNamespace_ReplacesWithUnderscores()
    {
        // Arrange
        var code = @"
namespace My.Test.App;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act
        var result = group.VariableName;

        // Assert
        Assert.Equal("group_My_Test_App_TestGroup", result);
        Assert.DoesNotContain(".", result);
    }

    [Fact]
    public void VariableName_WithNestedTypes_ReplacesPlusWithUnderscores()
    {
        // Arrange
        var code = @"
namespace TestApp;

public class OuterClass
{
    [MapGroup(""/api"")]
    public class TestGroup
    {
    }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetNestedClassSymbol(compilation, "OuterClass", "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act
        var result = group.VariableName;

        // Assert
        Assert.Contains("_OuterClass_TestGroup", result);
        Assert.DoesNotContain("+", result);
    }

    [Fact]
    public void VariableName_CachesResult()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act
        var result1 = group.VariableName;
        var result2 = group.VariableName;

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void MappingGroupMethodName_GeneratesCorrectName()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act
        var result = group.MappingGroupMethodName;

        // Assert
        Assert.Equal("MapGroup__TestApp_TestGroup", result);
    }

    [Fact]
    public void MappingGroupMethodName_CachesResult()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act
        var result1 = group.MappingGroupMethodName;
        var result2 = group.MappingGroupMethodName;

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void IsConfigurable_WithConfigurableGroup_ReturnsTrue()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder builder) { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act & Assert
        Assert.True(group.IsConfigurable);
    }
}
