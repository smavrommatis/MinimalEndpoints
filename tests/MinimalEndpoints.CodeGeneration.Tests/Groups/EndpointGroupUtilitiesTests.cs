using MinimalEndpoints.CodeGeneration.Groups;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups;

public class EndpointGroupUtilitiesTests
{
    #region IsMapGroupAttribute Tests

    [Fact]
    public void IsMapGroupAttribute_WithMapGroupAttribute_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGroup(""/api"")]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("ApiGroup");
        var attribute = symbol!.GetAttributes().First();

        // Act
        var result = attribute.AttributeClass!.IsMapGroupAttribute();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMapGroupAttribute_WithMapGetAttribute_ReturnsFalse()
    {
        // Arrange
        var code = @"
using MinimalEndpoints.Annotations;

[MapGet(""/api"")]
public class TestEndpoint { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestEndpoint");
        var attribute = symbol!.GetAttributes().First();

        // Act
        var result = attribute.AttributeClass!.IsMapGroupAttribute();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMapGroupAttribute_WrongNamespace_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace CustomNamespace;

public class MapGroupAttribute : System.Attribute
{
    public MapGroupAttribute(string prefix) { }
}

[MapGroup(""/api"")]
public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("CustomNamespace.ApiGroup");
        var attribute = symbol!.GetAttributes().First();

        // Act
        var result = attribute.AttributeClass!.IsMapGroupAttribute();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsConfigurableGroupEndpoint Tests

    [Fact]
    public void IsConfigurableGroupEndpoint_WithInterface_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public class ApiGroup : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.ApiGroup");

        // Act
        var result = symbol!.IsConfigurableGroupEndpoint();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConfigurableGroupEndpoint_WithoutInterface_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace TestApp;

public class ApiGroup { }";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.ApiGroup");

        // Act
        var result = symbol!.IsConfigurableGroupEndpoint();

        // Assert
        Assert.False(result);
    }

    #endregion
}
