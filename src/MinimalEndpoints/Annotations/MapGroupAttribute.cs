using System.Diagnostics.CodeAnalysis;

namespace MinimalEndpoints.Annotations;

/// <summary>
/// Defines an endpoint group with a common route prefix and optional shared configuration.
/// </summary>
/// <remarks>
/// Apply this attribute to a class implementing <see cref="IConfigurableGroup"/> to define
/// a reusable group that multiple endpoints can reference. The group provides a route
/// prefix and allows centralized configuration of authorization, rate limiting, and other
/// middleware for all endpoints in the group.
/// </remarks>
/// <example>
/// <code>
/// [MapGroup("/api/v1", GroupName = "V1 API")]
/// public class ApiV1Group : IConfigurableGroup
/// {
///     public void ConfigureGroup(RouteGroupBuilder group)
///     {
///         group.RequireAuthorization().WithOpenApi();
///     }
/// }
///
/// [MapGet("/products", Group = typeof(ApiV1Group))]
/// public class ListProductsEndpoint { }
/// // Results in: /api/v1/products
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MapGroupAttribute : Attribute
{
    /// <summary>
    /// Gets the route prefix for this group.
    /// </summary>
    /// <remarks>
    /// The prefix is prepended to all endpoint routes in this group.
    /// Leading and trailing slashes are handled automatically.
    /// </remarks>
    public string Prefix { get; }

    /// <summary>
    /// Gets or sets the parent group type for hierarchical grouping.
    /// </summary>
    /// <remarks>
    /// When specified, this group's prefix will be appended to the parent group's full path.
    /// The parent group must also be decorated with <see cref="MapGroupAttribute"/>/>.
    /// Cyclic group hierarchies are not allowed and will result in a compile-time error.
    /// </remarks>
    /// <example>
    /// <code>
    /// [MapGroup("/api")]
    /// public class ApiGroup { }
    ///
    /// [MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
    /// public class ApiV1Group { }
    /// // Results in prefix: /api/v1
    /// </code>
    /// </example>
    public Type? ParentGroup { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapGroupAttribute"/> class.
    /// </summary>
    /// <param name="prefix">The route prefix for all endpoints in this group.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefix"/> is null.</exception>
    public MapGroupAttribute([StringSyntax("Route")]string prefix)
    {
        Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
    }
}

