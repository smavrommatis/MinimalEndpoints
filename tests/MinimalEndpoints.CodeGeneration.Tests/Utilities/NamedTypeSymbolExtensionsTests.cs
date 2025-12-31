using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Tests.Utilities;

public class NamedTypeSymbolExtensionsTests
{
    #region IsConditionallyMapped Tests

    [Fact]
    public void IsConditionallyMapped_WithIConditionallyMappedInterface_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public class TestEndpoint : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.IsConditionallyMapped();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConditionallyMapped_WithoutInterface_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace TestApp;

public class TestEndpoint
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.IsConditionallyMapped();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsConditionallyMapped_WithInheritedInterface_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public interface IBaseEndpoint : IConditionallyMapped
{
}

public class TestEndpoint : IBaseEndpoint
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.IsConditionallyMapped();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConditionallyMapped_WithSimilarNamedInterface_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface IConditionallyMapped // Wrong namespace
{
}

public class TestEndpoint : IConditionallyMapped
{
}";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.IsConditionallyMapped();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsConditionallyMapped_WithMultipleInterfaces_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public interface ICustomInterface { }

public class TestEndpoint : ICustomInterface, IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) => true;
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.IsConditionallyMapped();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region HasInterface Tests

    [Fact]
    public void HasInterface_WithMatchingInterface_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public class TestEndpoint : IConfigurableEndpoint
{
    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint) { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.HasInterface("MinimalEndpoints", "IConfigurableEndpoint");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasInterface_WithoutInterface_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace TestApp;

public class TestEndpoint
{
}";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.HasInterface("MinimalEndpoints", "IConfigurableEndpoint");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasInterface_WrongNamespace_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface IConfigurableEndpoint { }

public class TestEndpoint : IConfigurableEndpoint
{
}";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.HasInterface("MinimalEndpoints", "IConfigurableEndpoint");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasInterface_WrongName_ReturnsFalse()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public class TestEndpoint : IConfigurableEndpoint
{
    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint) { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.HasInterface("MinimalEndpoints", "IConfigurableGroup");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasInterface_WithInheritedInterface_ReturnsTrue()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public interface IBaseEndpoint : IConfigurableEndpoint
{
}

public class TestEndpoint : IBaseEndpoint
{
    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint) { }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.HasInterface("MinimalEndpoints", "IConfigurableEndpoint");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasInterface_WithGenericInterface_ReturnsTrue()
    {
        // Arrange
        var code = @"
namespace TestApp;

public interface IGeneric<T> { }

public class TestEndpoint : IGeneric<string>
{
}";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.HasInterface("TestApp", "IGeneric");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasInterface_WithMultipleInterfaces_FindsCorrectOne()
    {
        // Arrange
        var code = @"
using MinimalEndpoints;

namespace TestApp;

public interface ICustomInterface { }

public class TestEndpoint : ICustomInterface, IConfigurableEndpoint, IConditionallyMapped
{
    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint) { }
    public static bool ShouldMap(IApplicationBuilder app) => true;
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act & Assert
        Assert.True(symbol!.HasInterface("MinimalEndpoints", "IConfigurableEndpoint"));
        Assert.True(symbol.HasInterface("MinimalEndpoints", "IConditionallyMapped"));
        Assert.True(symbol.HasInterface("TestApp", "ICustomInterface"));
        Assert.False(symbol.HasInterface("MinimalEndpoints", "IConfigurableGroup"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void HasInterface_EmptyNamespace_ReturnsFalse()
    {
        // Arrange
        var code = @"
public interface IGlobalInterface { }

public class TestEndpoint : IGlobalInterface
{
}";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("TestEndpoint");

        // Act
        var result = symbol!.HasInterface("", "IGlobalInterface");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsConditionallyMapped_WithNoInterfaces_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace TestApp;

public class TestEndpoint
{
}";

        var compilation = new CompilationBuilder(code).Build();
        var symbol = compilation.GetTypeByMetadataName("TestApp.TestEndpoint");

        // Act
        var result = symbol!.IsConditionallyMapped();

        // Assert
        Assert.False(result);
    }

    #endregion
}

