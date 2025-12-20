using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Maps an endpoint class to handle HTTP DELETE requests at the specified route pattern.
/// </summary>
/// <remarks>
/// Typically used for deleting existing resources.
/// </remarks>
/// <example>
/// <code>
/// [MapDelete("/api/users/{id}")]
/// public class DeleteUserEndpoint
/// {
///     private readonly IUserRepository _repository;
///
///     public DeleteUserEndpoint(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;IResult&gt; HandleAsync(int id, CancellationToken cancellationToken)
///     {
///         var deleted = await _repository.DeleteAsync(id, cancellationToken);
///         return deleted ? Results.NoContent() : Results.NotFound();
///     }
/// }
/// </code>
/// </example>
public sealed class MapDeleteAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Delete.Method];

    /// <summary>
    /// Initializes a new instance of the <see cref="MapDeleteAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern for this endpoint.</param>
    /// <param name="lifetime">The service lifetime for dependency injection. Default is <see cref="ServiceLifetime.Scoped"/>.</param>
    public MapDeleteAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
