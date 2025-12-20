# Basic Endpoints - All HTTP Methods

This guide covers all HTTP methods supported by MinimalEndpoints.

## HTTP Methods Overview

MinimalEndpoints supports all standard HTTP methods:

| Attribute | HTTP Method | Common Use |
|-----------|-------------|------------|
| `[MapGet]` | GET | Read/retrieve data |
| `[MapPost]` | POST | Create new resources |
| `[MapPut]` | PUT | Update/replace resources |
| `[MapPatch]` | PATCH | Partial updates |
| `[MapDelete]` | DELETE | Delete resources |
| `[MapHead]` | HEAD | Metadata only (like GET without body) |
| `[MapMethods]` | Custom | Multiple methods or custom verbs |

## GET - Retrieve Resources

### Simple GET

```csharp
[MapGet("/api/health")]
public class HealthCheckEndpoint
{
    public IResult Handle()
    {
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
```

### GET with Route Parameters

```csharp
[MapGet("/api/users/{id}")]
public class GetUserEndpoint
{
    private readonly IUserRepository _repository;

    public GetUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user != null
            ? Results.Ok(user)
            : Results.NotFound();
    }
}
```

### GET with Query Parameters

```csharp
[MapGet("/api/users")]
public class ListUsersEndpoint
{
    private readonly IUserRepository _repository;

    public ListUsersEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        string sortBy = "name")
    {
        var users = await _repository.SearchAsync(search, page, pageSize, sortBy);
        return Results.Ok(new
        {
            data = users,
            page,
            pageSize,
            total = users.Count
        });
    }
}
```

## POST - Create Resources

### POST with Body

```csharp
using Microsoft.AspNetCore.Mvc;

public record CreateUserRequest(string Name, string Email, string? Phone);

[MapPost("/api/users")]
public class CreateUserEndpoint
{
    private readonly IUserRepository _repository;
    private readonly ILogger<CreateUserEndpoint> _logger;

    public CreateUserEndpoint(
        IUserRepository repository,
        ILogger<CreateUserEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync([FromBody] CreateUserRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Name is required" });

        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "Email is required" });

        // Create user
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(user);

        _logger.LogInformation("User created: {UserId}", created.Id);

        return Results.Created($"/api/users/{created.Id}", created);
    }
}
```

### POST with Form Data

```csharp
[MapPost("/api/contact")]
public class ContactFormEndpoint
{
    public IResult Handle(
        [FromForm] string name,
        [FromForm] string email,
        [FromForm] string message)
    {
        // Process form data
        return Results.Ok(new { success = true });
    }
}
```

## PUT - Update/Replace Resources

```csharp
public record UpdateUserRequest(string Name, string Email, string? Phone);

[MapPut("/api/users/{id}")]
public class UpdateUserEndpoint
{
    private readonly IUserRepository _repository;

    public UpdateUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(
        int id,
        [FromBody] UpdateUserRequest request)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return Results.NotFound();

        // Update all fields (PUT replaces the entire resource)
        user.Name = request.Name;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user);

        return Results.Ok(user);
    }
}
```

## PATCH - Partial Updates

```csharp
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

[MapPatch("/api/users/{id}")]
public class PatchUserEndpoint
{
    private readonly IUserRepository _repository;

    public PatchUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(
        int id,
        [FromBody] JsonPatchDocument<User> patchDoc)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return Results.NotFound();

        // Apply patch
        patchDoc.ApplyTo(user);
        user.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user);

        return Results.Ok(user);
    }
}
```

Example PATCH request:
```json
[
  { "op": "replace", "path": "/name", "value": "New Name" },
  { "op": "replace", "path": "/email", "value": "new@email.com" }
]
```

## DELETE - Remove Resources

```csharp
[MapDelete("/api/users/{id}")]
public class DeleteUserEndpoint
{
    private readonly IUserRepository _repository;
    private readonly ILogger<DeleteUserEndpoint> _logger;

    public DeleteUserEndpoint(
        IUserRepository repository,
        ILogger<DeleteUserEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return Results.NotFound();

        await _repository.DeleteAsync(id);

        _logger.LogInformation("User deleted: {UserId}", id);

        return Results.NoContent(); // 204 No Content
    }
}
```

### Soft Delete

