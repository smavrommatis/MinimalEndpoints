# MinimalEndpoints Benchmarks

This directory contains the performance benchmark suite for MinimalEndpoints, built on
[BenchmarkDotNet](https://benchmarkdotnet.org/). It measures the cost of the source generator
and the analyzers as the number of endpoints in a compilation grows, which is the dimension
that matters for build-time impact in real projects.

The published numbers in the repository [`README.md`](../README.md#-performance) ("Performance"
section) and in [`docs/PERFORMANCE.md`](../docs/PERFORMANCE.md) are produced by running this suite.

## Project

- [`MinimalEndpoints.CodeGeneration.Benchmarks`](MinimalEndpoints.CodeGeneration.Benchmarks) —
  a single console project (`net10.0`) that hosts two benchmark classes and a BenchmarkDotNet
  entry point ([`Program.cs`](MinimalEndpoints.CodeGeneration.Benchmarks/Program.cs)).

## What is measured

Both benchmark classes construct synthetic in-memory compilations (via Roslyn) containing a
parameterized number of generated endpoint classes — **10, 50, and 100** endpoints for the
analyzer suite, and **10, 50, 100, and 500** for the generator suite. The endpoints are simple
`[MapGet]` classes with a `HandleAsync` method; the analyzer suite additionally includes a small
two-level `[MapGroup]` hierarchy so the group analyzer has work to do. All runs use the
`[MemoryDiagnoser]`, so allocations are reported alongside timings.

### `GeneratorPerformanceBenchmarks`

Measures the source generator end to end. Each invocation creates a fresh
`CSharpGeneratorDriver` around `MinimalEndpointsGenerator` and calls
`RunGeneratorsAndUpdateCompilation` on a compilation of N endpoints. This is the **cold**
generation path — a clean run with no prior generator state.

It also includes `GenerateEndpoints_100_Incremental`, a **warm** run: a driver is primed once on the
100-endpoint compilation, then re-run after a trivial one-line source edit. This exercises Roslyn's
incremental-generator caching rather than cold generation, and is the closest proxy in the suite to
an IDE / `dotnet watch` rebuild.

It also includes the **cross-assembly scan** benchmarks `Scan_Off_Referenced_100`,
`Scan_On_Referenced_100`, and `Scan_On_Referenced_100_Incremental`: the host compilation references a
compiled library of 100 endpoints (emitted to a PE image), and the generator runs with
`[assembly: ScanReferencedEndpoints]` absent (off — the scan short-circuits), present (on, cold — all 100
re-derived from metadata), and warm after an unrelated edit. Together they show the cost of opting in and
that the warm path stays cached.

### `AnalyzerPerformanceBenchmarks`

Measures analyzer execution. It builds a `CompilationWithAnalyzers` over the
`EndpointsAnalyzer` and `GroupsAnalyzer` and calls `GetAllDiagnosticsAsync` on a compilation of
N endpoints, reporting the time to surface all diagnostics.

## How to run

BenchmarkDotNet requires a Release build — it refuses to run reliably under Debug.

```bash
dotnet run -c Release --project benchmarks/MinimalEndpoints.CodeGeneration.Benchmarks
```

Or, from inside the project folder:

```bash
cd benchmarks/MinimalEndpoints.CodeGeneration.Benchmarks
dotnet run -c Release
```

To run only one class or one benchmark, pass a BenchmarkDotNet filter after `--`:

```bash
dotnet run -c Release --project benchmarks/MinimalEndpoints.CodeGeneration.Benchmarks -- --filter *GeneratorPerformance*
```

Results are written to the console and to `BenchmarkDotNet.Artifacts/` in the working directory.

## Methodology and known limitations

- **Results are machine-dependent.** Absolute numbers vary with CPU, memory, OS, and the .NET
  runtime version; treat the published tables as relative/illustrative rather than guarantees.
  Re-run the suite on your own hardware before quoting figures.
- **Most of the suite measures cold runs.** A fresh generator driver is created per invocation, so
  those numbers describe a clean compilation rather than the warm, incremental rebuilds that Roslyn's
  incremental-generator caching is designed to accelerate. The single `GenerateEndpoints_100_Incremental`
  benchmark is the exception: it measures a warm re-run and lands roughly 5× faster than the cold
  100-endpoint run (~0.42 ms vs ~2.1 ms), which is about what incremental caching buys you in a real
  IDE or `dotnet watch` session.
- **Synthetic compilations.** The benchmark inputs are generated endpoint classes, not real
  application code, so they exercise generator/analyzer throughput rather than any particular
  project's exact shape.
- These caveats are tracked as known limitations of the current suite; the numbers should be
  read as order-of-magnitude scaling indicators (how cost grows with endpoint count) rather than
  precise per-build budgets.
