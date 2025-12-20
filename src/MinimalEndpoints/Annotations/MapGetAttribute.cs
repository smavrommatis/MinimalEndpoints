using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Maps an endpoint class to handle HTTP GET requests at the specified route pattern.
/// </summary>
/// <remarks>
/// Apply this attribute to a class to automatically generate endpoint registration and mapping code
/// for HTTP GET requests. The class must contain a public instance method named "Handle" or "HandleAsync"
/// (or specify a custom method name using the <see cref="MapMethodsBaseAttribute.EntryPoint"/> property).
/// </remarks>
/// <example>
/// <code>
/// [MapGet("/api/users/{id}")]
/// public class GetUserEndpoint
/// {
///     private readonly IUserRepository _repository;
///
///     public GetUserEndpoint(IUserRepository repository)
///     {
///         _repository = repository;
///     }
///
///     public async Task&lt;IResult&gt; HandleAsync(int id, CancellationToken cancellationToken)
///     {
///         var user = await _repository.GetByIdAsync(id, cancellationToken);
///         return user is not null ? Results.Ok(user) : Results.NotFound();
///     }
/// }
/// </code>
/// </example>
public sealed class MapGetAttribute : MapMethodsBaseAttribute
{
    private static readonly string[] s_supportedMethods = [HttpMethod.Get.Method];

    /// <summary>
    /// Initializes a new instance of the <see cref="MapGetAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern for this endpoint (e.g., "/api/users/{id}").</param>
    /// <param name="lifetime">The service lifetime for dependency injection. Default is <see cref="ServiceLifetime.Scoped"/>.</param>
    public MapGetAttribute([StringSyntax("Route")] string pattern, ServiceLifetime lifetime = ServiceLifetime.Scoped) :
        base(pattern, s_supportedMethods, lifetime)
    {
    }
}
