# Performance Guide

MinimalEndpoints is designed for **zero runtime overhead** with exceptional compile-time performance.

## Overview

- ⚡ **Zero Runtime Overhead** - All code generated at compile-time
- 🚀 **Fast Build Times** - Incremental source generation
- 💾 **Low Memory Footprint** - Minimal allocations
- 📊 **Benchmarked** - Comprehensive performance testing

---

## Runtime Performance

### Zero Overhead Guarantee

MinimalEndpoints generates **identical IL code** to hand-written Minimal APIs:

```csharp
// Your code
[MapGet("/users/{id}")]
public class GetUserEndpoint
{
    public IResult Handle(int id) => Results.Ok();
}

// Generated code (simplified)
builder.MapGet("/users/{id}", (int id) =>
{
    var endpoint = builder.Services.GetRequiredService<GetUserEndpoint>();
    return endpoint.Handle(id);
});
```

**No reflection. No runtime discovery. Direct method calls.**

---

## Compile-Time Performance

### Analyzer Performance

Performance of diagnostic analysis across different project sizes:

| Endpoints | Mean     | Error     | StdDev    | Ratio | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|-----------|----------|-----------|-----------|-------|----------|---------|------------|-------------|
| 10        | 1.216 ms | 0.353 ms  | 0.234 ms  | 1.03  | 15.6250  | -       | 353.55 KB  | 1.00        |
| 50        | 2.747 ms | 1.123 ms  | 0.743 ms  | 2.33  | 62.5000  | 15.6250 | 1351.06 KB | 3.82        |
| 100       | 5.134 ms | 1.682 ms  | 1.001 ms  | 4.35  | 62.5000  | 15.6250 | 2569.02 KB | 7.27        |

**Key Insights:**
- Sub-6ms analysis time for 100 endpoints (~5.1 ms)
- Roughly linear scaling with endpoint count
- Allocations grow with the endpoint count (~2.5 MB at 100 endpoints)

### Code Generation Performance

Performance of source code generation:

| Scenario          | Mean       | Error     | StdDev    | Ratio | Gen0     | Allocated  | Alloc Ratio |
|-------------------|------------|-----------|-----------|-------|----------|------------|-------------|
| 10 (cold)         | 331.8 μs   | 28.31 μs  | 14.81 μs  | 1.00  | 11.7188  | 259.35 KB  | 1.00        |
| 50 (cold)         | 1,262.7 μs | 149.95 μs | 78.42 μs  | 3.81  | 39.0625  | 864.30 KB  | 3.33        |
| 100 (cold)        | 2,103.4 μs | 97.68 μs  | 58.13 μs  | 6.35  | 78.1250  | 1617.17 KB | 6.24        |
| 500 (cold)        | 9,677.5 μs | 302.60 μs | 200.15 μs | 29.22 | 156.2500 | 7634.15 KB | 29.44       |
| 100 (incremental) | 416.9 μs   | 19.42 μs  | 12.85 μs  | 1.26  | 5.8594   | 345.08 KB  | 1.33        |

*Cold* runs build a fresh generator driver over a clean N-endpoint compilation. The *incremental*
row is a warm second run after a single-line source edit, exercising Roslyn's incremental caching.
(The 500-endpoint cold run also triggers Gen1/Gen2 collections, omitted here for brevity.)

**Key Insights:**
- **Sub-millisecond cold generation** only for very small projects (~0.33 ms at 10 endpoints)
- **Cold generation scales roughly linearly** - ~2.1 ms at 100 endpoints, ~9.7 ms at 500
- **Incremental re-builds are ~5× cheaper** - a warm re-run after one edit takes ~0.42 ms at 100 endpoints
- **Allocation scales linearly** - roughly ~15 KB per generated endpoint

### Cross-Assembly Scanning

