using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using MinimalEndpoints.CodeFixes;
using MinimalEndpoints.CodeGeneration.Endpoints.Analyzers;
using MinimalEndpoints.Tests.Common;

namespace MinimalEndpoints.CodeFixes.Tests;

/// <summary>
/// Regression tests for the MINEP001 code fix when the endpoint class already declares a member
/// with the entry-point name. Adding a second <c>public IResult Handle()</c> next to an existing
/// (non-viable) <c>Handle</c> member produced uncompilable code (CS0111 / CS0102); the colliding
/// action must be suppressed so the fix never emits a duplicate.
/// </summary>
public class EntryPointCodeFixProvider_DuplicateMemberTests
{
    private static readonly IReadOnlyList<MetadataReference> SharedReferences =
        new CompilationBuilder("// references")
            .WithMvcReferences()
            .Build(validateCompilation: false)
            .References
            .ToList();

    [Fact]
    public async Task ExistingNonPublicHandle_SuppressesCollidingSyncAction_KeepsAsyncAction()
    {
        // A non-public Handle() leaves the class without a valid entry point (MINEP001 fires).
        // "Add Handle method" would emit a second zero-arg Handle() -> CS0111. It must be suppressed;
        // "Add HandleAsync method" (a different name) is still offered.
        const string code = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;

            namespace TestApp;

            [MapGet("/test")]
            public class TestEndpoint
            {
                private IResult Handle() => Results.Ok();
            }
            """;

        var keys = await GetOfferedEquivalenceKeysAsync(code);

        Assert.DoesNotContain("AddHandleMethod", keys);
        Assert.Contains("AddHandleAsyncMethod", keys);
    }

    [Fact]
    public async Task ExistingStaticHandleAsync_SuppressesCollidingAsyncAction_KeepsSyncAction()
    {
        // A static HandleAsync() is not a valid entry point (MINEP001 fires). The async action would
        // collide (CS0111) and is suppressed; the sync "Add Handle method" remains available.
        const string code = """
            using MinimalEndpoints.Annotations;
            using Microsoft.AspNetCore.Http;
            using System.Threading.Tasks;

            namespace TestApp;

            [MapGet("/test")]
            public class TestEndpoint
            {
                public static Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
            }
            """;

        var keys = await GetOfferedEquivalenceKeysAsync(code);

        Assert.DoesNotContain("AddHandleAsyncMethod", keys);
        Assert.Contains("AddHandleMethod", keys);
    }

    private static async Task<IReadOnlyList<string?>> GetOfferedEquivalenceKeysAsync(string code)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, SharedReferences)
            .AddDocument(documentId, "Test.cs", SourceText.From(code));

        var project = solution.GetProject(projectId)!;
        var compilation = (await project.GetCompilationAsync())!;

        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointsAnalyzer()));
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        var minep001 = diagnostics.Single(d => d.Id == "MINEP001");

        var document = solution.GetDocument(documentId)!;
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(
            document, minep001, (action, _) => actions.Add(action), CancellationToken.None);

        await new EntryPointCodeFixProvider().RegisterCodeFixesAsync(context);

        return actions.Select(a => a.EquivalenceKey).ToList();
    }
}
