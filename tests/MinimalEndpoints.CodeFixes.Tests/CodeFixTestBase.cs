using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using MinimalEndpoints.CodeFixes;
using MinimalEndpoints.CodeGeneration.Endpoints.Analyzers;
using MinimalEndpoints.Tests.Common;

namespace MinimalEndpoints.CodeFixes.Tests;

/// <summary>
/// A <see cref="CSharpCodeFixTest{TAnalyzer, TCodeFix, TVerifier}"/> pre-wired for the MinimalEndpoints
/// analyzer + code-fix pair (MINEP001 -> <see cref="EntryPointCodeFixProvider"/>).
/// </summary>
/// <remarks>
/// The Roslyn code-fix testing harness compiles the <c>TestCode</c>/<c>FixedCode</c> sources, so it needs the
/// BCL, ASP.NET Core (<c>IResult</c>, <c>Results</c>) and the MinimalEndpoints attribute assembly available.
/// Rather than restore a NuGet reference-assembly pack (and guess its exact net10 version), this reuses the
/// repo's existing <see cref="CompilationBuilder"/> reference set, gathered from the running test host. The
/// <see cref="ReferenceAssemblies"/> base is therefore intentionally empty — every reference comes from
/// <see cref="SolutionState.AdditionalReferences"/>.
/// </remarks>
internal sealed class MinEndpointsCodeFixTest
    : CSharpCodeFixTest<EndpointsAnalyzer, EntryPointCodeFixProvider, DefaultVerifier>
{
    private static readonly IReadOnlyList<MetadataReference> SharedReferences =
        new CompilationBuilder("// code-fix test references")
            .WithMvcReferences()
            .Build(validateCompilation: false)
            .References
            .ToList();

    public MinEndpointsCodeFixTest()
    {
        // Empty base: all references are supplied explicitly below (see remarks).
        ReferenceAssemblies = new ReferenceAssemblies("net10.0");
        TestState.AdditionalReferences.AddRange(SharedReferences);
    }
}
