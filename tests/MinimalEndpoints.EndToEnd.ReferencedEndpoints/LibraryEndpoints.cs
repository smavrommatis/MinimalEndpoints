using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.EndToEnd.ReferencedEndpoints;

/// <summary>
/// A configurable group defined in a REFERENCED compiled assembly. Exercises cross-assembly group
/// discovery and proves <see cref="IConfigurableGroup.ConfigureGroup"/> is invoked across the
/// assembly boundary (it sets the <c>X-Lib-Group</c> response header).
/// </summary>
[MapGroup("/lib")]
public class LibGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) =>
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Lib-Group"] = "lib";
            return await next(context);
        });
}

/// <summary>
/// An endpoint defined in the referenced assembly, mapped into the referenced group — so it must
/// answer under the composed prefix <c>/lib/ping</c>.
/// </summary>
[MapGet("/ping", Group = typeof(LibGroup))]
public class LibPingEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok(new { source = "referenced-library" }));
}
