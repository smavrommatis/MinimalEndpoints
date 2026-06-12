using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>POST endpoint that echoes a JSON body back, exercising body binding round-trips.</summary>
[MapPost("/echo")]
public class EchoEndpoint
{
    public Task<IResult> HandleAsync([FromBody] EchoRequest request) =>
        Task.FromResult(Results.Ok(new EchoResponse(request.Message)));
}

public record EchoRequest(string Message);

public record EchoResponse(string Message);
