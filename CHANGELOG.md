# Changelog

All notable changes to MinimalEndpoints will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **MINEP015** (error) — flags an endpoint whose Map attribute is malformed (a null route pattern or an
  empty HTTP-method set, e.g. `[MapMethods("/x", new string[0])]`); the generator declines such endpoints.
- **Record-class endpoints and groups** are now supported (`record class` declarations are recognized
  alongside plain classes).

### Fixed
- **MINEP004** diagnostics are now emitted in a deterministic order.
- Generated-file line endings are now deterministic.
- Using-directive ordering is now culture-invariant.
- Generator discovery now has widened exception safety so a malformed referenced assembly cannot crash it.
- **MINEP011** no longer cascades extra diagnostics from the same endpoint.
- Catch-all route segments are now considered in route-overlap (MINEP004) analysis.
- The code-fix Fix-All no longer duplicates using directives.
- Corrected the `IConfigurableGroup` XML-doc example (`RequireRateLimiting`) and the
  `ScanReferencedEndpoints` XML-doc structure.

## [1.3.0] - 2026-06-20

### Added
- **MINEP010** (error) — the entry point method must not be generic; a generic `Handle<T>()` cannot be
  mapped because the generated handler cannot supply type arguments.
- **MINEP011** (error) — an entry point parameter must not use a `ref`/`out`/`in` or pointer modifier
  (ASP.NET Core cannot model-bind it); such endpoints are no longer emitted.
- **MINEP012** (error) — the endpoint must be assignable to its `ServiceType` (implement the interface or
  derive from the base type); previously a non-implementing class miscompiled with CS0311. The generator
  now degrades a non-assignable `ServiceType` to concrete registration so the only error is MINEP012.
- **MINEP013** (warning) — flags two endpoints registering the same `ServiceType` (the DI container keeps
  only the last registration, so one route would run the other endpoint's class).
- **MINEP014** (warning) — flags an endpoint whose `Group` is a valid `[MapGroup]` but has an unsupported
  shape, so the endpoint is mapped without the group's prefix/configuration.

### Fixed
- An entry point **inherited** (not overridden) from a base class is now discovered instead of being
  falsely reported as missing (MINEP001) — entry-point resolution walks the base-class chain.
- A group's **`ParentGroup`** that is not a `[MapGroup]` type is now reported (the generalized **MINEP005**,
  "Invalid group type") instead of silently dropping the parent link.
- An endpoint/group **nested in an open generic type** (`Outer<T>.Inner`) is now reported as an unsupported
  shape (**MINEP008**) instead of emitting non-compiling code.
- The ambiguous-route check (**MINEP004**) no longer mistakes an `=` inside a route constraint
  (e.g. `{id:regex(^a=b$)}`) for an optional/default segment, removing a false positive.

### Changed
- A handler parameter's by-reference modifier now participates in the incremental generator's cache key
  (determinism hardening).

## [1.2.0] - 2026-06-14

### Added
- **Cross-assembly endpoint & group discovery.** A host can now opt in with
  `[assembly: ScanReferencedEndpoints]` to register `[Map*]` endpoints and `[MapGroup]` groups defined
  in *referenced compiled assemblies* (project or package references) — previously only endpoints in the
  current project were discovered. Groups compose across the assembly boundary (an endpoint or group in
  one assembly can join a group defined in another). Discovery stays fully compile-time with no runtime
  reflection; the default (no-attribute) build is byte-identical and zero-cost. Referenced endpoint/group
  types must be `public` (the host's generated code references them across the assembly boundary); a
  non-public `ServiceType` is ignored and the concrete class is registered. Only **directly**-referenced
  assemblies are scanned. Pass marker types to scan only specific assemblies —
  `[assembly: ScanReferencedEndpoints(typeof(SomeTypeInThatAssembly))]`; with no arguments, all referenced
  assemblies that use MinimalEndpoints are scanned.
- **MINEP009** — warns when an endpoint's `Group` (or a group's `ParentGroup`) refers to a public
  `[MapGroup]` type in a referenced assembly that cross-assembly scanning won't cover, so the group would
  be silently dropped (the endpoint mapped without its prefix/configuration). Tells you to add (or widen
  the targets of) `[assembly: ScanReferencedEndpoints]`.

### Documentation
- Documented that **route parameters in a group prefix** — e.g. `[MapGroup("/v{version}")]`, including
  constraints (`{version:int}`) and tokens inherited from parent groups — bind into endpoint handler
  parameters by name, with no `[FromRoute]` attribute or generator change required. Added end-to-end
  tests covering single-level, hierarchical, and constrained prefix tokens.

## [1.1.0] - 2026-06-13

