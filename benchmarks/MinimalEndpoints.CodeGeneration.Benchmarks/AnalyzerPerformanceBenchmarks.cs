using System.Collections.Immutable;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MinimalEndpoints.Annotations;
using MinimalEndpoints.CodeGeneration.Endpoints.Analyzers;
using MinimalEndpoints.CodeGeneration.Groups.Analyzers;

namespace MinimalEndpoints.CodeGeneration.Benchmarks;

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
    private ImmutableArray<DiagnosticAnalyzer> _analyzers;

    [GlobalSetup]
    public void Setup()
    {
        _compilation10 = CreateCompilationWithEndpoints(10);
        _compilation50 = CreateCompilationWithEndpoints(50);
        _compilation100 = CreateCompilationWithEndpoints(100);
        _analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new EndpointsAnalyzer(), new GroupsAnalyzer());
    }

    // A fresh CompilationWithAnalyzers per invocation: caching it in setup and reusing it measured
    // Roslyn's internal result cache (near-zero) instead of the actual analysis work.
    [Benchmark(Baseline = true)]
    public Task<ImmutableArray<Diagnostic>> AnalyzeEndpoints_10() =>
        _compilation10.WithAnalyzers(_analyzers).GetAllDiagnosticsAsync();

    [Benchmark]
    public Task<ImmutableArray<Diagnostic>> AnalyzeEndpoints_50() =>
        _compilation50.WithAnalyzers(_analyzers).GetAllDiagnosticsAsync();

    [Benchmark]
    public Task<ImmutableArray<Diagnostic>> AnalyzeEndpoints_100() =>
        _compilation100.WithAnalyzers(_analyzers).GetAllDiagnosticsAsync();

    private static Compilation CreateCompilationWithEndpoints(int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        sb.AppendLine("using Microsoft.AspNetCore.Routing;");
        sb.AppendLine("using MinimalEndpoints;");
        sb.AppendLine("using MinimalEndpoints.Annotations;");
        sb.AppendLine();

        // Add some groups
        sb.AppendLine(@"
[MapGroup(""/api"")]
public class ApiGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
}

[MapGroup(""/v1"", ParentGroup = typeof(ApiGroup))]
public class V1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group) { }
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

        var references = GetReferences();

        return CSharpCompilation.Create(
            $"AnalyzerBenchmarkAssembly_{count}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static ImmutableArray<PortableExecutableReference> GetReferences()
    {
        var references = new List<PortableExecutableReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IResult).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MapGetAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IConfigurableGroup).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Results).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Builder.IApplicationBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Routing.RouteGroupBuilder).Assembly.Location),
        };

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            {
                var name = assembly.GetName().Name;
                if (name == "System.Runtime" ||
                    name == "netstandard" ||
                    name?.StartsWith("System.") == true)
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
        }

        return [..references];
    }
}
