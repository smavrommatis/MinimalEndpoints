# Changelog

All notable changes to MinimalEndpoints will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-31

### 🎉 First Stable Release

This is the first production-ready stable release of MinimalEndpoints, bringing class-based organization to ASP.NET Core Minimal APIs with **zero runtime overhead**.

### ✨ Core Features

#### Source Generator
- **Zero Runtime Overhead**: All code generated at compile-time using Roslyn incremental source generators
- **Automatic Discovery**: Endpoints automatically discovered and registered
- **Fast Build Times**: Cold generation scales roughly linearly; warm/incremental re-builds ~5× cheaper
  - 10 endpoints: 330μs, 246KB allocated
  - 100 endpoints: 2.32ms, 1.5MB allocated (warm re-build ~0.44ms)
  - 500 endpoints: 9.0ms, 7.3MB allocated
- **Incremental Generation**: Only regenerates changed code

#### Diagnostic Analyzers (8 Total)
- **MINEP001**: Missing entry point method detection - ensures endpoint has Handle/HandleAsync method
- **MINEP002**: Multiple Map attributes validation - prevents conflicting attributes
- **MINEP003**: ServiceType interface validation - validates interface compatibility
- **MINEP004**: Ambiguous route detection - warns about duplicate route patterns
- **MINEP005**: Invalid endpoint group validation - ensures groups have [MapGroup] attribute
- **MINEP006**: Cyclic group hierarchy detection - prevents circular parent-child relationships
- **MINEP007**: Mixed endpoint/group detection - prevents classes from being both endpoint and group
- **MINEP008**: Unsupported endpoint/group shape detection - skips generic, file-local, or below-internal classes

#### Code Fixes
- Automatic entry point method generation
- Quick fixes for common diagnostic issues
- IntelliSense-driven development experience

### 🎯 Endpoint Features

#### HTTP Method Support
- `[MapGet]` - HTTP GET requests
- `[MapPost]` - HTTP POST requests
- `[MapPut]` - HTTP PUT requests
- `[MapDelete]` - HTTP DELETE requests
- `[MapPatch]` - HTTP PATCH requests
- `[MapHead]` - HTTP HEAD requests
- `[MapMethods]` - Multiple HTTP methods with custom verbs

#### Dependency Injection
- **Constructor Injection**: Full support for service dependencies
- **Parameter Injection**: Standard [FromServices], [FromBody], etc.
- **Service Lifetimes**: Control over Singleton, Scoped, Transient
- **Interface Registration**: ServiceType property for interface-based registration

#### Configuration
- **IConfigurableEndpoint**: Advanced endpoint configuration with static Configure method
- **IConfigurableGroup**: Optional group configuration interface
- **IConditionallyMapped**: Static ShouldMap for conditional endpoint registration
- **Custom Entry Points**: Specify custom handler method names

### 🏗️ Group Features

#### Hierarchical Groups
- **Route Prefixes**: Organize endpoints with shared route prefixes
- **Parent-Child Relationships**: Multi-level group hierarchies
- **Shared Configuration**: Authorization, rate limiting, CORS policies cascade
- **Compile-Time Validation**: Cycle detection prevents infinite loops

#### Group Configuration
- **IConfigurableGroup**: Optional interface for advanced group configuration
- Authorization policies
- Rate limiting rules
- Response caching
- CORS policies
- OpenAPI/Swagger tags
- Custom middleware

### 🔗 ASP.NET Core Integration

Works seamlessly with all built-in ASP.NET Core features:
- ✅ API Versioning (`Asp.Versioning.Http`)
- ✅ Output Caching (built-in)
- ✅ Rate Limiting (built-in)
- ✅ OpenTelemetry (automatic tracing)
- ✅ Authorization (policies, roles, claims)
- ✅ OpenAPI/Swagger
- ✅ CORS
- ✅ Health Checks

### 📊 Performance

**Compile-Time Performance:**
- Analyzer execution: 1.4ms (10 endpoints), 6.3ms (100 endpoints)
- Code generation: ~0.33ms (10 endpoints) to ~2.3ms (100 endpoints) cold; ~0.44ms warm
- Memory: ~246KB (10 endpoints) to ~1.5MB (100 endpoints), ~15KB per generated endpoint

**Runtime Performance:**
- Zero overhead compared to hand-written Minimal APIs
- No reflection at runtime
- Direct method invocation
- Standard ASP.NET Core routing

### 📚 Documentation

- Complete README with quick start guide
- 9 diagnostic documentation files (MINEP001-008, MINEP999)
- 3 comprehensive example guides
- Architecture documentation
- Performance benchmarking guide
- Migration guide from other approaches
- Troubleshooting guide
- Contributing guidelines

### 🧪 Testing

- 95%+ code coverage
- 240+ unit tests
- Integration test suite
- Benchmark suite for performance validation
- Sample projects (MinimalEndpoints.Sample, MinimalEndpoints.AdvancedSample)

### 📦 Technical Details

**Target Frameworks:**
- .NET 8.0, .NET 9.0, .NET 10.0 (runtime package multi-targets `net8.0;net9.0;net10.0`)
- Source generator/analyzer: Roslyn 4.8.0 floor (SDK 8.0.100 / VS 17.8)

**Key Dependencies:**
- Microsoft.CodeAnalysis.CSharp 4.8.0 (analyzer/generator)
- Microsoft.AspNetCore.App (runtime)

**NuGet Package:**
- Package ID: `Blackeye.MinimalEndpoints`
- First stable release: 1.0.0

### 🔒 Known Limitations

- Groups must not form circular hierarchies (MINEP006 detects this)
- Only one Map attribute per endpoint class (MINEP002 detects this)
- Entry point method must be public, instance (non-static) (MINEP001 validates this)
- Classes cannot be both endpoint and group (MINEP007 prevents this)

### 🚀 Getting Started

```bash
dotnet add package Blackeye.MinimalEndpoints
```

```csharp
using MinimalEndpoints.Annotations;

[MapGet("/hello")]
public class HelloEndpoint
{
    public IResult Handle() => Results.Ok("Hello, World!");
}
```

```csharp
// Program.cs
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMinimalEndpoints();

var app = builder.Build();
app.UseMinimalEndpoints();
app.Run();
```

### 📮 Support

- **Documentation**: https://github.com/smavrommatis/MinimalEndpoints/tree/main/docs
- **Issues**: https://github.com/smavrommatis/MinimalEndpoints/issues
- **Discussions**: https://github.com/smavrommatis/MinimalEndpoints/discussions
- **Security**: See SECURITY.md

### 🙏 Acknowledgments

- Inspired by ASP.NET Core Minimal APIs
- Built on Roslyn Source Generators and Analyzers
- Thanks to the .NET community for feedback

---

## [Unreleased]

Nothing yet. This is the first stable release!

---

[1.0.0]: https://github.com/smavrommatis/MinimalEndpoints/releases/tag/v1.0.0

