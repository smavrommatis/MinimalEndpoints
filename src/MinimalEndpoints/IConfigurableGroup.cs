using Microsoft.AspNetCore.Routing;

namespace MinimalEndpoints;

/// <summary>
/// Defines a contract for endpoint groups that can be configured with shared settings.
/// </summary>
/// <remarks>
/// Implement this interface to create reusable endpoint groups with common configuration
/// such as authentication, rate limiting, CORS policies, or OpenAPI metadata.
/// Endpoints reference the group using the <c>Group</c> property on mapping attributes.
/// </remarks>
/// <example>
/// <code>
/// [MapGroup("/api/v1")]
/// public class ApiV1Group : IConfigurableGroup
/// {
///     public void ConfigureGroup(RouteGroupBuilder group)
///     {
///         group.RequireAuthorization()
///              .WithOpenApi()
///              .WithRateLimiter("fixed");
///     }
/// }
///
/// [MapGet("/products", Group = typeof(ApiV1Group))]
/// public class ListProductsEndpoint
/// {
///     public Task&lt;IResult&gt; HandleAsync() =&gt; Task.FromResult(Results.Ok());
/// }
/// // Results in route: /api/v1/products
/// </code>
/// </example>
public interface IConfigurableGroup
{
    /// <summary>
    /// Configures the route group builder with shared settings for all endpoints in the group.
    /// </summary>
    /// <param name="group">The route group builder to configure.</param>
    /// <remarks>
    /// This method is called once during application startup when the group is created.
    /// Use it to apply middleware, authorization, metadata, or other configurations
    /// that should be shared by all endpoints in the group.
    /// </remarks>
    void ConfigureGroup(RouteGroupBuilder group);
}

