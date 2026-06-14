using MinimalEndpoints.Annotations;
using MinimalEndpoints.EndToEnd.ReferencedEndpoints;

// Opt into cross-assembly discovery: endpoints/groups from the referenced
// MinimalEndpoints.EndToEnd.ReferencedEndpoints library are registered/mapped by the host's generated
// AddMinimalEndpoints()/UseMinimalEndpoints() as if they were declared here.
[assembly: ScanReferencedEndpoints]

namespace MinimalEndpoints.EndToEnd.TestApp.Endpoints;

/// <summary>
/// A HOST endpoint mapped into a group defined in a REFERENCED assembly (<see cref="LibGroup"/>) —
/// proves cross-assembly group composition: the FQN-keyed hierarchy nests this host endpoint under the
/// referenced group, so it answers under <c>/lib/host-in-lib</c>.
/// </summary>
[MapGet("/host-in-lib", Group = typeof(LibGroup))]
public class HostInLibEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok(new { source = "host-in-referenced-group" }));
}
