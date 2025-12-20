using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Maps an endpoint class to handle HTTP HEAD requests at the specified route pattern.
/// </summary>
/// <remarks>
/// Typically used for checking resource existence or retrieving headers without the response body.
/// HEAD requests are identical to GET requests except the response body is empty.
/// </remarks>
/// <example>
/// <code>
/// [MapHead("/api/users/{id}")]
/// public class CheckUserExistsEndpoint
/// {
///     private readonly IUserRepository _repository;
///
///     public CheckUserExistsEndpoint(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;IResult&gt; HandleAsync(int id, CancellationToken ct)
///     {
///         var exists = await _repository.ExistsAsync(id, ct);
///         return exists ? Results.Ok() : Results.NotFound();
///     }
/// }
/// </code>
/// </example>
public sealed class MapHeadAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Head.Method];

    /// <summary>
    /// Initializes a new instance of the <see cref="MapHeadAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern for this endpoint.</param>
    /// <param name="lifetime">The service lifetime for dependency injection. Default is <see cref="ServiceLifetime.Scoped"/>.</param>
    public MapHeadAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
