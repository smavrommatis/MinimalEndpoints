using Microsoft.CodeAnalysis;
using static MinimalEndpoints.Tests.Common.CompilationUtilities;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups.Analyzers;

public class GroupsAnalyzer_AuditFixesTests
{
    [Fact]
    public void ParentGroupNotMapGroup_ReportsMinep005()
    {
        // A group's ParentGroup must itself be a [MapGroup]; otherwise the parent link is silently dropped
        // (the same defect MINEP005 already reports for an endpoint's Group).
        var code = @"
namespace TestApp;

public class NotAGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(NotAGroup))]
public class V1Group { }";

        var diagnostics = GetDiagnostics(code);

        var error = Assert.Single(diagnostics, d => d.Id == "MINEP005");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("NotAGroup", error.GetMessage());
        Assert.Contains("V1Group", error.GetMessage());
    }

    [Fact]
    public void ValidParentGroup_NoMinep005()
    {
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group { }";

        var diagnostics = GetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP005");
    }

    [Fact]
    public void DuplicateServiceType_ReportsMinep013()
    {
        // Two endpoints register the same ServiceType; the DI container resolves only the last
        // registration, so one route runs the other endpoint's class.
        var code = @"
namespace TestApp;

public interface ISharedService
{
    Task<IResult> HandleAsync();
}

[MapGet(""/a"", ServiceType = typeof(ISharedService))]
public class EndpointA : ISharedService
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/b"", ServiceType = typeof(ISharedService))]
public class EndpointB : ISharedService
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var diagnostics = GetDiagnostics(code);

        var warning = Assert.Single(diagnostics, d => d.Id == "MINEP013");
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("ISharedService", warning.GetMessage());
    }

    [Fact]
    public void DistinctServiceTypes_NoMinep013()
    {
        var code = @"
namespace TestApp;

public interface IServiceA { Task<IResult> HandleAsync(); }
public interface IServiceB { Task<IResult> HandleAsync(); }

[MapGet(""/a"", ServiceType = typeof(IServiceA))]
public class EndpointA : IServiceA
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/b"", ServiceType = typeof(IServiceB))]
public class EndpointB : IServiceB
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var diagnostics = GetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP013");
    }

    [Fact]
    public void RegexConstraintWithEquals_NoFalseMinep004()
    {
        // The regex constraint contains '=', which must NOT be treated as a default-value/optional segment.
        // Previously that produced a phantom bare-path occupancy ("/items") and a false ambiguity warning
        // against an endpoint genuinely mapped at "/items".
        var code = @"
namespace TestApp;

[MapGet(""/items/{id:regex(^a=b$)}"")]
public class GetItemEndpoint
{
    public IResult Handle() => Results.Ok();
}

[MapGet(""/items"")]
public class ListItemsEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var diagnostics = GetDiagnostics(code);

        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP004");
    }

    [Fact]
    public void TrailingOptionalParameter_StillReportsMinep004()
    {
        // Guard against over-tightening N6: a genuine trailing-optional parameter ("{id?}") still also
        // occupies the bare path, so it must still conflict with an endpoint mapped there.
        var code = @"
namespace TestApp;

[MapGet(""/items/{id?}"")]
public class GetItemEndpoint
{
    public IResult Handle() => Results.Ok();
}

[MapGet(""/items"")]
public class ListItemsEndpoint
{
    public IResult Handle() => Results.Ok();
}";

        var diagnostics = GetDiagnostics(code);

        Assert.Single(diagnostics, d => d.Id == "MINEP004");
    }
}
