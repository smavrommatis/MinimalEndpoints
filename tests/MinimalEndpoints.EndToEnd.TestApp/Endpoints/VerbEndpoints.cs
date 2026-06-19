using Microsoft.Extensions.DependencyInjection;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>JSON body for the PUT/PATCH verb endpoints.</summary>
public record ResourcePayload(string Value);

/// <summary>
/// PUT with a route id and a JSON body — exercises the generated MapPut wiring end-to-end (the
/// emission tests assert the call site; this proves the route actually answers a PUT).
/// </summary>
[MapPut("/resource/{id}")]
public class PutResourceEndpoint
{
    public Task<IResult> HandleAsync(string id, ResourcePayload payload) =>
        Task.FromResult(Results.Ok(new { id, payload.Value, method = "PUT" }));
}

/// <summary>PATCH with a route id and a JSON body — exercises the generated MapPatch wiring.</summary>
[MapPatch("/resource/{id}")]
public class PatchResourceEndpoint
{
    public Task<IResult> HandleAsync(string id, ResourcePayload payload) =>
        Task.FromResult(Results.Ok(new { id, payload.Value, method = "PATCH" }));
}

/// <summary>DELETE with a route id — exercises the generated MapDelete wiring; returns 204.</summary>
[MapDelete("/resource/{id}")]
public class DeleteResourceEndpoint
{
    public Task<IResult> HandleAsync(string id) => Task.FromResult(Results.NoContent());
}

/// <summary>
/// HEAD has no dedicated IEndpointRouteBuilder builder method, so the generator emits it via
/// MapMethods(pattern, ["HEAD"], Handler). This proves that path answers a real HEAD request.
/// </summary>
[MapHead("/resource")]
public class HeadResourceEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

/// <summary>
/// Default (Scoped) lifetime: a fresh instance per request scope, so its per-instance counter reads 1
/// each request. Confirms the generated services.AddScoped&lt;&gt; registration produces a working
/// endpoint (observationally like transient under single-resolution HTTP, but it exercises the scoped
/// registration path the other lifetime tests do not).
/// </summary>
[MapGet("/lifetime/scoped", ServiceLifetime.Scoped)]
public class ScopedCounterEndpoint
{
    private int _count;

    public Task<IResult> HandleAsync() =>
        Task.FromResult(Results.Ok(Interlocked.Increment(ref _count)));
}
