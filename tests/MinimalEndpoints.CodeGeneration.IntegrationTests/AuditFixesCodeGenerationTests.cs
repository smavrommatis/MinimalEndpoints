using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

public class AuditFixesCodeGenerationTests
{
    [Fact]
    public void InheritedEntryPoint_GeneratesAndCompiles()
    {
        // #2: the handler is inherited (not redeclared) from a non-abstract base. Previously this hard-failed
        // with MINEP001; now FindEntryPointMethod walks the base chain, so the endpoint maps and the generated
        // handler calls the inherited method. GenerateCodeAndCompile validates the output actually compiles.
        var code = @"
namespace TestApp.Endpoints;

public class BaseEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Microsoft.AspNetCore.Http.Results.Ok());
    }
}

[MapGet(""/test"")]
public class TestEndpoint : BaseEndpoint
{
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var (generatedCode, diagnostics) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        Assert.NotNull(generatedCode);
        Assert.Contains("Map__TestApp_Endpoints_TestEndpoint", generatedCode);
        Assert.Contains("HandleAsync(", generatedCode);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ByRefParameterEndpoint_IsDeclined_NotGenerated()
    {
        // #23: an endpoint whose entry point takes a by-ref parameter is declined by the generator (the
        // analyzer reports MINEP011 separately), so no map method is emitted — avoiding CS1620 in the output.
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/test"")]
public class TestEndpoint
{
    public IResult Handle(ref int id) => Microsoft.AspNetCore.Http.Results.Ok();
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        // The only endpoint was declined, so either no file is generated or it carries no map method for it.
        Assert.True(generatedCode is null || !generatedCode.Contains("Map__TestApp_Endpoints_TestEndpoint"));
    }

    [Fact]
    public void NonAssignableServiceType_DegradesToConcreteRegistration_AndCompiles()
    {
        // #27 generator degrade: the class is NOT assignable to its ServiceType, so emitting
        // AddScoped<IGetItem, GetItemEndpoint>() would be CS0311. The generator degrades to concrete
        // registration so the output compiles (the analyzer separately reports MINEP012). GenerateCodeAndCompile
        // validates the output compiles, so this test would throw before the degrade.
        var code = @"
namespace TestApp.Endpoints;

public interface IGetItem
{
    Task<IResult> HandleAsync();
}

[MapGet(""/items"", ServiceType = typeof(IGetItem))]
public class GetItemEndpoint   // does NOT implement IGetItem
{
    public Task<IResult> HandleAsync() => Task.FromResult(Microsoft.AspNetCore.Http.Results.Ok());
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var (generatedCode, diagnostics) = CompilationUtilities.GenerateCodeAndCompile(compilation);

        Assert.NotNull(generatedCode);
        // Degraded to concrete registration — the non-assignable interface is not emitted at all.
        Assert.DoesNotContain("IGetItem", generatedCode);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }
}
