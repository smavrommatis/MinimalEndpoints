using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.AdvancedSample.Groups;

/// <summary>
/// Defines the API V1 endpoint group with shared configuration.
/// </summary>
[MapGroup("/api/v1")]
public class ApiV1Group : IConfigurableGroup, IConditionallyMapped
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group
            .WithTags("V1")
            .CacheOutput(x => x.Expire(TimeSpan.FromMinutes(5)));
    }

    public static bool ShouldMap(IApplicationBuilder app) => true;
}
