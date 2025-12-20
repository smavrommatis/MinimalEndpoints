using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.Benchmarks;

/// <summary>
/// Benchmarks comparing memory allocations between different approaches.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 10, warmupCount: 3)]
public class MemoryAllocationBenchmarks
{
    private readonly TestService _service = new();

    [Benchmark(Baseline = true, Description = "MinimalEndpoints - Class-based")]
    public IResult MinimalEndpoints_ClassBased()
    {
        var endpoint = new TestEndpoint(_service);
        return endpoint.Handle(123);
    }

    [Benchmark(Description = "Traditional Minimal API - Lambda")]
    public IResult TraditionalMinimalApi_Lambda()
    {
        // Simulate lambda capture
        Func<int, IResult> lambda = (id) =>
        {
            var result = _service.ProcessData(id);
            return Results.Ok(result);
        };

        return lambda(123);
    }

    [Benchmark(Description = "MVC Controller Pattern")]
    public IResult MvcControllerPattern()
    {
        var controller = new TestController(_service);
        return controller.GetData(123);
    }

    // Test implementations
    private class TestService
    {
        public string ProcessData(int id) => $"Data_{id}";
    }

    private class TestEndpoint
    {
        private readonly TestService _service;

        public TestEndpoint(TestService service)
        {
            _service = service;
        }

        public IResult Handle(int id)
        {
            var result = _service.ProcessData(id);
            return Results.Ok(result);
        }
    }

    private class TestController
    {
        private readonly TestService _service;

        public TestController(TestService service)
        {
            _service = service;
        }

        public IResult GetData(int id)
        {
            var result = _service.ProcessData(id);
            return Results.Ok(result);
        }
    }
}
