using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.Sample.Endpoints;

[MapGet("/test/{id:int}")]
public class GetTest
{
    public Task<IResult> HandleAsync(int id)
    {
        return Task.FromResult(Results.Ok());
    }
}
