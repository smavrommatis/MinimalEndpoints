using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MinimalEndpoints.CodeGeneration.Benchmarks;

/// <summary>
/// Benchmarks for source generator performance with varying endpoint counts.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, iterationCount: 10, warmupCount: 3)]
public class GeneratorPerformanceBenchmarks
{
    private Compilation _compilation10 = null!;
    private Compilation _compilation50 = null!;
    private Compilation _compilation100 = null!;
    private Compilation _compilation500 = null!;
    private GeneratorDriver _warmDriver = null!;
    private Compilation _touched100 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _compilation10 = CreateCompilationWithEndpoints(10);
        _compilation50 = CreateCompilationWithEndpoints(50);
        _compilation100 = CreateCompilationWithEndpoints(100);
        _compilation500 = CreateCompilationWithEndpoints(500);

        // Prime a driver for the incremental (warm-run) benchmark: this first run populates the
        // generator's incremental cache; the measured run then feeds a trivially-changed compilation.
        _warmDriver = CSharpGeneratorDriver.Create(new MinimalEndpointsGenerator())
            .RunGenerators(_compilation100);
        _touched100 = _compilation100.AddSyntaxTrees(CSharpSyntaxTree.ParseText("// incremental touch"));
    }

    // Each method targets a pre-built compilation; the previous [Arguments(N)] on these parameterless
    // methods failed BenchmarkDotNet validation, so they are removed.
    [Benchmark(Baseline = true)]
    public void GenerateEndpoints_10() => RunGenerator(_compilation10);

    [Benchmark]
    public void GenerateEndpoints_50() => RunGenerator(_compilation50);

    [Benchmark]
    public void GenerateEndpoints_100() => RunGenerator(_compilation100);

    [Benchmark]
    public void GenerateEndpoints_500() => RunGenerator(_compilation500);

    // Warm second run after a trivial source edit — exercises Roslyn incremental caching rather than
    // a cold generation. Only meaningful because the pipeline models are symbol-free on this branch.
    [Benchmark]
    public void GenerateEndpoints_100_Incremental() => _warmDriver.RunGenerators(_touched100);

    private static void RunGenerator(Compilation compilation)
    {
        var generator = new MinimalEndpointsGenerator();

        CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    private static Compilation CreateCompilationWithEndpoints(int count)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Microsoft.AspNetCore.Http;");
        sb.AppendLine("using MinimalEndpoints.Annotations;");
        sb.AppendLine();

        for (var i = 0; i < count; i++)
        {
            sb.AppendLine($@"
[MapGet(""/api/endpoint{i}/{{id}}"")]
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

        // Reference everything the generated registration code needs (ServiceLifetime, the routing
        // builders, Results) so the input compilation is error-free and the benchmark measures real
        // generation work rather than degraded error-recovery.
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IResult).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Results).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MinimalEndpoints.Annotations.MapGetAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceLifetime).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Builder.IApplicationBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Routing.RouteGroupBuilder).Assembly.Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "System.Runtime").Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "Microsoft.AspNetCore.Http.Abstractions").Location),
        };

        var compilation = CSharpCompilation.Create(
            $"BenchmarkAssembly_{count}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(
                "Benchmark input compilation has errors: " +
                string.Join("; ", errors.Select(e => e.GetMessage())));
        }

        return compilation;
    }
}
