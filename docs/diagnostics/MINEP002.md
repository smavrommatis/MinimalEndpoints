# MINEP002: Multiple MapMethods Attributes Detected

## Diagnostic ID

`MINEP002`

## Severity

Error

## Description

An endpoint class has multiple MapMethods attributes (e.g., multiple `[MapGet]`, `[MapPost]`, etc.). Only one MapMethods attribute is allowed per endpoint class.

## Message

> Class '{0}' is marked with multiple MapMethods attributes. Only one MapMethods attribute is allowed per endpoint class. Remove duplicate attributes or use MapMethodsAttribute with an array of HTTP methods.

## Cause

This error occurs when an endpoint class has more than one mapping attribute, such as:
- Multiple `[MapGet]` attributes
- Both `[MapGet]` and `[MapPost]` on the same class
- Any combination of mapping attributes

## How to Fix

### Option 1: Remove Duplicate Attributes

Choose one HTTP method per endpoint class:

**❌ Incorrect:**
```csharp
[MapGet("/users")]
[MapPost("/users")]  // ❌ Multiple attributes
public class UsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
```

**✅ Correct:**
```csharp
[MapGet("/users")]  // ✅ Single attribute
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}

[MapPost("/users")]  // ✅ Separate class
public class CreateUserEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
```

### Option 2: Use MapMethodsAttribute for Multiple Methods

If you need to handle multiple HTTP methods, use `[MapMethods]`:

```csharp
[MapMethods("/users", new[] { "GET", "POST" })]  // ✅ Correct
public class UsersEndpoint
{
    public Task<IResult> HandleAsync(HttpContext context)
    {
        return context.Request.Method switch
        {
            "GET" => HandleGet(),
            "POST" => HandlePost(),
            _ => Task.FromResult(Results.BadRequest())
        };
    }

    private Task<IResult> HandleGet() => ...;
    private Task<IResult> HandlePost() => ...;
}
```

## Why This Error Exists

Each endpoint class should represent a single operation or closely related operations. Having multiple mapping attributes suggests:
1. The class is doing too much (violates Single Responsibility Principle)
2. The intent is unclear (which method should be used?)
3. The generated code would be ambiguous

## Best Practices

### Recommended: One Class Per Operation

```csharp
// ✅ Clear, focused endpoints
[MapGet("/users")]
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}

[MapPost("/users")]
public class CreateUserEndpoint
{
    public Task<IResult> HandleAsync([FromBody] CreateUserRequest request) => ...;
}

[MapPut("/users/{id}")]
public class UpdateUserEndpoint
{
    public Task<IResult> HandleAsync(int id, [FromBody] UpdateUserRequest request) => ...;
}

[MapDelete("/users/{id}")]
public class DeleteUserEndpoint
{
    public Task<IResult> HandleAsync(int id) => ...;
}
```

### Alternative: MapMethods for Related Operations

```csharp
// ✅ Use MapMethods when operations are closely related
[MapMethods("/health", new[] { "GET", "HEAD" })]
public class HealthCheckEndpoint
{
    public Task<IResult> HandleAsync(HttpContext context)
    {
        var isHealthy = CheckHealth();

        // HEAD returns same status but no body
        return Task.FromResult(
            isHealthy ? Results.Ok() : Results.ServiceUnavailable()
        );
    }
}
```

## Examples

### ❌ Incorrect Examples

**Duplicate same attribute:**
```csharp
[MapGet("/users")]
[MapGet("/api/users")]  // ❌ Don't do this
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
```

**Multiple different attributes:**
```csharp
[MapGet("/data")]
[MapPost("/data")]  // ❌ Don't do this
[MapPut("/data")]   // ❌ Don't do this
public class DataEndpoint
{
    public Task<IResult> HandleAsync() => ...;
}
```

### ✅ Correct Examples

**Separate classes:**
```csharp
[MapGet("/data")]
public class GetDataEndpoint
{
    public Task<IResult> HandleAsync() => Results.Ok(data);
}

[MapPost("/data")]
public class CreateDataEndpoint
{
    public Task<IResult> HandleAsync([FromBody] Data data) => Results.Created(...);
}
```

**MapMethods attribute:**
```csharp
[MapMethods("/data", new[] { "GET", "POST", "PUT" })]
public class DataEndpoint
{
    public Task<IResult> HandleAsync(HttpContext context)
    {
        return context.Request.Method switch
        {
            "GET" => GetData(),
            "POST" => CreateData(),
            "PUT" => UpdateData(),
            _ => Task.FromResult(Results.BadRequest())
        };
    }
}
```

## See Also

- [MINEP001: Endpoint Missing Entry Point Method](MINEP001.md)
- [MINEP003: ServiceType Interface Missing Entry Point](MINEP003.md)
- [Documentation: MapMethods Attribute](../README.md#supported-attributes)

