using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MinimalEndpoints.Analyzers.Benchmarks;

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

    [GlobalSetup]
    public void Setup()
    {
        _compilation10 = CreateCompilationWithEndpoints(10);
        _compilation50 = CreateCompilationWithEndpoints(50);
        _compilation100 = CreateCompilationWithEndpoints(100);
        _compilation500 = CreateCompilationWithEndpoints(500);
    }

    [Benchmark(Baseline = true)]
    [Arguments(10)]
    public void GenerateEndpoints_10()
    {
        RunGenerator(_compilation10);
    }

    [Benchmark]
    [Arguments(50)]
    public void GenerateEndpoints_50()
    {
        RunGenerator(_compilation50);
    }

    [Benchmark]
    [Arguments(100)]
    public void GenerateEndpoints_100()
    {
        RunGenerator(_compilation100);
    }

    [Benchmark]
    [Arguments(500)]
    public void GenerateEndpoints_500()
    {
        RunGenerator(_compilation500);
    }

    private static void RunGenerator(Compilation compilation)
    {
        var generator = new EndpointGenerator();

        CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
    }

    private static Compilation CreateCompilationWithEndpoints(int count)
    {
        var sb = new StringBuilder();
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

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IResult).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(MinimalEndpoints.Annotations.MapGetAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "System.Runtime").Location),
            MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "Microsoft.AspNetCore.Http.Abstractions").Location),
        };

        return CSharpCompilation.Create(
            $"BenchmarkAssembly_{count}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

