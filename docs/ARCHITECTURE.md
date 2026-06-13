# MinimalEndpoints Architecture

This document explains the internal architecture of MinimalEndpoints, how the source generator and analyzers work together, and the relationships between key components.

---

## 📐 Overview

MinimalEndpoints uses **Roslyn Source Generators** and **Analyzers** to provide compile-time code generation with zero runtime overhead. The system consists of these main components:

1. **Annotations** - Attributes that mark endpoint and group classes
2. **Analyzers** - Compile-time validation and diagnostics (`EndpointsAnalyzer` and `GroupsAnalyzer`)
3. **Source Generator** - Automatic code generation for endpoint registration (`MinimalEndpointsGenerator`)
4. **Code Fixes** - Quick fixes for selected diagnostics (`EntryPointCodeFixProvider`)

```
┌─────────────────────────────────────────────────────────────┐
│                      User Code                              │
│  [MapGet("/api/users")]                                    │
│  public class GetUsersEndpoint { ... }                     │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│              Roslyn Compilation                             │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐  │
│  │  Analyzers   │  │  Generator   │  │  Code Fixes     │  │
│  │  (Validate)  │  │  (Generate)  │  │  (Suggest)      │  │
│  └──────────────┘  └──────────────┘  └─────────────────┘  │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────┐
│           Generated Code (Compile-time)                     │
│  internal static partial class MinimalEndpointExtensions   │
│  public static IServiceCollection AddMinimalEndpoints(...) │
│  public static IApplicationBuilder UseMinimalEndpoints(...) │
└─────────────────────────────────────────────────────────────┘
```

---

## 🏗️ Component Architecture

### 1. Annotations Layer

**Location**: `src/MinimalEndpoints/Annotations/`

The annotations layer provides attributes that users apply to their endpoint classes:

```csharp
[MapGet("/api/users")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
```

**Key Classes**:
- `MapMethodsBaseAttribute` - Base class for all mapping attributes
  - Contains common properties: `Pattern`, `Lifetime`, `EntryPoint`, `ServiceType`, `Group`
  - All HTTP-specific attributes inherit from this base class
  - This inheritance pattern allows analyzers to check for any mapping attribute using a single base type
- `MapGetAttribute`, `MapPostAttribute`, etc. - HTTP method-specific attributes
  - Each specifies its HTTP method(s) in the constructor
  - Example: `[MapGet("/route")]` is equivalent to `[MapMethods("/route", "GET")]`
- `MapMethodsAttribute` - For custom HTTP method combinations
  - Allows specifying multiple methods: `[MapMethods("/route", new[] { "GET", "POST" })]`

**Properties**:
- `Pattern` - Route pattern (e.g., "/api/users/{id}")
- `Lifetime` - DI service lifetime (Singleton, Scoped, Transient) - default is Scoped
- `EntryPoint` - Custom entry point method name (default: "Handle" or "HandleAsync")
- `ServiceType` - Interface type for DI registration (enables interface-based injection)
- `Group` - Type of a `[MapGroup]`-decorated class (optionally implementing `IConfigurableGroup`) for shared configuration and route prefixing

### 2. Analyzer Layer

**Location**: `src/MinimalEndpoints.CodeGeneration/`

The analyzer layer provides compile-time diagnostics and validation. There are **two** analyzers, split by what they validate. The full diagnostic catalog is MINEP001–MINEP008 plus MINEP999 (the generator-reported failure diagnostic, see below).

#### EndpointsAnalyzer

`EndpointsAnalyzer` (`Endpoints/Analyzers/EndpointsAnalyzer.cs`) is a syntax-node analyzer (`RegisterSyntaxNodeAction` on `ClassDeclarationSyntax`) that validates an individual endpoint class.

**Diagnostics** (`SupportedDiagnostics`):
- **MINEP001**: Endpoint missing entry point method
- **MINEP002**: Multiple Map attributes detected
- **MINEP003**: `ServiceType` interface missing entry point method
- **MINEP005**: Invalid endpoint group type (the `Group` type is not decorated with `[MapGroup]`)
- **MINEP008**: Endpoint or group class has an unsupported shape (open generic, file-local, or below-`internal` accessibility)

