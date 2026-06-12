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
    public void ConfigureGroup(RouteGroupBuilder group) =>
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Group"] = "v1";
            return await next(context);
        });
}
