# MINEP012: Endpoint Is Not Assignable to Its ServiceType

## Diagnostic ID

`MINEP012`

## Severity

Error

## Description

The endpoint specifies a `ServiceType`, but the endpoint class does not implement or inherit it. The generated DI registration `services.AddX<ServiceType, Endpoint>()` requires an implicit reference conversion from the endpoint to the service type, so it would not compile.

## Message

> Endpoint '{1}' specifies ServiceType '{0}' but does not implement or inherit it. The generated registration would not compile. Implement '{0}' on '{1}' or change the ServiceType.

## Cause

`ServiceType = typeof(IService)` (or a base class) is set, but the endpoint class is not assignable to it. `MINEP003` only checks that the entry point method *exists* on the service type, not that the class is convertible to it — without this check the generated registration failed with `CS0311` (no implicit reference conversion).

## Generated Code Behavior

This diagnostic is an error, so the build fails. To avoid compounding it with a second, less obvious error (`CS0311`), the generator does **not** emit `services.AddX<ServiceType, Endpoint>()` for a non-assignable endpoint — it degrades to a concrete registration (`services.AddX<Endpoint>()`). The actionable diagnostic you see is `MINEP012`, not a raw compiler error.

## How to Fix

### Option 1: Implement the ServiceType

**❌ Incorrect:**
```csharp
public interface IGetItemEndpoint
{
    Task<IResult> HandleAsync();
}

[MapGet("/items", ServiceType = typeof(IGetItemEndpoint))]
public class GetItemEndpoint            // does NOT implement IGetItemEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}
```

**✅ Correct:**
```csharp
public interface IGetItemEndpoint
{
    Task<IResult> HandleAsync();
}

[MapGet("/items", ServiceType = typeof(IGetItemEndpoint))]
public class GetItemEndpoint : IGetItemEndpoint   // ✅ implements the service type
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}
```

### Option 2: Remove the ServiceType

If you don't need interface-based registration, drop the property and the concrete class is registered directly:

```csharp
[MapGet("/items")]
public class GetItemEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}
```

## See Also

- [MINEP003: ServiceType Interface Missing Entry Point](MINEP003.md)
- [Documentation: Diagnostics](../../README.md)
