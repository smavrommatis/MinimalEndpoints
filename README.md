# MinimalEndpoints

> **Elegant, class-based endpoints for ASP.NET Core Minimal APIs with zero runtime overhead**

[![NuGet](https://img.shields.io/nuget/v/Blackeye.MinimalEndpoints)](https://www.nuget.org/packages/Blackeye.MinimalEndpoints)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/smavrommatis/MinimalEndpoints)
[![License: BSD-2-Clause](https://img.shields.io/badge/License-BSD--2--Clause-blue.svg)](https://opensource.org/licenses/BSD-2-Clause)
[![Code Coverage](https://img.shields.io/badge/coverage-95%25-brightgreen)](https://github.com/smavrommatis/MinimalEndpoints)

MinimalEndpoints brings the benefits of class-based organization to ASP.NET Core Minimal APIs while maintaining their simplicity and performance. Using **source generators** and **Roslyn analyzers**, it provides compile-time code generation with zero runtime overhead.

---

## 📑 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Quick Start](#-quick-start)
- [Core Concepts](#-core-concepts)
  - [Endpoint Classes](#endpoint-classes)
  - [Supported Attributes](#supported-attributes)
  - [Handler Methods](#handler-methods)
- [Dependency Injection](#-dependency-injection)
  - [Constructor Injection](#constructor-injection)
  - [Parameter Injection](#parameter-injection)
- [Configuration](#-configuration)
  - [Service Lifetime](#service-lifetime)
  - [Endpoint Groups](#endpoint-groups)
  - [Hierarchical Groups](#hierarchical-groups)
  - [Service Interface](#service-interface)
  - [Advanced Configuration](#advanced-configuration)
- [Integration with ASP.NET Core](#-integration-with-aspnet-core)
  - [API Versioning](#api-versioning)
  - [Response Caching](#response-caching)
  - [Rate Limiting](#rate-limiting)
  - [OpenTelemetry](#opentelemetry)
  - [Authorization](#authorization)
- [How It Works](#-how-it-works)
- [Migration Guide](#-migration-guide)
- [Testing](#-testing)
- [Performance](#-performance)
- [Documentation](#-documentation)
- [Contributing](#-contributing)
- [License](#-license)
- [Support & Community](#-support--community)

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

### Endpoint Groups

Group related endpoints with shared configuration using `IConfigurableGroup`:

```csharp
// Define a group
[MapGroup("/api/v1")]
public class ApiV1Group : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .WithOpenApi()
             .RequireRateLimiting("fixed");
    }
}

// Use the group
[MapGet("/products", Group = typeof(ApiV1Group))]
public class ListProductsEndpoint
{
    public Task<IResult> HandleAsync() { }
}
// Results in: /api/v1/products with authorization and rate limiting
```

**Benefits of Groups:**
- **DRY**: Define route prefix once
- **Shared Config**: Authorization, rate limiting, CORS, etc.
- **Type-Safe**: Compile-time checking with `typeof()`
- **Versioning**: Easy API versioning (`/api/v1`, `/api/v2`)

#### Hierarchical Groups

Groups can have parent groups, creating multi-level route structures:

```csharp
// Root group
[MapGroup("/api")]
public class ApiGroup : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi();
    }
}

// Child group with parent
[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
    }
}

// Grandchild group
[MapGroup("/admin", ParentGroup = typeof(V1Group))]
public class AdminGroup : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization("Admin");
    }
}

// Endpoint uses the deepest group
[MapGet("/users", Group = typeof(AdminGroup))]
public class ListAdminUsersEndpoint
{
    public Task<IResult> HandleAsync() { }
}
// Results in: /api/v1/admin/users
// With OpenAPI + Authorization + Admin Authorization
```

**Hierarchical Groups Features:**
- **Nested Configuration**: Parent configurations cascade to children
- **API Versioning**: Organize by version hierarchies
- **Feature Modules**: Group by business domains
- **Compile-Time Validation**: Cycle detection prevents circular hierarchies (MINEP006)


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
        RouteHandlerBuilder endpoint)
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

## 🔗 Integration with ASP.NET Core

MinimalEndpoints generates standard Minimal API code, giving you **seamless integration** with all built-in ASP.NET Core features:

### API Versioning

Works with `Asp.Versioning.Http` package:

```csharp
[MapGet("/api/v{version:apiVersion}/users")]
public class GetUsersEndpoint : IConfigurableEndpoint
{
    public async Task<IResult> HandleAsync() => Results.Ok();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.HasApiVersion(new ApiVersion(1, 0));
    }
}
```

### Response Caching

Uses .NET's built-in output caching:

```csharp
[MapGet("/api/products")]
public class GetProductsEndpoint : IConfigurableEndpoint
{
    public async Task<IResult> HandleAsync() => Results.Ok();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.CacheOutput(policy => policy
            .Expire(TimeSpan.FromMinutes(5))
            .Tag("products"));
    }
}
```

### Rate Limiting

Uses .NET's built-in rate limiter:

```csharp
[MapPost("/api/orders")]
public class CreateOrderEndpoint : IConfigurableEndpoint
{
    public async Task<IResult> HandleAsync() => Results.Ok();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.RequireRateLimiting("fixed");
    }
}
```

### OpenTelemetry

Endpoints are automatically traced:

```csharp
// In Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation() // Automatically traces all endpoints
        .AddConsoleExporter());
```

### Authorization

Full support for ASP.NET Core authorization:

```csharp
[MapDelete("/api/users/{id}")]
public class DeleteUserEndpoint : IConfigurableEndpoint
{
    public async Task<IResult> HandleAsync(int id) => Results.NoContent();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.RequireAuthorization("AdminOnly");
    }
}
```

**📖 [Complete Integration Guide](docs/examples/11-aspnetcore-integration.md)** - Detailed examples for all features

---

## ✅ Validation

Use standard ASP.NET Core validation attributes:

````
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
````

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
- ✅ **MINEP004**: Warns about ambiguous route patterns
- ✅ **MINEP005**: Validates group types have `[MapGroup]` attribute
- ✅ **MINEP006**: Detects cyclic group hierarchies
- ✅ **MINEP007**: Prevents classes from being both endpoint and group

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

## 📊 Diagnostics

MinimalEndpoints includes built-in Roslyn analyzers that catch common mistakes at design-time:

### MINEP001: Missing Entry Point Method
Ensures your endpoint class has a valid entry point method (`Handle`, `HandleAsync`, or custom).

```csharp
// ❌ Error: No entry point method
[MapGet("/users")]
public class GetUsersEndpoint
{
    // Missing HandleAsync method!
}
```

[Learn more →](docs/diagnostics/MINEP001.md)

### MINEP002: Multiple MapMethods Attributes
Prevents multiple mapping attributes on the same class.

```csharp
// ❌ Error: Multiple attributes
[MapGet("/users")]
[MapPost("/users")]  // Can't have both!
public class UsersEndpoint { }
```

[Learn more →](docs/diagnostics/MINEP002.md)

### MINEP003: ServiceType Interface Validation
Ensures the interface specified in `ServiceType` contains the entry point method.

```csharp
// ❌ Error: Interface missing HandleAsync
public interface IGetUsers { }

[MapGet("/users", ServiceType = typeof(IGetUsers))]
public class GetUsersEndpoint : IGetUsers
{
    public Task<IResult> HandleAsync() => ...;
}
```

[Learn more →](docs/diagnostics/MINEP003.md)

### MINEP004: Ambiguous Route Patterns
Warns about duplicate route patterns that would cause routing conflicts.

```csharp
// ⚠️ Warning: Ambiguous routes
[MapGet("/users")]
public class GetUsersEndpoint { }

[MapGet("/users")]  // Same route!
public class ListUsersEndpoint { }
```

[Learn more →](docs/diagnostics/MINEP004.md)

### MINEP005: Invalid Group Type
Validates that groups have `[MapGroup]` attribute.

```csharp
// ❌ Error: Invalid group type
public class ApiGroup { }  // Missing [MapGroup] attribute

[MapGet("/users", Group = typeof(ApiGroup))]
public class GetUsersEndpoint { }
```

[Learn more →](docs/diagnostics/MINEP005.md)

### MINEP006: Cyclic Group Hierarchy
Detects circular parent-child relationships in group hierarchies.

```csharp
// ❌ Error: Cyclic hierarchy
[MapGroup("/api", ParentGroup = typeof(V1Group))]
public class ApiGroup : IConfigurableGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IConfigurableGroup { }
// Cycle: ApiGroup → V1Group → ApiGroup
```

[Learn more →](docs/diagnostics/MINEP006.md)

### MINEP007: Class Cannot Be Both Endpoint and Group
Prevents classes from having both endpoint and group attributes.

```csharp
// ❌ Error: Cannot be both
[MapGet("/users")]
[MapGroup("/api")]
public class InvalidClass { }
```

[Learn more →](docs/diagnostics/MINEP007.md)

---

## 📚 Examples & Samples

### Sample Projects

Explore complete working examples in the `samples/` directory:

- **[Basic Sample](samples/MinimalEndpoints.Sample/)** - Simple CRUD operations
- **[Advanced Sample](samples/MinimalEndpoints.AdvancedSample/)** - IConfigurableEndpoint, validation, OpenAPI
- **[Real-World Sample](samples/MinimalEndpoints.RealWorldSample/)** - Production-ready with database, auth, Docker

### Quick Examples

See [EXAMPLES.md](docs/EXAMPLES.md) for detailed examples including:
- Authentication & Authorization
- File Uploads
- Background Jobs
- Health Checks
- OpenAPI/Swagger Integration
- And more...

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

MinimalEndpoints has **zero runtime overhead** compared to traditional Minimal APIs:

### Generator Performance

Source generator execution time and memory usage:

#### Analyzer Performance (Diagnostic Analysis)

| Endpoints | Mean     | Error     | StdDev    | Gen0     | Gen1    | Allocated  |
|-----------|----------|-----------|-----------|----------|---------|------------|
| 10        | 1.525 ms | 0.555 ms  | 0.367 ms  | 39.0625  | 3.9063  | 832.22 KB  |
| 50        | 3.428 ms | 0.904 ms  | 0.538 ms  | 101.5625 | 31.2500 | 3437.14 KB |
| 100       | 6.669 ms | 3.676 ms  | 2.431 ms  | 203.1250 | 78.1250 | 8251.23 KB |

#### Code Generation Performance

| Endpoints | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-----------|-----------|----------|----------|--------|-----------|
| 10        | 48.27 μs  | 0.253 μs | 0.133 μs | 0.9766 | 20.34 KB  |
| 50        | 98.56 μs  | 0.803 μs | 0.420 μs | 0.9766 | 20.7 KB   |
| 100       | 160.81 μs | 0.594 μs | 0.393 μs | 0.9766 | 23.21 KB  |
| 500       | 649.14 μs | 6.088 μs | 3.623 μs | -      | 32.47 KB  |

**Key Takeaways:**
- ⚡ **Sub-millisecond generation** for typical projects (<100 endpoints)
- 💾 **Minimal memory allocation** (~20-30 KB for most projects)
- 🚀 **Linear scaling** with endpoint count
- ✅ **Incremental generation** - only regenerates what changed

**Why Zero Runtime Overhead?**

1. **No Reflection** - Everything generated at compile-time
2. **No Runtime Discovery** - Endpoints explicitly registered
3. **Direct Method Calls** - Generated code calls handlers directly
4. **No Extra Middleware** - Standard ASP.NET Core routing

📈 **[View Detailed Benchmarks](benchmarks/README.md)** - Comprehensive performance analysis

📖 **[Performance Guide](docs/PERFORMANCE.md)** - Optimization tips and best practices

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](docs/CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

### Building from Source

```bash
git clone https://github.com/blackeye/MinimalEndpoints.git
cd MinimalEndpoints
dotnet build
dotnet test
```

---

## 📚 Documentation

- **[Getting Started](docs/examples/01-getting-started.md)** - Your first endpoint in 5 minutes
- **[Architecture Guide](docs/ARCHITECTURE.md)** - How it works under the hood
- **[Examples](docs/examples/)** - Comprehensive examples for all scenarios
- **[Migration Guide](docs/MIGRATION.md)** - Migrating from other approaches
- **[Troubleshooting](docs/TROUBLESHOOTING.md)** - Common issues and solutions
- **[Performance](docs/PERFORMANCE.md)** - Benchmarks and optimization tips
- **[Diagnostics Reference](docs/diagnostics/)** - All MINEP diagnostic codes explained

---

## 📄 License

This project is licensed under the BSD 2-Clause License - see the [LICENSE](LICENSE) file for details.

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](docs/CONTRIBUTING.md) for details.

```bash
git clone https://github.com/smavrommatis/MinimalEndpoints.git
cd MinimalEndpoints
dotnet build
dotnet test
```

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

## 📮 Support & Community

- 📚 **[Documentation](docs/)** - Complete guides and references
- 💬 **[Discussions](https://github.com/smavrommatis/MinimalEndpoints/discussions)** - Ask questions and share ideas
- 🐛 **[Issue Tracker](https://github.com/smavrommatis/MinimalEndpoints/issues)** - Report bugs and request features
- 📧 **[Email](mailto:sotirios.mavrommatis+minimalendpoints@gmail.com)** - Direct support
- 🔒 **[Security](SECURITY.md)** - Report security vulnerabilities

---

## 🙏 Acknowledgments

- Inspired by the simplicity of ASP.NET Core Minimal APIs
- Built on the power of Roslyn Source Generators and Analyzers
- Thanks to the .NET community for feedback and contributions

---

## ⭐ Show Your Support

If MinimalEndpoints helps your project, please:
- ⭐ **Star this repository**
- 📢 **Share with your team**
- 💬 **Provide feedback** in [Discussions](https://github.com/smavrommatis/MinimalEndpoints/discussions)

---

**Made with ❤️ for the .NET community**
