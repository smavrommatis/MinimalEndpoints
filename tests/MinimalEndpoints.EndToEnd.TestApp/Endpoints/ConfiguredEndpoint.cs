using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>
/// Uses the <see cref="IConfigurableEndpoint"/> hook to attach an endpoint filter that sets a
/// response header, proving the generated code invokes <c>Configure</c>.
/// </summary>
[MapGet("/configured")]
public class ConfiguredEndpoint : IConfigurableEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok(new { configured = true }));

    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint) =>
        endpoint.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers["X-Configured"] = "true";
            return await next(context);
        });
}
