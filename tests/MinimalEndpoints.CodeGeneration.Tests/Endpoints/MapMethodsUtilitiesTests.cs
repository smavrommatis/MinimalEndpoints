using Microsoft.CodeAnalysis;
using MinimalEndpoints.CodeGeneration.Endpoints;
using MinimalEndpoints.CodeGeneration.Endpoints.Models;
using MinimalEndpoints.CodeGeneration.Models;
using MinimalEndpoints.CodeGeneration.Utilities;

namespace MinimalEndpoints.CodeGeneration.Tests.Endpoints;

public class MapMethodsUtilitiesTests
{
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnNull_WhenNoMapMethodAttributeIsPresent()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
public class TestClass { }";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.Null(result);
    }

    // MapGet Tests
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapGet_Simple()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapGet(""/test"")]
public class TestClass
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/test", result.Pattern);
        Assert.Equal(ServiceLifetime.Scoped, result.Lifetime);
        Assert.Equal("MapGet", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("GET", result.Methods);
        Assert.Null(result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapGet_WithProperties()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

public interface ITestService { }

[MapGet(""/api/test"", ServiceLifetime.Singleton, EntryPoint = ""Execute"", ServiceType = typeof(ITestService))]
public class TestClass : ITestService
{
    public void Execute() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/test", result.Pattern);
        Assert.Equal(ServiceLifetime.Singleton, result.Lifetime);
        Assert.Equal("MapGet", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("GET", result.Methods);
        Assert.Equal("Execute", result.EntryPoint);
        Assert.Equal("TestNamespace.ITestService", result.ServiceName);
    }

    // MapPost Tests
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapPost_Simple()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapPost(""/users"")]
public class CreateUserEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.CreateUserEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/users", result.Pattern);
        Assert.Equal(ServiceLifetime.Scoped, result.Lifetime); // Default is Scoped
        Assert.Equal("MapPost", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("POST", result.Methods);
        Assert.Null(result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapPost_WithProperties()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

public interface ICreateUserEndpoint { }

[MapPost(""/api/users"", ServiceLifetime.Transient, EntryPoint = ""HandleAsync"", ServiceType = typeof(ICreateUserEndpoint))]
public class CreateUserEndpoint : ICreateUserEndpoint
{
    public void HandleAsync() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.CreateUserEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/users", result.Pattern);
        Assert.Equal(ServiceLifetime.Transient, result.Lifetime);
        Assert.Equal("MapPost", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("POST", result.Methods);
        Assert.Equal("HandleAsync", result.EntryPoint);
        Assert.Equal("TestNamespace.ICreateUserEndpoint", result.ServiceName);
    }

    // MapPut Tests
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapPut_Simple()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapPut(""/users/{id}"")]
public class UpdateUserEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.UpdateUserEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/users/{id}", result.Pattern);
        Assert.Equal(ServiceLifetime.Scoped, result.Lifetime);
        Assert.Equal("MapPut", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("PUT", result.Methods);
        Assert.Null(result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapPut_WithProperties()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

public interface IUpdateEndpoint { }

[MapPut(""/api/users/{id}"", ServiceLifetime.Singleton, EntryPoint = ""ExecuteAsync"", ServiceType = typeof(IUpdateEndpoint))]
public class UpdateUserEndpoint : IUpdateEndpoint
{
    public void ExecuteAsync() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.UpdateUserEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/users/{id}", result.Pattern);
        Assert.Equal(ServiceLifetime.Singleton, result.Lifetime);
        Assert.Equal("MapPut", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("PUT", result.Methods);
        Assert.Equal("ExecuteAsync", result.EntryPoint);
        Assert.Equal("TestNamespace.IUpdateEndpoint", result.ServiceName);
    }

    // MapDelete Tests
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapDelete_Simple()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapDelete(""/users/{id}"")]
public class DeleteUserEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.DeleteUserEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/users/{id}", result.Pattern);
        Assert.Equal(ServiceLifetime.Scoped, result.Lifetime);
        Assert.Equal("MapDelete", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("DELETE", result.Methods);
        Assert.Null(result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapDelete_WithProperties()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

public interface IDeleteEndpoint { }

[MapDelete(""/api/users/{id}"", ServiceLifetime.Singleton, EntryPoint = ""RemoveAsync"", ServiceType = typeof(IDeleteEndpoint))]
public class DeleteUserEndpoint : IDeleteEndpoint
{
    public void RemoveAsync() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.DeleteUserEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/users/{id}", result.Pattern);
        Assert.Equal(ServiceLifetime.Singleton, result.Lifetime);
        Assert.Equal("MapDelete", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("DELETE", result.Methods);
        Assert.Equal("RemoveAsync", result.EntryPoint);
        Assert.Equal("TestNamespace.IDeleteEndpoint", result.ServiceName);
    }

    // MapPatch Tests
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapPatch_Simple()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapPatch(""/users/{id}"")]
public class PatchUserEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.PatchUserEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/users/{id}", result.Pattern);
        Assert.Equal(ServiceLifetime.Scoped, result.Lifetime);
        Assert.Equal("MapPatch", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("PATCH", result.Methods);
        Assert.Null(result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapPatch_WithProperties()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

public interface IPatchEndpoint { }

[MapPatch(""/api/users/{id}/status"", ServiceLifetime.Singleton, EntryPoint = ""PatchAsync"", ServiceType = typeof(IPatchEndpoint))]
public class PatchUserEndpoint : IPatchEndpoint
{
    public void PatchAsync() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.PatchUserEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/users/{id}/status", result.Pattern);
        Assert.Equal(ServiceLifetime.Singleton, result.Lifetime);
        Assert.Equal("MapPatch", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("PATCH", result.Methods);
        Assert.Equal("PatchAsync", result.EntryPoint);
        Assert.Equal("TestNamespace.IPatchEndpoint", result.ServiceName);
    }

    // MapHead Tests
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapHead_Simple()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapHead(""/health"")]
public class HealthCheckEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.HealthCheckEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/health", result.Pattern);
        Assert.Equal(ServiceLifetime.Scoped, result.Lifetime);
        Assert.Equal("MapHead", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("HEAD", result.Methods);
        Assert.Null(result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapHead_WithProperties()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

public interface IHealthCheck { }

[MapHead(""/api/health"", ServiceLifetime.Singleton, EntryPoint = ""CheckAsync"", ServiceType = typeof(IHealthCheck))]
public class HealthCheckEndpoint : IHealthCheck
{
    public void CheckAsync() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.HealthCheckEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/health", result.Pattern);
        Assert.Equal(ServiceLifetime.Singleton, result.Lifetime);
        Assert.Equal("MapHead", result.EndpointBuilderMethodName);
        Assert.Single(result.Methods);
        Assert.Contains("HEAD", result.Methods);
        Assert.Equal("CheckAsync", result.EntryPoint);
        Assert.Equal("TestNamespace.IHealthCheck", result.ServiceName);
    }

    // MapMethods Tests
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapMethods_Simple()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

[MapMethods(""/custom"", new[] { ""GET"", ""POST"" })]
public class CustomEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.CustomEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/custom", result.Pattern);
        Assert.Equal(ServiceLifetime.Scoped, result.Lifetime);
        Assert.Equal("MapMethods", result.EndpointBuilderMethodName);
        Assert.Equal(2, result.Methods.Length);
        Assert.Contains("GET", result.Methods);
        Assert.Contains("POST", result.Methods);
        Assert.Null(result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnInfo_ForMapMethods_WithProperties()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

public interface ICustomEndpoint { }

[MapMethods(""/api/custom"", new[] { ""PUT"", ""PATCH"", ""DELETE"" }, ServiceLifetime.Singleton, EntryPoint = ""ProcessAsync"", ServiceType = typeof(ICustomEndpoint))]
public class CustomEndpoint : ICustomEndpoint
{
    public void ProcessAsync() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.CustomEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/custom", result.Pattern);
        Assert.Equal(ServiceLifetime.Singleton, result.Lifetime);
        Assert.Equal("MapMethods", result.EndpointBuilderMethodName);
        Assert.Equal(3, result.Methods.Length);
        Assert.Contains("PUT", result.Methods);
        Assert.Contains("PATCH", result.Methods);
        Assert.Contains("DELETE", result.Methods);
        Assert.Equal("ProcessAsync", result.EntryPoint);
        Assert.Equal("TestNamespace.ICustomEndpoint", result.ServiceName);
    }

    // Edge Cases
    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldHandleEmptyPattern()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapGet("""")]
public class RootEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.RootEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("", result.Pattern);
        Assert.Equal("MapGet", result.EndpointBuilderMethodName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldHandleComplexPattern()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

[MapGet(""/api/v{version:apiVersion}/users/{userId:guid}/posts/{postId:int}"", ServiceLifetime.Scoped)]
public class ComplexPatternEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.ComplexPatternEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/api/v{version:apiVersion}/users/{userId:guid}/posts/{postId:int}", result.Pattern);
        Assert.Equal("MapGet", result.EndpointBuilderMethodName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldHandleGroup()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using MinimalEndpoints;

[MapGroup(""/api/v1"")]
public class ApiV1Group
{
}

[MapGet(""/test"", Group = typeof(ApiV1Group))]
public class TestEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.GroupType);
        Assert.Equal("TestNamespace.ApiV1Group", result.GroupType.ToDisplayString());
        Assert.Null(result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldHandleOnlyEntryPoint()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapPost(""/test"", EntryPoint = ""CustomHandler"")]
public class TestEndpoint
{
    public void CustomHandler() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CustomHandler", result.EntryPoint);
        Assert.Null(result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldHandleOnlyServiceType()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

public interface IMyService { }

[MapPut(""/test"", ServiceType = typeof(IMyService))]
public class TestEndpoint : IMyService
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestNamespace.IMyService", result.ServiceName);
        Assert.Null(result.EntryPoint);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldHandleNestedNamespaces()
    {
        // Arrange
        var code = @"
namespace TestNamespace.Api.V1.Endpoints;
using MinimalEndpoints.Annotations;

public interface IEndpoint { }

[MapGet(""/test"", ServiceType = typeof(IEndpoint))]
public class TestEndpoint : IEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.Api.V1.Endpoints.TestEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestNamespace.Api.V1.Endpoints.IEndpoint", result.ServiceName);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldHandleAllLifetimes()
    {
        // Arrange - Singleton
        var codeSingleton = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

[MapGet(""/singleton"", ServiceLifetime.Singleton)]
public class SingletonEndpoint
{
    public void Handle() { }
}";

        var compilationSingleton = new CompilationBuilder(codeSingleton)
            .WithMvcReferences()
            .Build();
        var symbolSingleton = compilationSingleton.GetTypeByMetadataName("TestNamespace.SingletonEndpoint");

        // Arrange - Scoped
        var codeScoped = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

[MapGet(""/scoped"", ServiceLifetime.Scoped)]
public class ScopedEndpoint
{
    public void Handle() { }
}";

        var compilationScoped = new CompilationBuilder(codeScoped)
            .WithMvcReferences()
            .Build();
        var symbolScoped = compilationScoped.GetTypeByMetadataName("TestNamespace.ScopedEndpoint");

        // Arrange - Transient
        var codeTransient = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using Microsoft.Extensions.DependencyInjection;

[MapGet(""/transient"", ServiceLifetime.Transient)]
public class TransientEndpoint
{
    public void Handle() { }
}";

        var compilationTransient = new CompilationBuilder(codeTransient)
            .WithMvcReferences()
            .Build();
        var symbolTransient = compilationTransient.GetTypeByMetadataName("TestNamespace.TransientEndpoint");

        // Act
        var resultSingleton = GetMapMethodsAttributeDefinition(symbolSingleton!);
        var resultScoped = GetMapMethodsAttributeDefinition(symbolScoped!);
        var resultTransient = GetMapMethodsAttributeDefinition(symbolTransient!);

        // Assert
        Assert.NotNull(resultSingleton);
        Assert.Equal(ServiceLifetime.Singleton, resultSingleton.Lifetime);
        Assert.NotNull(resultScoped);
        Assert.Equal(ServiceLifetime.Scoped, resultScoped.Lifetime);
        Assert.NotNull(resultTransient);
        Assert.Equal(ServiceLifetime.Transient, resultTransient.Lifetime);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldReturnNull_ForMultipleAttributes()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;
using System;

[Obsolete]
[MapGet(""/test"")]
public class TestEndpoint
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestEndpoint");

        // Act
        var result = GetMapMethodsAttributeDefinition(symbol!);

        // Assert
        Assert.NotNull(result); // Should still find the MapGet attribute
        Assert.Equal("/test", result.Pattern);
    }

    [Fact]
    public void GetMapMethodsAttributeDefinition_ShouldThrowException_WhenMultipleAttributesPresent()
    {
        // Arrange
        var code = @"
namespace TestNamespace;
using MinimalEndpoints.Annotations;

[MapGet(""/test"")]
[MapPost(""/test"")]
public class TestClass
{
    public void Handle() { }
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();
        var symbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");

        Assert.Throws<InvalidOperationException>(() => GetMapMethodsAttributeDefinition(symbol!));
    }

    private static MapMethodsAttributeDefinition? GetMapMethodsAttributeDefinition(INamedTypeSymbol symbol) =>
        ((EndpointDefinition)SymbolDefinitionFactory.TryCreateSymbol(symbol))?.MapMethodsAttribute;
}
