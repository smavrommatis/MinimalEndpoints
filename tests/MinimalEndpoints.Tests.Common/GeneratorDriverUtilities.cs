using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MinimalEndpoints.CodeGeneration;

namespace MinimalEndpoints.Tests.Common;

/// <summary>
/// Runs the real <see cref="MinimalEndpointsGenerator"/> through a
/// <see cref="CSharpGeneratorDriver"/> so tests exercise the actual
/// <c>IIncrementalGenerator</c> wiring (predicate, transform, collect,
/// <c>RegisterSourceOutput</c>, <c>AddSource</c>) rather than re-implementing it.
/// </summary>
public static class GeneratorDriverUtilities
{
    /// <summary>
    /// Executes the generator against <paramref name="compilation"/> and returns both the
    /// driver run result (generated trees, hint names, generator diagnostics, any thrown
    /// exception) and the output compilation (original trees plus generated source).
    /// </summary>
    public static (GeneratorDriverRunResult result, CSharpCompilation outputCompilation) RunGenerator(
        CSharpCompilation compilation)
    {
        var driver = CSharpGeneratorDriver
            .Create(new MinimalEndpointsGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        return (driver.GetRunResult(), (CSharpCompilation)outputCompilation);
    }
}
