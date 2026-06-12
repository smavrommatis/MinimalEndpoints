using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.Sample.Endpoints;

[MapGet("/v2/test-with-dependency")]
public class GetTestWithDependencyV2
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetTestWithDependencyV2(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IResult> HandleAsync()
    {
        var isNull = _httpContextAccessor.HttpContext is null;
        return Task.FromResult(Results.Ok($"HttpContext is null: {isNull}"));
    }
}