**Flow**:
```
User writes endpoint class
         ↓
Analyzer visits each ClassDeclarationSyntax
         ↓
Fast skip: no attribute lists, abstract, or no Map attribute → return
         ↓
For a class carrying exactly one Map attribute (reported once, even across partial parts):
  1. More than one Map attribute → MINEP002
  2. Unsupported shape (SymbolDefinitionFactory.ClassifyShape) → MINEP008
  3. No valid entry point method → MINEP001
  4. ServiceType specified but its interface lacks the entry point → MINEP003
  5. Group specified but the type has no [MapGroup] → MINEP005
         ↓
Report diagnostics to IDE
```

#### GroupsAnalyzer

`GroupsAnalyzer` (`Groups/Analyzers/GroupsAnalyzer.cs`) is a compilation-scoped analyzer. It registers a compilation-start action that collects lightweight per-symbol facts (`RegisterSymbolAction` on `NamedType`), then reports at compilation end (`RegisterCompilationEndAction`) once the whole hierarchy is known. The `MINEP004`/`MINEP006` descriptors carry `WellKnownDiagnosticTags.CompilationEnd`.

**Diagnostics** (`SupportedDiagnostics`):
- **MINEP004**: Ambiguous route pattern detected (same HTTP method + normalized route across endpoints, accounting for group prefixes)
- **MINEP006**: Cyclic group hierarchy detected
- **MINEP007**: A class is marked as both an endpoint and a group
- **MINEP008**: Unsupported group shape (reported here for group classes; `EndpointsAnalyzer` owns MINEP008 for endpoint classes, so they are not double-reported)

**Flow**:
```
Compilation start
         ↓
For each NamedType with a Map/Group attribute:
  - both an endpoint and a group attribute → MINEP007
  - endpoint class → collect EndpointRouteFacts (route + verbs + group)
  - group class → collect EndpointGroupDefinition (or MINEP008 for a bad shape)
         ↓
Compilation end:
  - GroupHierarchy.Build(...) resolves prefixes and detects cycles
  - report MINEP004 for routes that collide after prefixing
  - report MINEP006 for each cycle
```

#### Key Utilities

**`SymbolDefinitionFactory`** (`Models/SymbolDefinitionFactory.cs`):
- `Classify` / `TryCreateSymbol` - decide whether a symbol is an endpoint, a group, both (MINEP007), or neither
- `ClassifyShape` / `DescribeShapeRejection` - detect unsupported shapes for MINEP008
- Shared by both analyzers and the generator so all three agree on what a valid endpoint/group is

**`EndpointGroupUtilities` / `GroupHierarchy`** (`Groups/`):
- `GroupHierarchy.Build(...)` - builds the parent/child tree, resolves full route prefixes, and detects cycles (used by both `GroupsAnalyzer` and the generator)

**`WellKnownTypes`**:
- Stores constant names for types, namespaces, and the set of Map-attribute metadata names
- Provides fast string-based type checking

### 3. Source Generator Layer

**Location**: `src/MinimalEndpoints.CodeGeneration/`

The source generator creates extension methods at compile-time:

#### MinimalEndpointsGenerator (IIncrementalGenerator)

`MinimalEndpointsGenerator` (`MinimalEndpointsGenerator.cs`) is registered with `[Generator]` and drives the incremental pipeline:

**Pipeline**:
```
1. ForAttributeWithMetadataName — one provider per Map attribute
   │
   ├─ Predicate: node is ClassDeclarationSyntax (cheap syntax gate)
   │
   └─ Transform: SymbolDefinitionFactory.TryCreateSymbol(symbol)
                 → EndpointDefinition | EndpointGroupDefinition | null
         ↓
2. Collect() each provider, then Combine/Aggregate into one array
         ↓
3. GenerateEndpointExtensions:
   - Deduplicate by fully-qualified class name (defense in depth)
   - Split into endpoints vs. group definitions
   - GroupHierarchy.Build(groupDefinitions) — FQN-keyed, resolves parent links + prefixes
   - MinimalEndpointsFileBuilder.GenerateFile("MinimalEndpoints.Generated",
       "MinimalEndpointExtensions", endpoints, hierarchy)
   - AddSource("MinimalEndpointExtensions.g.cs", ...)
```

