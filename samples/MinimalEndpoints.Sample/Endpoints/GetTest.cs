using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.Sample.Endpoints;

[MapGet("/test/{id}")]
public class GetTest
{
    public async Task<IResult> HandleAsync(int id)
    {
        return Results.Ok();
    }
}
