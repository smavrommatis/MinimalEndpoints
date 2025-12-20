using MinimalEndpoints.Annotations;
using MinimalEndpoints.AdvancedSample.Services;

namespace MinimalEndpoints.AdvancedSample.Endpoints;

/// <summary>
/// Lists all products with optional filtering
/// </summary>
[MapGet("/api/products")]
public class ListProductsEndpoint : IConfigurableEndpoint
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ListProductsEndpoint> _logger;

    public ListProductsEndpoint(
        IProductRepository repository,
        ILogger<ListProductsEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync()
    {
        _logger.LogInformation("Retrieving all products");

        var products = await _repository.GetAllAsync();

        return Results.Ok(products);
    }

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint
            .WithName("ListProducts")
            .WithTags("Products")
            .AddOpenApiOperationTransformer((operation, context, ct) =>
            {
                operation.Summary = "Get all products";
                operation.Description = "Retrieves a list of all available products";
                return Task.CompletedTask;
            })
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));
    }
}