Because each provider uses `ForAttributeWithMetadataName` over the **merged** symbol, partial classes, attributes split across parts, and ambiguous multi-attribute combinations are resolved identically regardless of which provider fired. If the output step throws, the generator catches it and reports **MINEP999** (a clear build error) instead of the opaque CS8785.

#### MinimalEndpointsFileBuilder

Builds the generated C# file (`MinimalEndpointsFileBuilder.cs`):

```csharp
public static CSharpFileScope GenerateFile(
    string namespaceName,            // "MinimalEndpoints.Generated"
    string className,                // "MinimalEndpointExtensions"
    List<EndpointDefinition> endpoints,
    GroupHierarchy hierarchy)
{
    if (!endpoints.Any() && hierarchy.Count == 0)
        return null;

    var fileScope = CreateFileScope(namespaceName, className);
    var names = GeneratedNames.Build(endpoints, hierarchy.Groups); // disambiguates sanitized names

    fileScope.AddMinimalEndpointsRegistrationMethod(endpoints, hierarchy.Groups.ToImmutableArray());

    foreach (var group in hierarchy.Groups)
        fileScope.AddGroupMapMethod(group, hierarchy, names);

    foreach (var endpoint in endpoints)
        fileScope.AddMinimalEndpointMapMethod(endpoint, hierarchy, names);

    fileScope.AddMinimalEndpointsMapAllMethods(endpoints, hierarchy, names);

    return fileScope;
}
```

**Generated Structure**:

The generated class is `internal static partial class MinimalEndpointExtensions`: it is always called from the same assembly the generator ran in, and a fixed public type in a fixed namespace would collide (CS0436) when two referencing projects both run the generator. Only `AddMinimalEndpoints()` and `UseMinimalEndpoints()` are `public static`; every per-endpoint and per-group `Map*` method is `private static`.

```csharp
// Header (auto-generated comment)
using System;
using Microsoft.AspNetCore.Builder;
// ... other usings

namespace MinimalEndpoints.Generated;

[GeneratedCode("MinimalEndpoints.CodeGeneration.EndpointGenerator", "1.0.0")]
internal static partial class MinimalEndpointExtensions
{
    // DI Registration (endpoints only — groups are NOT registered in DI)
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services)
    {
        services.AddScoped<MyEndpoint>();
        return services;
    }

    // Group mapping (private; configuration is invoked statically)
    private static RouteGroupBuilder Map__MyNamespace_MyGroup(
        this IEndpointRouteBuilder builder,
        IApplicationBuilder app)
    {
        var group = builder.MapGroup("/api/v1");
        MyNamespace.MyGroup.ConfigureGroup(app, group); // static, only if IConfigurableGroup
        return group;
    }

    // Individual endpoint mapping (private)
    private static IEndpointRouteBuilder Map__MyNamespace_MyEndpoint(
        this IEndpointRouteBuilder builder,
        IApplicationBuilder app)
    {
        static Task<IResult> Handler([FromServices]MyEndpoint endpointInstance)
        {
            return endpointInstance.HandleAsync();
        }

        var endpoint = builder.MapGet("/route", Handler);
        // MyEndpoint.Configure(app, endpoint);  // emitted only if IConfigurableEndpoint
        return builder;
    }

    // Conditional endpoint mapping (when the class implements IConditionallyMapped)
    private static IEndpointRouteBuilder? Map__MyNamespace_MyConditionalEndpoint(
        this IEndpointRouteBuilder builder,
        IApplicationBuilder app)
    {
        if (!MyNamespace.MyConditionalEndpoint.ShouldMap(app))
        {
            return null;
        }
        // ... Handler + MapGet as above
        return builder;
    }

    // Map All
    public static IApplicationBuilder UseMinimalEndpoints(this IApplicationBuilder app)
    {
        var builder = app as IEndpointRouteBuilder ?? throw new ArgumentException(...);

        // Create and configure groups in hierarchy order (parents before children)
        var grp = builder.Map__MyNamespace_MyGroup(app);

        // Map endpoints (endpoints belonging to a group receive the RouteGroupBuilder)
        builder.Map__MyNamespace_MyEndpoint(app);
        return app;
    }
}
```

