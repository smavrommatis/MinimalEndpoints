# MinimalEndpoints Examples

This directory contains comprehensive examples demonstrating all features of MinimalEndpoints.

## 📚 Example Guides

### Getting Started
- **[01 - Getting Started](01-getting-started.md)** - Your first endpoint in 5 minutes
- **[02 - Basic Endpoints](02-basic-endpoints.md)** - Simple GET, POST, PUT, DELETE endpoints

### Core Concepts
- **[03 - Dependency Injection](03-dependency-injection.md)** - Constructor and parameter injection
- **[04 - Request Binding](04-request-binding.md)** - Route parameters, query strings, request bodies
- **[05 - Response Types](05-response-types.md)** - Returning different result types

### Advanced Features
- **[06 - Endpoint Groups](06-endpoint-groups.md)** - Organizing endpoints with IEndpointGroup
- **[07 - Hierarchical Groups](07-hierarchical-groups.md)** - Multi-level group structures
- **[08 - Configurable Endpoints](08-configurable-endpoints.md)** - Using IConfigurableEndpoint
- **[09 - Custom Entry Points](09-custom-entry-points.md)** - Custom method names
- **[10 - Service Interfaces](10-service-interfaces.md)** - Interface-based registration

### Integration & Best Practices
- **[11 - ASP.NET Core Integration](11-aspnetcore-integration.md)** - Versioning, caching, rate limiting, telemetry, authorization
- **[12 - Validation](12-validation.md)** - Input validation patterns
- **[13 - Error Handling](13-error-handling.md)** - Handling exceptions and errors
- **[14 - Testing](14-testing.md)** - Unit and integration testing
- **[15 - Async Patterns](15-async-patterns.md)** - Async/await and streaming

## 🎯 Quick Examples

### Simple GET Endpoint
```csharp
[MapGet("/hello")]
public class HelloEndpoint
{
    public IResult Handle() => Results.Ok("Hello, World!");
}
```

### Endpoint with Dependency Injection
```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    private readonly IUserRepository _repository;

    public GetUsersEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync()
    {
        var users = await _repository.GetAllAsync();
        return Results.Ok(users);
    }
}
```

### Endpoint with Groups
```csharp
[MapGroup("/api/v1")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization().WithOpenApi();
    }
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public IResult Handle() => Results.Ok();
}
// Route: /api/v1/products
```

## 📖 Learning Path

**New to MinimalEndpoints?** Follow this path:

1. Start with [Getting Started](01-getting-started.md)
2. Learn [Basic Endpoints](02-basic-endpoints.md)
3. Explore [Dependency Injection](03-dependency-injection.md)
4. Master [Endpoint Groups](06-endpoint-groups.md)
5. Dive into [ASP.NET Core Integration](11-aspnetcore-integration.md)

**Migrating from another approach?** See [Migration Guide](../MIGRATION.md)

**Having issues?** Check [Troubleshooting](../TROUBLESHOOTING.md)

## 🎓 Complete Example Projects

See the [samples](../../samples/) directory for complete working applications:
- **MinimalEndpoints.Sample** - Basic features demonstration
- **MinimalEndpoints.AdvancedSample** - Real-world patterns and practices

## 💡 Tips & Tricks

- **Prefer `HandleAsync`** - Use async methods for better scalability
- **Use Groups** - Organize related endpoints and share configuration
- **Implement `IConfigurableEndpoint`** - For advanced endpoint configuration
- **Service Lifetimes** - Default is `Scoped`, use `Singleton` for stateless endpoints
- **Type Safety** - Let analyzers catch errors at compile-time

## 🔗 Related Resources

- [Architecture](../ARCHITECTURE.md) - How it works
- [API Reference](../API_REFERENCE.md) - Complete API documentation
- [Diagnostics](../diagnostics/) - Analyzer error codes
- [Performance](../PERFORMANCE.md) - Benchmarks and optimization

---

**Have an example request?** [Open an issue](https://github.com/smavrommatis/MinimalEndpoints/issues) and we'll add it!

