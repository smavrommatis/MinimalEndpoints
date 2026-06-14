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

    // Cross-assembly scan: a host referencing a compiled library of 100 endpoints, scanning OFF vs ON,
    // plus an opted-in warm driver for the incremental case.
    private Compilation _hostScanOff100 = null!;
    private Compilation _hostScanOn100 = null!;
    private GeneratorDriver _warmScanOnDriver = null!;
    private Compilation _touchedScanOn100 = null!;

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

        _hostScanOff100 = CreateHostReferencingCompiledLibrary(100, optedIn: false);
        _hostScanOn100 = CreateHostReferencingCompiledLibrary(100, optedIn: true);
        _warmScanOnDriver = CSharpGeneratorDriver.Create(new MinimalEndpointsGenerator())
            .RunGenerators(_hostScanOn100);
        _touchedScanOn100 = _hostScanOn100.AddSyntaxTrees(CSharpSyntaxTree.ParseText("// incremental touch"));
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

    // --- Cross-assembly scan: scanning OFF vs ON, comparing the cost of [assembly: ScanReferencedEndpoints] ---

    // Baseline: host references the 100-endpoint library but does NOT opt in, so the scan short-circuits.
    [Benchmark]
    public void Scan_Off_Referenced_100() => RunGenerator(_hostScanOff100);

    // Opted in: the host re-derives all 100 referenced endpoints from metadata (cold cost of the scan).
    [Benchmark]
    public void Scan_On_Referenced_100() => RunGenerator(_hostScanOn100);

    // Opted in, warm second run after an unrelated touch: the structural comparer should keep the scan
    // result cached, so this stays close to a no-op rather than re-paying the full Scan_On cost.
    [Benchmark]
    public void Scan_On_Referenced_100_Incremental() => _warmScanOnDriver.RunGenerators(_touchedScanOn100);

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

        var references = CreateReferences();

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

    // Reference everything the generated registration code needs (ServiceLifetime, the routing
    // builders, Results) so input compilations are error-free and the benchmark measures real
    // generation work rather than degraded error-recovery. Shared by the source and host compilations.
    private static MetadataReference[] CreateReferences() =>
    [
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
    ];

    /// <summary>
    /// Builds a host compilation that references a compiled library of <paramref name="count"/>
    /// endpoints (emitted to a PE image, so the host sees metadata only — no syntax). When
    /// <paramref name="optedIn"/> is true the host carries <c>[assembly: ScanReferencedEndpoints]</c>,
    /// so every discovered endpoint comes from the referenced library, isolating the scan's cost.
    /// </summary>
    private static Compilation CreateHostReferencingCompiledLibrary(int count, bool optedIn)
    {
        var library = CreateCompilationWithEndpoints(count);

        using var peStream = new MemoryStream();
        var emitResult = library.Emit(peStream);
        if (!emitResult.Success)
        {
            throw new InvalidOperationException(
                "Benchmark library failed to emit: " +
                string.Join("; ", emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.GetMessage())));
        }

        MetadataReference[] references =
            [.. CreateReferences(), MetadataReference.CreateFromImage(peStream.ToArray())];

        var hostSource = optedIn
            ? "[assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints]"
            : "// cross-assembly scanning disabled";

        return CSharpCompilation.Create(
            "BenchmarkHost",
            [CSharpSyntaxTree.ParseText(hostSource)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
