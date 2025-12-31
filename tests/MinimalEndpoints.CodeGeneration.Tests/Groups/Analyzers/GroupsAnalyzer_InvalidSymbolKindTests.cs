using Microsoft.CodeAnalysis;

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

    private List<Diagnostic> GetDiagnostics(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        return CompilationUtilities.GenerateDiagnostics(compilation);
    }

}