> The `[GeneratedCode(...)]` attribute string `"MinimalEndpoints.CodeGeneration.EndpointGenerator"` above is reproduced verbatim from the generated file header — it is the tool name baked into the output, not a live class name.

### 4. Groups

Groups give a set of endpoints a shared route prefix and (optionally) shared configuration.

- **`MapGroupAttribute`** (`Annotations/MapGroupAttribute.cs`) marks a class as a group. It carries the route `Prefix` (constructor argument) and an optional `ParentGroup` type for hierarchical nesting. Cyclic hierarchies are rejected at compile time (MINEP006).
- **`IConfigurableGroup`** (optional) lets a group apply shared configuration. Its single member is **static**:

  ```csharp
  static abstract void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group);
  ```

  Because it is static, the generated code invokes it directly (`MyGroup.ConfigureGroup(app, group)`) — **groups are never registered in or resolved from DI**. A group with no `IConfigurableGroup` still works; the interface is purely opt-in.
- **`IConditionallyMapped`** (optional, also implemented by endpoints) gates mapping at startup:

  ```csharp
  static abstract bool ShouldMap(IApplicationBuilder app);
  ```

  When a group or endpoint returns `false`, it is skipped. Skipping a group also skips its child groups and their endpoints. The generator emits nullable-returning `Map*` methods (`RouteGroupBuilder?` / `IEndpointRouteBuilder?`) and null-propagation in `UseMinimalEndpoints` for conditionally mapped hierarchies.

Endpoints opt into a group via the `Group` property: `[MapGet("/products", Group = typeof(ApiV1Group))]`. The endpoint's `Map*` method then receives the group's `RouteGroupBuilder` and maps onto it, so the final route is the group prefix joined with the endpoint pattern.

The sibling endpoint contract `IConfigurableEndpoint` is static in the same way:

```csharp
static abstract void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint);
```

---

## 🔄 Data Flow

### Model Classes

#### EndpointDefinition
Core model representing an endpoint:

```csharp
internal sealed class EndpointDefinition
{
    public TypeDefinition ClassType { get; set; }
    public bool IsConfigurable { get; set; }
    public string MappingEndpointMethodName { get; }
    public MapMethodsAttributeDefinition MapMethodsAttribute { get; set; }
    public MethodInfo EntryPoint { get; set; }
}
```

#### TypeDefinition
Represents a type with smart name simplification:

```csharp
public class TypeDefinition
{
    private string _fullName; // "System.Threading.Tasks.Task<int>"
    private Dictionary<int, string> _displayStringCache;

    public string ToDisplayString(HashSet<string> usings)
    {
        // Returns "Task<int>" if System.Threading.Tasks is in usings
        // Returns full name otherwise
        // Cached for performance
    }
}
```

#### MethodInfo & ParameterInfo
Represent method signatures:

```csharp
internal class MethodInfo
{
    public string Name { get; set; }
    public TypeDefinition ReturnType { get; set; }
    public Dictionary<string, ParameterInfo> Parameters { get; set; }
    public bool IsAsync { get; set; }
}

internal class ParameterInfo
{
    public string Name { get; set; }
    public TypeDefinition Type { get; set; }
    public bool Nullable { get; set; }
    public List<AttributeDefinition> Attributes { get; set; }
}
```

---

## ⚡ Performance Optimizations

### 1. Incremental Generation
- Uses `IIncrementalGenerator` for optimal performance
- Only regenerates when inputs change
- Predicate filters syntax nodes before semantic analysis

### 2. Caching
- **TypeDefinition**: Caches display strings per usings set
- **EndpointDefinition**: Caches mapping method names
- **Attribute Lookup**: Uses FrozenDictionary for O(1) lookups

### 3. Minimal Allocations
- StringBuilder instead of string concatenation
- Foreach loops instead of LINQ where possible
- Pre-sized collections when size is known

### 4. Smart Type Simplification
- Only computes simplified names when needed
- Reuses cached results for repeated calls
- Avoids expensive SymbolDisplayFormat operations

---

## 🧪 Testing Architecture

### Test Organization

The `tests/` folder contains six projects:

```
tests/
  MinimalEndpoints.Tests.Common/                  # Shared test helpers (CompilationBuilder.cs)
  MinimalEndpoints.CodeGeneration.Tests/          # Unit tests: analyzers, file builder, models, utilities
    ├── Endpoints/Analyzers/EndpointsAnalyzerTests.cs
    ├── Groups/Analyzers/GroupsAnalyzer_AmbiguousRoutesTests.cs
    ├── Groups/Analyzers/GroupsAnalyzer_CyclicGroupHierarchyTests.cs
    ├── Groups/Analyzers/GroupsAnalyzer_InvalidSymbolKindTests.cs
    ├── Groups/Analyzers/GroupsAnalyzer_UnsupportedShapeTests.cs
    ├── MinimalEndpointsFileBuilderTests.cs
    ├── Models/        # SymbolDefinitionFactory, TypeDefinition, ...
    └── Utilities/     # extension-method tests
  MinimalEndpoints.CodeGeneration.IntegrationTests/  # Full generator-driver / end-to-end generation
  MinimalEndpoints.CodeFixes.Tests/               # EntryPointCodeFixProvider tests
  MinimalEndpoints.EndToEnd.Tests/                # HTTP tests against a running app
  MinimalEndpoints.EndToEnd.TestApp/              # The app under test for the E2E tests
```

`CompilationBuilder.cs` lives in **`MinimalEndpoints.Tests.Common`** (not in the test project itself); `MinimalEndpoints.CodeGeneration` exposes its internals to the test/common projects via `InternalsVisibleTo`.

### CompilationBuilder
Test helper for creating compilations:

```csharp
var compilation = new CompilationBuilder(sourceCode)
    .WithMvcReferences()
    .Build();
```

Features:
- Global usings support
- Automatic assembly references
- MVC/ASP.NET Core references
- Validation on build

### Test Patterns

**Analyzer Tests** (`EndpointsAnalyzerTests`, `GroupsAnalyzer_*Tests`):
```csharp
[Fact]
public void Analyzer_Scenario_ExpectedBehavior()
{
    var code = "...";
    var diagnostics = GetDiagnostics(code);
    Assert.Contains(diagnostics, d => d.Id == "MINEP001");
}
```

**Generator Tests** (`MinimalEndpoints.CodeGeneration.IntegrationTests`):
```csharp
[Fact]
public void GeneratedCode_Scenario_ExpectedOutput()
{
    var code = "...";
    var (generatedCode, _) = GenerateCodeAndCompile(compilation);
    Assert.Contains("expected output", generatedCode);
}
```

---

## 🔌 Extension Points

### Custom Attributes
Users can extend with custom mapping attributes by inheriting from `MapMethodsBaseAttribute`.

### IConfigurableEndpoint
Endpoints can implement this interface for custom configuration:

```csharp
public interface IConfigurableEndpoint
{
    static abstract void Configure(
        IApplicationBuilder app,
        RouteHandlerBuilder endpoint);
}
```

### ServiceType
Endpoints can specify an interface for DI registration:

```csharp
[MapGet("/api/users", ServiceType = typeof(IGetUsers))]
public class GetUsersEndpoint : IGetUsers
{
    // Implementation
}
```

This registers the endpoint as:
```csharp
services.AddScoped<IGetUsers, GetUsersEndpoint>();
```

---

## 📊 Dependency Graph

The **Core** project (`MinimalEndpoints`) references **both** the CodeGeneration and CodeFixes projects as analyzers — `OutputItemType="Analyzer"`, `ReferenceOutputAssembly="false"`, `PrivateAssets="all"` — so their assemblies ship in the analyzer slot of the package but are not runtime dependencies. CodeGeneration and CodeFixes do **not** reference each other (and CodeFixes has no `ProjectReference` at all).

```
┌────────────────────────────────────────────┐
│            MinimalEndpoints (Core)           │
│  - Annotations (MapGet/.../MapGroup)         │
│  - IConfigurableEndpoint / IConfigurableGroup│
│  - IConditionallyMapped                      │
│  (net8.0;net9.0;net10.0; the package)        │
└───────┬──────────────────────────┬───────────┘
        │ analyzer ref              │ analyzer ref
        │ (ReferenceOutputAssembly  │ (ReferenceOutputAssembly
        │  = false)                 │  = false)
        ▼                           ▼
┌──────────────────────────┐  ┌──────────────────────────┐
│ MinimalEndpoints.        │  │ MinimalEndpoints.CodeFixes │
│ CodeGeneration           │  │  - EntryPointCodeFix-      │
│  - MinimalEndpointsGen-  │  │    Provider                │
│    erator                │  │                            │
│  - EndpointsAnalyzer     │  │ (netstandard2.0;           │
│  - GroupsAnalyzer        │  │  no ProjectReference)      │
│  - MinimalEndpointsFile- │  └──────────────────────────┘
│    Builder, Models, etc. │
│ (netstandard2.0)         │
└──────────────────────────┘
```

