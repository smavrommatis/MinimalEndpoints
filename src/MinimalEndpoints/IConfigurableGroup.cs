using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace MinimalEndpoints;

/// <summary>
/// Defines a contract for endpoint groups that can be configured with shared settings.
/// </summary>
/// <remarks>
/// Implement this interface to create reusable endpoint groups with common configuration
/// such as authentication, rate limiting, CORS policies, or OpenAPI metadata.
/// Endpoints reference the group using the <c>Group</c> property on mapping attributes.
/// <para>
/// <see cref="ConfigureGroup"/> is <see langword="static"/> — like
/// <see cref="IConfigurableEndpoint.Configure"/> and <see cref="IConditionallyMapped.ShouldMap"/> —
/// so the generated code invokes it without registering or resolving a group instance.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [MapGroup("/api/v1")]
/// public class ApiV1Group : IConfigurableGroup
/// {
///     public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
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
    /// <param name="app">The application builder instance.</param>
    /// <param name="group">The route group builder to configure.</param>
    /// <remarks>
    /// This method is called once during application startup when the group is created.
    /// Use it to apply middleware, authorization, metadata, or other configurations
    /// that should be shared by all endpoints in the group.
    /// </remarks>
    static abstract void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group);
}
