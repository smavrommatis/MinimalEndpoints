using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Annotations;
using MinimalEndpoints.AdvancedSample.Models;
using MinimalEndpoints.AdvancedSample.Services;
using MinimalEndpoints.AdvancedSample.Groups;

namespace MinimalEndpoints.AdvancedSample.Endpoints;

/// <summary>
/// Updates an existing product by ID
/// </summary>
[MapPut("/{id:int}", Group = typeof(ProductsGroup))]
public class UpdateProductEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;
    private readonly ILogger<UpdateProductEndpoint> _logger;

    public UpdateProductEndpoint(
        IProductRepository repository,
        ILogger<UpdateProductEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(int id, [FromBody] UpdateProductRequest request)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", id);

        var updated = await _repository.UpdateAsync(id, request);

        if (updated == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return Results.NotFound(new { message = $"Product with ID {id} not found" });
        }

        return Results.Ok(updated);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("UpdateProduct")
            .WithTags("Products")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Update an existing product";
                operation.Description = "Updates the specified fields of an existing product";
                return Task.CompletedTask;
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
