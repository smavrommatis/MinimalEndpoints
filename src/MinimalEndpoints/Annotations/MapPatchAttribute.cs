using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Maps an endpoint class to handle HTTP PATCH requests at the specified route pattern.
/// </summary>
/// <remarks>
/// Typically used for partial updates to existing resources.
/// </remarks>
/// <example>
/// <code>
/// public record PatchUserRequest(string? Name, string? Email);
///
/// [MapPatch("/api/users/{id}")]
/// public class PatchUserEndpoint
/// {
///     private readonly IUserRepository _repository;
///
///     public PatchUserEndpoint(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;IResult&gt; HandleAsync(
///         int id,
///         [FromBody] PatchUserRequest request,
///         CancellationToken ct)
///     {
///         var updated = await _repository.PatchAsync(id, request, ct);
///         return updated ? Results.NoContent() : Results.NotFound();
///     }
/// }
/// </code>
/// </example>
public sealed class MapPatchAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Patch.Method];

    /// <summary>
    /// Initializes a new instance of the <see cref="MapPatchAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern for this endpoint.</param>
    /// <param name="lifetime">The service lifetime for dependency injection. Default is <see cref="ServiceLifetime.Scoped"/>.</param>
    public MapPatchAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
