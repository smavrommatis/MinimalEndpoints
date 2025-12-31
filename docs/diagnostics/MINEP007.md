# MINEP007: Class Cannot Be Both Endpoint and Group

## Diagnostic ID

`MINEP007`

## Severity

Error

## Description

A class cannot be decorated with both a MapGroup attribute and a Map* endpoint attribute (MapGet, MapPost, etc.). A class must be either an endpoint or a group, not both.

## Message

> The type '{0}' is marked as both an Endpoint and a Group. A class cannot be decorated with both MapGroupAttribute and a Map* endpoint attribute.

## Cause

This error occurs when a class has both:
1. A `[MapGroup]` attribute
2. Any endpoint mapping attribute (`[MapGet]`, `[MapPost]`, `[MapPut]`, `[MapDelete]`, `[MapPatch]`, `[MapHead]`, or `[MapMethods]`)

## How to Fix

### Option 1: Make It an Endpoint

Remove the `[MapGroup]` attribute if this should be an endpoint:

```csharp
// ❌ Error: Both endpoint and group
[MapGet("/users")]
[MapGroup("/api")]
public class UsersClass { }

// ✅ Fixed: Endpoint only
[MapGet("/users")]
public class GetUsersEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

### Option 2: Make It a Group

Remove the endpoint mapping attribute if this should be a group:

```csharp
// ❌ Error: Both endpoint and group
[MapGet("/users")]
[MapGroup("/api")]
public class UsersClass { }

// ✅ Fixed: Group only
[MapGroup("/api")]
public class ApiGroup : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi();
    }
}
```

### Option 3: Split Into Two Classes

Create separate classes for the endpoint and group:

```csharp
// ❌ Error: Trying to do both
[MapGet("/users")]
[MapGroup("/api")]
public class UsersClass { }

// ✅ Fixed: Separate classes
[MapGroup("/api")]
public class ApiGroup : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.WithOpenApi();
    }
}

[MapGet("/users", Group = typeof(ApiGroup))]
public class GetUsersEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

## Examples

### ❌ Incorrect Usage

```csharp
// Error: Cannot be both endpoint and group
[MapGet("/data")]
[MapGroup("/api")]
public class DataClass
{
    public IResult Handle() => Results.Ok();
}
```

```csharp
// Error: Multiple endpoint types with group
[MapPost("/users")]
[MapGroup("/api/v1")]
public class UserCreation { }
```

### ✅ Correct Usage

```csharp
// Endpoint only
[MapGet("/users")]
public class GetUsersEndpoint
{
    public IResult Handle() => Results.Ok(new[] { "User1", "User2" });
}
```

```csharp
// Group only
[MapGroup("/api/v1")]
public class ApiV1Group : IConfigurableGroup
{
    public void ConfigureGroup(RouteGroupBuilder group)
    {
        group.RequireAuthorization();
    }
}
```

```csharp
// Endpoint using a group
[MapGet("/products", Group = typeof(ApiV1Group))]
public class GetProductsEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

## Rationale

This restriction exists because:

1. **Clear Intent**: A class should have a single, clear purpose
2. **Type Safety**: Prevents confusion about how the class is registered
3. **Code Generation**: Simplifies the generator logic
4. **Best Practices**: Follows single responsibility principle

## Related Diagnostics

- **MINEP005**: Invalid Group Type - Validates group types have `[MapGroup]` attribute
- **MINEP002**: Multiple MapMethods Attributes - Prevents multiple endpoint attributes

## See Also

- [Endpoint Groups Documentation](../../README.md#endpoint-groups)
- [MapGroup Attribute Reference](../../README.md#supported-attributes)
- [IConfigurableGroup Interface](../../README.md#endpoint-groups)

