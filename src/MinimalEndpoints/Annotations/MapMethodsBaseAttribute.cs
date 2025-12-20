using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Base class for all HTTP method mapping attributes.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for all endpoint mapping attributes.
/// Use specific derived attributes like <see cref="MapGetAttribute"/>, <see cref="MapPostAttribute"/>, etc.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple =  false)]
public abstract class MapMethodsBaseAttribute : Attribute
{
    /// <summary>
    /// Gets the route pattern for the endpoint.
    /// </summary>
    /// <value>A route pattern string that may contain route parameters (e.g., "/api/users/{id}").</value>
    public string Pattern { get; }

    /// <summary>
    /// Gets the HTTP methods this endpoint responds to.
    /// </summary>
    /// <value>An array of HTTP method names (e.g., ["GET"], ["POST", "PUT"]).</value>
    public string[] Methods { get; }

    /// <summary>
    /// Gets the service lifetime for dependency injection registration.
    /// </summary>
    /// <value>The service lifetime. Default is <see cref="ServiceLifetime.Scoped"/>.</value>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Gets or sets an optional route prefix to prepend to the endpoint pattern.
    /// </summary>
    /// <value>A string prefix that will be prepended to the pattern, or null for no prefix.</value>
    /// <example>
    /// Setting <c>GroupPrefix = "api/v1"</c> with <c>Pattern = "/users"</c> results in route <c>"/api/v1/users"</c>.
    /// </example>
    public string? GroupPrefix { get; set; }

    /// <summary>
    /// Gets or sets the name of the entry point method to invoke.
    /// </summary>
    /// <value>
    /// The method name, or null to use default detection (Handle or HandleAsync).
    /// </value>
    /// <remarks>
    /// If not specified, the generator will look for methods named "Handle" or "HandleAsync",
    /// preferring async methods. Use this property when you want to use a different method name.
    /// </remarks>
    /// <example>
    /// <code>
    /// [MapGet("/custom", EntryPoint = "ProcessRequest")]
    /// public class CustomEndpoint
    /// {
    ///     public IResult ProcessRequest() { /* ... */ }
    /// }
    /// </code>
    /// </example>
    public string? EntryPoint { get; set; }

    /// <summary>
    /// Gets or sets the service interface type to register instead of the concrete class.
    /// </summary>
    /// <value>
    /// The interface type to use for service registration, or null to register the concrete class.
    /// </value>
    /// <remarks>
    /// When set, the endpoint will be registered as the specified interface type with the
    /// concrete class as the implementation. The interface must contain the entry point method.
    /// </remarks>
    /// <example>
    /// <code>
    /// public interface IGetUserEndpoint
    /// {
    ///     Task&lt;IResult&gt; HandleAsync(int id);
    /// }
    ///
    /// [MapGet("/users/{id}", ServiceType = typeof(IGetUserEndpoint))]
    /// public class GetUserEndpoint : IGetUserEndpoint
    /// {
    ///     public Task&lt;IResult&gt; HandleAsync(int id) { /* ... */ }
    /// }
    /// </code>
    /// </example>
    public Type? ServiceType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapMethodsBaseAttribute"/> class.
    /// </summary>
    /// <param name="pattern">The route pattern for the endpoint.</param>
    /// <param name="methods">The HTTP methods this endpoint responds to.</param>
    /// <param name="lifetime">The service lifetime for dependency injection. Default is <see cref="ServiceLifetime.Scoped"/>.</param>
    protected MapMethodsBaseAttribute(
        [StringSyntax("Route")] string pattern,
        string[] methods,
        ServiceLifetime lifetime = ServiceLifetime.Scoped
    )
    {
        Pattern = pattern;
        Methods = methods;
        Lifetime = lifetime;
    }
}
