# ASP.NET Core Integration Examples

This guide shows how MinimalEndpoints seamlessly integrates with built-in ASP.NET Core features like API Versioning, Response Caching, Rate Limiting, OpenTelemetry, and Authorization.

**Important:** MinimalEndpoints generates standard Minimal API code, so you can use **ALL** built-in ASP.NET Core features without any wrappers or custom implementations.

## Table of Contents

1. [API Versioning](#api-versioning)
2. [Response Caching](#response-caching)
3. [Rate Limiting](#rate-limiting)
4. [OpenTelemetry](#opentelemetry)
5. [Authorization](#authorization)
6. [Combining Features](#combining-features)

---

## API Versioning

MinimalEndpoints works seamlessly with `Asp.Versioning.Http` package.

### Setup

```bash
dotnet add package Asp.Versioning.Http
```

### Configure in Program.cs

```csharp
using Asp.Versioning;
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddMinimalEndpoints();

var app = builder.Build();

app.UseMinimalEndpoints();
app.Run();
```

### Version 1.0 Endpoints

```csharp
using Asp.Versioning;
using MinimalEndpoints.Annotations;

[MapGet("/api/v{version:apiVersion}/users")]
public class GetUsersV1Endpoint : IConfigurableEndpoint
{
    private readonly IUserRepository _repository;

    public GetUsersV1Endpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var users = await _repository.GetAllAsync();
        return Results.Ok(users);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .HasApiVersion(new ApiVersion(1, 0))
            .WithName("GetUsersV1")
            .WithTags("Users");
    }
}

[MapGet("/api/v{version:apiVersion}/users/{id}")]
public class GetUserByIdV1Endpoint : IConfigurableEndpoint
{
    private readonly IUserRepository _repository;

    public GetUserByIdV1Endpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .HasApiVersion(new ApiVersion(1, 0))
            .WithName("GetUserByIdV1");
    }
}
```

### Version 2.0 Endpoints (with breaking changes)

```csharp
public record UserV2Dto(int Id, string FullName, string Email, DateTime CreatedAt);

[MapGet("/api/v{version:apiVersion}/users")]
public class GetUsersV2Endpoint : IConfigurableEndpoint
{
    private readonly IUserRepository _repository;

    public GetUsersV2Endpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var users = await _repository.GetAllAsync();

        // V2 returns different DTO with combined name field
        var usersV2 = users.Select(u => new UserV2Dto(
            u.Id,
            $"{u.FirstName} {u.LastName}",
            u.Email,
            u.CreatedAt
        ));

        return Results.Ok(usersV2);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .HasApiVersion(new ApiVersion(2, 0))
            .WithName("GetUsersV2")
            .WithTags("Users");
    }
}
```

### Using Groups for Versioning

```csharp
[MapGroup("/api/v1")]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.HasApiVersion(new ApiVersion(1, 0))
             .WithOpenApi();
    }
}

[MapGroup("/api/v2")]
public class V2Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.HasApiVersion(new ApiVersion(2, 0))
             .WithOpenApi();
    }
}

// All endpoints in V1Group automatically have version 1.0
[MapGet("/users", Group = typeof(V1Group))]
public class GetUsersV1Endpoint
{
    public async Task<IResult> HandleAsync() => Results.Ok();
}

// All endpoints in V2Group automatically have version 2.0
[MapGet("/users", Group = typeof(V2Group))]
public class GetUsersV2Endpoint
{
    public async Task<IResult> HandleAsync() => Results.Ok();
}
```

---

## Response Caching

MinimalEndpoints works with .NET's built-in output caching (introduced in .NET 7).

### Setup

```csharp
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Add output caching
builder.Services.AddOutputCache(options =>
{
    // Named policies
    options.AddPolicy("Default", builder => builder.Expire(TimeSpan.FromMinutes(1)));
    options.AddPolicy("Products", builder =>
        builder.Expire(TimeSpan.FromMinutes(5))
               .Tag("products"));
    options.AddPolicy("Users", builder =>
        builder.Expire(TimeSpan.FromMinutes(2))
               .SetVaryByQuery("page", "pageSize"));
});

builder.Services.AddMinimalEndpoints();

var app = builder.Build();

app.UseOutputCache(); // Must be before UseMinimalEndpoints
app.UseMinimalEndpoints();

app.Run();
```

### Cached Endpoint

```csharp
[MapGet("/api/products")]
public class GetProductsEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;

    public GetProductsEndpoint(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var products = await _repository.GetAllAsync();
        return Results.Ok(products);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .CacheOutput("Products") // Use named policy
            .WithName("GetProducts")
            .WithTags("Products");
    }
}
```

### Dynamic Caching

```csharp
[MapGet("/api/products/{id}")]
public class GetProductByIdEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;

    public GetProductByIdEndpoint(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product != null ? Results.Ok(product) : Results.NotFound();
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .CacheOutput(policy => policy
                .Expire(TimeSpan.FromMinutes(10))
                .SetVaryByRouteValue("id") // Cache per product ID
                .Tag("products"))
            .WithName("GetProductById");
    }
}
```

### Cache Invalidation

```csharp
using Microsoft.AspNetCore.OutputCaching;

[MapPost("/api/products")]
public class CreateProductEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;
    private readonly IOutputCacheStore _cacheStore;

    public CreateProductEndpoint(
        IProductRepository repository,
        IOutputCacheStore cacheStore)
    {
        _repository = repository;
        _cacheStore = cacheStore;
    }

    public async Task<IResult> HandleAsync([FromBody] CreateProductRequest request)
    {
        var product = await _repository.CreateAsync(request);

        // Invalidate products cache
        await _cacheStore.EvictByTagAsync("products", default);

        return Results.Created($"/api/products/{product.Id}", product);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("CreateProduct")
            .WithTags("Products");
    }
}
```

---

## Rate Limiting

MinimalEndpoints works with .NET's built-in rate limiting (introduced in .NET 7).

### Setup

```csharp
using System.Threading.RateLimiting;
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    // Fixed window limiter
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });

    // Sliding window limiter
    options.AddSlidingWindowLimiter("sliding", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 6; // 10 seconds per segment
    });

    // Token bucket limiter
    options.AddTokenBucketLimiter("token", opt =>
    {
        opt.TokenLimit = 100;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.TokensPerPeriod = 10;
        opt.AutoReplenishment = true;
    });

    // Concurrency limiter
    options.AddConcurrencyLimiter("concurrency", opt =>
    {
        opt.PermitLimit = 10;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
    });

    // Per-user limiter
    options.AddPolicy("perUser", context =>
    {
        var username = context.User.Identity?.Name ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(username, _ => new()
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});

builder.Services.AddMinimalEndpoints();

var app = builder.Build();

app.UseRateLimiter(); // Must be before UseMinimalEndpoints
app.UseMinimalEndpoints();

app.Run();
```

### Rate Limited Endpoints

```csharp
[MapPost("/api/orders")]
public class CreateOrderEndpoint : IConfigurableEndpoint
{
    private readonly IOrderService _orderService;

    public CreateOrderEndpoint(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IResult> HandleAsync([FromBody] CreateOrderRequest request)
    {
        var order = await _orderService.CreateAsync(request);
        return Results.Created($"/api/orders/{order.Id}", order);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireRateLimiting("fixed") // Apply rate limiting
            .WithName("CreateOrder")
            .WithTags("Orders");
    }
}
```

### Per-User Rate Limiting

```csharp
[MapGet("/api/reports/generate")]
public class GenerateReportEndpoint : IConfigurableEndpoint
{
    private readonly IReportService _reportService;

    public GenerateReportEndpoint(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IResult> HandleAsync()
    {
        var report = await _reportService.GenerateAsync();
        return Results.Ok(report);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireRateLimiting("perUser") // Per-user limiting
            .RequireAuthorization()
            .WithName("GenerateReport");
    }
}
```

### Group-Level Rate Limiting

```csharp
[MapGroup("/api/public")]
public class PublicApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        // All endpoints in this group are rate limited
        group.RequireRateLimiting("sliding");
    }
}

[MapGet("/search", Group = typeof(PublicApiGroup))]
public class SearchEndpoint
{
    public async Task<IResult> HandleAsync(string query)
    {
        // This endpoint is automatically rate limited
        return Results.Ok();
    }
}
```

---

## OpenTelemetry

MinimalEndpoints endpoints are automatically traced when you configure OpenTelemetry.

### Setup

```bash
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http
```

### Configure in Program.cs

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("MinimalEndpointsApi"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = (context) => !context.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddSource("MinimalEndpoints")
        .AddConsoleExporter()); // Or use Jaeger, Zipkin, etc.

builder.Services.AddMinimalEndpoints();

var app = builder.Build();

app.UseMinimalEndpoints();
app.Run();
```

### Custom Tracing in Endpoints

```csharp
using System.Diagnostics;
using MinimalEndpoints.Annotations;

[MapGet("/api/orders/{id}")]
public class GetOrderEndpoint
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private static readonly ActivitySource ActivitySource = new("MinimalEndpoints");

    public GetOrderEndpoint(
        IOrderRepository orderRepository,
        IPaymentService paymentService)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        using var activity = ActivitySource.StartActivity("GetOrder");
        activity?.SetTag("order.id", id);

        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
        {
            activity?.SetTag("order.found", false);
            return Results.NotFound();
        }

        activity?.SetTag("order.found", true);

        // Custom span for payment check
        using (var paymentActivity = ActivitySource.StartActivity("CheckPaymentStatus"))
        {
            paymentActivity?.SetTag("order.id", id);
            var paymentStatus = await _paymentService.GetStatusAsync(order.PaymentId);
            order.PaymentStatus = paymentStatus;
        }

        return Results.Ok(order);
    }
}
```

### Automatic Metrics

```csharp
using System.Diagnostics.Metrics;

[MapPost("/api/orders")]
public class CreateOrderEndpoint : IConfigurableEndpoint
{
    private readonly IOrderService _orderService;
    private static readonly Meter Meter = new("MinimalEndpoints");
    private static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>("orders.created");
    private static readonly Histogram<double> OrderValue = Meter.CreateHistogram<double>("order.value");

    public CreateOrderEndpoint(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IResult> HandleAsync([FromBody] CreateOrderRequest request)
    {
        var order = await _orderService.CreateAsync(request);

        // Record metrics
        OrdersCreated.Add(1, new KeyValuePair<string, object?>("status", "success"));
        OrderValue.Record(order.TotalAmount);

        return Results.Created($"/api/orders/{order.Id}", order);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("CreateOrder")
            .WithTags("Orders");
    }
}
```

---

## Authorization

MinimalEndpoints works seamlessly with ASP.NET Core's built-in authorization.

### Setup

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-auth-server.com";
        options.Audience = "your-api";
    });

// Add authorization with policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
    .AddPolicy("CanEditUsers", policy => policy.RequireClaim("permission", "users:write"))
    .AddPolicy("MinimumAge", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c => c.Type == "age" && int.Parse(c.Value) >= 18)));

builder.Services.AddMinimalEndpoints();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseMinimalEndpoints();

app.Run();
```

### Authorized Endpoints

```csharp
// Require any authenticated user
[MapGet("/api/profile")]
public class GetProfileEndpoint : IConfigurableEndpoint
{
    private readonly IUserService _userService;

    public GetProfileEndpoint(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IResult> HandleAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        var user = await _userService.GetByIdAsync(userId);
        return Results.Ok(user);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireAuthorization() // Requires authentication
            .WithName("GetProfile");
    }
}

// Require specific role
[MapDelete("/api/users/{id}")]
public class DeleteUserEndpoint : IConfigurableEndpoint
{
    private readonly IUserService _userService;

    public DeleteUserEndpoint(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        await _userService.DeleteAsync(id);
        return Results.NoContent();
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireAuthorization("AdminOnly") // Requires Admin role
            .WithName("DeleteUser");
    }
}

// Multiple policies
[MapPut("/api/users/{id}")]
public class UpdateUserEndpoint : IConfigurableEndpoint
{
    private readonly IUserService _userService;

    public UpdateUserEndpoint(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IResult> HandleAsync(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateAsync(id, request);
        return Results.Ok(user);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .RequireAuthorization("CanEditUsers") // Custom policy
            .WithName("UpdateUser");
    }
}
```

### Group-Level Authorization

```csharp
[MapGroup("/api/admin")]
public class AdminGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        // All endpoints in this group require Admin role
        group.RequireAuthorization("AdminOnly");
    }
}

[MapGet("/users", Group = typeof(AdminGroup))]
public class GetAllUsersEndpoint
{
    public async Task<IResult> HandleAsync()
    {
        // Automatically requires Admin role
        return Results.Ok();
    }
}
```

### Custom Authorization Handler

```csharp
using Microsoft.AspNetCore.Authorization;

// Custom requirement
public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }
    public MinimumAgeRequirement(int minimumAge) => MinimumAge = minimumAge;
}

// Custom handler
public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        var ageClaim = context.User.FindFirst("age");
        if (ageClaim != null && int.TryParse(ageClaim.Value, out var age))
        {
            if (age >= requirement.MinimumAge)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}

// Register in Program.cs
builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Adult", policy => policy.AddRequirements(new MinimumAgeRequirement(18)));

// Use in endpoint
[MapGet("/api/restricted")]
public class RestrictedEndpoint : IConfigurableEndpoint
{
    public IResult Handle() => Results.Ok();

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.RequireAuthorization("Adult");
    }
}
```

---

## Combining Features

You can combine all these features together:

```csharp
[MapGet("/api/v{version:apiVersion}/reports")]
public class GetReportsEndpoint : IConfigurableEndpoint
{
    private readonly IReportService _reportService;

    public GetReportsEndpoint(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IResult> HandleAsync()
    {
        var reports = await _reportService.GetAllAsync();
        return Results.Ok(reports);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            // API Versioning
            .HasApiVersion(new ApiVersion(1, 0))

            // Response Caching
            .CacheOutput(policy => policy
                .Expire(TimeSpan.FromMinutes(5))
                .Tag("reports"))

            // Rate Limiting
            .RequireRateLimiting("perUser")

            // Authorization
            .RequireAuthorization("CanViewReports")

            // Metadata
            .WithName("GetReports")
            .WithTags("Reports")
            .WithOpenApi();
    }
}
```

### Complete Example with All Features

```csharp
[MapGroup("/api/v{version:apiVersion}")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group
            .RequireAuthorization() // All endpoints require auth
            .RequireRateLimiting("sliding") // All endpoints rate limited
            .WithOpenApi(); // All endpoints in OpenAPI
    }
}

[MapGet("/products", Group = typeof(ApiGroup))]
public class GetProductsEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;

    public GetProductsEndpoint(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var products = await _repository.GetAllAsync();
        return Results.Ok(products);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .HasApiVersion(new ApiVersion(1, 0))
            .CacheOutput("Products")
            .WithName("GetProducts")
            .WithTags("Products");
    }
}
```

---

## Best Practices

1. **Use Groups for Shared Configuration** - Apply common settings (auth, rate limiting, caching) at the group level
2. **Cache Wisely** - Cache read operations, invalidate on writes
3. **Rate Limit Public APIs** - Protect your API from abuse
4. **Trace Important Operations** - Use OpenTelemetry for visibility
5. **Secure by Default** - Require authorization, then selectively allow anonymous access
6. **Version Early** - Start with v1 even if you don't have v2 yet

---

## See Also

- [Basic Endpoints](02-basic-endpoints.md) - HTTP method examples
- [Endpoint Groups](06-endpoint-groups.md) - Organizing endpoints
- [Configurable Endpoints](08-configurable-endpoints.md) - Advanced configuration
- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core/)

---

**Last Updated:** December 21, 2025

