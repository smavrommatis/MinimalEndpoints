# MINEP010: Entry Point Method Must Not Be Generic

## Diagnostic ID

`MINEP010`

## Severity

Error

## Description

The endpoint's entry point method (`Handle`/`HandleAsync` or a custom `EntryPoint`) is generic. The generated route handler delegate cannot supply type arguments, so a generic entry point cannot be mapped.

## Message

> The entry point method '{0}' on endpoint '{1}' is generic. The generated handler cannot supply type arguments, so a generic entry point cannot be mapped. Make the entry point a non-generic method.

## Cause

The only method matching the entry point name is generic (declares type parameters). The generator would have to emit `Task<T> Handler(...)` / `instance.HandleAsync(...)` with an unbound `T`, which fails to compile (`CS0246` / `CS0411`).

## How to Fix

### Make the entry point non-generic

**❌ Incorrect:**
```csharp
[MapGet("/items")]
public class GetItemsEndpoint
{
    public Task<IResult> HandleAsync<T>() => Task.FromResult(Results.Ok());
}
```

**✅ Correct:**
```csharp
[MapGet("/items")]
public class GetItemsEndpoint
{
    public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok());
}
```

If the handler genuinely needs a type parameter, resolve it through a dependency (a generic service injected via `[FromServices]`) rather than on the handler method itself.

## See Also

- [MINEP001: Missing Entry Point Method](MINEP001.md)
- [Documentation: Diagnostics](../../README.md)