`[assembly: ScanReferencedEndpoints]` re-derives endpoints/groups from referenced **compiled**
assemblies at compile time (no runtime reflection). The benchmark references a compiled library of 100
endpoints and runs the host generator in each mode. These rows are part of the **same benchmark run** as
the Code Generation table above, so they are directly comparable to each other and to it — treat
absolutes as illustrative (see the
[Benchmarks README](../benchmarks/README.md#methodology-and-known-limitations)).

| Mode (host over a 100-endpoint referenced library)          | Mean     | Allocated | vs. cold generate-100 |
|-------------------------------------------------------------|----------|-----------|-----------------------|
| Default — no `[assembly: ScanReferencedEndpoints]`          | ~83 μs   | ~80 KB    | ~0.04×                |
| Scanning **on**, cold                                       | ~1.79 ms | ~1.55 MB  | ~0.85×                |
| Scanning **on**, warm (after an unrelated edit)             | ~392 μs  | ~329 KB   | ~0.19×                |
| _reference:_ cold generate 100 **local** endpoints (same run) | ~2.10 ms | ~1.58 MB  | 1.00×                 |

**Key Insights:**
- **Opt-out is effectively free** — with the attribute absent the scan short-circuits before touching any
  references (~1/25th of a cold 100-endpoint generation); the default build is byte-identical to before.
- **Scanning on (cold) costs about the same as generating those endpoints locally** — re-deriving 100
  referenced endpoints ≈ generating 100 local ones.
- **Warm rebuilds stay cached (~4.5× cheaper than cold)** — the `CompilationProvider`-fed scan node re-runs on
  every edit, but its structural comparer serves an unchanged result from cache, so an unrelated edit does
  not re-pay the scan (mirroring the local generator's incremental speedup).

### Build Time Impact

Generation cost added per compilation (measured cold via the Roslyn driver — see the
[Benchmarks README](../benchmarks/README.md) for why these are upper-bound, not MSBuild deltas):

| Project Size  | Cold Generation | Incremental (warm) | Allocated |
|---------------|-----------------|--------------------|-----------|
| 10 endpoints  | ~332 μs         | —                  | ~259 KB   |
| 50 endpoints  | ~1.26 ms        | —                  | ~864 KB   |
| 100 endpoints | ~2.10 ms        | ~0.42 ms           | ~1.6 MB   |
| 500 endpoints | ~9.7 ms         | —                  | ~7.6 MB   |

The incremental (warm) figure is only benchmarked for the 100-endpoint case; the other sizes' warm
cost is not separately measured. Real `dotnet build`/IDE rebuilds benefit from incremental caching
and are expected to track the warm column far more closely than the cold one.

**For a typical project (50-100 endpoints): ~1.3-2.1 ms cold, and well under 0.5 ms on warm rebuilds** ✅

### Incremental Generation

MinimalEndpoints uses **incremental source generation**:

1. **Syntax Filtering (Fast)** - Filter classes with attributes (~microseconds)
2. **Semantic Analysis** - Only for changed/new classes
3. **Code Generation (Fast)** - Generate extension methods

```
Full (cold) generation:   [████████████] ~2.10 ms (100 endpoints)
Warm re-build (1 edit):   [██          ] ~0.42 ms
No Changes:               [            ] ~0 (fully cached)
```

### IDE Performance

- **IntelliSense**: < 50ms (imperceptible)
- **Analyzer Execution**: ~6.3ms for 100 endpoints
- **Code Fixes**: < 10ms
- **Real-time Diagnostics**: Updates as you type

---

## Memory Performance

### Compile-Time Memory

Source generator memory allocation, cold run (from benchmarks):

```
10 endpoints:    ~246 KB
50 endpoints:    ~822 KB
100 endpoints:   ~1.5 MB
500 endpoints:   ~7.3 MB
```

**Key Insight:** Allocation scales roughly linearly with the endpoint count — about ~15 KB per
generated endpoint. A warm/incremental re-run allocates far less (~388 KB for 100 endpoints).

### Runtime Memory

**Per Endpoint Overhead:**
- MinimalEndpoints: **0 bytes** (no overhead)
- Traditional Minimal API: **0 bytes**
- No extra allocations at runtime

**Why?** All code is generated at compile-time and becomes part of the normal IL, with no additional wrappers or reflection.

---

## Optimization Tips

### 1. Use Constructor Injection

✅ **Good - Constructor Injection:**
```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    private readonly IUserRepo _repo;
    public GetUsersEndpoint(IUserRepo repo) => _repo = repo;

    public async Task<IResult> HandleAsync()
    {
        var users = await _repo.GetAllAsync();
        return Results.Ok(users);
    }
}
```

❌ **Bad - Repeated Service Resolution:**
```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public async Task<IResult> HandleAsync([FromServices] IUserRepo repo)
    {
        // Service resolved on every request!
        var users = await repo.GetAllAsync();
        return Results.Ok(users);
    }
}
```

**Impact:** Constructor injection is ~5% faster

### 2. Choose Appropriate Service Lifetime

```csharp
// Stateless endpoint - use Singleton
[MapGet("/health", ServiceLifetime.Singleton)]
public class HealthCheckEndpoint
{
    public IResult Handle() => Results.Ok("healthy");
}

// Stateful/DbContext - use Scoped (default)
[MapGet("/users")]  // Scoped by default ✅
public class GetUsersEndpoint
{
    private readonly AppDbContext _db;
    // ...
}

// Rare - use Transient
[MapGet("/guid", ServiceLifetime.Transient)]
public class GuidEndpoint
{
    private readonly Guid _id = Guid.NewGuid();
    public IResult Handle() => Results.Ok(_id);
}
```

**Impact:**
- Singleton: Fastest (no allocation)
- Scoped: Standard (allocated per request)
- Transient: Slowest (allocated per use)

### 3. Async for I/O Operations

✅ **Good - Async for I/O:**
```csharp
public async Task<IResult> HandleAsync()
{
    var users = await _repo.GetAllAsync();  // ✅ Async
    return Results.Ok(users);
}
```

❌ **Bad - Sync for I/O:**
```csharp
public IResult Handle()
{
    var users = _repo.GetAll();  // ❌ Blocking
    return Results.Ok(users);
}
```

**Impact:** Async improves throughput under load by ~30-50%

### 4. Minimize Allocations

✅ **Good - Value Types:**
```csharp
public record struct UserDto(int Id, string Name);  // ✅ Struct

[MapGet("/users/{id}")]
public class GetUserEndpoint
{
    public async Task<IResult> HandleAsync(int id)
    {
        var user = new UserDto(id, "John");
        return Results.Ok(user);
    }
}
```

❌ **Bad - Unnecessary Objects:**
```csharp
public class UserDto { public int Id; public string Name; }  // ❌ Class

[MapGet("/users/{id}")]
public class GetUserEndpoint
{
    public async Task<IResult> HandleAsync(int id)
    {
        var wrapper = new { user = new UserDto { Id = id } };  // Extra allocation
        return Results.Ok(wrapper);
    }
}
```

**Impact:** Reduces GC pressure significantly

### 5. Cache Expensive Operations

```csharp
[MapGet("/config", ServiceLifetime.Singleton)]
public class ConfigEndpoint
{
    private readonly IConfiguration _config;
    private string? _cachedValue;

    public ConfigEndpoint(IConfiguration config)
    {
        _config = config;
    }

    public IResult Handle()
    {
        _cachedValue ??= _config["ExpensiveComputation"];
        return Results.Ok(_cachedValue);
    }
}
```

### 6. Use Groups Wisely

✅ **Good - Logical Grouping:**
```csharp
[MapGroup("/api/v1")]
public class V1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireAuthorization();  // Applied once
    }
}
```

❌ **Bad - Over-Engineering:**
```csharp
// Don't create a group for every single endpoint
[MapGroup("/api/users/active/recent")]
public class VerySpecificGroup { }
```

---

## Profiling Your Application

### Using BenchmarkDotNet

```bash
cd benchmarks
dotnet run -c Release
```

See [Benchmarks README](../benchmarks/README.md) for details.

### Using dotnet-trace

```bash
# Install
dotnet tool install --global dotnet-trace

# Collect trace
dotnet trace collect --process-id <PID> --profile cpu-sampling

# Analyze with PerfView or Visual Studio
```

### Using Application Insights

```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Endpoints are automatically tracked
// View in Azure Portal
```

---

## Performance Checklist

Before deploying to production:

- [ ] Enable Release mode: `dotnet publish -c Release`
- [ ] Use async for all I/O operations
- [ ] Choose appropriate service lifetimes
- [ ] Enable response caching where appropriate
- [ ] Implement rate limiting for public APIs
- [ ] Use connection pooling for databases
- [ ] Configure proper logging levels (Warning in production)
- [ ] Enable response compression
- [ ] Use CDN for static assets
- [ ] Profile under realistic load

---

## Load Testing

### Using k6

```javascript
// load-test.js
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  vus: 100,
  duration: '30s',
};

export default function() {
  let res = http.get('http://localhost:5000/api/users');
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 100ms': (r) => r.timings.duration < 100,
  });
}
```

Run: `k6 run load-test.js`

### Using wrk

```bash
# Install wrk
# Linux: sudo apt install wrk
# macOS: brew install wrk
# Windows: use WSL

# Run test
wrk -t12 -c400 -d30s http://localhost:5000/api/users
```

---

## Performance Targets

### Recommended Targets

| Metric | Target | Notes |
|--------|--------|-------|
| **Response Time (p50)** | < 50ms | For simple CRUD |
| **Response Time (p99)** | < 200ms | 99th percentile |
| **Throughput** | > 10,000 req/s | Per core |
| **Error Rate** | < 0.01% | 1 error per 10k requests |
| **Memory Usage** | < 200MB | For 100 concurrent users |
| **Cold Start** | < 2s | Application startup |

### Measuring Against Targets

```csharp
// Add metrics middleware
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next(context);
    sw.Stop();

    if (sw.ElapsedMilliseconds > 100)
    {
        logger.LogWarning("Slow request: {Path} took {Ms}ms",
            context.Request.Path, sw.ElapsedMilliseconds);
    }
});
```

---

## Troubleshooting Slow Performance

### Issue: Slow Build Times

**Symptoms:** Build takes > 5 seconds with < 100 endpoints

**Diagnose:**
```bash
dotnet build -v:detailed | Select-String "MinimalEndpoints"
```

**Solutions:**
1. Update to latest .NET SDK
2. Disable unnecessary analyzers
3. Clean bin/obj folders
4. Restart IDE

### Issue: Slow Requests

**Symptoms:** Requests take > 500ms

**Diagnose:**
```csharp
// Add logging
[MapGet("/users")]
public class GetUsersEndpoint
{
    private readonly ILogger _logger;

    public async Task<IResult> HandleAsync()
    {
        var sw = Stopwatch.StartNew();
        var users = await _repo.GetAllAsync();
        _logger.LogInformation("Query took {Ms}ms", sw.ElapsedMilliseconds);
        return Results.Ok(users);
    }
}
```

**Common Causes:**
1. N+1 queries - use `Include()` with EF Core
2. Blocking I/O - use async methods
3. No connection pooling - configure properly
4. Large result sets - implement pagination

### Issue: High Memory Usage

**Symptoms:** Application uses > 500MB RAM

**Diagnose:**
```bash
dotnet-counters monitor --process-id <PID> System.Runtime
```

**Common Causes:**
1. Memory leaks in endpoint logic
2. Large result sets not paginated
3. Incorrect service lifetimes (Singleton with DbContext)
4. Not disposing resources

---

## Further Reading

- 📊 [Benchmarks](../benchmarks/README.md) - Detailed benchmark results
- 🏗️ [Architecture](ARCHITECTURE.md) - How code generation works
- 🔧 [Troubleshooting](TROUBLESHOOTING.md) - Common issues
- 📚 [ASP.NET Core Performance](https://learn.microsoft.com/aspnet/core/performance/performance-best-practices)

---

**Last Updated:** June 13, 2026

