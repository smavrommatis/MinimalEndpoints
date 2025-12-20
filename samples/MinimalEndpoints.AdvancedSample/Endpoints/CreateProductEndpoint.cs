using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Annotations;
using MinimalEndpoints.AdvancedSample.Models;
using MinimalEndpoints.AdvancedSample.Services;
using MinimalEndpoints.AdvancedSample.Groups;

namespace MinimalEndpoints.AdvancedSample.Endpoints;

/// <summary>
/// Creates a new product with validation
/// </summary>
[MapPost("/", Group = typeof(ProductsGroup))]
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

    public async Task<IResult> HandleAsync([FromBody] CreateProductRequest request)
    {
        _logger.LogInformation("Creating new product: {ProductName}", request.Name);

        // Validate
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { message = "Product name is required" });
        }

        if (request.Price <= 0)
        {
            return Results.BadRequest(new { message = "Price must be greater than zero" });
        }

        // Create product
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(product);

        _logger.LogInformation("Product created with ID: {ProductId}", created.Id);

        return Results.Created($"/api/products/{created.Id}", created);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("CreateProduct")
            .WithTags("Products")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Create a new product";
                operation.Description = "Creates a new product in the catalog";
                return Task.CompletedTask;
            })
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }
}

