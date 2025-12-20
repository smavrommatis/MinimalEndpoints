using Microsoft.AspNetCore.Builder;

namespace MinimalEndpoints;

/// <summary>
/// Defines a contract for endpoints that require custom configuration after mapping.
/// </summary>
/// <remarks>
/// Implement this interface on your endpoint class to provide custom configuration
/// for the endpoint convention builder. The <see cref="Configure"/> method will be
/// automatically called by the generated code after the endpoint is mapped.
/// </remarks>
/// <example>
/// <code>
/// [MapGet("/api/users")]
/// public class GetUsersEndpoint : IConfigurableEndpoint
/// {
///     public Task&lt;IResult&gt; HandleAsync()
///     {
///         return Task.FromResult(Results.Ok(new[] { "User1", "User2" }));
///     }
///
///     public static void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint)
///     {
///         endpoint
///             .WithName("GetUsers")
///             .WithTags("Users")
///             .RequireAuthorization()
///             .CacheOutput(x => x.Expire(TimeSpan.FromMinutes(5)));
///     }
/// }
/// </code>
/// </example>
public interface IConfigurableEndpoint
{
    /// <summary>
    /// Configures the endpoint convention builder with additional settings.
    /// </summary>
    /// <param name="app">The application builder instance.</param>
    /// <param name="endpoint">The endpoint convention builder to configure.</param>
    static abstract void Configure(IApplicationBuilder app, IEndpointConventionBuilder endpoint);
}
