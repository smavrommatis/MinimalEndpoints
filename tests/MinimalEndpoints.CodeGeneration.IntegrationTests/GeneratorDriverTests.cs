using Microsoft.CodeAnalysis;

namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// Exercises the real <see cref="MinimalEndpointsGenerator"/> end-to-end through a
/// <c>CSharpGeneratorDriver</c> (predicate, transform, Collect, RegisterSourceOutput,
/// AddSource). These assert generator wiring that the hand-rolled
/// <see cref="CompilationUtilities.GenerateCodeAndCompile"/> path cannot reach, and cover
/// scenarios (multiple syntax trees, abstract classes) the old first-tree-only helper
/// could not express.
/// </summary>
public class GeneratorDriverTests
{
    [Fact]
    public void Generator_SimpleEndpoint_ProducesExpectedSource()
    {
        // Arrange
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/test"")]
public class TestEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // Act
        var (result, outputCompilation) = GeneratorDriverUtilities.RunGenerator(compilation);

        // Assert: exactly one generated tree with the expected hint name
        Assert.Single(result.GeneratedTrees);
        var generatedSource = Assert.Single(result.Results[0].GeneratedSources);
        Assert.EndsWith("MinimalEndpointExtensions.g.cs", generatedSource.HintName);

        // Assert: the generator did not throw and emitted no diagnostics
        Assert.Null(result.Results[0].Exception);
        Assert.Empty(result.Diagnostics);

        // Assert: the generated code compiles cleanly alongside the input
        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Empty(errors);

        // Assert: the expected public extension methods are present
        var generated = generatedSource.SourceText.ToString();
        Assert.Contains("AddMinimalEndpoints", generated);
        Assert.Contains("UseMinimalEndpoints", generated);
    }

    [Fact]
    public void Generator_EndpointsInMultipleSyntaxTrees_AllMapped()
    {
        // Arrange: two endpoints declared in two separate source files
        var firstSource = @"
namespace TestApp.A;

[MapGet(""/first"")]
public class FirstEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var secondSource = @"
namespace TestApp.B;

[MapGet(""/second"")]
public class SecondEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(firstSource)
            .WithAdditionalSource(secondSource)
            .WithMvcReferences()
            .Build();

        // Act
        var (result, outputCompilation) = GeneratorDriverUtilities.RunGenerator(compilation);

        // Assert: both endpoints, discovered across both trees, are mapped
        var generated = Assert.Single(result.GeneratedTrees).ToString();
        Assert.Contains("Map__TestApp_A_FirstEndpoint", generated);
        Assert.Contains("Map__TestApp_B_SecondEndpoint", generated);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.Empty(errors);
    }

    [Fact]
    public void Generator_AbstractEndpointClass_IsSkipped()
    {
        // Arrange: an abstract class and a concrete class, both carrying a Map attribute
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/base"")]
public abstract class BaseEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/concrete"")]
public class ConcreteEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // Act
        var (result, _) = GeneratorDriverUtilities.RunGenerator(compilation);

        // Assert: the concrete endpoint is mapped, the abstract one is filtered out
        var generated = Assert.Single(result.GeneratedTrees).ToString();
        Assert.Contains("Map__TestApp_Endpoints_ConcreteEndpoint", generated);
        Assert.DoesNotContain("Map__TestApp_Endpoints_BaseEndpoint", generated);
    }

    [Fact]
    public void Generator_IncompleteAttributeAlongsideValidEndpoint_DoesNotCrashAndStillGenerates()
    {
        // Arrange: a malformed '[MapGet]' (no pattern — mid-typing) next to a valid endpoint.
        // The malformed attribute must not crash the generator (CS8785) and drop ALL output.
        var code = @"
namespace TestApp.Endpoints;

[MapGet]
public class HalfTypedEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/valid"")]
public class ValidEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        // Act
        var (result, _) = GeneratorDriverUtilities.RunGenerator(compilation);

        // Assert: the generator did not throw, and the valid endpoint still generated.
        Assert.Null(result.Results[0].Exception);
        var generated = Assert.Single(result.GeneratedTrees).ToString();
        Assert.Contains("Map__TestApp_Endpoints_ValidEndpoint", generated);
        Assert.DoesNotContain("Map__TestApp_Endpoints_HalfTypedEndpoint", generated);
    }

    [Fact]
    public void Generator_WithMultipleMapAttributesOnOneClass_DoesNotCrashGenerator()
    {
        // Arrange: an ambiguous class with two endpoint attributes next to a valid endpoint.
        // SingleOrDefault throws on >1 match; without the fix this crashes the whole generator
        // (CS8785) and drops ALL output, breaking every AddMinimalEndpoints() call site.
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/ambiguous"")]
[MapPost(""/ambiguous"")]
public class AmbiguousEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/valid"")]
public class ValidEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // Act
        var (result, _) = GeneratorDriverUtilities.RunGenerator(compilation);

        // Assert: the generator did not throw, and the valid endpoint still generated.
        Assert.Null(result.Results[0].Exception);
        var generated = Assert.Single(result.GeneratedTrees).ToString();
        Assert.Contains("Map__TestApp_Endpoints_ValidEndpoint", generated);
        Assert.DoesNotContain("Map__TestApp_Endpoints_AmbiguousEndpoint", generated);
    }

    [Fact]
    public void Generator_NoEndpoints_GeneratesNoSource()
    {
        // Arrange: a class with no Map attribute
        var code = @"
namespace TestApp.Endpoints;

public class NotAnEndpoint
{
}";

        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build();

        // Act
        var (result, _) = GeneratorDriverUtilities.RunGenerator(compilation);

        // Assert: the generator emits no source when there are no endpoints
        Assert.Empty(result.GeneratedTrees);
    }
}
