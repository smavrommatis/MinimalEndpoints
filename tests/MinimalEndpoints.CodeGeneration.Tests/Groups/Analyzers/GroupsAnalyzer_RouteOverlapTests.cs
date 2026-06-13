using Microsoft.CodeAnalysis;
using static MinimalEndpoints.Tests.Common.CompilationUtilities;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups.Analyzers;

/// <summary>
/// Route-overlap (MINEP004) edge cases: group-prefix joining without a leading slash, a duplicated
/// verb in a single endpoint, and a trailing optional route parameter overlapping the bare path.
/// </summary>
public class GroupsAnalyzer_RouteOverlapTests
{
    [Fact]
    public void NestedGroupPrefixWithoutLeadingSlash_DetectsConflict()
    {
        // The child group prefix "v1" has no leading slash. The runtime joins MapGroup("/api") and
        // MapGroup("v1") to "/api/v1", so the analyzer must too — concatenating to "/apiv1" misses
        // the conflict with the direct "/api/v1/users" endpoint.
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }

[MapGet(""/users"", Group = typeof(V1Group))]
public class GetUsersInGroupEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/api/v1/users"")]
public class GetUsersDirectEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var diagnostics = GetDiagnostics(code);

        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void SingleEndpointWithDuplicateVerb_DoesNotSelfConflict()
    {
        // A repeated verb in one [MapMethods] array must not pair the endpoint with itself and
        // report "X conflicts with X".
        var code = @"
namespace TestApp;

[MapMethods(""/a"", new[] { ""GET"", ""GET"" })]
public class DuplicateVerbEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var diagnostics = GetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }

    [Fact]
    public void TrailingOptionalParameter_ConflictsWithBarePath()
    {
        // "/users/{id?}" also matches "/users" (the parameter is optional), so it overlaps a
        // separate "/users" endpoint for the same verb.
        var code = @"
namespace TestApp;

[MapGet(""/users/{id?}"")]
public class GetUserOptionalEndpoint
{
    public Task<IResult> HandleAsync(int? id = null) => Task.FromResult(Results.Ok());
}

[MapGet(""/users"")]
public class ListUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var diagnostics = GetDiagnostics(code);

        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP004");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void RequiredTrailingParameter_DoesNotConflictWithBarePath()
    {
        // Guard against over-expansion: a REQUIRED "{id}" does not match "/users", so it must not be
        // reported as conflicting with a bare "/users" endpoint.
        var code = @"
namespace TestApp;

[MapGet(""/users/{id}"")]
public class GetUserEndpoint
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.Ok());
}

[MapGet(""/users"")]
public class ListUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var diagnostics = GetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }
}
