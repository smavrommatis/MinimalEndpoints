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

    // --- Sources for the incremental group-hierarchy route-recomputation scenario ---

    private const string GetNumberNoGroupSource = @"
namespace TestApp;

[MapGet(""/{number}"")]
public class GetNumber
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

    private const string GetNumberGroupedSource = @"
namespace TestApp;

[MapGet(""/{number}"", Group = typeof(ProductsGroup))]
public class GetNumber
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

    private const string ProductsGroupRootSource = @"
namespace TestApp;

[MapGroup(""/products"")]
public class ProductsGroup { }";

    private const string ProductsGroupChildSource = @"
namespace TestApp;

[MapGroup(""/products"", ParentGroup = typeof(VersionGroup))]
public class ProductsGroup { }";

    private const string VersionGroupSource = @"
namespace TestApp;

[MapGroup(""/v{version}"")]
public class VersionGroup { }";

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
    public void IncrementalGroupHierarchy_BuildingParentChainAcrossEdits_RecomputesEndpointRoute()
    {
        // Exercises group-hierarchy edge cases under INCREMENTAL re-generation: as group membership
        // and parent links change across compilations on a single driver, the endpoint's effective
        // route must be recomputed — even when the endpoint's OWN tree is unchanged (cache-served)
        // and only a group's parent link changes. The generator composes the effective route from the
        // MapGroup nesting (group prefixes) + the endpoint pattern, so each step asserts that wiring.

        // --- Step 1 ---------------------------------------------------------------------------------
        // GetNumber belongs to no group; ProductsGroup ("/products") is completely unrelated.
        // Expected effective route: /{number}
        var compilation = new CompilationBuilder(GetNumberNoGroupSource)
            .WithAdditionalSource(ProductsGroupRootSource)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        var driver = CreateTrackingDriver();
        driver = driver.RunGenerators(compilation);
        var step1 = GetGeneratedText(driver);

        // Mapped directly on the root builder, in no group → effective route is just "/{number}".
        Assert.Contains("var endpoint = builder.MapGet(\"/{number}\", Handler);", step1);
        Assert.Contains("builder.Map__TestApp_GetNumber(app);", step1);
        Assert.DoesNotContain("builder.Map__TestApp_GetNumber(app, group_", step1);

        // --- Step 2 ---------------------------------------------------------------------------------
        // GetNumber joins ProductsGroup; add an unrelated VersionGroup ("/v{version}").
        // Expected effective route: /products/{number}
        var endpointTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("class GetNumber"));
        compilation = compilation
            .ReplaceSyntaxTree(endpointTree, CSharpSyntaxTree.ParseText(GetNumberGroupedSource))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(VersionGroupSource));

        driver = driver.RunGenerators(compilation);
        var step2 = GetGeneratedText(driver);

        // Endpoint now routed through ProductsGroup, which is a root group prefixing "/products".
        Assert.Contains("var endpoint = group.MapGet(\"/{number}\", Handler);", step2);
        Assert.Contains("var group = builder.MapGroup(\"/products\");", step2);
        Assert.Contains("var group_TestApp_ProductsGroup = builder.MapGroup__TestApp_ProductsGroup(app);", step2);
        Assert.Contains("builder.Map__TestApp_GetNumber(app, group_TestApp_ProductsGroup);", step2);
        // VersionGroup exists but is unrelated to the endpoint at this point (still a root group).
        Assert.Contains("var group = builder.MapGroup(\"/v{version}\");", step2);

        // --- Step 3 ---------------------------------------------------------------------------------
        // ProductsGroup gets VersionGroup as its parent. GetNumber's own tree is UNCHANGED (cache-
        // served) and ProductsGroup is still its direct group — yet the route must now flow through
        // the new two-level chain. Expected effective route: /v{version}/products/{number}
        var productsGroupTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("class ProductsGroup"));
        compilation = compilation.ReplaceSyntaxTree(
            productsGroupTree, CSharpSyntaxTree.ParseText(ProductsGroupChildSource));

        driver = driver.RunGenerators(compilation);
        var step3 = GetGeneratedText(driver);

        // VersionGroup is now the root ("/v{version}"); ProductsGroup is nested under it ("/products");
        // the endpoint is still in ProductsGroup → effective route is /v{version}/products/{number}.
        Assert.Contains("var group = builder.MapGroup(\"/v{version}\");", step3);
        Assert.Contains("var group = parentGroup.MapGroup(\"/products\");", step3);
        Assert.Contains("var group_TestApp_VersionGroup = builder.MapGroup__TestApp_VersionGroup(app);", step3);
        Assert.Contains(
            "var group_TestApp_ProductsGroup = builder.MapGroup__TestApp_ProductsGroup(app, group_TestApp_VersionGroup);",
            step3);
        Assert.Contains("builder.Map__TestApp_GetNumber(app, group_TestApp_ProductsGroup);", step3);
        Assert.Contains("var endpoint = group.MapGet(\"/{number}\", Handler);", step3);
    }

    private const string EndpointWithBodySource = @"
namespace TestApp.Endpoints;

[MapGet(""/test"")]
public class BodyEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

    private const string EndpointWithEditedBodySource = @"
namespace TestApp.Endpoints;

