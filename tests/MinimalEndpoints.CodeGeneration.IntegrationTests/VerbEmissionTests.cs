namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// Generation coverage for the verb-emission paths that previously had none: the multi-verb
/// <c>[MapMethods]</c> array branch (with <c>ToUpperInvariant</c> normalization) and the
/// single-verb <c>MapDelete</c>/<c>MapPatch</c>/<c>MapHead</c> emissions. These were only
/// exercised at attribute-parsing level, so an array-emission or casing regression would have
/// gone unnoticed.
/// </summary>
public class VerbEmissionTests
{
    private static string Generate(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation);
        return generatedCode;
    }

    [Fact]
    public void MapMethods_MultipleVerbs_EmitsUppercasedArray()
    {
        var code = @"
namespace TestApp.Endpoints;

[MapMethods(""/multi"", new[] { ""get"", ""post"" })]
public class MultiVerbEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var generated = Generate(code);

        Assert.Contains(@"MapMethods(""/multi"", [""GET"", ""POST""], Handler)", generated);
    }

    [Fact]
    public void MapDelete_EmitsMapDeleteCall()
    {
        var code = @"
namespace TestApp.Endpoints;

[MapDelete(""/users/{id}"")]
public class DeleteUserEndpoint
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.NoContent());
}";

        var generated = Generate(code);

        Assert.Contains(@".MapDelete(""/users/{id}"", Handler)", generated);
    }

    [Fact]
    public void MapPatch_EmitsMapPatchCall()
    {
        var code = @"
namespace TestApp.Endpoints;

[MapPatch(""/users/{id}"")]
public class PatchUserEndpoint
{
    public Task<IResult> HandleAsync(int id) => Task.FromResult(Results.NoContent());
}";

        var generated = Generate(code);

        Assert.Contains(@".MapPatch(""/users/{id}"", Handler)", generated);
    }

    [Fact]
    public void MapHead_GeneratesCompilableMapping()
    {
        var code = @"
namespace TestApp.Endpoints;

[MapHead(""/health"")]
public class HealthHeadEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var generated = Generate(code);

        // HEAD has no dedicated IEndpointRouteBuilder method, so it is emitted via MapMethods.
        Assert.Contains(@"MapMethods(""/health"", [""HEAD""], Handler)", generated);
    }

    [Fact]
    public void MapMethods_SingleVerb_GeneratesCompilableMapping()
    {
        var code = @"
namespace TestApp.Endpoints;

[MapMethods(""/single"", new[] { ""GET"" })]
public class SingleVerbEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var generated = Generate(code);

        // A single-element [MapMethods] still emits the methods array.
        Assert.Contains(@"MapMethods(""/single"", [""GET""], Handler)", generated);
    }
}
