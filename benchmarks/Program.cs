using BenchmarkDotNet.Running;

namespace MinimalEndpoints.Benchmarks;

/// <summary>
/// Entry point for running benchmarks.
/// Run with: dotnet run -c Release
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}

