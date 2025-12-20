# MinimalEndpoints

> **Elegant, class-based endpoints for ASP.NET Core Minimal APIs with zero runtime overhead**

[![NuGet](https://img.shields.io/nuget/v/Blackeye.MinimalEndpoints)](https://www.nuget.org/packages/Blackeye.MinimalEndpoints)
[![Build](https://img.shields.io/github/actions/workflow/status/yourusername/MinimalEndpoints/build.yml)](https://github.com/yourusername/MinimalEndpoints/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

MinimalEndpoints brings the benefits of class-based organization to ASP.NET Core Minimal APIs while maintaining their simplicity and performance. Using **source generators** and **Roslyn analyzers**, it provides compile-time code generation with zero runtime overhead.

---

## ✨ Features

- 🎯 **Class-Based Organization** - Organize endpoints as classes instead of scattered lambdas
- ⚡ **Zero Runtime Overhead** - Everything is generated at compile time
- 🔧 **Source Generators** - Automatic registration and mapping code generation
- 📊 **Roslyn Analyzers** - Catch errors at design time with helpful diagnostics
- 🛠️ **Code Fixes** - Automatic fixes for common issues
- 💉 **Dependency Injection** - Full support for constructor and parameter injection
- 🏷️ **Type Safety** - Strongly-typed handlers with compile-time validation
- 🎨 **Flexible Configuration** - Support for all ASP.NET Core endpoint features
- 🚀 **High Performance** - No reflection, no runtime overhead

---

## 📦 Installation

```bash
dotnet add package Blackeye.MinimalEndpoints
```

Or via Package Manager:

```powershell
Install-Package Blackeye.MinimalEndpoints
```

---

## 🚀 Quick Start

### 1. Create an Endpoint

```csharp
using MinimalEndpoints.Annotations;

[MapGet("/hello")]
public class HelloEndpoint
{
    public IResult Handle()
    {
        return Results.Ok("Hello, World!");
    }
}
```

### 2. Register in Program.cs

```csharp
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Register all endpoints
builder.Services.AddMinimalEndpoints();

var app = builder.Build();

// Map all endpoints
app.UseMinimalEndpoints();

app.Run();
```

That's it! Your endpoint is automatically discovered, registered, and mapped.

---

## 📚 Core Concepts

### Endpoint Classes

An endpoint is a class decorated with a mapping attribute (`[MapGet]`, `[MapPost]`, etc.) and a handler method:

```csharp
[MapGet("/users/{id}")]
public class GetUserEndpoint
{
    public async Task<IResult> HandleAsync(int id)
    {
        // Your logic here
        return Results.Ok(new { id, name = "John" });
    }
}
```

### Supported Attributes

- `[MapGet(pattern)]` - HTTP GET
- `[MapPost(pattern)]` - HTTP POST
- `[MapPut(pattern)]` - HTTP PUT
- `[MapDelete(pattern)]` - HTTP DELETE
- `[MapPatch(pattern)]` - HTTP PATCH
- `[MapHead(pattern)]` - HTTP HEAD
- `[MapMethods(pattern, methods)]` - Multiple HTTP methods

### Handler Methods

By default, MinimalEndpoints looks for methods named `Handle` or `HandleAsync`. You can also specify a custom entry point:

```csharp
[MapPost("/process", EntryPoint = "ProcessRequest")]
public class ProcessEndpoint
{
    public Task<IResult> ProcessRequest()
    {
        // ...
    }
}
```

---

## 💉 Dependency Injection

### Constructor Injection

```csharp
[MapGet("/products")]
public class GetProductsEndpoint
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductsEndpoint> _logger;

    public GetProductsEndpoint(
        IProductRepository repository,
        ILogger<GetProductsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync()
    {
        var products = await _repository.GetAllAsync();
        return Results.Ok(products);
    }
}
```

### Parameter Injection

```csharp
[MapGet("/user")]
public class GetCurrentUserEndpoint
{
    public Task<IResult> HandleAsync(
        [FromServices] IUserService userService,
        HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        var user = await userService.GetByIdAsync(userId);
        return Results.Ok(user);
    }
}
```

---

## 🎨 Configuration

### Service Lifetime

Control the service lifetime (default is `Scoped`):

```csharp
[MapGet("/config", ServiceLifetime.Singleton)]
public class ConfigEndpoint { }

[MapGet("/guid", ServiceLifetime.Transient)]
public class GuidEndpoint { }
```

### Service Interface

Register as an interface instead of concrete class:

```csharp
public interface IHealthCheck { }

[MapGet("/health", ServiceName = typeof(IHealthCheck))]
public class HealthCheckEndpoint : IHealthCheck
{
    public Task<IResult> HandleAsync() { }
}
```

### Advanced Configuration

Implement `IConfigurableEndpoint` for advanced endpoint configuration:

```csharp
using MinimalEndpoints;

[MapGet("/admin/users")]
public class GetAdminUsersEndpoint : IConfigurableEndpoint
{
    public async Task<IResult> HandleAsync()
    {
        // Implementation
    }

    public static void Configure(
        IApplicationBuilder app,
        IEndpointConventionBuilder endpoint)
    {
        endpoint
            .RequireAuthorization("AdminPolicy")
            .WithTags("Admin")
            .WithName("GetAdminUsers")
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    }
}
```

---

## ✅ Validation

Use standard ASP.NET Core validation attributes:

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

public record CreateUserRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; }

    [Required, EmailAddress]
    public string Email { get; init; }
}

[MapPost("/users")]
public class CreateUserEndpoint
{
    public async Task<IResult> HandleAsync(
        [FromBody] CreateUserRequest request)
    {
        // Validation happens automatically
        // ...
    }
}
```

---

## 🔧 How It Works

MinimalEndpoints uses **Roslyn Source Generators** to automatically create registration and mapping code at compile-time:

### 1. You Write

```csharp
[MapGet("/api/users/{id}")]
public class GetUserEndpoint
{
    private readonly IUserRepository _repo;

    public GetUserEndpoint(IUserRepository repo) => _repo = repo;

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}
```

### 2. Generator Creates

```csharp
// MinimalEndpointExtensions.g.cs (auto-generated)
public static class MinimalEndpointExtensions
{
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services)
    {
        services.AddScoped<GetUserEndpoint>();
        // ... other endpoints
        return services;
    }

    public static IEndpointRouteBuilder Map__Api_GetUserEndpoint(
        this IEndpointRouteBuilder builder,
        IApplicationBuilder app)
    {
        static Task<IResult> Handler(
            [FromServices] GetUserEndpoint endpoint,
            int id)
        {
            return endpoint.HandleAsync(id);
        }

        var endpoint = builder.MapGet("/api/users/{id}", Handler);
        return builder;
    }

    public static IApplicationBuilder UseMinimalEndpoints(this IApplicationBuilder app)
    {
        var builder = app as IEndpointRouteBuilder ?? throw new InvalidOperationException();
        builder.Map__Api_GetUserEndpoint(app);
        // ... other endpoints
        return app;
    }
}
```

### 3. Analyzers Validate

While you type, Roslyn analyzers check for common mistakes:

- ✅ **MINEP001**: Ensures entry point method exists
- ✅ **MINEP002**: Detects multiple mapping attributes
- ✅ **MINEP003**: Validates ServiceType interface compatibility

All validation happens at design-time with helpful error messages and quick fixes.

---

## 🎯 Migration Guide

### From Controller-Based APIs

**Before** (Controller):
```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repo;

    public UsersController(IUserRepository repo) => _repo = repo;

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        return user != null ? Ok(user) : NotFound();
    }
}
```

**After** (MinimalEndpoints):
```csharp
[MapGet("/api/users/{id}")]
public class GetUserEndpoint
{
    private readonly IUserRepository _repo;

