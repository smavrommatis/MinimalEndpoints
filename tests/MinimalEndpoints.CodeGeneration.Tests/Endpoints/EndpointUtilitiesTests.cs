using MinimalEndpoints.CodeGeneration.Endpoints;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Tests.Endpoints;

public class EndpointUtilitiesTests
{
    [Fact]
    public void IsConfigurableEndpoint_ShouldReturnTrue_ForConfigurableEndpoint()
    {
        // Arrange
        var code = @"
using Microsoft.AspNetCore.Builder;

namespace TestNamespace;

public class TestEndpoint : MinimalEndpoints.IConfigurableEndpoint
{
    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol?.IsConfigurableEndpoint();

        // Assert
        Assert.NotNull(result);
        Assert.True(result);
    }

    [Fact]
    public void IsConfigurableEndpoint_ShouldReturnFalse_ForNonConfigurableEndpoint()
    {
        // Arrange
        var code = @"
namespace TestNamespace;

public class TestEndpoint
{
    public void Handle()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.IsConfigurableEndpoint();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsConfigurableEndpoint_ShouldReturnFalse_ForDifferentInterface()
    {
        // Arrange
        var code = @"
namespace TestNamespace;

public interface IConfigurableEndpoint
{
    void SomeMethod();
}

public class TestEndpoint : IConfigurableEndpoint
{
    public void SomeMethod()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.IsConfigurableEndpoint();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FindEntryPointMethod_ShouldReturnHandleAsync_WhenHandleAsyncIsPresent()
    {
        // Arrange
        var code = @"
namespace TestNamespace;

public class TestEndpoint
{
    public void HandleAsync()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.FindEntryPointMethod(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HandleAsync", result.Name);
    }

    [Fact]
    public void FindEntryPointMethod_ShouldReturnHandle_WhenHandleIsPresent()
    {
        // Arrange
        var code = @"
namespace TestNamespace;

public class TestEndpoint
{
    public void Handle()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.FindEntryPointMethod(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Handle", result.Name);
    }

    [Fact]
    public void FindEntryPointMethod_ShouldPreferHandleAsync_WhenBothPresent()
    {
        // Arrange
        var code = @"
namespace TestNamespace;

public class TestEndpoint
{
    public void Handle()
    {
    }

    public void HandleAsync()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.FindEntryPointMethod(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HandleAsync", result.Name);
    }

    [Fact]
    public void FindEntryPointMethod_ShouldReturnNull_WhenNoHandleMethods()
    {
        // Arrange
        var code = @"
namespace TestNamespace;

public class TestEndpoint
{
    public void SomeOtherMethod()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.FindEntryPointMethod(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindEntryPointMethod_ShouldReturnPreferredMethod_WhenSpecified()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;
namespace TestNamespace;

public class TestEndpoint
{
    public void Handle()
    {
    }

    public Task HandleAsync()
    {
        return Task.CompletedTask;
    }

    public void CustomMethod()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.FindEntryPointMethod("CustomMethod");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CustomMethod", result.Name);
    }

    [Fact]
    public void FindEntryPointMethod_ShouldReturnNull_WhenPreferredMethodNotFound()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;
namespace TestNamespace;

public class TestEndpoint
{
    public void Handle()
    {
    }

    public Task HandleAsync()
    {
        return Task.CompletedTask;
    }
}";
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.FindEntryPointMethod("NonExistentMethod");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindEntryPointMethod_ShouldIgnoreStaticMethods()
    {
        // Arrange
        var code = @"
namespace TestNamespace;

public class TestEndpoint
{
    public static void Handle()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.FindEntryPointMethod(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindEntryPointMethod_ShouldIgnorePrivateMethods()
    {
        // Arrange
        var code = @"
namespace TestNamespace;

public class TestEndpoint
{
    private void Handle()
    {
    }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = symbol.FindEntryPointMethod(null);

        // Assert
        Assert.Null(result);
    }
}
