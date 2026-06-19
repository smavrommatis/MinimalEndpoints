using Microsoft.AspNetCore.Http;
using MinimalEndpoints.Annotations;
using MinimalEndpoints.AdvancedSample.Services;
using MinimalEndpoints.AdvancedSample.Groups;

namespace MinimalEndpoints.AdvancedSample.Endpoints;

/// <summary>
/// Deletes a product by ID
/// </summary>
[MapDelete("/{id:int}", Group = typeof(ProductsGroup))]
public class DeleteProductEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;
    private readonly ILogger<DeleteProductEndpoint> _logger;

    public DeleteProductEndpoint(
        IProductRepository repository,
        ILogger<DeleteProductEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);

        var deleted = await _repository.DeleteAsync(id);

        if (!deleted)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return Results.NotFound(new { message = $"Product with ID {id} not found" });
        }

        return Results.NoContent();
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("DeleteProduct")
            .WithTags("Products")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Delete a product";
                operation.Description = "Deletes the product with the specified identifier";
                return Task.CompletedTask;
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}
