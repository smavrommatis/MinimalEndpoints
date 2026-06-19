# MinimalEndpoints - Advanced Sample

This sample demonstrates advanced features of MinimalEndpoints including:

## Features Demonstrated

### 1. IConfigurableEndpoint
All endpoints implement `IConfigurableEndpoint` to configure:
- OpenAPI documentation (operation summaries/descriptions)
- Caching policies
- HTTP status codes
- Tags and names

### 2. Hierarchical Groups (`[MapGroup]`)
Endpoints are routed through a two-level group hierarchy rather than carrying full paths:
- `ApiV1Group` declares the `/api/v1` prefix.
- `ProductsGroup` declares `/products` and sets `ApiV1Group` as its `ParentGroup`.
- An endpoint such as `[MapGet("/", Group = typeof(ProductsGroup))]` therefore resolves to
  `/api/v1/products`. The prefixes are composed by the generator, so endpoint patterns stay
  short and the prefix lives in one place.

### 3. Conditional Mapping (`IConditionallyMapped`)
`ApiV1Group` implements `IConditionallyMapped`. Its `ShouldMap` method is evaluated once during
startup and decides whether the group (and everything under it) is mapped:

```csharp
public static bool ShouldMap(IApplicationBuilder app) => true;
```

Returning `false` would skip the group, its endpoints, and any child groups — useful for
feature flags or environment-gated APIs.

### 4. Dependency Injection
- Constructor injection with `IProductRepository`
- Logger injection
- Service lifetimes

### 5. Route Constraints
- Integer constraints: `/{id:int}`
- Type-safe parameter binding

### 6. Validation
- Request validation
- Error responses
- BadRequest with details

### 7. OpenAPI Integration
- Built-in OpenAPI document via `AddOpenApi()` / `MapOpenApi()`
- Interactive API reference served by [Scalar](https://github.com/scalar/scalar)
  (`MapScalarApiReference()`)
- Per-endpoint operation summaries and descriptions via `AddOpenApiOperationTransformer(...)`

### 8. Output Caching
Output caching is wired up: `Program.cs` calls `AddOutputCache()` and `UseOutputCache()`, and
the `.CacheOutput(...)` policies on `ApiV1Group` and `ListProductsEndpoint` are backed by that
middleware (list responses are cached for 5 minutes).

### 9. Logging
- Structured logging
- Request/response logging

## Running the Sample

```bash
dotnet run
```

Then navigate to (Development environment only):
- Scalar API reference: `https://localhost:7207/scalar`
- OpenAPI document: `https://localhost:7207/openapi/v1.json`
- API base: `https://localhost:7207/api/v1/products`

The sample listens on `https://localhost:7207` and `http://localhost:5160`.

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/products` | List all products |
| GET | `/api/v1/products/{id:int}` | Get product by ID |
| POST | `/api/v1/products` | Create new product |
| PUT | `/api/v1/products/{id:int}` | Update an existing product |
| DELETE | `/api/v1/products/{id:int}` | Delete a product |

## Example Requests

### List Products
```bash
curl -k https://localhost:7207/api/v1/products
```

### Get Product
```bash
curl -k https://localhost:7207/api/v1/products/1
```

### Create Product
```bash
curl -k -X POST https://localhost:7207/api/v1/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "New Product",
    "description": "Product description",
    "price": 99.99,
    "stock": 10
  }'
```

A successful create returns `201 Created` with a `Location` header pointing at the named
`GetProduct` route (`/api/v1/products/{id}`).

## Key Files

- `Program.cs` - Application startup, OpenAPI/Scalar, and output-caching configuration
- `Endpoints/` - Endpoint implementations
  - `ListProductsEndpoint.cs` - GET `/api/v1/products`
  - `GetProductEndpoint.cs` - GET `/api/v1/products/{id:int}`
  - `CreateProductEndpoint.cs` - POST `/api/v1/products`
  - `UpdateProductEndpoint.cs` - PUT `/api/v1/products/{id:int}`
  - `DeleteProductEndpoint.cs` - DELETE `/api/v1/products/{id:int}`
- `Groups/` - Route group hierarchy
  - `ApiV1Group.cs` - `/api/v1` prefix, shared configuration, conditional mapping
  - `ProductsGroup.cs` - `/products` child group (parent: `ApiV1Group`)
- `Services/` - Application services
  - `ProductRepository.cs` - In-memory product storage (`InMemoryProductRepository`)
- `Models/` - Domain models and DTOs (`Product`, `CreateProductRequest`)

## Learning Points

### Hierarchical Group Definition
```csharp
[MapGroup("/api/v1")]
public class ApiV1Group : IConfigurableGroup, IConditionallyMapped
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group
            .WithTags("V1")
            .CacheOutput(x => x.Expire(TimeSpan.FromMinutes(5)));
    }

    public static bool ShouldMap(IApplicationBuilder app) => true;
}

[MapGroup("/products", ParentGroup = typeof(ApiV1Group))]
public class ProductsGroup { }
```

`ProductsGroup` is a bare class — implementing `IConfigurableGroup` is optional; a group only
needs `[MapGroup]`.

### IConfigurableEndpoint Pattern
```csharp
[MapGet("/", Group = typeof(ProductsGroup))]  // -> /api/v1/products
public class ListProductsEndpoint : IConfigurableEndpoint
{
    public async Task<IResult> HandleAsync() { }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("ListProducts")
            .WithTags("Products")
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    }
}
```

### Route Constraints
```csharp
[MapGet("/{id:int}", Group = typeof(ProductsGroup))]  // -> /api/v1/products/{id:int}
public class GetProductEndpoint
{
    public async Task<IResult> HandleAsync(int id) { }
}
```

### Request Validation
```csharp
public async Task<IResult> HandleAsync([FromBody] CreateProductRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "Product name is required" });
    }
    // ...
}
```

## Next Steps

1. Explore the code in the `Endpoints/` and `Groups/` directories
2. Try adding new endpoints to `ProductsGroup` (or a new group under `ApiV1Group`)
3. Modify the repository to use a real database
4. Add authentication and authorization in `ApiV1Group.ConfigureGroup`

## See Also

- [MinimalEndpoints Documentation](../../docs/)
- [Basic Sample](../MinimalEndpoints.Sample/)
