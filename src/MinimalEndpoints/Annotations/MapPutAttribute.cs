using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Maps an endpoint class to handle HTTP PUT requests at the specified route pattern.
/// </summary>
/// <remarks>
/// Typically used for updating existing resources completely (full replacement).
/// </remarks>
/// <example>
/// <code>
/// public record UpdateUserRequest(string Name, string Email);
///
/// [MapPut("/api/users/{id}")]
/// public class UpdateUserEndpoint
/// {
///     private readonly IUserRepository _repository;
///
///     public UpdateUserEndpoint(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;IResult&gt; HandleAsync(
///         int id,
///         [FromBody] UpdateUserRequest request,
///         CancellationToken ct)
///     {
///         var updated = await _repository.UpdateAsync(id, request, ct);
///         return updated ? Results.NoContent() : Results.NotFound();
///     }
/// }
/// </code>
/// </example>
public sealed class MapPutAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Put.Method];

    /// <summary>
    /// Initializes a new instance of the <see cref="MapPutAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern for this endpoint.</param>
    /// <param name="lifetime">The service lifetime for dependency injection. Default is <see cref="ServiceLifetime.Scoped"/>.</param>
    public MapPutAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
