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

## 🔍 Analyzers & Code Fixes

MinimalEndpoints includes Roslyn analyzers that catch common mistakes:

### ME0001: Missing Entry Point

```csharp
[MapGet("/test")]
public class TestEndpoint  // ❌ Error: Missing entry point
{
    // No Handle or HandleAsync method
}
```

**Code Fix:** Automatically adds a `HandleAsync` method

### ME0002: Multiple Mapping Attributes

```csharp
[MapGet("/test")]
[MapPost("/test")]  // ❌ Error: Multiple attributes
public class TestEndpoint { }
```

---

## 📖 Examples

### Complete CRUD Example

See [EXAMPLES.md](docs/EXAMPLES.md) for comprehensive examples including:
- Basic endpoints
- Parameter binding (route, query, body)
- Dependency injection
- Validation
- Complex types and generics
- Complete CRUD operations

### Simple API

```csharp
// Endpoints/GetWeather.cs
[MapGet("/weather")]
public class GetWeatherEndpoint
{
    public IResult Handle()
    {
        return Results.Ok(new
        {
            temperature = 72,
            condition = "Sunny"
        });
    }
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalEndpoints();

var app = builder.Build();
app.UseMinimalEndpoints();
app.Run();
```

---

## 🏗️ How It Works

MinimalEndpoints uses **Source Generators** to analyze your code at compile time and generate registration and mapping code. This means:

1. **Zero Runtime Overhead** - No reflection or runtime discovery
2. **Compile-Time Safety** - Errors are caught before you run
3. **Clean Generated Code** - You can inspect what's generated
4. **IDE Support** - IntelliSense and navigation work perfectly

### Generated Code

For this endpoint:

```csharp
[MapGet("/hello")]
public class HelloEndpoint
{
    public IResult Handle() => Results.Ok("Hello!");
}
```

MinimalEndpoints generates:

```csharp
// MinimalEndpointExtensions.g.cs
public static class MinimalEndpointExtensions
{
    public static IServiceCollection AddMinimalEndpoints(
        this IServiceCollection services)
    {
        services.AddScoped<HelloEndpoint>();
        return services;
    }

    public static IEndpointRouteBuilder Map__HelloEndpoint(
        this IEndpointRouteBuilder builder,
        IApplicationBuilder app)
    {
        static IResult Handler(
            [FromServices]HelloEndpoint endpointInstance)
        {
            return endpointInstance.Handle();
        }

        var endpoint = builder.MapGet("/hello", Handler);
        return builder;
    }

    public static IApplicationBuilder UseMinimalEndpoints(
        this IApplicationBuilder app)
    {
        var builder = app as IEndpointRouteBuilder ??
            throw new ArgumentException(/* ... */);
        builder.Map__HelloEndpoint(app);
        return app;
    }
}
```

---

## 🎯 Benefits Over Traditional Minimal APIs

### Before (Traditional Minimal API)

```csharp
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/products", async (IProductRepository repo) =>
{
    var products = await repo.GetAllAsync();
    return Results.Ok(products);
});

app.MapGet("/products/{id}", async (int id, IProductRepository repo) =>
{
    var product = await repo.GetByIdAsync(id);
    return product != null ? Results.Ok(product) : Results.NotFound();
});

app.MapPost("/products", async (CreateProductRequest request, IProductRepository repo) =>
{
    var product = await repo.CreateAsync(request);
    return Results.Created($"/products/{product.Id}", product);
});

// Hundreds more endpoints in Program.cs...
```

**Problems:**
- ❌ All logic in one file
- ❌ Hard to test
- ❌ Difficult to reuse
- ❌ No clear organization
- ❌ Dependency injection is unclear

### After (MinimalEndpoints)

```csharp
// Endpoints/Products/ListProducts.cs
[MapGet("/products")]
public class ListProductsEndpoint
{
    private readonly IProductRepository _repository;

    public ListProductsEndpoint(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var products = await _repository.GetAllAsync();
        return Results.Ok(products);
    }
}

// Endpoints/Products/GetProduct.cs
[MapGet("/products/{id}")]
public class GetProductEndpoint
{
    // ... similar structure
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalEndpoints();

var app = builder.Build();
app.UseMinimalEndpoints();
app.Run();
```

**Benefits:**
- ✅ One class per endpoint
- ✅ Easy to test
- ✅ Reusable components
- ✅ Clear organization
- ✅ Explicit dependencies
- ✅ Still gets Minimal API performance!

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
