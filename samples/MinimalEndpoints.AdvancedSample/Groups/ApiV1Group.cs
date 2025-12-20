using Microsoft.AspNetCore.Builder;
using MinimalEndpoints;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.AdvancedSample.Groups;

/// <summary>
/// Defines the API V1 endpoint group with shared configuration.
/// </summary>
[MapGroup("/api/v1", GroupName = "V1 API")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group
             .WithTags("V1")
             .CacheOutput(x => x.Expire(TimeSpan.FromMinutes(5)));
    }
}
