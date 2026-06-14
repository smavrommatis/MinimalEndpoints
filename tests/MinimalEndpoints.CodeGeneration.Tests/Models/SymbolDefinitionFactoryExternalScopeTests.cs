using MinimalEndpoints.CodeGeneration.Models;
using static MinimalEndpoints.Tests.Common.SymbolTestHelpers;

namespace MinimalEndpoints.CodeGeneration.Tests.Models;

/// <summary>
/// Covers the <see cref="AccessibilityScope"/> gate used by cross-assembly discovery. In
/// <see cref="AccessibilityScope.External"/> scope the host re-derives a referenced type into its OWN
/// assembly, so only <c>public</c> types are referenceable; the default
/// <see cref="AccessibilityScope.SameAssembly"/> scope keeps the original internal-allowed behaviour.
/// </summary>
public class SymbolDefinitionFactoryExternalScopeTests
{
    private const string PublicEndpoint = @"
namespace TestApp;

[MapGet(""/test"")]
public class PublicEndpoint
{
    public IResult Handle() => Results.Ok();
}";

    private const string InternalEndpoint = @"
namespace TestApp;

[MapGet(""/test"")]
internal class InternalEndpoint
{
    public IResult Handle() => Results.Ok();
}";

    [Fact]
    public void TryCreateSymbol_External_PublicEndpoint_ReturnsDefinition()
    {
        var compilation = new CompilationBuilder(PublicEndpoint).WithMvcReferences().Build();
        var symbol = GetClassSymbol(compilation, "PublicEndpoint");

        var result = SymbolDefinitionFactory.TryCreateSymbol(symbol, AccessibilityScope.External);

        Assert.NotNull(result);
    }

    [Fact]
    public void TryCreateSymbol_External_InternalEndpoint_ReturnsNull()
    {
        var compilation = new CompilationBuilder(InternalEndpoint).WithMvcReferences().Build();
        var symbol = GetClassSymbol(compilation, "InternalEndpoint");

        var result = SymbolDefinitionFactory.TryCreateSymbol(symbol, AccessibilityScope.External);

        Assert.Null(result);
    }

    [Fact]
    public void TryCreateSymbol_SameAssembly_InternalEndpoint_ReturnsDefinition()
    {
        // Default scope is unchanged: same-assembly generated code can reference an internal endpoint.
        var compilation = new CompilationBuilder(InternalEndpoint).WithMvcReferences().Build();
        var symbol = GetClassSymbol(compilation, "InternalEndpoint");

        var result = SymbolDefinitionFactory.TryCreateSymbol(symbol);

        Assert.NotNull(result);
    }

    [Fact]
    public void ClassifyShape_External_InternalEndpoint_ReturnsInaccessible()
    {
        var compilation = new CompilationBuilder(InternalEndpoint).WithMvcReferences().Build();
        var symbol = GetClassSymbol(compilation, "InternalEndpoint");

        Assert.Equal(
            ShapeRejection.Inaccessible,
            SymbolDefinitionFactory.ClassifyShape(symbol, AccessibilityScope.External));
    }

    [Fact]
    public void ClassifyShape_External_PublicEndpoint_ReturnsNone()
    {
        var compilation = new CompilationBuilder(PublicEndpoint).WithMvcReferences().Build();
        var symbol = GetClassSymbol(compilation, "PublicEndpoint");

        Assert.Equal(
            ShapeRejection.None,
            SymbolDefinitionFactory.ClassifyShape(symbol, AccessibilityScope.External));
    }

    [Fact]
    public void TryCreateSymbol_External_PublicEndpointNestedInInternal_ReturnsNull()
    {
        // External requires public ALL THE WAY UP the nesting chain: a public endpoint nested in an
        // internal type cannot be named from the host assembly.
        var code = @"
namespace TestApp;

internal class Outer
{
    [MapGet(""/n"")]
    public class NestedEndpoint
    {
        public IResult Handle() => Results.Ok();
    }
}";
        var compilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var symbol = GetNestedClassSymbol(compilation, "Outer", "NestedEndpoint");

        Assert.Null(SymbolDefinitionFactory.TryCreateSymbol(symbol, AccessibilityScope.External));
        // Same-assembly generated code CAN reference it (nested-in-internal is fine within the assembly).
        Assert.NotNull(SymbolDefinitionFactory.TryCreateSymbol(symbol));
    }
}
