# MinimalEndpoints Architecture

This document explains the internal architecture of MinimalEndpoints, how the source generator and analyzers work together, and the relationships between key components.

---

## 📐 Overview

MinimalEndpoints uses **Roslyn Source Generators** and **Analyzers** to provide compile-time code generation with zero runtime overhead. The system consists of three main components:

1. **Annotations** - Attributes that mark endpoint classes
2. **Analyzers** - Compile-time validation and diagnostics
3. **Source Generator** - Automatic code generation for endpoint registration

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
- `MapGetAttribute`, `MapPostAttribute`, etc. - HTTP method-specific attributes
- `MapMethodsAttribute` - For custom HTTP method combinations

**Properties**:
- `Pattern` - Route pattern (e.g., "/api/users/{id}")
- `Lifetime` - DI service lifetime (Singleton, Scoped, Transient)
- `GroupPrefix` - Optional route prefix
- `EntryPoint` - Custom entry point method name
- `ServiceType` - Interface type for DI registration

### 2. Analyzer Layer

**Location**: `src/MinimalEndpoints.Analyzers/`

The analyzer layer provides compile-time diagnostics and validation:

#### MinimalEndpointsAnalyzer

Validates endpoint classes and reports errors:

**Diagnostics**:
- **MINEP001**: Missing entry point method
- **MINEP002**: Multiple MapMethods attributes
- **MINEP003**: ServiceType interface missing entry point

**Flow**:
```
User writes endpoint class
         ↓
Analyzer scans syntax tree
         ↓
For each class with [MapGet/MapPost/etc]:
  1. Check for multiple attributes → MINEP002
  2. Find entry point method → MINEP001 if not found
  3. If ServiceType specified, validate interface → MINEP003
         ↓
Report diagnostics to IDE
```

#### Key Utilities

**`MapMethodsUtilities`**:
- Extracts MapMethods attribute metadata
- Maps attribute types to HTTP methods
- Handles named arguments (GroupPrefix, EntryPoint, ServiceType)

**`EndpointUtilities`**:
- Finds entry point methods (Handle, HandleAsync, or custom)
- Checks for IConfigurableEndpoint implementation
- Validates method signatures

**`WellKnownTypes`**:
- Stores constant names for types and namespaces
- Provides fast string-based type checking

### 3. Source Generator Layer

**Location**: `src/MinimalEndpoints.Analyzers/`

The source generator creates extension methods at compile-time:

#### EndpointGenerator (IIncrementalGenerator)

**Pipeline**:
```
1. CreateSyntaxProvider (predicate + transform)
   │
   ├─ Predicate: Fast syntax check (is ClassDeclarationSyntax with attributes?)
   │
   └─ Transform: Semantic analysis (extract endpoint metadata)
         ↓
2. Collect all endpoint definitions
         ↓
3. Generate code:
   - AddMinimalEndpoints() - DI registration
   - Map__[ClassName]() - Individual endpoint mapping
   - UseMinimalEndpoints() - Map all endpoints
```

#### EndpointCodeGenerator

Builds the generated C# file:

```csharp
public static CSharpFileScope GenerateCode(...)
{
    var fileScope = CreateFileScope(namespace, className)
        .AddMinimalEndpointsRegistrationMethod(endpoints);

    foreach (var endpoint in endpoints)
    {
        fileScope.AddMinimalEndpointMapMethod(endpoint);
    }

    fileScope.AddMinimalEndpointsMapAllMethods(endpoints);

    return fileScope;
}
```

**Generated Structure**:
```csharp
// Header (auto-generated comment)
using System;
using Microsoft.AspNetCore.Builder;
// ... other usings

namespace MinimalEndpoints.Generated;

[GeneratedCode("...", "1.0.0")]
public static partial class MinimalEndpointExtensions
{
    // DI Registration
    public static IServiceCollection AddMinimalEndpoints(this IServiceCollection services)
    {
        services.AddScoped<MyEndpoint>();
        return services;
    }

    // Individual Mapping
    public static IEndpointRouteBuilder Map__MyNamespace_MyEndpoint(
        this IEndpointRouteBuilder builder,
        IApplicationBuilder app)
    {
        static Task<IResult> Handler([FromServices]MyEndpoint endpoint)
        {
            return endpoint.HandleAsync();
        }

        var endpoint = builder.MapGet("/route", Handler);
        return builder;
    }

    // Map All
    public static IApplicationBuilder UseMinimalEndpoints(this IApplicationBuilder app)
    {
        var builder = app as IEndpointRouteBuilder ?? throw...;
        builder.Map__MyNamespace_MyEndpoint(app);
        return app;
    }
}
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

```
tests/
  MinimalEndpoints.Analyzers.Tests/
    ├── CompilationBuilder.cs          # Test helper
    ├── MinimalEndpointsAnalyzerTests.cs  # Analyzer tests
    ├── Integration/
    │   └── EndToEndCodeGenerationTests.cs  # E2E tests
    ├── Models/
    │   └── [Model Tests]
    └── Utilities/
        └── [Utility Tests]
```

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

**Analyzer Tests**:
```csharp
[Fact]
public void AnalyzerName_Scenario_ExpectedBehavior()
{
    var code = "...";
    var diagnostics = GetDiagnostics(code);
    Assert.Contains(diagnostics, d => d.Id == "MINEP001");
}
```

**Generator Tests**:
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
        IEndpointConventionBuilder endpoint);
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

```
┌─────────────────────────────────────┐
│     MinimalEndpoints (Core)         │
│  - Annotations                      │
│  - IConfigurableEndpoint            │
└────────────┬────────────────────────┘
             │ references
             ▼
┌─────────────────────────────────────┐
│  MinimalEndpoints.Analyzers         │
│  - EndpointGenerator                │
│  - MinimalEndpointsAnalyzer         │
│  - Models & Utilities               │
└────────────┬────────────────────────┘
             │ references
             ▼
┌─────────────────────────────────────┐
│  MinimalEndpoints.CodeFixes         │
│  - EntryPointCodeFixProvider        │
│  - (Future code fixes)              │
└─────────────────────────────────────┘
```

**External Dependencies**:
- Microsoft.CodeAnalysis.CSharp (Roslyn)
- Microsoft.CodeAnalysis.Analyzers
- Microsoft.AspNetCore.Http.Abstractions (for IResult)

---

## 🚀 Build & Distribution

### NuGet Package Structure

```
Blackeye.MinimalEndpoints.nupkg
├── lib/
│   └── net8.0/
│       └── MinimalEndpoints.dll
├── analyzers/
│   └── dotnet/
│       └── cs/
│           ├── MinimalEndpoints.Analyzers.dll
│           └── MinimalEndpoints.CodeFixes.dll
└── [package metadata]
```

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
- **Better diagnostics**: Rich error messages with context
- **Code fixes**: Can offer automatic fixes (future)
- **Independent validation**: Works even if generation is disabled

---

## 🔍 Debugging Tips

### View Generated Code
In Visual Studio:
- Solution Explorer → Project → Dependencies → Analyzers → MinimalEndpoints.Analyzers → MinimalEndpointExtensions.g.cs

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

Potential areas for expansion:

1. **More Analyzers**: Detect unused endpoints, ambiguous routes
2. **Code Fixes**: Auto-generate missing methods, fix common errors
3. **Attribute Providers**: Custom metadata attributes
4. **Endpoint Filters**: Generate filter pipeline code
5. **OpenAPI Generation**: Integrate with Swagger/OpenAPI
6. **AOT Support**: Ensure compatibility with Native AOT

---

**Version**: 1.0.0
**Last Updated**: 2025-12-20

