# MINEP001: Endpoint Missing Entry Point Method

## Diagnostic ID

`MINEP001`

## Severity

Error

## Description

An endpoint class is marked with a MapMethods attribute (e.g., `[MapGet]`, `[MapPost]`) but does not contain a valid entry point method.

## Message

> Class '{0}' is marked with MapMethodsAttribute but does not contain a valid entry point method. Add a public instance method named 'Handle', 'HandleAsync', or specify a custom method name using the EntryPoint property.

## Cause

This error occurs when:
1. The endpoint class has no public instance methods named `Handle` or `HandleAsync`
2. A custom `EntryPoint` is specified but the method doesn't exist
3. The entry point method is private, static, or has incorrect accessibility

## How to Fix

### Option 1: Add a HandleAsync Method

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    // Add this method
    public async Task<IResult> HandleAsync()
    {
        return Results.Ok(new[] { "User1", "User2" });
    }
}
```

### Option 2: Add a Handle Method

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    // Add this method
    public IResult Handle()
    {
        return Results.Ok(new[] { "User1", "User2" });
    }
}
```

### Option 3: Specify Custom Entry Point

```csharp
[MapGet("/users", EntryPoint = "Execute")]
public class GetUsersEndpoint
{
    // Custom entry point
    public async Task<IResult> Execute()
    {
        return Results.Ok(new[] { "User1", "User2" });
    }
}
```

## Requirements for Entry Point Methods

The entry point method must be:
- ✅ **Public** - Accessible from generated code
- ✅ **Instance** (not static) - Allows dependency injection
- ✅ **Returns** `IResult`, `Task<IResult>`, or compatible type
- ✅ **Named** `Handle`, `HandleAsync`, or specified in `EntryPoint` property

## Examples

### ❌ Incorrect - Private Method

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    private Task<IResult> HandleAsync() // ❌ Private
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### ❌ Incorrect - Static Method

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public static Task<IResult> HandleAsync() // ❌ Static
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### ❌ Incorrect - Wrong Name

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public Task<IResult> ProcessAsync() // ❌ Wrong name
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### ✅ Correct - HandleAsync

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public async Task<IResult> HandleAsync() // ✅ Correct
    {
        return Results.Ok();
    }
}
```

### ✅ Correct - Handle

```csharp
[MapGet("/users")]
public class GetUsersEndpoint
{
    public IResult Handle() // ✅ Correct
    {
        return Results.Ok();
    }
}
```

### ✅ Correct - Custom Entry Point

```csharp
[MapGet("/users", EntryPoint = "GetUsers")]
public class GetUsersEndpoint
{
    public Task<IResult> GetUsers() // ✅ Correct
    {
        return Task.FromResult(Results.Ok());
    }
}
```

## See Also

- [MINEP002: Multiple MapMethods Attributes](MINEP002.md)
- [MINEP003: ServiceType Interface Missing Entry Point](MINEP003.md)
- [Documentation: Creating Endpoints](../README.md#endpoint-classes)

