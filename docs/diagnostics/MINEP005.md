# MINEP005: Invalid Group Type

## Diagnostic ID

`MINEP005`

## Severity

Error

## Description

A type referenced as a group — either an endpoint's `Group` or a group's `ParentGroup` — is not decorated with the `[MapGroup]` attribute.

## Message

> The type '{0}' referenced as a group by '{1}' is not decorated with MapGroupAttribute. Ensure the group type has the [MapGroup] attribute.

## Cause

This error occurs when an endpoint's `Group` property — or a group's `ParentGroup` property — references a class that lacks the `[MapGroup]` attribute.

## How to Fix

### Option 1: Add [MapGroup] Attribute

**❌ Incorrect:**
```csharp
// Missing [MapGroup]
public class ApiV1Group
{
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint { }
```

**✅ Correct:**
```csharp
[MapGroup("/api/v1")]  // ✅ Add attribute
public class ApiV1Group
{
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint { }
```

### Option 2: Add Configuration (Optional)

For advanced configuration, implement `IConfigurableGroup`:

```csharp
[MapGroup("/api/v1")]
public class ApiV1Group : IConfigurableGroup  // Optional interface
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .WithOpenApi();
    }
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

### Option 3: Remove Group Property

If you don't need grouping:

```csharp
[MapGet("/api/v1/products")]  // ✅ Include prefix in route
public class GetProductsEndpoint { }
```

### Also applies to ParentGroup

A group's `ParentGroup` must likewise reference a `[MapGroup]` type. Otherwise the parent link is silently
dropped and the child group loses the parent's prefix.

**❌ Incorrect:**
```csharp
public class ApiGroup { } // missing [MapGroup]

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group { }
```

**✅ Correct:**
```csharp
[MapGroup("/api")]
public class ApiGroup { }

[MapGroup("/v1", ParentGroup = typeof(ApiGroup))]
public class V1Group { }
// Results in prefix: /api/v1
```

## Requirements for Endpoint Groups

A valid endpoint group must:
1. ✅ Be decorated with `[MapGroup("prefix")]` attribute
2. ⚪ Optionally implement `IConfigurableGroup` interface for configuration

## Examples

### ❌ Incorrect - Missing Attribute

```csharp
// ❌ No [MapGroup] attribute
public class ApiV1Group
{
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint { }
```

### ✅ Correct - Simple Group

```csharp
[MapGroup("/api/v1")]
public class ApiV1Group
{
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public IResult Handle() => Results.Ok();
}
// Results in: /api/v1/products
```

### ✅ Correct - Group with Configuration

```csharp
[MapGroup("/api/v1")]
public class ApiV1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .WithOpenApi()
             .RequireRateLimiting("fixed");
    }
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public IResult Handle() => Results.Ok();
}
// Results in: /api/v1/products with authorization and rate limiting
```

## What Groups Enable

Using groups provides several benefits:

### 1. Route Prefix
```csharp
[MapGroup("/api/v1")]
public class ApiV1Group
{
}

[MapGet("/products", Group = typeof(ApiV1Group))]  // → /api/v1/products
[MapGet("/orders", Group = typeof(ApiV1Group))]    // → /api/v1/orders
```

### 2. Shared Authorization
```csharp
[MapGroup("/admin")]
public class AdminGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireAuthorization("AdminPolicy");
    }
}

// All endpoints in AdminGroup require admin authorization
[MapGet("/users", Group = typeof(AdminGroup))]
[MapGet("/settings", Group = typeof(AdminGroup))]
```

### 3. Rate Limiting
```csharp
[MapGroup("/api")]
public class ApiGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireRateLimiting("fixed");
    }
}
```

### 4. OpenAPI Grouping
```csharp
[MapGroup("/api/v1")]
public class ApiV1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.WithOpenApi()
             .WithTags("V1");
    }
}
```

### 5. CORS Policies
```csharp
[MapGroup("/public")]
public class PublicGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireCors("AllowAll");
    }
}
```

## Multiple Groups Example

```csharp
// API V1 - Requires authorization
[MapGroup("/api/v1")]
public class ApiV1Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireAuthorization().WithOpenApi();
    }
}

// API V2 - Different configuration
[MapGroup("/api/v2")]
public class ApiV2Group : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .RequireRateLimiting("sliding")
             .WithOpenApi();
    }
}

// Public - No auth
[MapGroup("/public")]
public class PublicGroup : IConfigurableGroup
{
    public static void ConfigureGroup(IApplicationBuilder app, RouteGroupBuilder group)
    {
        group.AllowAnonymous();
    }
}

// Endpoints
[MapGet("/products", Group = typeof(ApiV1Group))]   // /api/v1/products
[MapGet("/products", Group = typeof(ApiV2Group))]   // /api/v2/products (no MINEP004!)
[MapGet("/health", Group = typeof(PublicGroup))]    // /public/health
```

## Relationship with MINEP004

Groups help avoid MINEP004 (ambiguous routes):

```csharp
// ❌ Without groups - MINEP004 warning
[MapGet("/products")]
public class GetProductsV1 { }

[MapGet("/products")]  // ⚠️ MINEP004: Ambiguous!
public class GetProductsV2 { }

// ✅ With groups - No warning
[MapGet("/products", Group = typeof(ApiV1Group))]  // /api/v1/products
public class GetProductsV1 { }

[MapGet("/products", Group = typeof(ApiV2Group))]  // /api/v2/products
public class GetProductsV2 { }  // ✅ Different routes!
```

## See Also

- [MINEP001: Missing Entry Point Method](MINEP001.md)
- [MINEP002: Multiple Attributes](MINEP002.md)
- [MINEP003: ServiceType Validation](MINEP003.md)
- [MINEP004: Ambiguous Routes](MINEP004.md)
- [Documentation: Endpoint Groups](../../README.md#endpoint-groups)

