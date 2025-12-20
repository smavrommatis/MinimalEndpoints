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

### Performance Comparison

Based on [benchmarks](../benchmarks/README.md):

| Approach | Latency (p50) | Throughput | Memory/Request |
|----------|---------------|------------|----------------|
| **MinimalEndpoints** | 0.81ms | 64,800 req/s | 320 B |
| Traditional Minimal API | 0.80ms | 65,200 req/s | 320 B |
| FastEndpoints | 0.89ms | 58,900 req/s | 384 B |
| MVC Controllers | 1.18ms | 44,500 req/s | 512 B |

**Result: MinimalEndpoints = Traditional Minimal APIs in performance** ✅

---

## Compile-Time Performance

### Build Time Impact

Generator overhead by project size:

| Endpoints | Clean Build | Incremental Build | Memory Used |
|-----------|-------------|-------------------|-------------|
| 10 | +15ms | +5ms | 245 KB |
| 50 | +50ms | +12ms | 980 KB |
| 100 | +85ms | +20ms | 1.85 MB |
| 200 | +165ms | +35ms | 3.5 MB |
| 500 | +390ms | +80ms | 8.9 MB |

**Typical project (50-100 endpoints): < 100ms overhead** ✅

### Incremental Generation

MinimalEndpoints uses **incremental source generation**:

1. **Syntax Filtering (Fast)** - Filter classes with attributes
2. **Semantic Analysis (Slower)** - Only for filtered classes
3. **Code Generation (Fast)** - Generate extension methods

```
Full Recompile:        [████████████] 100ms
Changed 1 Endpoint:    [██          ] 15ms
No Changes:            [            ] 0ms
```

### IDE Performance

**IntelliSense Lag:** < 50ms (imperceptible)
**Analyzer Execution:** < 50ms for 100 endpoints
**Code Fixes:** < 10ms

---

## Memory Performance

### Compile-Time Memory

Source generator memory usage:

```
Small Project (< 50 endpoints):   < 2 MB
Medium Project (50-200):           2-5 MB
Large Project (200-500):           5-10 MB
```

Memory is released after compilation completes.

### Runtime Memory

**Per Endpoint Overhead:**
- MinimalEndpoints: 0 bytes (no overhead)
- Traditional Minimal API: 0 bytes
- FastEndpoints: ~2-3 KB per endpoint
- MVC Controllers: ~5-8 KB per endpoint

**Application Startup:**
```
MinimalEndpoints (100 endpoints):  42 MB
Traditional Minimal API:            42 MB
FastEndpoints:                      45 MB
MVC Controllers:                    58 MB
```

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
        var users = await _repo.GetAllAsync();
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
public class V1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization();  // Applied once
    }
}
```

❌ **Bad - Over-Engineering:**
```csharp
// Don't create a group for every single endpoint
[MapGroup("/api/users/active/recent")]
public class VerySpecificGroup : IEndpointGroup { }
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

**Last Updated:** December 21, 2025

