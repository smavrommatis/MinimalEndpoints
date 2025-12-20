# MinimalEndpoints Examples

## Table of Contents
1. [Basic Endpoint](#basic-endpoint)
2. [Endpoint with Parameters](#endpoint-with-parameters)
3. [Endpoint with Dependency Injection](#endpoint-with-dependency-injection)
4. [Endpoint with Validation](#endpoint-with-validation)
5. [Configurable Endpoint](#configurable-endpoint)
6. [Custom Entry Point](#custom-entry-point)
7. [Service Interface](#service-interface)
8. [Multiple HTTP Methods](#multiple-http-methods)
9. [Different Lifetimes](#different-lifetimes)
10. [Complex Types](#complex-types)

---

## Basic Endpoint

The simplest possible endpoint:

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

**Generated:**
- Registration: `services.AddScoped<HelloEndpoint>()`
- Mapping: `builder.MapGet("/hello", Handler)`

---

## Endpoint with Parameters

### Route Parameters

```csharp
using MinimalEndpoints.Annotations;

[MapGet("/users/{id}")]
public class GetUserEndpoint
{
    public async Task<IResult> HandleAsync(int id)
    {
        // id is automatically bound from route
        var user = await GetUserById(id);
        return user != null
            ? Results.Ok(user)
            : Results.NotFound();
    }
}
```

### Query Parameters

```csharp
using MinimalEndpoints.Annotations;

[MapGet("/users")]
public class SearchUsersEndpoint
{
    public async Task<IResult> HandleAsync(
        string? search = null,
        int page = 1,
        int pageSize = 10)
    {
        var users = await SearchUsers(search, page, pageSize);
        return Results.Ok(users);
    }
}
```

### Request Body

```csharp
using MinimalEndpoints.Annotations;
using Microsoft.AspNetCore.Mvc;

public record CreateUserRequest(string Name, string Email);

[MapPost("/users")]
public class CreateUserEndpoint
{
    public async Task<IResult> HandleAsync(
        [FromBody] CreateUserRequest request)
    {
        var user = await CreateUser(request);
        return Results.Created($"/users/{user.Id}", user);
    }
}
```

---

## Endpoint with Dependency Injection

### Constructor Injection

```csharp
using MinimalEndpoints.Annotations;

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
        _logger.LogInformation("Fetching products");
        var products = await _repository.GetAllAsync();
        return Results.Ok(products);
    }
}
```

### Parameter Injection

```csharp
using MinimalEndpoints.Annotations;
using Microsoft.AspNetCore.Mvc;

[MapGet("/current-user")]
public class GetCurrentUserEndpoint
{
    public async Task<IResult> HandleAsync(
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IUserService userService)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (userId == null)
            return Results.Unauthorized();

        var user = await userService.GetByIdAsync(userId);
        return Results.Ok(user);
    }
}
```

---

## Endpoint with Validation

```csharp
using MinimalEndpoints.Annotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

public record UpdateUserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; init; }

    [Required, EmailAddress]
    public string Email { get; init; }

    [Range(18, 120)]
    public int Age { get; init; }
}

[MapPut("/users/{id}")]
public class UpdateUserEndpoint
{
    private readonly IUserRepository _repository;

    public UpdateUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(
        [FromRoute] int id,
        [FromBody] UpdateUserRequest request)
    {
        // Validation happens automatically via model binding
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return Results.NotFound();

        user.Name = request.Name;
        user.Email = request.Email;
        user.Age = request.Age;

        await _repository.UpdateAsync(user);
        return Results.NoContent();
    }
}
```

---

## Configurable Endpoint

Add metadata, authorization, caching, etc.:

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;
using Microsoft.AspNetCore.Authorization;

[MapGet("/admin/users")]
public class GetAdminUsersEndpoint : IConfigurableEndpoint
{
    private readonly IUserRepository _repository;

    public GetAdminUsersEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var users = await _repository.GetAllAsync();
        return Results.Ok(users);
    }

    public static void Configure(
        IApplicationBuilder app,
        IEndpointConventionBuilder endpoint)
    {
        endpoint
            .RequireAuthorization("AdminPolicy")
            .WithTags("Admin", "Users")
            .WithName("GetAdminUsers")
            .WithDescription("Retrieves all users (admin only)")
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    }
}
```

---

## Custom Entry Point

Use a different method name instead of `Handle` or `HandleAsync`:

```csharp
using MinimalEndpoints.Annotations;

[MapPost("/orders/{orderId}/process", EntryPoint = "ProcessOrder")]
public class ProcessOrderEndpoint
{
    private readonly IOrderService _orderService;

    public ProcessOrderEndpoint(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // Custom entry point name
    public async Task<IResult> ProcessOrder(int orderId)
    {
        var result = await _orderService.ProcessAsync(orderId);
        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result.Error);
    }

    // This method won't be used as the entry point
    public async Task<bool> ValidateOrder(int orderId)
    {
        return await _orderService.IsValidAsync(orderId);
    }
}
```

---

## Service Interface

Register endpoint as an interface:

```csharp
using MinimalEndpoints.Annotations;

public interface IHealthCheckEndpoint
{
    Task<IResult> HandleAsync();
}

[MapGet("/health", ServiceName = typeof(IHealthCheckEndpoint))]
public class HealthCheckEndpoint : IHealthCheckEndpoint
{
    private readonly IDatabase _database;

    public HealthCheckEndpoint(IDatabase database)
    {
        _database = database;
    }

    public async Task<IResult> HandleAsync()
    {
        var isHealthy = await _database.PingAsync();
        return isHealthy
            ? Results.Ok(new { status = "healthy" })
            : Results.ServiceUnavailable();
    }
}
```

**Generated:**
```csharp
services.AddScoped<IHealthCheckEndpoint, HealthCheckEndpoint>();
```

---

## Multiple HTTP Methods

### Using MapMethods

```csharp
using MinimalEndpoints.Annotations;

[MapMethods("/data", new[] { "GET", "POST", "PUT" })]
public class DataEndpoint
{
    public async Task<IResult> HandleAsync(HttpContext context)
    {
        return context.Request.Method switch
        {
            "GET" => await HandleGet(),
            "POST" => await HandlePost(),
            "PUT" => await HandlePut(),
            _ => Results.BadRequest()
        };
    }

    private Task<IResult> HandleGet() =>
        Task.FromResult(Results.Ok("GET response"));

    private Task<IResult> HandlePost() =>
        Task.FromResult(Results.Ok("POST response"));

    private Task<IResult> HandlePut() =>
        Task.FromResult(Results.Ok("PUT response"));
}
```

---

## Different Lifetimes

### Singleton

```csharp
using MinimalEndpoints.Annotations;

[MapGet("/config", ServiceLifetime.Singleton)]
public class GetConfigEndpoint
{
    private readonly IConfiguration _configuration;

    public GetConfigEndpoint(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IResult Handle()
    {
        return Results.Ok(new
        {
            Environment = _configuration["Environment"],
            Version = _configuration["Version"]
        });
    }
}
```

### Transient

```csharp
using MinimalEndpoints.Annotations;

[MapGet("/guid", ServiceLifetime.Transient)]
public class GetGuidEndpoint
{
    // New instance for each request
    private readonly Guid _instanceId = Guid.NewGuid();

    public IResult Handle()
    {
        return Results.Ok(new { instanceId = _instanceId });
    }
}
```

### Scoped (Default)

```csharp
using MinimalEndpoints.Annotations;

[MapGet("/weather")]  // Scoped by default
public class GetWeatherEndpoint
{
    private readonly IWeatherService _weatherService;

    public GetWeatherEndpoint(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task<IResult> HandleAsync()
    {
        var weather = await _weatherService.GetCurrentWeatherAsync();
        return Results.Ok(weather);
    }
}
```

---

## Complex Types

### Nested Generics

```csharp
using MinimalEndpoints.Annotations;
using System.Collections.Generic;

public record ProcessDataRequest(Dictionary<string, List<string>> Data);
public record ProcessDataResponse(Dictionary<string, List<int>> Results);

[MapPost("/process-data")]
public class ProcessDataEndpoint
{
    public async Task<ProcessDataResponse> HandleAsync(
        ProcessDataRequest request)
    {
        var results = new Dictionary<string, List<int>>();

        foreach (var (key, values) in request.Data)
        {
            results[key] = values.Select(v => v.Length).ToList();
        }

        return new ProcessDataResponse(results);
    }
}
```

### Arrays and Collections

```csharp
using MinimalEndpoints.Annotations;

public record BatchRequest
{
    public int[] Ids { get; init; }
    public string[] Tags { get; init; }
}

[MapPost("/batch-process")]
public class BatchProcessEndpoint
{
    public async Task<IResult> HandleAsync(BatchRequest request)
    {
        var results = await ProcessBatch(request.Ids, request.Tags);
        return Results.Ok(results);
    }

    private Task<object[]> ProcessBatch(int[] ids, string[] tags)
    {
        // Process batch
        return Task.FromResult(new object[0]);
    }
}
```

### Nullable Types

```csharp
using MinimalEndpoints.Annotations;

[MapGet("/search")]
public class SearchEndpoint
{
    public async Task<IResult> HandleAsync(
        string? query = null,
        int? page = null,
        int? pageSize = null,
        bool? includeInactive = null)
    {
        var results = await Search(
            query ?? "",
            page ?? 1,
            pageSize ?? 10,
            includeInactive ?? false
        );

        return Results.Ok(results);
    }
}
```

---

## Real-World Example: Complete CRUD

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Endpoints.Products;

// --- Models ---

public record Product
{
    public int Id { get; init; }
    public string Name { get; init; }
    public decimal Price { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateProductRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; }

    [Range(0.01, 999999.99)]
    public decimal Price { get; init; }
}

public record UpdateProductRequest
{
    [StringLength(200)]
    public string? Name { get; init; }

    [Range(0.01, 999999.99)]
    public decimal? Price { get; init; }
}

// --- GET /products ---

[MapGet("/products")]
public class ListProductsEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;

    public ListProductsEndpoint(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(
        int page = 1,
        int pageSize = 10)
    {
        var products = await _repository.GetPagedAsync(page, pageSize);
        return Results.Ok(products);
    }

    public static void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint)
    {
        endpoint
            .WithTags("Products")
            .WithName("ListProducts")
            .CacheOutput(c => c.Expire(TimeSpan.FromMinutes(5)));
    }
}

// --- GET /products/{id} ---

[MapGet("/products/{id}")]
public class GetProductEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;

    public GetProductEndpoint(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product != null
            ? Results.Ok(product)
            : Results.NotFound();
    }

    public static void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint)
    {
        endpoint
            .WithTags("Products")
            .WithName("GetProduct")
            .CacheOutput();
    }
}

// --- POST /products ---

[MapPost("/products")]
public class CreateProductEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;
    private readonly ILogger<CreateProductEndpoint> _logger;

    public CreateProductEndpoint(
        IProductRepository repository,
        ILogger<CreateProductEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(
        [FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("Creating product: {Name}", request.Name);

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(product);
        return Results.Created($"/products/{created.Id}", created);
    }

    public static void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint)
    {
        endpoint
            .WithTags("Products")
            .WithName("CreateProduct");
    }
}

// --- PUT /products/{id} ---

[MapPut("/products/{id}")]
public class UpdateProductEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;

    public UpdateProductEndpoint(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(
        int id,
        [FromBody] UpdateProductRequest request)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return Results.NotFound();

        var updated = product with
        {
            Name = request.Name ?? product.Name,
            Price = request.Price ?? product.Price
        };

        await _repository.UpdateAsync(updated);
        return Results.Ok(updated);
    }

    public static void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint)
    {
        endpoint
            .WithTags("Products")
            .WithName("UpdateProduct");
    }
}

// --- DELETE /products/{id} ---

[MapDelete("/products/{id}")]
public class DeleteProductEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;

    public DeleteProductEndpoint(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var exists = await _repository.ExistsAsync(id);
        if (!exists)
            return Results.NotFound();

        await _repository.DeleteAsync(id);
        return Results.NoContent();
    }

    public static void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint)
    {
        endpoint
            .WithTags("Products")
            .WithName("DeleteProduct");
    }
}
```

## Program.cs Setup

```csharp
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Register all endpoints
builder.Services.AddMinimalEndpoints();

// Register dependencies
builder.Services.AddScoped<IProductRepository, ProductRepository>();

var app = builder.Build();

// Map all endpoints
app.UseMinimalEndpoints();

app.Run();
```

That's it! All endpoints are automatically discovered, registered, and mapped.

