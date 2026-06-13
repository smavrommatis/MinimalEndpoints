using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.EndToEnd.TestApp.Groups;

/// <summary>Root group contributing the <c>/api</c> prefix.</summary>
[MapGroup("/api")]
public class ApiGroup;

/// <summary>
/// Nested group contributing <c>/v1</c> on top of <see cref="ApiGroup"/> (full prefix
/// <c>/api/v1</c>) and applying a group-level filter that sets a header, proving
/// <see cref="IConfigurableGroup.ConfigureGroup"/> runs.
/// </summary>
[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class ApiV1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) =>
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Group"] = "v1";
            return await next(context);
        });
}

/// <summary>
/// Group whose prefix carries a route token (<c>/store/v{version}</c>). Verifies that a group-level
/// route parameter flows into endpoint handler parameters by name with NO generator change: the
/// prefix is emitted verbatim into <c>MapGroup(...)</c> and handler parameters are forwarded as
/// declared, so an endpoint that declares <c>string version</c> binds the segment.
/// </summary>
[MapGroup("/store/v{version}")]
public class VersionedStoreGroup;

/// <summary>
/// Nested group adding a CONSTRAINED token (<c>p{page:int}</c>) beneath <see cref="VersionedStoreGroup"/>
/// (composed prefix <c>/store/v{version}/p{page:int}</c>). Verifies that parent and child prefix tokens
/// compose and that a route constraint passes through verbatim, so an endpoint binds both
/// <c>version</c> and <c>page</c>.
/// </summary>
[MapGroup("/p{page:int}", ParentGroup = typeof(VersionedStoreGroup))]
public class VersionedStorePageGroup;
