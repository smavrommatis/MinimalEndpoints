using MinimalEndpoints.AdvancedSample.Groups;
using MinimalEndpoints.AdvancedSample.Services;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.AdvancedSample.Endpoints;

/// <summary>
/// Gets a specific product by ID with route constraint
/// </summary>
[MapGet("/{id:int}", Group = typeof(ProductsGroup))]
public class GetProductEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductEndpoint> _logger;

    public GetProductEndpoint(
        IProductRepository repository,
        ILogger<GetProductEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        _logger.LogInformation("Retrieving product with ID: {ProductId}", id);

        var product = await _repository.GetByIdAsync(id);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return Results.NotFound(new { message = $"Product with ID {id} not found" });
        }

        return Results.Ok(product);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("GetProduct")
            .WithTags("Products")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Get product by ID";
                operation.Description = "Retrieves a specific product by its unique identifier";
                return Task.CompletedTask;
            })
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
