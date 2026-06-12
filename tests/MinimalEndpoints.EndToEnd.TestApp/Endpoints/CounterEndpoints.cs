using Microsoft.Extensions.DependencyInjection;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>
/// Registered as a singleton, so the same instance handles every request and its counter
/// accumulates across requests.
/// </summary>
[MapGet("/lifetime/singleton", ServiceLifetime.Singleton)]
public class SingletonCounterEndpoint
{
    private int _count;

    public Task<IResult> HandleAsync() =>
        Task.FromResult(Results.Ok(Interlocked.Increment(ref _count)));
}

/// <summary>
/// Registered as transient, so a fresh instance is resolved per request and its counter
/// always reads 1.
/// </summary>
[MapGet("/lifetime/transient", ServiceLifetime.Transient)]
public class TransientCounterEndpoint
{
    private int _count;

    public Task<IResult> HandleAsync() =>
        Task.FromResult(Results.Ok(Interlocked.Increment(ref _count)));
}
