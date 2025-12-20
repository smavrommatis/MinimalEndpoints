# Getting Started with MinimalEndpoints

This guide will get you up and running with MinimalEndpoints in 5 minutes.

## Prerequisites

- .NET 8.0 or later (recommended: .NET 10.0)
- Visual Studio 2022, VS Code, or JetBrains Rider

## Installation

Install the NuGet package:

```bash
dotnet add package Blackeye.MinimalEndpoints
```

Or using Package Manager Console:

```powershell
Install-Package Blackeye.MinimalEndpoints
```

## Your First Endpoint

### Step 1: Create an Endpoint Class

Create a new file `HelloEndpoint.cs`:

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

### Step 2: Register MinimalEndpoints

In your `Program.cs`, add two lines:

```csharp
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Add this line - registers all endpoint classes
builder.Services.AddMinimalEndpoints();

var app = builder.Build();

// Add this line - maps all endpoints to routes
app.UseMinimalEndpoints();

app.Run();
```

### Step 3: Run Your Application

```bash
dotnet run
```

Navigate to `https://localhost:5001/hello` and you'll see:

```json
"Hello, World!"
```

## What Just Happened?

1. **Source Generator** scanned your code at compile-time
2. Found the `[MapGet]` attribute on `HelloEndpoint`
3. Generated extension methods:
   - `AddMinimalEndpoints()` - registers `HelloEndpoint` in DI
   - `UseMinimalEndpoints()` - maps the endpoint to the route
4. No runtime reflection or discovery needed!

## Adding More Endpoints

Create a `GetUserEndpoint.cs`:

```csharp
[MapGet("/users/{id}")]
public class GetUserEndpoint
{
    public IResult Handle(int id)
    {
        return Results.Ok(new { Id = id, Name = "John Doe" });
    }
}
```

No need to update `Program.cs` - the generator finds it automatically!

Test it:
```
GET https://localhost:5001/users/123
```

Response:
```json
{
  "id": 123,
  "name": "John Doe"
}
```

## Adding POST Endpoints

Create a `CreateUserEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

public record CreateUserRequest(string Name, string Email);

[MapPost("/users")]
public class CreateUserEndpoint
{
    public IResult Handle([FromBody] CreateUserRequest request)
    {
        // Validate and create user
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest("Name is required");

        var user = new { Id = 1, request.Name, request.Email };
        return Results.Created($"/users/{user.Id}", user);
    }
}
```

Test it:
```bash
curl -X POST https://localhost:5001/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Jane","email":"jane@example.com"}'
```

## Using Dependency Injection

Update your endpoint to use services:

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    private readonly ILogger<GetUsersEndpoint> _logger;

    // Constructor injection
    public GetUsersEndpoint(ILogger<GetUsersEndpoint> logger)
    {
        _logger = logger;
    }

    public IResult Handle()
    {
        _logger.LogInformation("Getting all users");

        var users = new[]
        {
            new { Id = 1, Name = "John" },
            new { Id = 2, Name = "Jane" }
        };

        return Results.Ok(users);
    }
}
```

The endpoint is automatically registered as `Scoped` in the DI container!

## Project Structure

Here's a recommended structure:

```
MyApi/
├── Program.cs
├── Endpoints/
│   ├── Users/
│   │   ├── GetUserEndpoint.cs
│   │   ├── GetUsersEndpoint.cs
│   │   ├── CreateUserEndpoint.cs
│   │   └── UpdateUserEndpoint.cs
│   └── Products/
│       ├── GetProductEndpoint.cs
│       └── CreateProductEndpoint.cs
└── Models/
    ├── User.cs
    └── Product.cs
```

## Viewing Generated Code

Want to see what's generated? In Visual Studio or Rider:

1. Right-click the project → **Analyze** → **View Source Generators**
2. Find `MinimalEndpoints.Analyzers` → `MinimalEndpointExtensions.g.cs`

You'll see:
```csharp
public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services)
{
    services.AddScoped<HelloEndpoint>();
    services.AddScoped<GetUserEndpoint>();
    services.AddScoped<CreateUserEndpoint>();
    // ... more registrations
    return services;
}

public static IApplicationBuilder UseMinimalEndpoints(this IApplicationBuilder app)
{
    var builder = app as IEndpointRouteBuilder;
    builder.Map__HelloEndpoint(app);
    builder.Map__GetUserEndpoint(app);
    builder.Map__CreateUserEndpoint(app);
    // ... more mappings
    return app;
}
```

## Compile-Time Safety

Try this - remove the `Handle` method:

```csharp
[MapGet("/broken")]
public class BrokenEndpoint
{
    // Oops, no Handle method!
}
```

You'll immediately see:

```
Error MINEP001: Class 'BrokenEndpoint' is marked with MapMethodsAttribute
but does not contain a valid entry point method. Add a public instance
method named 'Handle', 'HandleAsync', or specify a custom method name
using the EntryPoint property.
```

With a quick fix option to add the method automatically!

## Common Patterns

### Async Endpoints (Recommended)

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public async Task<IResult> HandleAsync()
    {
        await Task.Delay(100); // Simulate async work
        return Results.Ok();
    }
}
```

### Query Parameters

```csharp
[MapGet("/search")]
public class SearchEndpoint
{
    public IResult Handle(string? query, int page = 1, int pageSize = 10)
    {
        // Parameters automatically bound from query string
        return Results.Ok(new { query, page, pageSize });
    }
}
```

### Multiple HTTP Methods

```csharp
[MapMethods("/data", new[] { "GET", "HEAD" })]
public class DataEndpoint
{
    public IResult Handle() => Results.Ok("Data");
}
```

## Next Steps

Now that you have the basics:

1. **[Basic Endpoints](02-basic-endpoints.md)** - Explore all HTTP methods
2. **[Dependency Injection](03-dependency-injection.md)** - Advanced DI patterns
3. **[Endpoint Groups](06-endpoint-groups.md)** - Organize related endpoints
4. **[Configurable Endpoints](08-configurable-endpoints.md)** - Advanced configuration

## Troubleshooting

### Generator Not Running?

1. Clean and rebuild: `dotnet clean && dotnet build`
2. Restart IDE
3. Check that package is properly installed: `dotnet list package`

### Endpoints Not Found?

1. Ensure you called both `AddMinimalEndpoints()` and `UseMinimalEndpoints()`
2. Check the endpoint class is `public` and not `abstract`
3. Verify the attribute is correct: `[MapGet("/route")]`

### More Issues?

See [Troubleshooting Guide](../TROUBLESHOOTING.md) for common problems and solutions.

---

**Ready for more?** Continue to [Basic Endpoints →](02-basic-endpoints.md)

