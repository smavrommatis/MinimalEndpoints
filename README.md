# MinimalEndpoints

> **Elegant, class-based endpoints for ASP.NET Core Minimal APIs with zero runtime overhead**

[![NuGet](https://img.shields.io/nuget/v/Blackeye.MinimalEndpoints)](https://www.nuget.org/packages/Blackeye.MinimalEndpoints)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/smavrommatis/MinimalEndpoints)
[![License: BSD-2-Clause](https://img.shields.io/badge/License-BSD--2--Clause-blue.svg)](https://opensource.org/licenses/BSD-2-Clause)
[![Code Coverage](https://img.shields.io/badge/coverage%20gate-60%25%20line%2Fbranch%2Fmethod-brightgreen)](https://github.com/smavrommatis/MinimalEndpoints)

MinimalEndpoints brings the benefits of class-based organization to ASP.NET Core Minimal APIs while maintaining their simplicity and performance. Using **source generators** and **Roslyn analyzers**, it provides compile-time code generation with zero runtime overhead.

---

## 📑 Table of Contents

- [Features](#-features)
- [Installation](#-installation)
- [Requirements](#-requirements)
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
  - [Route Parameters in Group Prefixes](#route-parameters-in-group-prefixes)
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

## ✅ Requirements

- **.NET 8, 9, or 10** — the package multi-targets `net8.0;net9.0;net10.0`.
- **C# 11 or later** — the group/endpoint configuration interfaces
  (`IConfigurableGroup.ConfigureGroup`, `IConditionallyMapped.ShouldMap`) use
  static abstract interface members, a C# 11 / .NET 7+ language feature.

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
    public async Task<IResult> HandleAsync(
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
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
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
    public Task<IResult> HandleAsync() => ...;
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
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.WithOpenApi();
    }
}

// Child group with parent
[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireAuthorization();
    }
}

// Grandchild group
[MapGroup("/admin", ParentGroup = typeof(V1Group))]
public class AdminGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireAuthorization("Admin");
    }
}

// Endpoint uses the deepest group
[MapGet("/users", Group = typeof(AdminGroup))]
public class ListAdminUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
// Results in: /api/v1/admin/users
// With OpenAPI + Authorization + Admin Authorization
```

**Hierarchical Groups Features:**
- **Nested Configuration**: Parent configurations cascade to children
- **API Versioning**: Organize by version hierarchies
- **Feature Modules**: Group by business domains
- **Compile-Time Validation**: Cycle detection prevents circular hierarchies (MINEP006)

#### Route Parameters in Group Prefixes

A group prefix can contain route parameters. Any endpoint in the group binds them by declaring a
handler parameter of the same name — no `[FromRoute]` and no generator configuration required. The
prefix is emitted verbatim into `MapGroup(...)`, so ASP.NET Core binds the route values to handler
parameters by name:

```csharp
[MapGroup("/api/v{version}")]
public class VersionedApiGroup { }

[MapGet("/products", Group = typeof(VersionedApiGroup))]
public class ListProductsEndpoint
{
    // The {version} token from the group prefix binds here by name.
    public Task<IResult> HandleAsync(string version) => ...;
}
// GET /api/v2/products  ->  version == "2"
```

Route constraints and tokens inherited from parent groups behave the same way:

```csharp
[MapGroup("/tenants/{tenant}")]
public class TenantGroup { }

// Adds a constrained token; the parent's {tenant} token still flows through.
[MapGroup("/v{version:int}", ParentGroup = typeof(TenantGroup))]
public class TenantApiGroup { }

[MapGet("/orders", Group = typeof(TenantApiGroup))]
public class ListOrdersEndpoint
{
    public Task<IResult> HandleAsync(string tenant, int version) => ...;
}
// GET /tenants/acme/v3/orders  ->  tenant == "acme", version == 3
// GET /tenants/acme/vX/orders  ->  404 (the :int constraint rejects "X")
```

**Notes:**
- **Bind by name**: the handler parameter name must match the token name (case-insensitive). A
  mismatch is not bound from the route — it falls back to query-string binding, so a required
  parameter returns `400`. `[FromRoute]` is optional and behaves identically.
- **Constraints & composition**: route constraints (`{version:int}`), optional tokens, and tokens
  inherited from parent groups all behave exactly as in a hand-written `MapGroup` prefix.
- **No reflection**: this is standard ASP.NET Core route binding — AOT-friendly, with no runtime
  reflection added by the generator.


### Service Interface

Register as an interface instead of concrete class:

```csharp
public interface IHealthCheck { }

[MapGet("/health", ServiceType = typeof(IHealthCheck))]
public class HealthCheckEndpoint : IHealthCheck
{
    public Task<IResult> HandleAsync() => ...;
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

### Conditional Mapping

Implement `IConditionallyMapped` to decide **at startup** whether an endpoint or group is mapped, based on the `IApplicationBuilder` (environment, configuration, feature flags). Implement `static bool ShouldMap(IApplicationBuilder app)`:

```csharp
using MinimalEndpoints;

// Endpoint mapped only outside Production
[MapGet("/diagnostics")]
public class DiagnosticsEndpoint : IConditionallyMapped
{
    public IResult Handle() => Results.Ok("diagnostics");

    public static bool ShouldMap(IApplicationBuilder app) =>
        app.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
}

// A group can be conditional too — when it is skipped, all of its endpoints
// (and any child groups) are skipped with it.
[MapGroup("/api/v2")]
public class ApiV2Group : IConditionallyMapped
{
    public static bool ShouldMap(IApplicationBuilder app) =>
        app.ApplicationServices.GetRequiredService<IConfiguration>().GetValue<bool>("Features:V2");
}
```

Semantics (these are exactly what the generator emits):

- `ShouldMap` gates **route mapping only** — services are still registered with `AddMinimalEndpoints()` regardless, so DI is unaffected.
- A `false` result on an endpoint skips just that endpoint; a `false` result on a group skips the group **and every endpoint and child group beneath it**.
- It is evaluated once during `UseMinimalEndpoints()`.

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

You can annotate request types with standard DataAnnotations attributes, but be
aware that **ASP.NET Core minimal APIs do NOT run DataAnnotations validation
automatically**. Annotated payloads reach your handler unvalidated unless you
either enable the .NET 10 validation source generator with
`builder.Services.AddValidation()` (MinimalEndpoints itself exposes no
`WithValidation()` helper — apply any extra filters through
`IConfigurableEndpoint.Configure`, e.g. `endpoint.AddEndpointFilter(...)`) or
validate explicitly inside the handler. On net8.0/net9.0, `AddValidation()` is
unavailable, so validate manually or use a library such as FluentValidation.

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
        // DataAnnotations are NOT enforced by minimal APIs on their own.
        // Validate explicitly (or enable AddValidation() on .NET 10):
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, context, results, validateAllProperties: true))
            return Results.ValidationProblem(
                results.ToDictionary(r => r.MemberNames.FirstOrDefault() ?? "", r => new[] { r.ErrorMessage ?? "" }));

        // ... request is valid
        return Results.Ok();
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
internal static partial class MinimalEndpointExtensions
{
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services)
    {
        services.AddScoped<GetUserEndpoint>();
        // ... other endpoints
        return services;
    }

    private static IEndpointRouteBuilder Map__Api_GetUserEndpoint(
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
        var builder = app as IEndpointRouteBuilder
            ?? throw new ArgumentException("IApplicationBuilder is not an IEndpointRouteBuilder");
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
- ✅ **MINEP008**: Flags endpoint/group classes with an unsupported shape (open generic, file-local, or below `internal`)

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
3. Confirm the analyzer reference is present (Dependencies → Analyzers → `MinimalEndpoints.CodeGeneration`)
4. Ensure you have the correct NuGet package version and a supported .NET SDK (8.0+)

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

### MINEP002: Multiple Map Attributes
Prevents multiple `Map*` attributes on the same class. To handle several HTTP methods on one route, use a single `[MapMethods("/route", new[] { "GET", "POST" })]`.

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

### MINEP008: Unsupported Endpoint or Group Shape
Warns (does not error) when an endpoint or group class cannot be mapped because of its shape — an open generic type, a file-local type, or a type whose effective accessibility is below `internal`. The generator skips such classes, so this surfaces *why* nothing was generated for them.

```csharp
// ⚠️ Warning: open generic types cannot be referenced from generated code
[MapGet("/items")]
public class GetItemsEndpoint<T>
{
    public IResult Handle() => Results.Ok();
}
```

[Learn more →](docs/diagnostics/MINEP008.md)

---

## 📚 Examples & Samples

### Sample Projects

Explore complete working examples in the `samples/` directory:

- **[Basic Sample](samples/MinimalEndpoints.Sample/)** - Minimal GET/POST endpoints with dependency injection
- **[Advanced Sample](samples/MinimalEndpoints.AdvancedSample/)** - CRUD with groups, IConfigurableEndpoint, validation, OpenAPI

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
| 10        | 1.359 ms | 0.603 ms  | 0.399 ms  | 15.6250  | -       | 345.29 KB  |
| 50        | 2.819 ms | 1.027 ms  | 0.680 ms  | 54.6875  | 7.8125  | 1314.10 KB |
| 100       | 6.320 ms | 3.490 ms  | 2.308 ms  | 62.5000  | 15.6250 | 2512.93 KB |

#### Code Generation Performance

| Scenario          | Mean       | Error     | StdDev    | Gen0     | Allocated  |
|-------------------|------------|-----------|-----------|----------|------------|
| 10 (cold)         | 329.5 μs   | 44.85 μs  | 26.69 μs  | 11.7188  | 245.99 KB  |
| 50 (cold)         | 1,069.6 μs | 100.77 μs | 52.70 μs  | 39.0625  | 821.77 KB  |
| 100 (cold)        | 2,321.7 μs | 280.33 μs | 146.62 μs | 62.5000  | 1538.35 KB |
| 500 (cold)        | 8,999.8 μs | 177.88 μs | 117.66 μs | 171.8750 | 7259.79 KB |
| 100 (incremental) | 444.0 μs   | 3.30 μs   | 2.18 μs   | 19.5313  | 387.52 KB  |

_**Cold** = a fresh generator driver over a clean N-endpoint compilation. **Incremental** = a warm
re-run after a single-line edit, exercising Roslyn's incremental caching. The 500-endpoint cold run
also triggers Gen1/Gen2 collections; see the [detailed report](benchmarks/README.md)._

**Key Takeaways:**
- ⚡ **Fast cold generation** — ~0.33 ms for 10 endpoints, scaling roughly linearly to ~9 ms for 500
- ✅ **Incremental re-builds are ~5× cheaper** — a warm re-run after a one-line edit on a 100-endpoint project takes ~0.44 ms vs ~2.3 ms cold
- 💾 **Allocation scales linearly** at roughly ~15 KB per generated endpoint
- 🔍 **Analyzer diagnostics** stay under ~7 ms for 100 endpoints

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
git clone https://github.com/smavrommatis/MinimalEndpoints.git
cd MinimalEndpoints
dotnet build
dotnet test
```

See [CHANGELOG.md](CHANGELOG.md) for version history.

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