[MapGet(""/test"")]
public class BodyEndpoint
{
    public Task<IResult> HandleAsync()
    {
        // A model-invariant edit: the method body and trivia are not part of the equality key.
        var result = Results.Ok();
        return Task.FromResult(result);
    }
}";

    [Fact]
    public void SecondRun_AfterModelInvariantEditToEndpointOwnTree_ReportsCachedOutputs()
    {
        // The gold-standard caching regression guard: edit the endpoint's OWN tree in a way that
        // BuildEqualityKey ignores (the method body + trivia). The transform re-runs against the new
        // syntax, but because the definition's value-equality key is unchanged, the merged provider
        // and the source output must be served from cache. A future field-wise/record equality
        // regression (e.g. a reference-typed collection getting reference equality) would flip these
        // to Modified and fail here — the unrelated-tree tests would not catch it.
        var compilation = new CompilationBuilder(EndpointWithBodySource)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        var driver = CreateTrackingDriver();

        driver = driver.RunGenerators(compilation);
        var firstText = GetGeneratedText(driver);

        var endpointTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("class BodyEndpoint"));
        var editedCompilation = compilation.ReplaceSyntaxTree(
            endpointTree,
            CSharpSyntaxTree.ParseText(EndpointWithEditedBodySource));

        driver = driver.RunGenerators(editedCompilation);
        var secondText = GetGeneratedText(driver);

        AssertAllOutputsCached(driver);
        Assert.Equal(firstText, secondText);
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

    [Fact]
    public void SecondRun_AfterModelRelevantEdit_FlipsMergedStepToModified()
    {
        // Negative control for the caching tests: a model-RELEVANT edit (changing the route pattern,
        // which IS part of BuildEqualityKey) must flip the merged-definitions step to Modified. Without
        // this, the positive caching tests could pass trivially if the harness reported every step
        // Cached regardless of input — this proves the harness can actually observe a change.
        var compilation = new CompilationBuilder(EndpointSource)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        var driver = CreateTrackingDriver();
        driver = driver.RunGenerators(compilation);

        var endpointTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("class TestEndpoint"));
        var editedCompilation = compilation.ReplaceSyntaxTree(
            endpointTree,
            CSharpSyntaxTree.ParseText(EndpointSource.Replace("/test", "/changed")));

        driver = driver.RunGenerators(editedCompilation);

        var result = driver.GetRunResult().Results[0];
        Assert.True(
            result.TrackedSteps.TryGetValue(MinimalEndpointsGenerator.MergedProviderTrackingName, out var mergedRuns),
            $"Expected a tracked step named '{MinimalEndpointsGenerator.MergedProviderTrackingName}'.");

        var reasons = mergedRuns.SelectMany(run => run.Outputs).Select(o => o.Reason).ToList();
        Assert.Contains(IncrementalStepRunReason.Modified, reasons);
    }

    private const string AttributedParamSource = @"
namespace TestApp.Endpoints;

[MapGet(""/items"")]
public class AttributedParamEndpoint
{
    public Task<IResult> HandleAsync([FromQuery] int page) => Task.FromResult(Results.Ok());
}";

    private const string AttributedParamEditedBodySource = @"
namespace TestApp.Endpoints;

[MapGet(""/items"")]
public class AttributedParamEndpoint
{
    public Task<IResult> HandleAsync([FromQuery] int page)
    {
        var result = Results.Ok();
        return Task.FromResult(result);
    }
}";

    [Fact]
    public void SecondRun_AttributedParameter_ModelInvariantEdit_ReportsCachedOutputs()
    {
        // Exercises the List<AttributeDefinition> equality path (a parameter carrying [FromQuery]):
        // its attributes are folded into the string equality key at construction, so a body-only edit
        // must still serve the merged step and the output from cache.
        var compilation = new CompilationBuilder(AttributedParamSource)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        var driver = CreateTrackingDriver();
        driver = driver.RunGenerators(compilation);
        var firstText = GetGeneratedText(driver);

        var endpointTree = compilation.SyntaxTrees.Single(t => t.ToString().Contains("class AttributedParamEndpoint"));
        var editedCompilation = compilation.ReplaceSyntaxTree(
            endpointTree,
            CSharpSyntaxTree.ParseText(AttributedParamEditedBodySource));

        driver = driver.RunGenerators(editedCompilation);
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

        // Also assert the named INTERMEDIATE merged-definitions provider was served from cache. The
        // final output step staying Cached is necessary but not sufficient to prove incrementality:
        // this localizes a regression to the merged step (where a model value-equality break would
        // first surface) instead of only observing identical generated text.
        Assert.True(
            result.TrackedSteps.TryGetValue(MinimalEndpointsGenerator.MergedProviderTrackingName, out var mergedRuns),
            $"Expected a tracked step named '{MinimalEndpointsGenerator.MergedProviderTrackingName}'.");

        var mergedOutputs = mergedRuns.SelectMany(run => run.Outputs).ToList();
        Assert.NotEmpty(mergedOutputs);
        Assert.All(mergedOutputs, output =>
            Assert.True(
                output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged,
                $"Expected the merged-definitions step Cached/Unchanged but it reported {output.Reason}."));
    }
}
