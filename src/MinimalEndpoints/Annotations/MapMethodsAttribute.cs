using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Maps an endpoint class to handle custom HTTP method combinations at the specified route pattern.
/// </summary>
/// <remarks>
/// Use this attribute when you need to handle multiple HTTP methods with a single endpoint,
/// or when using custom/non-standard HTTP methods.
/// </remarks>
/// <example>
/// <code>
/// // Handle both PUT and PATCH for updates
/// [MapMethods("/api/users/{id}", new[] { "PUT", "PATCH" })]
/// public class UpdateUserEndpoint
/// {
///     public async Task&lt;IResult&gt; HandleAsync(int id, [FromBody] UpdateRequest request)
///     {
///         // Handle both PUT and PATCH
///         return Results.NoContent();
///     }
/// }
///
/// // Custom HTTP method
/// [MapMethods("/api/lock/{id}", new[] { "LOCK" })]
/// public class LockResourceEndpoint
/// {
///     public Task&lt;IResult&gt; HandleAsync(int id)
///     {
///         // Custom LOCK method implementation
///         return Task.FromResult(Results.Ok());
///     }
/// }
/// </code>
/// </example>
public sealed class MapMethodsAttribute : MapMethodsBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapMethodsAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern for this endpoint.</param>
    /// <param name="methods">An array of HTTP methods this endpoint responds to (e.g., ["GET", "POST"]).</param>
    /// <param name="lifetime">The service lifetime for dependency injection. Default is <see cref="ServiceLifetime.Scoped"/>.</param>
    public MapMethodsAttribute(
        [StringSyntax("Route")] string pattern,
        string[] methods,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    ) : base(pattern, methods, lifetime)
    {

    }
}