A large correctness, reliability, and packaging release. The source generator no longer crashes on
malformed or ambiguous code, incremental generation is now correctly cached, group configuration
moved to a static contract, and the package multi-targets net8.0/net9.0/net10.0.

### Added
- **MINEP008** — diagnostic for endpoint/group classes with an unsupported shape (open generic,
  file-local, or below-`internal` accessibility); such classes are skipped instead of producing
  non-compiling generated code.
- **MINEP999** — surfaces an unexpected generator failure as a clear, actionable build error instead
  of the opaque CS8785.
- Multi-targeting: the runtime package now ships **net8.0, net9.0, and net10.0** (was net10.0-only).

### Changed
- **`IConfigurableGroup.ConfigureGroup` is now `static abstract void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)`** (was instance `void ConfigureGroup(RouteGroupBuilder)`); groups are no longer registered in or resolved from DI, matching the already-static endpoint `Configure` and `ShouldMap`.
- The generated extension class is now `internal`, avoiding CS0436 collisions when two referencing projects both run the generator. **Endpoints and groups may now be `internal`** (only open-generic, file-local, or below-`internal` types are skipped).
- Discovery uses `ForAttributeWithMetadataName`; pipeline models are value-equatable and keyed by fully-qualified name, so **incremental generation is correctly cached** (warm rebuilds reuse output).
- Lowered the Roslyn floor to **4.8.0** (SDK 8.0.100 / VS 17.8) so older toolchains run the generator.
- Type names are rendered from the symbol via a Roslyn `SymbolDisplayFormat` (tuples, arrays, nullable reference types, nested generics, pointers).
- Publishing uses **NuGet Trusted Publishing (OIDC)** instead of a stored API key; release builds are deterministic with full SourceLink; CI enforces a 60% line/branch/method coverage gate on the merged total across test projects.

### Fixed
- The generator/analyzer no longer crash (CS8785 / AD0001) on malformed, mid-typing, or ambiguous endpoint source (multiple Map attributes, duplicated `[MapGroup]`, etc.).
- `[MapHead]` and single-element `[MapMethods]` now emit a valid `MapMethods(pattern, [verbs], Handler)` call (previously emitted a non-existent `MapHead` extension / a malformed call).
- Generated handler signatures render complex types correctly: tuples containing multi-argument generics, generic-of-array (`List<int[]>`), jagged + multidimensional arrays, and nullable reference annotations.
- Entry-point methods named with a reserved C# keyword are escaped at the generated call site.
- Optional-parameter defaults are reproduced as valid C# literals; floating-point defaults use round-trippable `G9`/`G17` format.
- **MINEP004**: nested group prefixes without a leading slash join correctly to the parent route; a duplicated verb in one `[MapMethods]` no longer reports a route as conflicting with itself; a trailing optional parameter (`/users/{id?}`) is detected as overlapping the bare path.
- The MINEP001 code fix now produces a compilable method (no CS1998; adds the required usings; honors a custom `EntryPoint`; escapes keyword names) and no longer offers an action that would create a duplicate member (CS0111/CS0102).
- Numerous documentation and sample accuracy fixes.

## [1.0.0] - 2025-12-31

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

#### Diagnostic Analyzers (7 Total)
- **MINEP001**: Missing entry point method detection - ensures endpoint has Handle/HandleAsync method
- **MINEP002**: Multiple Map attributes validation - prevents conflicting attributes
- **MINEP003**: ServiceType interface validation - validates interface compatibility
- **MINEP004**: Ambiguous route detection - warns about duplicate route patterns
- **MINEP005**: Invalid endpoint group validation - ensures groups have [MapGroup] attribute
- **MINEP006**: Cyclic group hierarchy detection - prevents circular parent-child relationships
- **MINEP007**: Mixed endpoint/group detection - prevents classes from being both endpoint and group

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
- 7 diagnostic documentation files (MINEP001-007)
- 3 comprehensive example guides
- Architecture documentation
- Performance benchmarking guide
- Migration guide from other approaches
- Troubleshooting guide
- Contributing guidelines

### 🧪 Testing

- Comprehensive unit test suite
- Benchmark suite for performance validation
- Sample projects (MinimalEndpoints.Sample, MinimalEndpoints.AdvancedSample)

### 📦 Technical Details

**Target Frameworks:**
- .NET 10.0 (runtime package targets `net10.0`)
- Source generator/analyzer: Roslyn 5.0.0

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

[Unreleased]: https://github.com/smavrommatis/MinimalEndpoints/compare/v1.3.0...HEAD
[1.3.0]: https://github.com/smavrommatis/MinimalEndpoints/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/smavrommatis/MinimalEndpoints/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/smavrommatis/MinimalEndpoints/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/smavrommatis/MinimalEndpoints/releases/tag/v1.0.0