    public GetUserEndpoint(IUserRepository repo) => _repo = repo;

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}
```

**Key Differences**:
- No controller base class
- `IResult` instead of `ActionResult<T>`
- `Results.Ok()` instead of `Ok()`
- Endpoint = one class = one route
- Dependency injection via constructor (same)

### From Minimal APIs

**Before** (Minimal API):
```csharp
app.MapGet("/api/users/{id}", async (int id, IUserRepository repo) =>
{
    var user = await repo.GetByIdAsync(id);
    return user != null ? Results.Ok(user) : Results.NotFound();
});
```

**After** (MinimalEndpoints):
```csharp
[MapGet("/api/users/{id}")]
public class GetUserEndpoint
{
    private readonly IUserRepository _repo;

    public GetUserEndpoint(IUserRepository repo) => _repo = repo;

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}
```

**Benefits**:
- Better organization for complex logic
- Constructor injection for shared dependencies
- Easier to test in isolation
- Reusable across multiple routes (with ServiceType)
- Same performance characteristics

---

## 🐛 Troubleshooting

### Generator Not Running

**Problem**: Generated code is not appearing

**Solutions**:
1. Clean and rebuild: `dotnet clean && dotnet build`
2. Restart IDE (Visual Studio / Rider / VS Code)
3. Check `.csproj` has `<OutputType>Exe</OutputType>` or `<OutputType>Library</OutputType>`
4. Ensure you have the correct NuGet package version

### Analyzer Errors

**Problem**: False positive from MINEP001

**Solution**: Ensure your entry point method is:
- Public
- Instance (not static)
- Named `Handle`, `HandleAsync`, or specified in `EntryPoint` property
- Returns `IResult`, `Task<IResult>`, or compatible type

### ServiceType Issues

**Problem**: MINEP003 error about missing method

**Solution**: Ensure the interface specified in `ServiceType` contains the entry point method:
```csharp
public interface IMyEndpoint
{
    Task<IResult> HandleAsync(); // Must match endpoint method
}

