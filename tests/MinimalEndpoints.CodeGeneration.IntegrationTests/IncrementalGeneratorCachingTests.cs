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
