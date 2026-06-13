using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MinimalEndpoints.CodeGeneration.IntegrationTests;

/// <summary>
/// Guards the documented compatibility contract that the generated source compiles for consumers
/// pinned below LangVersion 12 (the generator deliberately avoids C# 12 collection expressions,
/// emitting an explicit <c>new string[] { ... }</c> instead).
/// </summary>
public class GeneratedCodeLanguageVersionTests
{
    [Fact]
    public void GeneratedOutput_CompilesUnderCSharp11()
    {
        // A representative endpoint: routed pattern, a route parameter, and an optional default
        // (the default-value emission path). Explicit (non-global) usings so the source parses
        // under an older language version on its own.
        const string code = @"
using MinimalEndpoints.Annotations;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TestApp;

[MapGet(""/widgets/{id}"")]
public class GetWidgetEndpoint
{
    public Task<IResult> HandleAsync(int id, int count = 10) => Task.FromResult(Results.Ok());
}";

        var genCompilation = new CompilationBuilder(code).WithMvcReferences().Build();
        var (generated, _) = CompilationUtilities.GenerateCodeAndCompile(genCompilation);
        Assert.NotNull(generated);

        // Recompile the user source + the generated source pinned to C# 11 — the version directly
        // below the collection-expression floor. A regression that emitted a C# 12 construct would
        // surface here as a compile error.
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp11);
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText(code, parseOptions),
            CSharpSyntaxTree.ParseText(generated, parseOptions),
        };

        var pinned = CSharpCompilation.Create(
            "PinnedLangVersion",
            trees,
            genCompilation.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = pinned.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(
            errors.Count == 0,
            "Generated code failed to compile under C# 11: " +
            string.Join("; ", errors.Select(e => $"{e.Id} {e.GetMessage()}")));
    }
}
