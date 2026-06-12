using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>
/// Conditionally mapped based on configuration. When <c>Features:ConditionalEndpoint</c> is
/// false the generated code skips the mapping entirely, so the route 404s at runtime.
/// </summary>
[MapGet("/conditional")]
public class ConditionalEndpoint : IConditionallyMapped
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok(new { mapped = true }));

    public static bool ShouldMap(IApplicationBuilder app) =>
        app.ApplicationServices.GetRequiredService<IConfiguration>()
            .GetValue("Features:ConditionalEndpoint", defaultValue: true);
}
