using MinimalEndpoints.Annotations;
using MinimalEndpoints.EndToEnd.TestApp.Groups;

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>
/// Belongs to a nested group, so it must answer under the composed prefix
/// <c>/api/v1/grouped</c> and 404 at the unprefixed path.
/// </summary>
[MapGet("/grouped", Group = typeof(ApiV1Group))]
public class GroupedEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok(new { grouped = true }));
}