```csharp
[MapDelete("/api/users/{id}")]
public class SoftDeleteUserEndpoint
{
    private readonly IUserRepository _repository;

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user == null)
            return Results.NotFound();

        // Soft delete - mark as deleted but keep in database
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(user);

        return Results.NoContent();
    }
}
```

## HEAD - Metadata Only

```csharp
[MapHead("/api/users/{id}")]
public class UserExistsEndpoint
{
    private readonly IUserRepository _repository;

    public UserExistsEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var exists = await _repository.ExistsAsync(id);
        return exists ? Results.Ok() : Results.NotFound();
    }
}
```

## Multiple HTTP Methods

### Same Handler for Multiple Methods

```csharp
[MapMethods("/api/ping", new[] { "GET", "HEAD" })]
public class PingEndpoint
{
    public IResult Handle()
    {
        return Results.Ok(new { message = "pong", timestamp = DateTime.UtcNow });
    }
}
```

### Custom HTTP Methods

```csharp
[MapMethods("/api/data", new[] { "GET", "OPTIONS" })]
public class DataEndpoint
{
    public IResult Handle(HttpContext context)
    {
        if (context.Request.Method == "OPTIONS")
        {
            return Results.Ok(new { methods = new[] { "GET", "OPTIONS" } });
        }

        return Results.Ok(new { data = "Some data" });
    }
}
```

## Complete CRUD Example

Here's a complete CRUD implementation for a Product resource:

```csharp
// Models
public record Product(int Id, string Name, decimal Price, int Stock);
public record CreateProductRequest(string Name, decimal Price, int Stock);
public record UpdateProductRequest(string Name, decimal Price, int Stock);

// GET /api/products
[MapGet("/api/products")]
public class ListProductsEndpoint
{
    private readonly IProductRepository _repository;

    public async Task<IResult> HandleAsync()
    {
        var products = await _repository.GetAllAsync();
        return Results.Ok(products);
    }
}

// GET /api/products/{id}
[MapGet("/api/products/{id}")]
public class GetProductEndpoint
{
    private readonly IProductRepository _repository;

    public async Task<IResult> HandleAsync(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        return product != null ? Results.Ok(product) : Results.NotFound();
    }
}

// POST /api/products
[MapPost("/api/products")]
public class CreateProductEndpoint
{
    private readonly IProductRepository _repository;

    public async Task<IResult> HandleAsync([FromBody] CreateProductRequest request)
    {
        var product = new Product(0, request.Name, request.Price, request.Stock);
        var created = await _repository.CreateAsync(product);
        return Results.Created($"/api/products/{created.Id}", created);
    }
}

// PUT /api/products/{id}
[MapPut("/api/products/{id}")]
public class UpdateProductEndpoint
{
    private readonly IProductRepository _repository;

    public async Task<IResult> HandleAsync(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null) return Results.NotFound();

        var updated = product with
        {
            Name = request.Name,
            Price = request.Price,
            Stock = request.Stock
        };

        await _repository.UpdateAsync(updated);
        return Results.Ok(updated);
    }
}

// DELETE /api/products/{id}
[MapDelete("/api/products/{id}")]
public class DeleteProductEndpoint
{
    private readonly IProductRepository _repository;

    public async Task<IResult> HandleAsync(int id)
    {
        var exists = await _repository.ExistsAsync(id);
        if (!exists) return Results.NotFound();

        await _repository.DeleteAsync(id);
        return Results.NoContent();
    }
}
```

## Best Practices

1. **Use Async Methods** - Always prefer `HandleAsync` for database/network operations
2. **Return Appropriate Status Codes**:
   - `Results.Ok()` - 200 OK
   - `Results.Created()` - 201 Created
   - `Results.NoContent()` - 204 No Content
   - `Results.BadRequest()` - 400 Bad Request
   - `Results.NotFound()` - 404 Not Found
   - `Results.Conflict()` - 409 Conflict
3. **Validate Input** - Always validate request data before processing
4. **Use Records for DTOs** - Immutable records are perfect for request/response types
5. **Log Important Actions** - Log creates, updates, and deletes
6. **Follow REST Conventions** - Use appropriate HTTP methods for their intended purpose

## Next Steps

- **[Request Binding](04-request-binding.md)** - Deep dive into parameter binding
- **[Dependency Injection](03-dependency-injection.md)** - Advanced DI patterns
- **[Validation](12-validation.md)** - Input validation strategies

---

[← Back to Examples](README.md) | [Next: Dependency Injection →](03-dependency-injection.md)

