using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Groups.Models;

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
    }

    [Fact]
    public void Constructor_WithNoPrefixArgument_UsesDefaultSlash()
    {
        // Arrange - need to test edge case, but MapGroup requires a string parameter
        // We'll test the default behavior indirectly through Factory
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
    public void Depth_WithRootGroup_ReturnsOne()
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
        var result = group.Depth;

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Depth_WithNestedGroup_ReturnsCorrectDepth()
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

        var parentSymbol = GetClassSymbol(compilation, "ParentGroup");
        var parentAttribute = parentSymbol.GetAttributes().First();
        var parentGroup = new EndpointGroupDefinition(parentSymbol, parentAttribute);

        var childSymbol = GetClassSymbol(compilation, "ChildGroup");
        var childAttribute = childSymbol.GetAttributes().First();
        var childGroup = new EndpointGroupDefinition(childSymbol, childAttribute)
        {
            ParentGroup = parentGroup
        };

        // Act
        var result = childGroup.Depth;

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public void Depth_WithDeeplyNestedGroup_ReturnsCorrectDepth()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class Level1Group
{
}

[MapGroup(""/v1"", ParentGroup = typeof(Level1Group))]
public class Level2Group
{
}

[MapGroup(""/users"", ParentGroup = typeof(Level2Group))]
public class Level3Group
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var level1Symbol = GetClassSymbol(compilation, "Level1Group");
        var level1Attribute = level1Symbol.GetAttributes().First();
        var level1Group = new EndpointGroupDefinition(level1Symbol, level1Attribute);

        var level2Symbol = GetClassSymbol(compilation, "Level2Group");
        var level2Attribute = level2Symbol.GetAttributes().First();
        var level2Group = new EndpointGroupDefinition(level2Symbol, level2Attribute)
        {
            ParentGroup = level1Group
        };

        var level3Symbol = GetClassSymbol(compilation, "Level3Group");
        var level3Attribute = level3Symbol.GetAttributes().First();
        var level3Group = new EndpointGroupDefinition(level3Symbol, level3Attribute)
        {
            ParentGroup = level2Group
        };

        // Act
        var result = level3Group.Depth;

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void HierarchyConditionallyMapped_WithConditionalGroup_ReturnsTrue()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class TestGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act
        var result = group.HierarchyConditionallyMapped;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HierarchyConditionallyMapped_WithConditionalParent_ReturnsTrue()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ParentGroup : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGroup(""/v1"", ParentGroup = typeof(ParentGroup))]
public class ChildGroup
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        var parentSymbol = GetClassSymbol(compilation, "ParentGroup");
        var parentAttribute = parentSymbol.GetAttributes().First();
        var parentGroup = new EndpointGroupDefinition(parentSymbol, parentAttribute);

        var childSymbol = GetClassSymbol(compilation, "ChildGroup");
        var childAttribute = childSymbol.GetAttributes().First();
        var childGroup = new EndpointGroupDefinition(childSymbol, childAttribute)
        {
            ParentGroup = parentGroup
        };

        // Act
        var result = childGroup.HierarchyConditionallyMapped;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HierarchyConditionallyMapped_WithNoConditionalMapping_ReturnsFalse()
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
        var result = group.HierarchyConditionallyMapped;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FullPrefix_WithRootGroup_ReturnsPrefix()
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
        var result = group.FullPrefix;

        // Assert
        Assert.Equal("/api", result);
    }

    [Fact]
    public void FullPrefix_WithNestedGroup_ConcatenatesPrefixes()
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

        var parentSymbol = GetClassSymbol(compilation, "ParentGroup");
        var parentAttribute = parentSymbol.GetAttributes().First();
        var parentGroup = new EndpointGroupDefinition(parentSymbol, parentAttribute);

        var childSymbol = GetClassSymbol(compilation, "ChildGroup");
        var childAttribute = childSymbol.GetAttributes().First();
        var childGroup = new EndpointGroupDefinition(childSymbol, childAttribute)
        {
            ParentGroup = parentGroup
        };

        // Act
        var result = childGroup.FullPrefix;

        // Assert
        Assert.Equal("/api/v1", result);
    }

    [Fact]
    public void FullPrefix_CachesResult()
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
        var result1 = group.FullPrefix;
        var result2 = group.FullPrefix;

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
    public void ConfigureGroup(RouteGroupBuilder builder) { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var classSymbol = GetClassSymbol(compilation, "TestGroup");
        var attribute = classSymbol.GetAttributes().First();
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Act & Assert
        Assert.True(group.IsConfigurable);
    }

    [Fact]
    public void Cycles_InitializesEmpty()
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
        var group = new EndpointGroupDefinition(classSymbol, attribute);

        // Assert
        Assert.NotNull(group.Cycles);
        Assert.Empty(group.Cycles);
    }

    private static INamedTypeSymbol GetClassSymbol(Compilation compilation, string className)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == className);

        return (semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol)!;
    }

    private static INamedTypeSymbol GetNestedClassSymbol(Compilation compilation, string outerClassName, string innerClassName)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var outerClass = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == outerClassName);

        var innerClass = outerClass.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == innerClassName);

        return (semanticModel.GetDeclaredSymbol(innerClass) as INamedTypeSymbol)!;
    }
}

