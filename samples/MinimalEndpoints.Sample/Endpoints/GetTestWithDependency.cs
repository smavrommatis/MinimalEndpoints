using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.Sample.Endpoints;

[MapGet("/test-with-dependency")]
public class GetTestWithDependency
{
    public Task<IResult> HandleAsync([FromServices]IHttpContextAccessor httpContextAccessor)
    {
        var isNull = httpContextAccessor.HttpContext is null;
        return Task.FromResult(Results.Ok($"HttpContext is null: {isNull}"));
    }
}
