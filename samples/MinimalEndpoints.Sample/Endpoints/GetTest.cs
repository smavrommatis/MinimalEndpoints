using System.ComponentModel.DataAnnotations;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.Sample.Endpoints;

[MapGet("/test/{id}")]
public class GetTest
{
    public async Task<IResult> HandleAsync([RegularExpression(@"\d")]int id)
    {
        return Results.Ok();
    }
}
