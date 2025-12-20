# MINEP005: Invalid Endpoint Group Type

## Diagnostic ID

`MINEP005`

## Severity

Error

## Description

The `Group` property on a mapping attribute references a type that either doesn't implement `IEndpointGroup` or isn't decorated with `[MapGroup]` attribute.

## Message

> The Group type '{0}' specified for endpoint '{1}' does not implement IEndpointGroup or is not decorated with MapGroupAttribute. Ensure the group class implements IEndpointGroup and has the [MapGroup] attribute.

## Cause

This error occurs when:
1. The `Group` property references a class without `[MapGroup]` attribute
2. The group class doesn't implement `IEndpointGroup` interface
3. Both the attribute and interface are missing

## How to Fix

### Option 1: Add Both Attribute and Interface

**❌ Incorrect:**
```csharp
// Missing [MapGroup] and IEndpointGroup
public class ApiV1Group
{
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint { }
```

**✅ Correct:**
```csharp
[MapGroup("/api/v1")]  // ✅ Add attribute
public class ApiV1Group : IEndpointGroup  // ✅ Implement interface
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
    }
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint { }
```

### Option 2: Remove Group Property

If you don't need grouping:

```csharp
[MapGet("/api/v1/products")]  // ✅ Include prefix in route
public class GetProductsEndpoint { }
```

## Requirements for Endpoint Groups

A valid endpoint group must:
1. ✅ Be decorated with `[MapGroup("prefix")]` attribute
2. ✅ Implement `IEndpointGroup` interface
3. ✅ Implement `ConfigureGroup(RouteGroupBuilder)` method

## Examples

### ❌ Incorrect - Missing Attribute

```csharp
// ❌ No [MapGroup] attribute
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint { }
```

### ❌ Incorrect - Missing Interface

```csharp
[MapGroup("/api/v1")]
public class ApiV1Group  // ❌ Doesn't implement IEndpointGroup
{
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint { }
```

### ✅ Correct - Complete Group

```csharp
[MapGroup("/api/v1", GroupName = "V1 API")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .WithOpenApi()
             .WithRateLimiter("fixed");
    }
}

[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}
// Results in: /api/v1/products with authorization
```

## What Groups Enable

Using groups provides several benefits:

### 1. Route Prefix
```csharp
[MapGroup("/api/v1")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group) { }
}

[MapGet("/products", Group = typeof(ApiV1Group))]  // → /api/v1/products
[MapGet("/orders", Group = typeof(ApiV1Group))]    // → /api/v1/orders
```

### 2. Shared Authorization
```csharp
[MapGroup("/admin")]
public class AdminGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
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
public class ApiGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithRateLimiter("fixed");
    }
}
```

### 4. OpenAPI Grouping
```csharp
[MapGroup("/api/v1", GroupName = "V1 API")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi()
             .WithTags("V1");
    }
}
```

### 5. CORS Policies
```csharp
[MapGroup("/public")]
public class PublicGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireCors("AllowAll");
    }
}
```

## Multiple Groups Example

```csharp
// API V1 - Requires authorization
[MapGroup("/api/v1", GroupName = "V1 API")]
public class ApiV1Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization().WithOpenApi();
    }
}

// API V2 - Different configuration
[MapGroup("/api/v2", GroupName = "V2 API")]
public class ApiV2Group : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization()
             .WithRateLimiter("sliding")
             .WithOpenApi();
    }
}

// Public - No auth
[MapGroup("/public")]
public class PublicGroup : IEndpointGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
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
- [Documentation: Endpoint Groups](../README.md#endpoint-groups)