[MapGet("/route", ServiceType = typeof(IMyEndpoint))]
public class MyEndpoint : IMyEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
```

### Missing Using Directives

**Problem**: Cannot find `MapGetAttribute`

**Solution**: Add using directive:
```csharp
using MinimalEndpoints.Annotations;
```

Or use global using in `GlobalUsings.cs`:
```csharp
global using MinimalEndpoints.Annotations;
```

### IntelliSense Not Working

**Problem**: `AddMinimalEndpoints()` not showing in IntelliSense

**Solution**:
1. Ensure project has built successfully
2. Check generated code is visible in IDE
3. Add explicit using: `using MinimalEndpoints.Generated;`
4. Try "Reload All Projects" in IDE

---

## 🧪 Testing

Endpoints are easy to test because they're just classes:

```csharp
public class GetUserEndpointTests
{
    [Fact]
    public async Task HandleAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "John" });

        var endpoint = new GetUserEndpoint(mockRepo.Object);

        // Act
        var result = await endpoint.HandleAsync(1);

        // Assert
        var okResult = Assert.IsType<Ok<User>>(result);
        Assert.Equal("John", okResult.Value.Name);
    }
}
```

---

## 📊 Performance

MinimalEndpoints has **zero runtime overhead** compared to traditional Minimal APIs because:

1. **No Reflection** - Everything is generated at compile time
2. **No Runtime Discovery** - Endpoints are explicitly registered
3. **Direct Method Calls** - Generated code calls your handlers directly
4. **No Middleware** - Just standard ASP.NET Core routing

Benchmark results show identical performance to hand-written Minimal APIs.

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](docs/CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

### Building from Source

```bash
git clone https://github.com/yourusername/MinimalEndpoints.git
cd MinimalEndpoints
dotnet build
dotnet test
```

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Inspired by the simplicity of ASP.NET Core Minimal APIs
- Built on the power of Roslyn Source Generators
- Thanks to the .NET community for feedback and contributions

---

## 📮 Support

- 📚 [Documentation](docs/)
- 💬 [Discussions](https://github.com/yourusername/MinimalEndpoints/discussions)
- 🐛 [Issue Tracker](https://github.com/yourusername/MinimalEndpoints/issues)
- 📧 [Email](mailto:sotirios.mavrommatis@gmail.com)

---

## ⭐ Show Your Support

If you find this project useful, please give it a star! ⭐

---

**Made with ❤️ by the community**
