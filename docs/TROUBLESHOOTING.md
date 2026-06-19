# Troubleshooting Guide

Common issues and their solutions when using MinimalEndpoints.

## Table of Contents

- [Source Generator Issues](#source-generator-issues)
- [Analyzer Warnings](#analyzer-warnings)
- [Compilation Errors](#compilation-errors)
- [Runtime Issues](#runtime-issues)
- [IDE Issues](#ide-issues)
- [Performance Issues](#performance-issues)

---

## Source Generator Issues

### Generated Code Not Appearing

**Symptoms:**
- `AddMinimalEndpoints()` not found
- `UseMinimalEndpoints()` not found
- No IntelliSense for generated methods

**Solutions:**

1. **Clean and Rebuild**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Restart IDE**
   - Visual Studio: Restart
   - VS Code: Reload window (Ctrl+Shift+P → "Reload Window")
   - Rider: File → Invalidate Caches → Invalidate and Restart

3. **Check Package Installation**
   ```bash
   dotnet list package | Select-String "MinimalEndpoints"
   ```

   Should show:
   ```
   > Blackeye.MinimalEndpoints    1.3.0
   ```

4. **Verify Project Configuration**

   Ensure your project file has:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk.Web">
     <PropertyGroup>
       <TargetFramework>net8.0</TargetFramework> <!-- net8.0, net9.0, or net10.0 are all supported -->
       <Nullable>enable</Nullable>
     </PropertyGroup>
   </Project>
   ```

5. **Endpoints in another project / referenced assembly**

   The generator discovers endpoints declared in the **current project's source**. If your
   `AddMinimalEndpoints()`/`UseMinimalEndpoints()` call is in the same project as the endpoints, just
   reference the generated namespace:
   ```csharp
   using MinimalEndpoints.Generated; // resolves to THIS project's generated extensions
   ```

   Endpoints defined in a **referenced compiled assembly** (another project or a NuGet package) are
   **not** discovered by default. To register them, opt in once on the host assembly:
   ```csharp
   [assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints]
   ```
   The host then scans its referenced assemblies (only those that reference MinimalEndpoints) and
   registers their endpoints/groups as if they were local — composing groups across the boundary too.

   To enable it across many host projects at once, emit the attribute from MSBuild instead:
   ```xml
   <!-- Directory.Build.props -->
   <ItemGroup>
     <AssemblyAttribute Include="MinimalEndpoints.Annotations.ScanReferencedEndpointsAttribute" />
   </ItemGroup>
   ```

   **Requirements:** referenced endpoint/group classes must be `public` (the host references them
   across the assembly boundary); non-public ones are skipped. A non-public `ServiceType` is ignored —
   the endpoint is registered as its concrete class. Only assemblies the host references **directly**
   are scanned (purely transitive package references are not). Endpoints in the **same** project may
   remain `internal`.

### View Generated Code

**Visual Studio:**
1. Solution Explorer → Dependencies → Analyzers
2. Expand `MinimalEndpoints.CodeGeneration`
3. View `MinimalEndpointExtensions.g.cs`

**Rider:**
1. Right-click project → Show Generated Files
2. Find `MinimalEndpointExtensions.g.cs`

**VS Code:**

First opt in to writing generated files to disk by adding this to the consuming project:
```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Then, after a build, the generated files are written under:
```bash
# <tfm> matches your project's target framework (e.g. net8.0, net9.0, net10.0):
obj/Debug/<tfm>/generated/MinimalEndpoints.CodeGeneration/MinimalEndpoints.CodeGeneration.MinimalEndpointsGenerator/
```

### Generator Not Running

**Confirm the analyzer is loaded:**

- In Visual Studio or Rider, expand the project's **Dependencies → Analyzers** node and look for
  `MinimalEndpoints.CodeGeneration` (the source generator `MinimalEndpointsGenerator` ships there).
- Or inspect the generated output directly by setting `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>`
  on the consuming project, building, and checking for `MinimalEndpointExtensions.g.cs` under
  `obj/Debug/<tfm>/generated/...` (see [View Generated Code](#view-generated-code)).
- For a deeper trace, build with a binary log (`dotnet build -bl`) and open `msbuild.binlog` in the
  MSBuild Structured Log Viewer to inspect analyzer/generator execution.

If the generator isn't loading, try:

1. **Update .NET SDK**
   ```bash
   dotnet --version  # Should be 8.0 or higher
   ```

2. **Check for Conflicting Packages**
   ```bash
   dotnet list package --include-transitive | Select-String "Microsoft.CodeAnalysis"
   ```

---

## Analyzer Warnings

### MINEP001: Missing Entry Point Method

**Error:**
```
Class 'MyEndpoint' is marked with MapMethodsAttribute but does not contain a valid entry point method.
```

**Cause:** Endpoint class doesn't have a `Handle` or `HandleAsync` method.

**Solution:**

Option 1 - Add HandleAsync:
```csharp
[MapGet("/test")]
public class MyEndpoint
{
    public async Task<IResult> HandleAsync()
    {
        return Results.Ok();
    }
}
```

Option 2 - Add Handle:
```csharp
[MapGet("/test")]
public class MyEndpoint
{
    public IResult Handle()
    {
        return Results.Ok();
    }
}
```

Option 3 - Use custom entry point:
```csharp
[MapGet("/test", EntryPoint = "Execute")]
public class MyEndpoint
{
    public IResult Execute()
    {
        return Results.Ok();
    }
}
```

**Quick Fix:** Use IDE quick action (Ctrl+. or Alt+Enter) to automatically add method.

---

### MINEP002: Multiple Attributes Detected

**Error:**
```
Class 'MyEndpoint' is marked with multiple MapMethods attributes. Only one MapMethods attribute is allowed per endpoint class.
```

**Cause:** Endpoint has more than one mapping attribute.

**Bad:**
```csharp
[MapGet("/test")]
[MapPost("/test")]  // ❌ Two attributes
public class MyEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

**Solution:**

Use `MapMethods` for multiple HTTP methods:
```csharp
[MapMethods("/test", new[] { "GET", "POST" })]
public class MyEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

Or create separate endpoints:
```csharp
[MapGet("/test")]
public class GetTestEndpoint
{
    public IResult Handle() => Results.Ok();
}

[MapPost("/test")]
public class PostTestEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

---

### MINEP003: ServiceType Interface Missing Entry Point

**Error:**
```
The ServiceType 'IMyEndpoint' specified for endpoint 'MyEndpoint' does not contain the entry point method 'HandleAsync'.
```

**Cause:** Interface registered with `ServiceType` doesn't declare the entry point method.

**Bad:**
```csharp
public interface IMyEndpoint
{
    // Missing HandleAsync
}

[MapGet("/test", ServiceType = typeof(IMyEndpoint))]
public class MyEndpoint : IMyEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}
```

**Solution:**

Add method to interface:
```csharp
public interface IMyEndpoint
{
    Task<IResult> HandleAsync(); // ✅ Added
}

[MapGet("/test", ServiceType = typeof(IMyEndpoint))]
public class MyEndpoint : IMyEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}
```

---

### MINEP004: Ambiguous Route Detected

**Warning:**
```
Endpoint 'MyEndpoint' has route pattern '/api/users' that conflicts with endpoint 'OtherEndpoint'.
```

**Cause:** Two endpoints with same HTTP method and route pattern.

**Bad:**
```csharp
[MapGet("/api/users")]
public class GetUsersEndpoint { }

[MapGet("/api/users")]  // ❌ Duplicate route
public class ListUsersEndpoint { }
```

**Solution:**

1. **Use Different Routes:**
   ```csharp
   [MapGet("/api/users")]
   public class GetUsersEndpoint { }

   [MapGet("/api/users/list")]  // ✅ Different route
   public class ListUsersEndpoint { }
   ```

2. **Consolidate Endpoints:**
   ```csharp
   [MapGet("/api/users")]
   public class GetUsersEndpoint
   {
       public IResult Handle(string? filter = null)
       {
           // Handle both cases in one endpoint
       }
   }
   ```

3. **Use Route Constraints:**
   ```csharp
   [MapGet("/api/items/{id:int}")]
   public class GetItemByIdEndpoint { }

   [MapGet("/api/items/{slug:alpha}")]
   public class GetItemBySlugEndpoint { }
   ```

---

### MINEP005: Invalid Group Type

**Error:**
```
The Group type 'MyGroup' specified for endpoint 'MyEndpoint' is not decorated with MapGroupAttribute.
```

**Cause:** Group class is missing `[MapGroup]` attribute.

**Bad:**
```csharp
public class MyGroup  // ❌ Missing [MapGroup]
{
}

[MapGet("/test", Group = typeof(MyGroup))]
public class MyEndpoint { }
```

**Solution:**

```csharp
[MapGroup("/api")]  // ✅ Add attribute
public class MyGroup  // IConfigurableGroup is optional
{
}

// Or with configuration
[MapGroup("/api")]
public class MyGroup : IConfigurableGroup  // ✅ Optional interface
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.WithOpenApi();
    }
}

[MapGet("/test", Group = typeof(MyGroup))]
public class MyEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

---

### MINEP006: Cyclic Group Hierarchy Detected

**Error:**
```
Group 'GroupA' has a cyclic hierarchy: GroupA -> GroupB -> GroupA.
```

**Cause:** Groups form a circular reference through `ParentGroup` properties.

**Bad:**
```csharp
[MapGroup("/a", ParentGroup = typeof(GroupB))]
public class GroupA : IConfigurableGroup { }

[MapGroup("/b", ParentGroup = typeof(GroupA))]  // ❌ Circular reference
public class GroupB : IConfigurableGroup { }
```

**Solution:**

Remove circular reference:
```csharp
[MapGroup("/a")]  // ✅ No parent
public class GroupA : IConfigurableGroup { }

[MapGroup("/b", ParentGroup = typeof(GroupA))]  // ✅ Valid hierarchy
public class GroupB : IConfigurableGroup { }
```

---

## Compilation Errors

### 'IResult' Not Found

**Error:**
```
The type or namespace name 'IResult' could not be found
```

**Solution:**

Add using statement:
```csharp
using Microsoft.AspNetCore.Http; // Add this

[MapGet("/test")]
public class MyEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

Or use fully qualified name:
```csharp
public Microsoft.AspNetCore.Http.IResult Handle() => ...
```

---

### 'FromBody' Not Found

**Error:**
```
The type or namespace name 'FromBodyAttribute' could not be found
```

**Solution:**

Add using statement:
```csharp
using Microsoft.AspNetCore.Mvc; // Add this

[MapPost("/users")]
public class CreateUserEndpoint
{
    public IResult Handle([FromBody] CreateUserRequest request) => ...
}
```

---

### Namespace 'MinimalEndpoints.Generated' Not Found

**Error:**
```
The type or namespace name 'Generated' does not exist in the namespace 'MinimalEndpoints'
```

**Cause:** Generator hasn't run or endpoint classes don't exist.

**Solution:**

1. **Ensure you have at least one endpoint:**
   ```csharp
   [MapGet("/test")]
   public class TestEndpoint
   {
       public IResult Handle() => Results.Ok();
   }
   ```

2. **Rebuild:**
   ```bash
   dotnet clean
   dotnet build
   ```

3. **Check the endpoint is referenceable from generated code:**
   ```csharp
   public class TestEndpoint  // public or internal are both fine; private/protected nesting is not
   ```

---

## Runtime Issues

### Endpoints Return 404 Not Found

**Symptoms:**
- Build succeeds
- App starts
- All requests return 404

**Solutions:**

1. **Check UseMinimalEndpoints Call**
   ```csharp
   var app = builder.Build();

   // BEFORE middleware that might block requests
   app.UseMinimalEndpoints();  // Must be called!

   app.Run();
   ```

2. **Check Middleware Order**

   `UseMinimalEndpoints()` only maps the endpoint routes — it is not order-sensitive terminal
   middleware. The usual ASP.NET Core ordering rules apply: register authentication and
   authorization **before** the endpoints they protect, exactly as the samples and the
   [ASP.NET Core integration guide](examples/11-aspnetcore-integration.md) do:
   ```csharp
   var app = builder.Build();

   app.UseHttpsRedirection();
   app.UseAuthentication();
   app.UseAuthorization();

   app.UseMinimalEndpoints();  // Maps all endpoints

   app.Run();
   ```

   The most common cause of a blanket 404 is simply forgetting to call `app.UseMinimalEndpoints()`
   at all (see step 1), not the relative order of the auth middleware.

3. **Verify Route Patterns**
   ```csharp
   [MapGet("/api/test")]  // Include leading slash
   public class TestEndpoint { }
   ```

---

### Dependency Injection Fails

**Symptoms:**
```
Unable to resolve service for type 'IMyService'
```

**Solution:**

Register services before `AddMinimalEndpoints()`:
```csharp
builder.Services.AddScoped<IMyService, MyService>();  // Register first
builder.Services.AddMinimalEndpoints();  // Then add endpoints
```

---

### Group Configuration Not Applied

**Symptoms:**
- Endpoint exists but group configuration (auth, rate limiting, etc.) not working

**Solution:**

1. **Ensure Group Has [MapGroup] and IConfigurableGroup (if needed):**
   ```csharp
   [MapGroup("/api")]
   public class ApiGroup : IConfigurableGroup  // For configuration
   {
       public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
       {
           group.RequireAuthorization();  // Configuration here
       }
   }
   ```

2. **Reference Group Correctly:**
   ```csharp
   [MapGet("/users", Group = typeof(ApiGroup))]  // typeof(...)
   public class GetUsersEndpoint { }
   ```

---

## IDE Issues

### IntelliSense Not Working

**Visual Studio:**
1. Tools → Options → Text Editor → C# → IntelliSense
2. Check "Show completion list after a character is typed"
3. Restart Visual Studio

**VS Code:**
1. Check C# extension is installed and up to date
2. Reload window: Ctrl+Shift+P → "Reload Window"
3. Check OmniSharp logs: Ctrl+Shift+P → "OmniSharp: Show Output"

**Rider:**
1. File → Invalidate Caches → Invalidate and Restart
2. Check Roslyn analyzer is enabled: Settings → Tools → Roslyn Analyzers

---

### Code Fixes Not Appearing

**Solution:**

1. **Ensure Cursor is on Error:**
   - Place cursor on the class name with the error
   - Press Ctrl+. (VS) or Alt+Enter (Rider)

2. **Check Analyzer is Enabled:**

   Visual Studio:
   - Solution Explorer → Dependencies → Analyzers
   - Should see `MinimalEndpoints.CodeGeneration`

   Rider:
   - Settings → Tools → Roslyn Analyzers
   - Verify enabled

---

### Slow IDE Performance

**Symptoms:**
- Typing lag
- Slow IntelliSense
- IDE freezes

**Solutions:**

1. **Disable Unnecessary Analyzers:**
   ```xml
   <!-- In .editorconfig -->
   [*.cs]
   # Disable specific analyzers if needed
   dotnet_diagnostic.CA1848.severity = none
   ```

2. **Generated Files Are Already Excluded from Analysis:**

   You do not need to configure anything here — both analyzers (`EndpointsAnalyzer` and
   `GroupsAnalyzer`) report `GeneratedCodeAnalysisFlags.None`, so they never run over the
   generated `*.g.cs` output. If you want to silence other analyzers on generated code, mark
   files as generated via `.editorconfig` (`generated_code = true`).

3. **Increase IDE Memory:**

   Visual Studio: Help → Performance Tuning

   Rider: Help → Edit Custom VM Options
   ```
   -Xmx4096m
   ```

---

## Performance Issues

### Slow Build Times

**Symptoms:**
- Build takes longer after adding MinimalEndpoints

**Solutions:**

1. **Check Endpoint Count:**
   ```bash
   # Count endpoints
   Get-ChildItem -Recurse -Filter "*Endpoint.cs" | Measure-Object
   ```

   If > 1000 endpoints, consider splitting into multiple projects.

2. **Use Incremental Build:**
   ```bash
   dotnet build --no-restore  # Skip restore
   dotnet build --no-dependencies  # Skip dependencies
   ```

   The generator is implemented as an incremental source generator, so unchanged endpoints are
   not re-generated between builds — keep restores and dependency builds out of the hot loop and
   the per-build generator cost stays minimal.

---

### Slow Application Startup

**Note:** MinimalEndpoints has zero runtime overhead, so it shouldn't affect startup.

If experiencing slow startup:

1. **Profile Startup:**
   ```csharp
   var stopwatch = Stopwatch.StartNew();
   builder.Services.AddMinimalEndpoints();
   Console.WriteLine($"AddMinimalEndpoints: {stopwatch.ElapsedMilliseconds}ms");
   ```

2. **Check Service Registrations:**

   If slow, check what services your endpoints inject:
   ```csharp
   // Bad: Singleton endpoint with Scoped dependencies
   [MapGet("/test", ServiceLifetime.Singleton)]  // ❌
   public class TestEndpoint
   {
       public TestEndpoint(DbContext db) { }  // Scoped
   }

   // Good: Scoped endpoint
   [MapGet("/test")]  // Default: Scoped ✅
   public class TestEndpoint
   {
       public TestEndpoint(DbContext db) { }
   }
   ```

---

## Debugging Tips

### Enable Verbose Logging

```csharp
builder.Logging.SetMinimumLevel(LogLevel.Trace);
builder.Logging.AddConsole();
```

### View Generated Source

```bash
# Set <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles> on the consuming project,
# then after a build find the generated files (<tfm> = your target framework, e.g. net8.0):
ls obj/Debug/<tfm>/generated/MinimalEndpoints.CodeGeneration/MinimalEndpoints.CodeGeneration.MinimalEndpointsGenerator/*.cs
```

### Test Endpoints Individually

```csharp
[Fact]
public async Task TestEndpoint()
{
    var endpoint = new MyEndpoint(mockRepo.Object);
    var result = await endpoint.HandleAsync();

    var okResult = Assert.IsType<Ok<User>>(result);
    Assert.NotNull(okResult.Value);
}
```

### Check Generated Registration

```csharp
// In a test or Program.cs
var services = builder.Services;
foreach (var service in services)
{
    if (service.ServiceType.Name.Contains("Endpoint"))
        Console.WriteLine($"{service.ServiceType.Name}: {service.Lifetime}");
}
```

---

## Common Patterns

### Suppressing Warnings

```csharp
#pragma warning disable MINEP004 // Ambiguous routes - intentional
[MapGet("/api/users")]
public class GetUsersEndpoint { }

[MapGet("/api/users")]  // Same route, different behavior
public class GetUsersV2Endpoint { }
#pragma warning restore MINEP004
```

Or in .editorconfig:
```ini
[*.cs]
dotnet_diagnostic.MINEP004.severity = suggestion  # Warning → Suggestion
```

---

## Still Having Issues?

### Before Asking for Help

1. ✅ Clean and rebuild
2. ✅ Restart IDE
3. ✅ Check this troubleshooting guide
4. ✅ Review [documentation](README.md)
5. ✅ Search [existing issues](https://github.com/smavrommatis/MinimalEndpoints/issues)

### How to Report Issues

When reporting issues, include:

```
**Environment:**
- OS: Windows 11 / macOS / Linux
- .NET SDK: [output of `dotnet --version`]
- IDE: Visual Studio 2022 17.8 / Rider 2024.1 / VS Code
- Package Version: [from dotnet list package]

**Reproduction:**
- Minimal code sample
- Steps to reproduce
- Expected vs actual behavior

**Logs:**
- Build output (dotnet build -v:detailed)
- Error messages (full text)
- Screenshots if applicable
```

### Get Help

- 💬 [GitHub Discussions](https://github.com/smavrommatis/MinimalEndpoints/discussions) - Ask questions
- 🐛 [GitHub Issues](https://github.com/smavrommatis/MinimalEndpoints/issues) - Report bugs
- 📧 [Email](mailto:sotirios.mavrommatis+minimalendpoints@gmail.com) - Direct support

---

**Last Updated:** 2026-06-20

