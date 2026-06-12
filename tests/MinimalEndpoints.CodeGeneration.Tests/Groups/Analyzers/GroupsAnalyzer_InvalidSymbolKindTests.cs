using Microsoft.CodeAnalysis;
using static MinimalEndpoints.Tests.Common.CompilationUtilities;

namespace MinimalEndpoints.CodeGeneration.Tests.Groups.Analyzers;

public class GroupsAnalyzer_InvalidSymbolKindTests
{
    [Fact]
    public void WithEndpointAndGroupAttributes_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapGet(""/users"")]
[MapGroup(""/api"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}
";
        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.Contains(diagnostics, d => d.Id == "MINEP007");
    }

    [Fact]
    public void WithGroupAndMapMethods_ReportsError()
    {
        // Arrange
        var code = @"
namespace TestApp;

[MapMethods(""/api"", new[] { ""GET"", ""POST"" })]
[MapGroup(""/v1"")]
public class InvalidClass
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.Contains(diagnostics, d => d.Id == "MINEP007");
    }

    [Fact]
    public void EndpointOnlyClass_NoMinep007()
    {
        // A class with a single endpoint attribute and no group attribute is a valid endpoint —
        // MINEP007 (marked as both an Endpoint and a Group) must not fire.
        var code = @"
namespace TestApp;

[MapGet(""/users"")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP007");
    }

    [Fact]
    public void GroupOnlyClass_NoMinep007()
    {
        // A class with a single group attribute and no endpoint attribute is a valid group —
        // MINEP007 must not fire.
        var code = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}";

        // Act
        var diagnostics = GetDiagnostics(code);

        // Assert
        Assert.DoesNotContain(diagnostics, d => d.Id == "MINEP007");
    }
}

