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
11. [Endpoint Groups](#endpoint-groups)
12. [Hierarchical Groups](#hierarchical-groups)

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
        RouteHandlerBuilder endpoint)
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

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
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

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
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

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
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

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
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

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
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

---

## ServiceType with IConfigurableEndpoint

Combine interface-based DI with endpoint configuration:

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// Define the service interface
public interface IGetUsersEndpoint
{
    Task<IResult> HandleAsync(int page, int pageSize);
}

// Implement both the interface and IConfigurableEndpoint
[MapGet("/api/users", ServiceType = typeof(IGetUsersEndpoint))]
public class GetUsersEndpoint : IGetUsersEndpoint, IConfigurableEndpoint
{
    private readonly IUserRepository _repository;

    public GetUsersEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var users = await _repository.GetPagedAsync(page, pageSize);
        return Results.Ok(users);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("GetUsers")
            .WithTags("Users")
            .WithOpenApi()
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    }
}

// Usage in your code
public class SomeService
{
    private readonly IGetUsersEndpoint _getUsersEndpoint;

    public SomeService(IGetUsersEndpoint getUsersEndpoint)
    {
        _getUsersEndpoint = getUsersEndpoint;
    }
}
```

---

## Complex Generic Parameters

Handle complex generic types seamlessly:

```csharp
using MinimalEndpoints.Annotations;

public class Result<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; } = new();
}

[MapPost("/api/process")]
public class ProcessDataEndpoint
{
    public async Task<Result<Dictionary<string, List<int>>>> HandleAsync(
        [FromBody] Dictionary<string, object> request)
    {
        // Process the data
        var processed = new Dictionary<string, List<int>>();

        foreach (var kvp in request)
        {
            // Convert values to List<int>
            var values = ConvertToIntList(kvp.Value);
            processed[kvp.Key] = values;
        }

        return new Result<Dictionary<string, List<int>>>
        {
            Success = true,
            Data = processed
        };
    }

    private List<int> ConvertToIntList(object value)
    {
        // Conversion logic
        return new List<int> { 1, 2, 3 };
    }
}
```

---

## Middleware Integration

Add middleware directly to your endpoint:

```csharp
using Microsoft.AspNetCore.RateLimiting;

[MapPost("/api/submit")]
public class SubmitDataEndpoint : IConfigurableEndpoint
{
    public async Task<IResult> HandleAsync([FromBody] SubmissionData data)
    {
        // Process submission
        return Results.Ok(new { submitted = true });
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireRateLimiting("fixed")
            .RequireAuthorization()
            .WithName("SubmitData")
            .WithTags("Submissions");
    }
}
```

---

## Authentication & Authorization

Secure your endpoints with auth policies:

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

[MapGet("/api/admin/reports")]
public class AdminReportsEndpoint : IConfigurableEndpoint
{
    private readonly IReportService _reportService;

    public AdminReportsEndpoint(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IResult> HandleAsync(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var reports = await _reportService.GetReportsAsync(from, to, userId);
        return Results.Ok(reports);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireAuthorization(policy => policy
                .RequireRole("Admin")
                .RequireClaim("permissions", "reports:read"))
            .WithTags("Admin", "Reports")
            .WithName("GetAdminReports");
    }
}

// Setup in Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminReports", policy =>
        policy.RequireRole("Admin"));
});
```

---

## OpenAPI / Swagger Integration

Generate OpenAPI documentation for your endpoints:

```csharp
using MinimalEndpoints.Annotations;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Creates a new user in the system
/// </summary>
[MapPost("/api/users")]
public class CreateUserEndpoint : IConfigurableEndpoint
{
    private readonly IUserService _userService;

    public CreateUserEndpoint(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="request">User creation details</param>
    /// <returns>Created user details</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="409">User already exists</response>
    public async Task<Results<Created<UserResponse>, BadRequest<ValidationProblemDetails>, Conflict>> HandleAsync(
        [FromBody] CreateUserRequest request)
    {
        if (await _userService.UserExistsAsync(request.Email))
        {
            return TypedResults.Conflict();
        }

        var user = await _userService.CreateAsync(request);
        return TypedResults.Created($"/api/users/{user.Id}", user);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("CreateUser")
            .WithTags("Users")
            .WithOpenApi(operation =>
            {
                operation.Summary = "Create a new user";
                operation.Description = "Creates a new user account with the provided details";
                return operation;
            })
            .Produces<UserResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
    }
}

public record CreateUserRequest(
    [Required] string Email,
    [Required] string Name,
    string? PhoneNumber);

public record UserResponse(int Id, string Email, string Name);
```

---

## File Upload Endpoint

Handle file uploads with proper validation:

```csharp
using MinimalEndpoints.Annotations;

[MapPost("/api/files/upload")]
public class FileUploadEndpoint : IConfigurableEndpoint
{
    private readonly IFileStorage _storage;
    private readonly ILogger<FileUploadEndpoint> _logger;

    public FileUploadEndpoint(IFileStorage storage, ILogger<FileUploadEndpoint> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(
        IFormFile file,
        [FromForm] string? description,
        CancellationToken cancellationToken)
    {
        // Validate file
        if (file.Length == 0)
            return Results.BadRequest("File is empty");

        if (file.Length > 10 * 1024 * 1024) // 10 MB
            return Results.BadRequest("File too large");

        var allowedExtensions = new[] { ".jpg", ".png", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return Results.BadRequest("Invalid file type");

        // Save file
        var fileId = await _storage.SaveAsync(file.OpenReadStream(), file.FileName, cancellationToken);

        _logger.LogInformation("File uploaded: {FileName} ({FileId})", file.FileName, fileId);

        return Results.Ok(new { fileId, fileName = file.FileName, description });
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("UploadFile")
            .WithTags("Files")
            .DisableAntiforgery() // If not using CSRF tokens
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status400BadRequest);
    }
}
```

---

## Background Job Trigger

Trigger background jobs from endpoints:

```csharp
using MinimalEndpoints.Annotations;

[MapPost("/api/jobs/process-orders")]
public class TriggerOrderProcessingEndpoint : IConfigurableEndpoint
{
    private readonly IBackgroundJobClient _jobClient;

    public TriggerOrderProcessingEndpoint(IBackgroundJobClient jobClient)
    {
        _jobClient = jobClient;
    }

    public Task<IResult> HandleAsync([FromQuery] DateTime? since)
    {
        var jobId = _jobClient.Enqueue<IOrderProcessingJob>(
            job => job.ProcessOrdersAsync(since ?? DateTime.UtcNow.AddDays(-1), CancellationToken.None));

        return Task.FromResult(Results.Ok(new { jobId, status = "queued" }));
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireAuthorization("JobTrigger")
            .WithName("TriggerOrderProcessing")
            .WithTags("Jobs", "Orders");
    }
}
```

---

## Health Check Endpoint

Create custom health checks:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MinimalEndpoints.Annotations;

[MapGet("/health")]
public class HealthCheckEndpoint
{
    private readonly HealthCheckService _healthCheckService;

    public HealthCheckEndpoint(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<IResult> HandleAsync()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var result = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                data = e.Value.Data
            })
        };

        return report.Status == HealthStatus.Healthy
            ? Results.Ok(result)
            : Results.StatusCode(503); // Service Unavailable
    }
}

// Setup in Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RedisHealthCheck>("redis");
```

---

## Endpoint Groups

Group related endpoints with shared configuration:

### Basic Group

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// Define the group
[MapGroup("/api/v1", GroupName = "V1 API")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .WithOpenApi()
             .WithTags("V1");
    }
}

// Use the group in endpoints
[MapGet("/products", Group = typeof(ApiV1Group))]
public class ListProductsEndpoint
{
    public Task<IResult> HandleAsync()
    {
        // Results in route: /api/v1/products
        // With authorization, OpenAPI, and "V1" tag
        return Task.FromResult(Results.Ok(new[] { "Product1", "Product2" }));
    }
}

[MapGet("/products/{id}", Group = typeof(ApiV1Group))]
public class GetProductEndpoint
{
    public Task<IResult> HandleAsync(int id)
    {
        // Also uses /api/v1 prefix with same configuration
        return Task.FromResult(Results.Ok(new { id, name = "Product" }));
    }
}
```

### Group with Rate Limiting

```csharp
using System.Threading.RateLimiting;
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// Configure in Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});

// Define group with rate limiting
[MapGroup("/api", GroupName = "Public API")]
public class PublicApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireRateLimiting("api")
             .WithOpenApi();
    }
}

[MapGet("/status", Group = typeof(PublicApiGroup))]
public class GetStatusEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok(new { status = "healthy" }));
    }
}
```

### Group with CORS

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// Configure in Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
    {
        policy.WithOrigins("https://example.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Define group with CORS
[MapGroup("/api/external")]
public class ExternalApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireCors("ApiPolicy")
             .WithOpenApi();
    }
}

[MapGet("/data", Group = typeof(ExternalApiGroup))]
public class GetExternalDataEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok(new { data = "public" }));
    }
}
```

### Multiple API Versions

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// V1 API Group
[MapGroup("/api/v1", GroupName = "V1")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("V1")
             .WithOpenApi();
    }
}

// V2 API Group
[MapGroup("/api/v2", GroupName = "V2")]
public class ApiV2Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("V2")
             .WithOpenApi()
             .RequireRateLimiting("strict");
    }
}

// V1 Endpoint
[MapGet("/products", Group = typeof(ApiV1Group))]
public class ListProductsV1Endpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok(new[] { "V1Product" }));
    }
}

// V2 Endpoint with new features
[MapGet("/products", Group = typeof(ApiV2Group))]
public class ListProductsV2Endpoint
{
    public Task<IResult> HandleAsync([FromQuery] bool includeDetails = false)
    {
        return Task.FromResult(Results.Ok(new
        {
            products = new[] { "V2Product" },
            details = includeDetails ? "Enhanced data" : null
        }));
    }
}
```

---

## Hierarchical Groups

Create multi-level group hierarchies with cascading configuration:

### Basic Hierarchy

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// Root group
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi();  // Applies to all descendants
    }
}

// Child group
[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization();  // Applies to all V1 endpoints
    }
}

// Endpoint uses child group
[MapGet("/products", Group = typeof(V1Group))]
public class ListProductsEndpoint
{
    public Task<IResult> HandleAsync()
    {
        // Route: /api/v1/products
        // Configuration: OpenAPI + Authorization
        return Task.FromResult(Results.Ok(new[] { "Product1" }));
    }
}
```

### Three-Level Hierarchy

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// Level 1: Root
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi()
             .WithTags("API");
    }
}

// Level 2: Version
[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .WithTags("V1");
    }
}

// Level 3: Feature/Module
[MapGroup("/admin", ParentGroup = typeof(V1Group))]
public class AdminGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization("Admin")
             .WithTags("Admin")
             .RequireRateLimiting("admin");
    }
}

// Endpoint at deepest level
[MapGet("/users", Group = typeof(AdminGroup))]
public class ListAdminUsersEndpoint
{
    private readonly IUserRepository _userRepo;

    public ListAdminUsersEndpoint(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<IResult> HandleAsync()
    {
        // Route: /api/v1/admin/users
        // Configuration: OpenAPI + Auth + Admin Auth + Rate Limiting + Tags
        var users = await _userRepo.GetAllAsync();
        return Results.Ok(users);
    }
}
```

### Multiple Branches

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// Root group
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi();
    }
}

// Branch 1: Public API (V1)
[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("V1");
    }
}

// Branch 2: Internal API (V2)
[MapGroup("/v2", ParentGroup = typeof(ApiGroup))]
public class V2Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .WithTags("V2");
    }
}

// Sub-branch: V1 Products
[MapGroup("/products", ParentGroup = typeof(V1Group))]
public class V1ProductsGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("Products");
    }
}

// Sub-branch: V2 Products (with auth)
[MapGroup("/products", ParentGroup = typeof(V2Group))]
public class V2ProductsGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("Products");
    }
}

// Endpoint in V1 branch
[MapGet("/", Group = typeof(V1ProductsGroup))]
public class ListProductsV1Endpoint
{
    public Task<IResult> HandleAsync()
    {
        // Route: /api/v1/products
        // No auth required
        return Task.FromResult(Results.Ok(new[] { "Product1" }));
    }
}

// Endpoint in V2 branch
[MapGet("/", Group = typeof(V2ProductsGroup))]
public class ListProductsV2Endpoint
{
    public Task<IResult> HandleAsync()
    {
        // Route: /api/v2/products
        // Auth required (from V2Group)
        return Task.FromResult(Results.Ok(new[] { "Product1", "Product2" }));
    }
}
```

### Feature Module Organization

```csharp
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

// Root API group
[MapGroup("/api")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi()
             .RequireAuthorization();
    }
}

// Orders module
[MapGroup("/orders", ParentGroup = typeof(ApiGroup))]
public class OrdersGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("Orders")
             .RequireRateLimiting("orders");
    }
}

// Products module
[MapGroup("/products", ParentGroup = typeof(ApiGroup))]
public class ProductsGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("Products");
    }
}

// Users module
[MapGroup("/users", ParentGroup = typeof(ApiGroup))]
public class UsersGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithTags("Users")
             .RequireAuthorization("UserManagement");
    }
}

// Orders endpoints
[MapGet("/", Group = typeof(OrdersGroup))]
public class ListOrdersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        // Route: /api/orders
        return Task.FromResult(Results.Ok(Array.Empty<object>()));
    }
}

[MapGet("/{id}", Group = typeof(OrdersGroup))]
public class GetOrderEndpoint
{
    public Task<IResult> HandleAsync(int id)
    {
        // Route: /api/orders/{id}
        return Task.FromResult(Results.Ok(new { id }));
    }
}

// Products endpoints
[MapGet("/", Group = typeof(ProductsGroup))]
public class ListProductsEndpoint
{
    public Task<IResult> HandleAsync()
    {
        // Route: /api/products
        return Task.FromResult(Results.Ok(Array.Empty<object>()));
    }
}
```

### Benefits Demonstrated

**Hierarchical groups provide:**

1. **Nested Configuration**: Parent settings cascade to children
2. **Organized Structure**: Clear module/feature boundaries
3. **Reusable Configuration**: Define once at parent level
4. **API Versioning**: Easy to version by hierarchy
5. **Type-Safety**: Compile-time validation with MINEP006 (cycle detection)

**Example hierarchy:**
```
ApiGroup (/api)                          [OpenAPI]
  ├─ V1Group (/api/v1)                   [OpenAPI + Auth]
  │   ├─ OrdersGroup (/api/v1/orders)    [OpenAPI + Auth + RateLimit]
  │   └─ ProductsGroup (/api/v1/products)[OpenAPI + Auth]
  └─ V2Group (/api/v2)                   [OpenAPI + AdminAuth]
      └─ ProductsGroup (/api/v2/products)[OpenAPI + AdminAuth + Cache]
```


