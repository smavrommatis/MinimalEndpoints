using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>Plain GET endpoint returning a body.</summary>
[MapGet("/ping")]
public class PingEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok(new { message = "pong" }));
}
