using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.Sample.Endpoints;

[MapGet("/test-with-dependency")]
public class GetTestWithDependency
{
    public async Task<IResult> HandleAsync([FromServices]IHttpContextAccessor httpContextAccessor)
    {
        var isNull = httpContextAccessor.HttpContext is null;
        return Results.Ok($"HttpContext is null: {isNull}");
    }
}
