using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.CodeGeneration.Endpoints.Analyzers;
using MinimalEndpoints.CodeGeneration.Groups.Analyzers;

namespace MinimalEndpoints.Tests.Common;

public static class CompilationUtilities
{
    public static List<Diagnostic> GenerateDiagnostics(CSharpCompilation compilation)
    {
        return GenerateAllDiagnostics(compilation)
            .Where(d => d.Id.StartsWith("MINEP"))
            .ToList();
    }

    /// <summary>
    /// Runs the MinimalEndpoints analyzers and returns ALL resulting diagnostics. Unlike
    /// <see cref="GenerateDiagnostics"/>, this does NOT filter to the MINEP prefix, so callers
    /// can assert on analyzer-infrastructure diagnostics such as AD0001 (an analyzer crash),
    /// which the MINEP filter would otherwise hide.
    /// </summary>
    public static List<Diagnostic> GenerateAllDiagnostics(CSharpCompilation compilation)
    {
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new EndpointsAnalyzer(),
            new GroupsAnalyzer());

        return compilation.WithAnalyzers(analyzers)
            .GetAllDiagnosticsAsync()
            .GetAwaiter()
            .GetResult()
            .ToList();
    }

    /// <summary>
    /// Builds a compilation from <paramref name="code"/> (with MVC references, validation off)
    /// and returns the MINEP analyzer diagnostics. Shared by the analyzer test classes, which
    /// previously each carried an identical private copy of this helper.
    /// </summary>
    public static List<Diagnostic> GetDiagnostics(string code)
    {
        var compilation = new CompilationBuilder(code)
            .WithMvcReferences()
            .Build(validateCompilation: false);

        return GenerateDiagnostics(compilation);
    }

    public static (string generatedCode, IEnumerable<Diagnostic> diagnostics) GenerateCodeAndCompile(
        CSharpCompilation compilation,
        bool validateCompilation = true
    )
    {
        // Run the real incremental generator (predicate, transform, Collect,
        // RegisterSourceOutput, AddSource) rather than re-implementing its pipeline.
        var (result, outputCompilation) = GeneratorDriverUtilities.RunGenerator(compilation);

        var generatedCode = result.GeneratedTrees.FirstOrDefault()?.ToString();

        // No endpoints discovered -> the generator emits no source.
        if (generatedCode == null)
        {
            return (null!, Enumerable.Empty<Diagnostic>());
        }

        // The output compilation already includes the original trees plus the generated
        // source, so its diagnostics verify that the generated code compiles cleanly.
        var diagnostics = outputCompilation.GetDiagnostics();

        if (validateCompilation)
        {
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

            if (errors.Length > 0)
            {
                var errorMessages = string.Join(Environment.NewLine,
                    errors.Select(e => $"{e.Id}: {e.GetMessage()}"));
                throw new InvalidOperationException(
                    $"Compilation failed with {errors.Length} error(s):{Environment.NewLine}{errorMessages}");
            }
        }

        return (generatedCode, diagnostics);
    }
}
