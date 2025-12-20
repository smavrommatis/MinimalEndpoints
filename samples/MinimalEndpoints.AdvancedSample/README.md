# MinimalEndpoints - Advanced Sample

This sample demonstrates advanced features of MinimalEndpoints including:

## Features Demonstrated

### 1. IConfigurableEndpoint
All endpoints implement `IConfigurableEndpoint` to configure:
- OpenAPI/Swagger documentation
- Caching policies
- HTTP status codes
- Tags and names

### 2. Dependency Injection
- Constructor injection with `IProductRepository`
- Logger injection
- Service lifetimes

### 3. Route Constraints
- Integer constraints: `/api/products/{id:int}`
- Type-safe parameter binding

### 4. Validation
- Request validation
- Error responses
- BadRequest with details

### 5. OpenAPI Integration
- Swagger UI
- XML documentation
- Operation summaries and descriptions

### 6. Logging
- Structured logging
- Request/response logging

### 7. Caching
- Output caching with expiration

## Running the Sample

```bash
dotnet run
```

Then navigate to:
- Swagger UI: `https://localhost:5001/swagger`
- API Endpoints: `https://localhost:5001/api/products`

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products` | List all products |
| GET | `/api/products/{id}` | Get product by ID |
| POST | `/api/products` | Create new product |
| PUT | `/api/products/{id}` | Update product |
| DELETE | `/api/products/{id}` | Delete product |

## Example Requests

### List Products
```bash
curl https://localhost:5001/api/products
```

### Get Product
```bash
curl https://localhost:5001/api/products/1
```

### Create Product
```bash
curl -X POST https://localhost:5001/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "New Product",
    "description": "Product description",
    "price": 99.99,
    "stock": 10
  }'
```

### Update Product
```bash
curl -X PUT https://localhost:5001/api/products/1 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Product",
    "price": 149.99
  }'
```

### Delete Product
```bash
curl -X DELETE https://localhost:5001/api/products/1
```

## Key Files

- `Program.cs` - Application startup and configuration
- `Endpoints/` - Endpoint implementations
  - `ListProductsEndpoint.cs` - GET /api/products
  - `GetProductEndpoint.cs` - GET /api/products/{id}
  - `CreateProductEndpoint.cs` - POST /api/products
  - `UpdateProductEndpoint.cs` - PUT /api/products/{id}
  - `DeleteProductEndpoint.cs` - DELETE /api/products/{id}
- `Services/` - Application services
  - `ProductRepository.cs` - In-memory product storage
  - `AuthenticationService.cs` - Simple authentication
- `Models/` - Domain models and DTOs

## Learning Points

### IConfigurableEndpoint Pattern
```csharp
[MapGet("/api/products")]
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
[MapGet("/api/products/{id:int}")]  // Only matches numeric IDs
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
        return Results.BadRequest(new { message = "Name is required" });
    }
    // ...
}
```

## Next Steps

1. Explore the code in `Endpoints/` directory
2. Try adding new endpoints
3. Modify the repository to use a real database
4. Add authentication and authorization
5. Check out the Real-World Sample for production patterns

## See Also

- [MinimalEndpoints Documentation](../../docs/)
- [Basic Sample](../MinimalEndpoints.Sample/)
- [Real-World Sample](../MinimalEndpoints.RealWorldSample/)

