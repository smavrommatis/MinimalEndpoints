# MINEP004: Ambiguous Route Pattern Detected

## Diagnostic ID

`MINEP004`

## Severity

Warning

## Version History

- **v1.0.0 (2025-12-20)**: Initial implementation. Route parameters are normalized to `{param}` regardless of name or constraints. This means `/users/{id:int}` and `/users/{userId:int}` are both treated as `/users/{param}` and flagged as ambiguous.

> **Note**: If this behavior causes issues in your scenario, please [report it](https://github.com/yourusername/MinimalEndpoints/issues/new?labels=analyzer,MINEP004) so we can improve the analyzer. See the [When to Suppress](#when-to-suppress) section for workarounds.

## Description

Multiple endpoints have identical route patterns for the same HTTP method, which will cause routing ambiguity and unpredictable behavior at runtime.

## Message

> Endpoint '{0}' has route pattern '{1}' that conflicts with endpoint '{2}'. Multiple endpoints with the same HTTP method and route pattern will cause routing ambiguity.

## Cause

This warning occurs when:
1. Two or more endpoint classes have the same route pattern
2. They handle the same HTTP method
3. The patterns would match the same incoming requests

## How to Fix

### Option 1: Use Different Route Patterns

Make the route patterns distinct:

**❌ Ambiguous:**
```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}

[MapGet("/users")]  // ❌ Same pattern
public class GetAllUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
```

**✅ Resolved:**
```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}

[MapGet("/users/all")]  // ✅ Different pattern
public class GetAllUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
```

### Option 2: Use Route Constraints

Differentiate patterns using route constraints:

**✅ Resolved with Constraints:**
```csharp
[MapGet("/items/{id:int}")]  // Matches: /items/123
public class GetItemByIdEndpoint
{
    public Task<IResult> HandleAsync(int id) => ...;
}

[MapGet("/items/{slug:alpha}")]  // Matches: /items/some-item
public class GetItemBySlugEndpoint
{
    public Task<IResult> HandleAsync(string slug) => ...;
}
```

### Option 3: Consolidate Endpoints

Combine similar endpoints into one:

**✅ Resolved by Consolidation:**
```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? filter = null)
    {
        // Handle all user retrieval logic here
        if (includeInactive)
        {
            // Include inactive users
        }

        if (filter != null)
        {
            // Apply filter
        }

        return ...;
    }
}
```

### Option 4: Use Query Parameters

Differentiate using query parameters instead of route patterns:

**✅ Resolved with Query Parameters:**
```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync(
        [FromQuery] string? view = null)  // ?view=admin or ?view=public
    {
        return view switch
        {
            "admin" => GetAdminUsers(),
            "public" => GetPublicUsers(),
            _ => GetDefaultUsers()
        };
    }
}
```

## Understanding Route Matching

ASP.NET Core routes endpoints based on:
1. **HTTP Method** (GET, POST, etc.)
2. **Route Pattern** (/users, /users/{id}, etc.)

**Important**: Parameter names and constraints are ignored when detecting ambiguity because:
- `/users/{id:int}` and `/users/{userId:int}` both match `/users/123`
- `/users/{id}` and `/users/{name}` both match `/users/john`
- Different parameter names don't make routes distinguishable at runtime

### Examples of Conflicts

**❌ Conflict: Same Parameter Position**
```csharp
[MapGet("/items/{id:int}")]      // Matches: /items/123
[MapGet("/items/{userId:int}")]  // ❌ Also matches: /items/123 - AMBIGUOUS!
```

**❌ Conflict: Different Parameter Names**
```csharp
[MapGet("/users/{id}")]
[MapGet("/users/{userId}")]  // ❌ Same pattern structure - AMBIGUOUS!
```

**❌ Conflict: Different Constraints**
```csharp
[MapGet("/products/{code:alpha}")]   // Matches: /products/ABC
[MapGet("/products/{id:int}")]       // Matches: /products/123
// ❌ AMBIGUOUS - both define /products/{parameter}
```

**✅ No Conflict: Different HTTP Methods**
```csharp
[MapGet("/users/{id}")]   // GET /users/123
[MapPost("/users/{id}")]  // POST /users/123  ✅ Different methods
```

**✅ No Conflict: Different Patterns**
```csharp
[MapGet("/users")]           // GET /users
[MapGet("/users/{id}")]      // GET /users/123  ✅ Different patterns
[MapGet("/users/{id}/posts")] // GET /users/123/posts  ✅ Different patterns
```

## Real-World Scenarios

### Scenario 1: CRUD Operations

**✅ Correct - No Conflicts:**
```csharp
[MapGet("/users")]          // List all users
public class ListUsersEndpoint { }

[MapGet("/users/{id}")]     // Get specific user
public class GetUserEndpoint { }

[MapPost("/users")]         // Create user
public class CreateUserEndpoint { }

[MapPut("/users/{id}")]     // Update user
public class UpdateUserEndpoint { }

[MapDelete("/users/{id}")]  // Delete user
public class DeleteUserEndpoint { }
```

### Scenario 2: Versioned APIs

**✅ Correct - Use Version in Path:**
```csharp
[MapGet("/v1/users")]
public class GetUsersV1Endpoint { }

[MapGet("/v2/users")]  // ✅ Different path
public class GetUsersV2Endpoint { }
```

### Scenario 3: Filtered Views

**Option A - Query Parameters:**
```csharp
[MapGet("/users")]  // ?status=active or ?status=inactive
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync([FromQuery] string? status) { }
}
```

**Option B - Separate Routes:**
```csharp
[MapGet("/users/active")]
public class GetActiveUsersEndpoint { }

[MapGet("/users/inactive")]
public class GetInactiveUsersEndpoint { }
```

### Scenario 4: Admin vs Public

**✅ Correct - Different Paths:**
```csharp
[MapGet("/public/users")]
public class GetPublicUsersEndpoint { }

[MapGet("/admin/users")]
public class GetAdminUsersEndpoint { }
```

## Best Practices

### ✅ DO: Use RESTful Conventions

```csharp
GET    /users           // List
GET    /users/{id}      // Get
POST   /users           // Create
PUT    /users/{id}      // Update
PATCH  /users/{id}      // Partial Update
DELETE /users/{id}      // Delete
```

### ✅ DO: Use Route Constraints

```csharp
[MapGet("/orders/{id:int}")]      // Numeric IDs
[MapGet("/orders/{ref:guid}")]    // GUID references
[MapGet("/orders/{code:alpha}")]  // Alphanumeric codes
```

### ✅ DO: Use Query Parameters for Filters

```csharp
[MapGet("/products")]  // ?category=electronics&minPrice=100
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync(
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice) { }
}
```

### ❌ DON'T: Duplicate Patterns

```csharp
// ❌ Don't do this
[MapGet("/users")]
public class GetUsersEndpoint { }

[MapGet("/users")]  // Ambiguous!
public class ListUsersEndpoint { }
```

### ❌ DON'T: Use Similar Patterns Without Constraints

```csharp
// ❌ Ambiguous
[MapGet("/items/{id}")]
[MapGet("/items/{code}")]
```

## Route Constraint Types

Common route constraints you can use:

| Constraint | Example | Matches |
|------------|---------|---------|
| `:int` | `/users/{id:int}` | `/users/123` |
| `:guid` | `/orders/{id:guid}` | `/orders/abc-def-...` |
| `:alpha` | `/items/{name:alpha}` | `/items/widget` |
| `:length(n)` | `/codes/{code:length(6)}` | `/codes/ABC123` |
| `:min(n)` | `/pages/{page:min(1)}` | `/pages/5` |
| `:regex()` | `/files/{name:regex(^[a-z]+$)}` | `/files/document` |

## When to Suppress

This warning should rarely be suppressed, but you might consider it when:

### 1. Routes Are in Different Assemblies
If routes are defined in different projects/assemblies and never loaded together:
```csharp
#pragma warning disable MINEP004
[MapGet("/users/{id}")]
public class GetUserEndpoint { }
#pragma warning restore MINEP004
```

### 2. Routes Are Conditionally Registered
If you use feature flags or environment-based registration:
```csharp
// In Startup/Program.cs
if (featureFlags.UseV2Endpoints)
{
    builder.Services.AddEndpoint<GetUsersV2Endpoint>();
}
else
{
    builder.Services.AddEndpoint<GetUsersV1Endpoint>();
}

// Both endpoints can have same route since only one registers
#pragma warning disable MINEP004
[MapGet("/users")]
public class GetUsersV1Endpoint { }

[MapGet("/users")]
public class GetUsersV2Endpoint { }
#pragma warning restore MINEP004
```

### 3. Using Route Constraints for Disambiguation
If you're intentionally using constraints to differentiate routes:
```csharp
// WARNING: This is fragile and depends on registration order!
// Not recommended, but if you must:
#pragma warning disable MINEP004
[MapGet("/items/{id:int}")]      // Handles integers
public class GetItemByIdEndpoint { }

[MapGet("/items/{code:alpha}")]  // Handles alpha codes
public class GetItemByCodeEndpoint { }
#pragma warning restore MINEP004
```

### How to Suppress

**Option 1: Suppress for Specific Endpoint**
```csharp
#pragma warning disable MINEP004
[MapGet("/users/{id}")]
public class MyEndpoint { }
#pragma warning restore MINEP004
```

**Option 2: Suppress in .editorconfig**
```ini
# Suppress MINEP004 for entire project
[*.cs]
dotnet_diagnostic.MINEP004.severity = none
```

**Option 3: Suppress in Project File**
```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);MINEP004</NoWarn>
</PropertyGroup>
```

---

## Known Limitations

### Route Constraints Are Ignored
The analyzer treats all route parameters as equivalent, regardless of constraints. This is by design because:

1. **ASP.NET Core evaluates all matching patterns** - Constraints don't prevent ambiguity
2. **Registration order matters** - The first matching route wins
3. **It's error-prone** - Relying on constraints for disambiguation is fragile

**Example:**
```csharp
[MapGet("/items/{id:int}")]      // Matches /items/123
[MapGet("/items/{code:alpha}")]  // Matches /items/abc

// ⚠️ MINEP004 warns about this because both define /items/{param}
// Even though they seem distinct, routing behavior depends on registration order
```

### Optional Parameters
Optional route parameters may cause false positives:
```csharp
[MapGet("/users/{id?}")]
[MapGet("/users")]
// ⚠️ May warn, but these handle different URL patterns
```

**Workaround**: Use query parameters instead of optional route parameters.

---

## False Positives? Report Them!

If you encounter a scenario where MINEP004 produces a **false positive** (warns about routes that are NOT actually ambiguous), please help us improve the analyzer!

### How to Report

**GitHub Issue**: [Create an issue](https://github.com/yourusername/MinimalEndpoints/issues/new?labels=analyzer,MINEP004&template=analyzer-issue.md)

**Include:**
1. The route patterns that triggered the warning
2. Why you believe they are NOT ambiguous
3. A minimal code example
4. ASP.NET Core version

**Example Report:**
```
Title: MINEP004 false positive with optional parameters

Routes:
- [MapGet("/users/{id?}")]
- [MapGet("/users")]

Expected: No warning (these handle different patterns)
Actual: MINEP004 warning

ASP.NET Core version: 10.0
```

### Our Commitment

We take analyzer accuracy seriously. If you report a legitimate false positive:
- ✅ We'll investigate promptly
- ✅ We'll update the analyzer logic if needed
- ✅ We'll add test coverage for your scenario
- ✅ We'll document the behavior

Your feedback helps make MinimalEndpoints better for everyone!

---

- [MINEP001: Endpoint Missing Entry Point Method](MINEP001.md)
- [MINEP002: Multiple MapMethods Attributes](MINEP002.md)
- [MINEP003: ServiceType Interface Missing Entry Point](MINEP003.md)
- [ASP.NET Core Routing Documentation](https://learn.microsoft.com/aspnet/core/fundamentals/routing)
- [Route Constraints](https://learn.microsoft.com/aspnet/core/fundamentals/routing#route-constraints)

