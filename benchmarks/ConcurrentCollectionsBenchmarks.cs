using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;

namespace MinimalEndpoints.Benchmarks;

/// <summary>
/// Benchmarks comparing lock-based collections vs concurrent collections.
/// Simulates the pattern used in analyzers.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 3)]
public class ConcurrentCollectionsBenchmarks
{
    private const int ItemCount = 100;
    private const int ThreadCount = 8;

    [Benchmark(Baseline = true, Description = "Dictionary with Lock")]
    public void Dictionary_WithLock()
    {
        var dict = new Dictionary<int, string>();
        var lockObj = new object();

        Parallel.For(0, ItemCount, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount }, i =>
        {
            lock (lockObj)
            {
                dict[i] = $"Value_{i}";
            }
        });

        // Read operations
        foreach (var key in Enumerable.Range(0, ItemCount))
        {
            lock (lockObj)
            {
                _ = dict.ContainsKey(key);
            }
        }
    }

    [Benchmark(Description = "ConcurrentDictionary")]
    public void ConcurrentDictionary_NoLock()
    {
        var dict = new ConcurrentDictionary<int, string>();

        Parallel.For(0, ItemCount, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount }, i =>
        {
            dict.TryAdd(i, $"Value_{i}");
        });

        // Read operations
        foreach (var key in Enumerable.Range(0, ItemCount))
        {
            _ = dict.ContainsKey(key);
        }
    }

    [Benchmark(Description = "List with Lock")]
    public void List_WithLock()
    {
        var list = new List<string>();
        var lockObj = new object();

        Parallel.For(0, ItemCount, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount }, i =>
        {
            lock (lockObj)
            {
                list.Add($"Value_{i}");
            }
        });

        // Read operations
        lock (lockObj)
        {
            _ = list.Count;
        }
    }

    [Benchmark(Description = "ConcurrentBag")]
    public void ConcurrentBag_NoLock()
    {
        var bag = new ConcurrentBag<string>();

        Parallel.For(0, ItemCount, new ParallelOptions { MaxDegreeOfParallelism = ThreadCount }, i =>
        {
            bag.Add($"Value_{i}");
        });

        // Read operations
        _ = bag.Count;
    }
}

