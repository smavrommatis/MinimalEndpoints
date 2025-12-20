using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace MinimalEndpoints.Benchmarks;

/// <summary>
/// Benchmarks for code generation utilities and string building performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 3)]
public class CodeGenerationBenchmarks
{
    private const int EndpointCount = 100;

    [Benchmark(Baseline = true, Description = "StringBuilder with Pre-allocated Capacity")]
    public string StringBuilder_PreAllocated()
    {
        var capacity = EndpointCount * 200; // Estimate per endpoint
        var sb = new StringBuilder(capacity);

        for (int i = 0; i < EndpointCount; i++)
        {
            sb.AppendLine($"public static IEndpointRouteBuilder Map__Endpoint{i}(this IEndpointRouteBuilder builder)");
            sb.AppendLine("{");
            sb.AppendLine($"    builder.MapGet(\"/endpoint{i}\", Handler);");
            sb.AppendLine("    return builder;");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    [Benchmark(Description = "StringBuilder without Pre-allocation")]
    public string StringBuilder_NoPreAllocation()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < EndpointCount; i++)
        {
            sb.AppendLine($"public static IEndpointRouteBuilder Map__Endpoint{i}(this IEndpointRouteBuilder builder)");
            sb.AppendLine("{");
            sb.AppendLine($"    builder.MapGet(\"/endpoint{i}\", Handler);");
            sb.AppendLine("    return builder;");
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    [Benchmark(Description = "String Concatenation")]
    public string StringConcatenation()
    {
        var result = "";

        for (int i = 0; i < EndpointCount; i++)
        {
            result += $"public static IEndpointRouteBuilder Map__Endpoint{i}(this IEndpointRouteBuilder builder)\n";
            result += "{\n";
            result += $"    builder.MapGet(\"/endpoint{i}\", Handler);\n";
            result += "    return builder;\n";
            result += "}\n";
        }

        return result;
    }

    [Benchmark(Description = "String.Join with Array")]
    public string StringJoin_Array()
    {
        var lines = new string[EndpointCount * 5];
        int index = 0;

        for (int i = 0; i < EndpointCount; i++)
        {
            lines[index++] = $"public static IEndpointRouteBuilder Map__Endpoint{i}(this IEndpointRouteBuilder builder)";
            lines[index++] = "{";
            lines[index++] = $"    builder.MapGet(\"/endpoint{i}\", Handler);";
            lines[index++] = "    return builder;";
            lines[index++] = "}";
        }

        return string.Join("\n", lines);
    }
}

