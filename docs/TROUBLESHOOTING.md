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
   > Blackeye.MinimalEndpoints    1.0.0
   ```

4. **Verify Project Configuration**

   Ensure your project file has:
   ```xml
   <Project Sdk="Microsoft.NET.Sdk.Web">
     <PropertyGroup>
       <TargetFramework>net8.0</TargetFramework> <!-- or net9.0, net10.0 -->
       <Nullable>enable</Nullable>
     </PropertyGroup>
   </Project>
   ```

5. **Check for Multiple Projects**

   Make sure you're referencing the correct project:
   ```csharp
   // In the project that has endpoints
   using MinimalEndpoints.Generated; // This should work
   ```

### View Generated Code

**Visual Studio:**
1. Solution Explorer → Dependencies → Analyzers
2. Expand `MinimalEndpoints.CodeGeneration`
3. View `MinimalEndpointExtensions.g.cs`

**Rider:**
1. Right-click project → Show Generated Files
2. Find `MinimalEndpointExtensions.g.cs`

**VS Code:**
```bash
# Generated files are in:
obj/Debug/net8.0/generated/MinimalEndpoints.CodeGeneration/MinimalEndpoints.CodeGeneration.EndpointGenerator/
```

### Generator Not Running

**Check MSBuild output:**

```bash
dotnet build -v:detailed | Select-String "MinimalEndpoints"
```

Look for lines like:
```
Executing generator 'MinimalEndpoints.CodeGeneration.EndpointGenerator'...
```

If not present, the generator isn't loading. Try:

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
The Group type 'MyGroup' specified for endpoint 'MyEndpoint' does not implement IEndpointGroup or is not decorated with MapGroupAttribute.
```

**Cause:** Group class doesn't implement `IEndpointGroup` or missing `[MapGroup]` attribute.

**Bad:**
```csharp
public class MyGroup  // ❌ Missing IEndpointGroup and [MapGroup]
{
}

[MapGet("/test", Group = typeof(MyGroup))]
public class MyEndpoint { }
```

**Solution:**

```csharp
[MapGroup("/api")]  // ✅ Add attribute
public class MyGroup : IEndpointGroup  // ✅ Implement interface
{
    public void ConfigureGroup(RouteGroupBuilder group)
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
public class GroupA : IEndpointGroup { }

[MapGroup("/b", ParentGroup = typeof(GroupA))]  // ❌ Circular reference
public class GroupB : IEndpointGroup { }
```

**Solution:**

Remove circular reference:
```csharp
[MapGroup("/a")]  // ✅ No parent
public class GroupA : IEndpointGroup { }

[MapGroup("/b", ParentGroup = typeof(GroupA))]  // ✅ Valid hierarchy
public class GroupB : IEndpointGroup { }
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

3. **Check if endpoints are public:**
   ```csharp
   public class TestEndpoint  // Must be public, not internal
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

   Bad:
   ```csharp
   app.UseRouting();
   app.UseAuthorization();
   app.UseMinimalEndpoints();  // Too late!
   app.UseEndpoints(endpoints => { });
   ```

   Good:
   ```csharp
   app.UseRouting();
   app.UseMinimalEndpoints();  // Before UseEndpoints
   app.UseAuthorization();
   app.Run();
   ```

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

1. **Ensure Group Implements IEndpointGroup:**
   ```csharp
   [MapGroup("/api")]
   public class ApiGroup : IEndpointGroup  // Must implement
   {
       public void ConfigureGroup(RouteGroupBuilder group)
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

2. **Exclude Generated Files from Analysis:**
   ```xml
   <PropertyGroup>
     <GeneratedCodeAnalysis>None</GeneratedCodeAnalysis>
   </PropertyGroup>
   ```

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

3. **Exclude from Hot Reload:**

   If endpoints rarely change:
   ```xml
   <PropertyGroup>
     <GenerateRoslynSourceGeneratorAttribute>false</GenerateRoslynSourceGeneratorAttribute>
   </PropertyGroup>
   ```

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
# After build, find generated files:
ls obj/Debug/net8.0/generated/MinimalEndpoints.CodeGeneration/**/*.cs
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

**Last Updated:** December 21, 2025

