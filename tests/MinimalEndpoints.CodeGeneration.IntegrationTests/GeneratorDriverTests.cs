using System;
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
    public void Generator_WithAbstractModifierOnOtherPart_SkipsClass()
    {
        // The `abstract` modifier is on a different partial part than the [MapGet]. The merged
        // symbol is abstract; the symbol-level gate must skip it (the old syntax-level filter saw
        // only the attributed, non-abstract part and would have generated a registration for an
        // uninstantiable type).
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/split"")]
public partial class SplitEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

public abstract partial class SplitEndpoint
{
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

        // Assert: the concrete endpoint is mapped, the abstract split one is filtered out entirely
        var generated = Assert.Single(result.GeneratedTrees).ToString();
        Assert.Contains("Map__TestApp_Endpoints_ConcreteEndpoint", generated);
        Assert.DoesNotContain("SplitEndpoint", generated);
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

    [Fact]
    public void GeneratedCode_PartialGroupWithAttributeOnSecondPart_DoesNotThrow()
    {
        // A partial group with [MapGroup] on one part and an unrelated attribute on another.
        // Per-declaration discovery used to produce two definitions for the same symbol and threw
        // (ArgumentException) on the duplicate key, crashing the generator. Discovery must now
        // succeed and emit exactly one group method.
        var code = @"
using System.Diagnostics.CodeAnalysis;

namespace TestApp.Endpoints;

[MapGroup(""/api"")]
public partial class ApiGroup { }

[ExcludeFromCodeCoverage]
public partial class ApiGroup { }

[MapGet(""/users"", Group = typeof(ApiGroup))]
public class GetUsers
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        // Act
        var (result, _) = GeneratorDriverUtilities.RunGenerator(compilation);

        // Assert: no generator exception, exactly one group definition emitted.
        Assert.Null(result.Results[0].Exception);
        var generated = Assert.Single(result.GeneratedTrees).ToString();
        // Groups are no longer registered in DI; a bare group emits no AddSingleton at all.
        Assert.Equal(0, CountOccurrences(generated, "services.AddSingleton<TestApp.Endpoints.ApiGroup>();"));
        Assert.Equal(1, CountOccurrences(generated, "RouteGroupBuilder MapGroup__TestApp_Endpoints_ApiGroup("));
    }

    [Fact]
    public void GeneratedCode_PartialEndpointWithAttributeOnSecondPart_EmitsSingleMapping()
    {
        // A partial endpoint with [MapGet] on one part and an unrelated attribute on another.
        // Per-declaration discovery used to append the handler body twice → CS0128 (duplicate
        // Handler local / duplicate registration). The generated code must compile and contain a
        // single registration and a single Handler.
        var code = @"
using System;

namespace TestApp.Endpoints;

[MapGet(""/x"")]
public partial class SplitEndpoint
{
    public IResult Handle() => Results.Ok();
}

[Serializable]
public partial class SplitEndpoint { }";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        // GenerateCodeAndCompile validates that the generated code itself compiles — it would
        // surface CS0128 if the partial parts produced duplicate definitions.
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation, validateCompilation: true);

        Assert.NotNull(generatedCode);
        Assert.Equal(1, CountOccurrences(generatedCode, "services.AddScoped<TestApp.Endpoints.SplitEndpoint>();"));
        Assert.Equal(1, CountOccurrences(generatedCode, "static IResult Handler("));
    }

    [Fact]
    public void GeneratedCode_WithGenericEndpointClass_GeneratesNothingForIt_NoCrash()
    {
        // An open generic endpoint cannot be emitted (unbound type parameter, '<'/'>' in the method
        // name). Discovery must skip it without crashing the generator, and still map the others.
        var code = @"
namespace TestApp.Endpoints;

[MapGet(""/items"")]
public class ListEndpoint<T>
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}

[MapGet(""/concrete"")]
public class ConcreteEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        // Act
        var (result, _) = GeneratorDriverUtilities.RunGenerator(compilation);

        // Assert: no crash, the concrete endpoint is mapped, the generic one is skipped entirely.
        Assert.Null(result.Results[0].Exception);
        var generated = Assert.Single(result.GeneratedTrees).ToString();
        Assert.Contains("Map__TestApp_Endpoints_ConcreteEndpoint", generated);
        Assert.DoesNotContain("ListEndpoint", generated);
    }

    [Fact]
    public void GeneratedCode_WithSanitizedNameCollision_Disambiguates()
    {
        // namespace My.App and namespace My_App both yield the base method name Map__My_App_Foo
        // once '.' is replaced by '_'. The names must be disambiguated so the generated code
        // compiles (no CS0128 duplicate method) and both endpoints are registered.
        var code = @"
namespace My.App
{
    [MapGet(""/a"")]
    public class Foo
    {
        public IResult Handle() => Results.Ok();
    }
}

namespace My_App
{
    [MapGet(""/b"")]
    public class Foo
    {
        public IResult Handle() => Results.Ok();
    }
}";

        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();

        // GenerateCodeAndCompile validates the generated code compiles — it would throw CS0128 if
        // the two classes emitted methods with the same name.
        var (generatedCode, _) = CompilationUtilities.GenerateCodeAndCompile(compilation, validateCompilation: true);

        Assert.NotNull(generatedCode);
        Assert.Equal(1, CountOccurrences(generatedCode, "services.AddScoped<My.App.Foo>();"));
        Assert.Equal(1, CountOccurrences(generatedCode, "services.AddScoped<My_App.Foo>();"));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
