using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Maps an endpoint class to handle HTTP POST requests at the specified route pattern.
/// </summary>
/// <remarks>
/// Typically used for creating new resources or submitting data to the server.
/// </remarks>
/// <example>
/// <code>
/// public record CreateUserRequest(string Name, string Email);
///
/// [MapPost("/api/users")]
/// public class CreateUserEndpoint
/// {
///     private readonly IUserRepository _repository;
///
///     public CreateUserEndpoint(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;IResult&gt; HandleAsync(
///         [FromBody] CreateUserRequest request,
///         CancellationToken cancellationToken)
///     {
///         var user = await _repository.CreateAsync(request, cancellationToken);
///         return Results.Created($"/api/users/{user.Id}", user);
///     }
/// }
/// </code>
/// </example>
public sealed class MapPostAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Post.Method];

    /// <summary>
    /// Initializes a new instance of the <see cref="MapPostAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern for this endpoint.</param>
    /// <param name="lifetime">The service lifetime for dependency injection. Default is <see cref="ServiceLifetime.Scoped"/>.</param>
    public MapPostAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
