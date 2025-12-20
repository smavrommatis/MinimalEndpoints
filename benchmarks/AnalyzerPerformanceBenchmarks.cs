using System.Collections.Immutable;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.Analyzers;

namespace MinimalEndpoints.Benchmarks;

/// <summary>
/// Benchmarks for analyzer execution performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 3)]
public class AnalyzerPerformanceBenchmarks
{
    private Compilation _compilation10 = null!;
    private Compilation _compilation50 = null!;
    private Compilation _compilation100 = null!;
    private CompilationWithAnalyzers _withAnalyzers10 = null!;
    private CompilationWithAnalyzers _withAnalyzers50 = null!;
    private CompilationWithAnalyzers _withAnalyzers100 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _compilation10 = CreateCompilationWithEndpoints(10);
        _compilation50 = CreateCompilationWithEndpoints(50);
        _compilation100 = CreateCompilationWithEndpoints(100);

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new MinimalEndpointsAnalyzer(),
            new AmbiguousRouteAnalyzer(),
            new GroupHierarchyAnalyzer());

        _withAnalyzers10 = _compilation10.WithAnalyzers(analyzers);
        _withAnalyzers50 = _compilation50.WithAnalyzers(analyzers);
        _withAnalyzers100 = _compilation100.WithAnalyzers(analyzers);
    }

    [Benchmark(Baseline = true)]
    public async Task<ImmutableArray<Diagnostic>> AnalyzeEndpoints_10()
    {
        return await _withAnalyzers10.GetAllDiagnosticsAsync();
    }

    [Benchmark]
    public async Task<ImmutableArray<Diagnostic>> AnalyzeEndpoints_50()
    {
        return await _withAnalyzers50.GetAllDiagnosticsAsync();
    }

    [Benchmark]
    public async Task<ImmutableArray<Diagnostic>> AnalyzeEndpoints_100()
    {
        return await _withAnalyzers100.GetAllDiagnosticsAsync();
    }

    private static Compilation CreateCompilationWithEndpoints(int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        sb.AppendLine("using Microsoft.AspNetCore.Routing;");
        sb.AppendLine("using MinimalEndpoints;");
        sb.AppendLine("using MinimalEndpoints.Annotations;");
        sb.AppendLine();

        // Add some groups
        sb.AppendLine(@"
[MapGroup(""/api"")]
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}
");

        // Add endpoints with various patterns
        for (int i = 0; i < count; i++)
        {
            var groupRef = i % 3 == 0 ? ", Group = typeof(V1Group)" : "";

            sb.AppendLine($@"
[MapGet(""/endpoint{i}/{{id}}""{groupRef})]
public class Endpoint{i}
{{
    public Task<IResult> HandleAsync(int id)
    {{
        return Task.FromResult(Results.Ok());
    }}
}}
");
        }

        var syntaxTree = CSharpSyntaxTree.ParseText(sb.ToString());

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IResult).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MinimalEndpoints.Annotations.MapGetAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MinimalEndpoints.IEndpointGroup).Assembly.Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "System.Runtime").Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "Microsoft.AspNetCore.Http.Abstractions").Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "Microsoft.AspNetCore.Routing.Abstractions").Location),
        };

        return CSharpCompilation.Create(
            $"AnalyzerBenchmarkAssembly_{count}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

