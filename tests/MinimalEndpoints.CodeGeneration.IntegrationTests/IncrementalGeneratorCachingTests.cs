using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MinimalEndpoints.CodeGeneration;

namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// Verifies the incremental-generator caching contract: after an edit to an UNRELATED syntax
/// tree (and on a no-op re-run), the generator's tracked output steps must be served from
/// cache (<see cref="IncrementalStepRunReason.Cached"/> / <see
/// cref="IncrementalStepRunReason.Unchanged"/>) and the generated text must be identical
/// across runs. This holds because the pipeline definitions expose value equality over their
/// generation inputs rather than the raw Roslyn symbols they capture.
/// </summary>
public class IncrementalGeneratorCachingTests
{
    private const string EndpointSource = @"
namespace TestApp.Endpoints;

[MapGet(""/test"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

    private const string UnrelatedSource = "public class Unrelated { }";

    private const string GroupSource = @"
namespace TestApp;

[MapGroup(""/api"")]
public class ApiGroup { }";

    private const string GroupedEndpointSource = @"
namespace TestApp;

[MapGet(""/users"", Group = typeof(ApiGroup))]
public class GetUsers
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

    private const string EditedGroupedEndpointSource = @"
namespace TestApp;

[MapGet(""/people"", Group = typeof(ApiGroup))]
public class GetUsers
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

    [Fact]
    public void SecondRun_AfterEditToGroupedEndpointTree_KeepsGroupRouting()
    {
        // Regression: editing ONLY the endpoint's own tree re-transforms it against the new
        // compilation, while the group's definition is served from cache (built against the
        // previous compilation). With the old symbol-keyed group lookup, SymbolEqualityComparer
        // never matched across compilations, so the endpoint silently lost its group prefix and
        // was emitted ungrouped — no diagnostic. Resolving the link by fully-qualified-name string
        // makes the cached group resolvable across compilations.
        var compilation = new CompilationBuilder(GroupSource)
            .WithAdditionalSource(GroupedEndpointSource)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        var driver = CreateTrackingDriver();

        driver = driver.RunGenerators(compilation);
        var firstText = GetGeneratedText(driver);

        // Sanity: the endpoint is grouped on the first run.
        Assert.Contains("builder.Map__TestApp_GetUsers(app, group_TestApp_ApiGroup);", firstText);

        // Edit ONLY the endpoint tree (changing its route changes its cache key, forcing the
        // output to recompute) while keeping its Group reference; the group tree is untouched.
        var endpointTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("class GetUsers"));
        var editedCompilation = compilation.ReplaceSyntaxTree(
            endpointTree,
            CSharpSyntaxTree.ParseText(EditedGroupedEndpointSource));

        driver = driver.RunGenerators(editedCompilation);
        var secondText = GetGeneratedText(driver);

        // The endpoint must still be routed through its group after the incremental edit.
        Assert.Contains("var group_TestApp_ApiGroup = builder.MapGroup__TestApp_ApiGroup(app);", secondText);
        Assert.Contains("builder.Map__TestApp_GetUsers(app, group_TestApp_ApiGroup);", secondText);
        Assert.DoesNotContain("builder.Map__TestApp_GetUsers(app);", secondText);
    }

    [Fact]
    public void SecondRun_AfterEditToUnrelatedTree_ReportsCachedOutputs()
    {
        var compilation = new CompilationBuilder(EndpointSource)
            .WithAdditionalSource(UnrelatedSource)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        var driver = CreateTrackingDriver();

        // First run establishes the baseline cache.
        driver = driver.RunGenerators(compilation);
        var firstText = GetGeneratedText(driver);

        // Edit ONLY the unrelated tree; the endpoint tree is untouched, so a correctly
        // caching pipeline must re-serve the endpoint output from cache.
        var unrelatedTree = compilation.SyntaxTrees.Single(t => t.ToString() == UnrelatedSource);
        var editedCompilation = compilation.ReplaceSyntaxTree(
            unrelatedTree,
            CSharpSyntaxTree.ParseText("public class Unrelated { /* edit */ }"));

        driver = driver.RunGenerators(editedCompilation);
        var secondText = GetGeneratedText(driver);

        AssertAllOutputsCached(driver);
        Assert.Equal(firstText, secondText);
    }

    [Fact]
    public void SecondRun_NoChanges_ReportsCachedOutputs()
    {
        var compilation = new CompilationBuilder(EndpointSource)
            .WithAdditionalSource(UnrelatedSource)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        var driver = CreateTrackingDriver();

        driver = driver.RunGenerators(compilation);
        var firstText = GetGeneratedText(driver);

        // Re-run against the same compilation: nothing changed.
        driver = driver.RunGenerators(compilation);
        var secondText = GetGeneratedText(driver);

        AssertAllOutputsCached(driver);
        Assert.Equal(firstText, secondText);
    }

    private static GeneratorDriver CreateTrackingDriver() =>
        CSharpGeneratorDriver.Create(
            generators: new[] { new MinimalEndpointsGenerator().AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

    private static string GetGeneratedText(GeneratorDriver driver) =>
        driver.GetRunResult().Results[0].GeneratedSources.Single().SourceText.ToString();

    private static void AssertAllOutputsCached(GeneratorDriver driver)
    {
        var result = driver.GetRunResult().Results[0];

        var outputs = result.TrackedOutputSteps
            .SelectMany(step => step.Value)
            .SelectMany(run => run.Outputs)
            .ToList();

        Assert.NotEmpty(outputs);
        Assert.All(outputs, output =>
            Assert.True(
                output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                $"Expected Cached/Unchanged but a tracked output step reported {output.Reason}."));
    }
}
