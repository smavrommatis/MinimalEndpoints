using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MinimalEndpoints.Annotations;

namespace MinimalEndpoints.ExternalSample;

/// <summary>
/// A configurable group defined in a SEPARATE compiled assembly. The AdvancedSample discovers it via
/// <c>[assembly: ScanReferencedEndpoints]</c> and composes it across the assembly boundary — its
/// <see cref="IConfigurableGroup.ConfigureGroup"/> runs even though the host never declares it.
/// </summary>
[MapGroup("/external")]
public class ExternalGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) =>
        group.WithTags("External");
}

/// <summary>
/// An endpoint defined in the referenced library, mapped into <see cref="ExternalGroup"/>, so it
/// answers under the composed prefix <c>/external/info</c> — without the AdvancedSample declaring it.
/// </summary>
[MapGet("/info", Group = typeof(ExternalGroup))]
public class ExternalInfoEndpoint
{
    public Task<IResult> HandleAsync() =>
        Task.FromResult(Results.Ok(new { source = "external-referenced-assembly" }));
}