Both analyzer projects target `netstandard2.0` (a Roslyn requirement) and pin a Roslyn floor of `Microsoft.CodeAnalysis.CSharp` **4.8.0** (SDK 8.0.100 / VS 17.8) — the oldest toolchain that supports net8.0.

**External Dependencies**:
- Microsoft.CodeAnalysis.CSharp 4.8.0 (Roslyn floor)
- Microsoft.CodeAnalysis.Analyzers
- Microsoft.AspNetCore.App (FrameworkReference, for `IResult` etc.)

---

## 🚀 Build & Distribution

### NuGet Package Structure

```
Blackeye.MinimalEndpoints.nupkg
├── lib/
│   ├── net8.0/
│   │   └── MinimalEndpoints.dll
│   ├── net9.0/
│   │   └── MinimalEndpoints.dll
│   └── net10.0/
│       └── MinimalEndpoints.dll
├── analyzers/
│   └── dotnet/
│       └── cs/
│           ├── MinimalEndpoints.CodeGeneration.dll
│           └── MinimalEndpoints.CodeFixes.dll
└── [package metadata]
```

The package multi-targets `net8.0;net9.0;net10.0`, so the consumer prerequisite is **.NET 8.0 or later**.

### How It Works at Install Time

1. User installs NuGet package
2. Roslyn discovers analyzers/generators in `analyzers/` folder
3. Analyzers run during compilation in IDE
4. Generator creates source files (visible in IDE as `[Generated]`)
5. Generated code is included in build output

**Zero Runtime Overhead**: All code is generated at compile-time, no reflection or runtime discovery.

---

## 📚 Key Design Decisions

### Why Source Generators?
- **Compile-time validation**: Catch errors before runtime
- **Zero overhead**: No reflection, no startup cost
- **Great IDE experience**: IntelliSense, go-to-definition work
- **Type safety**: Strongly-typed, no magic strings

### Why Incremental Generators?
- **Performance**: Only regenerate when needed
- **IDE responsiveness**: Faster feedback
- **Scalability**: Works well with large codebases

### Why Separate Analyzers?
- **Focused responsibilities**: `EndpointsAnalyzer` validates a single endpoint class via syntax-node actions; `GroupsAnalyzer` needs the whole compilation to detect cross-endpoint route conflicts and group cycles, so it runs as a compilation-start/-end analyzer
- **Better diagnostics**: Rich error messages with context, with help links per rule
- **Code fixes**: `EntryPointCodeFixProvider` offers an automatic fix for MINEP001
- **Independent validation**: Works even if generation is disabled

---

## 🔍 Debugging Tips

### View Generated Code
In Visual Studio:
- Solution Explorer → Project → Dependencies → Analyzers → MinimalEndpoints.CodeGeneration → MinimalEndpointExtensions.g.cs

### Debug Generator
Add to generator project:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### Debug Analyzer
- Attach debugger to Visual Studio or dotnet build process
- Set breakpoints in analyzer code
- Use `Debugger.Launch()` for automated attach

---

## 🎯 Future Enhancements

Potential areas for expansion (ambiguous-route detection already ships as MINEP004, and a MINEP001 code fix already ships):

1. **More Analyzers**: Detect unused endpoints
2. **More Code Fixes**: Cover additional diagnostics beyond MINEP001
3. **Attribute Providers**: Custom metadata attributes
4. **Endpoint Filters**: Generate filter pipeline code
5. **OpenAPI Generation**: Integrate with Swagger/OpenAPI
6. **AOT Support**: Ensure compatibility with Native AOT

---

**Version**: 1.1.0
**Last Updated**: 2026-06-13

