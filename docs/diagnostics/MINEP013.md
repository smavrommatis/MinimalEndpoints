# MINEP013: Multiple Endpoints Register the Same ServiceType

## Diagnostic ID

`MINEP013`

## Severity

Warning

## Description

Two or more endpoints register the same `ServiceType`. The DI container resolves only the last registration for a given service type, so one endpoint's route will run a different endpoint's class.

## Message

> Endpoints '{0}' and '{1}' both register ServiceType '{2}'. The DI container resolves only the last registration, so one endpoint's route will run the other endpoint's class. Use a distinct ServiceType per endpoint.

## Cause

Each endpoint registered via `ServiceType` is added as `services.AddX<ServiceType, Endpoint>()`. When two endpoints share one service type, the container keeps the last registration; the handler for each endpoint resolves `[FromServices] ServiceType`, so both receive the last-registered class.

## How to Fix

### Use a distinct ServiceType per endpoint

**❌ Incorrect:**
```csharp
public interface IEndpoint { Task<IResult> HandleAsync(); }

[MapGet("/a", ServiceType = typeof(IEndpoint))]
public class EndpointA : IEndpoint { public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok()); }

[MapGet("/b", ServiceType = typeof(IEndpoint))]   // ⚠️ same ServiceType
public class EndpointB : IEndpoint { public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok()); }
```

**✅ Correct:**
```csharp
public interface IEndpointA { Task<IResult> HandleAsync(); }
public interface IEndpointB { Task<IResult> HandleAsync(); }

[MapGet("/a", ServiceType = typeof(IEndpointA))]
public class EndpointA : IEndpointA { public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok()); }

[MapGet("/b", ServiceType = typeof(IEndpointB))]
public class EndpointB : IEndpointB { public Task<IResult> HandleAsync() => Task.FromResult(Results.Ok()); }
```

Or remove `ServiceType` from one/both endpoints to register the concrete classes directly.

> Note: this warning is computed over endpoints in the current compilation only; collisions across a referenced assembly are not detected by the analyzer.

## See Also

- [MINEP003: ServiceType Interface Missing Entry Point](MINEP003.md)
- [MINEP012: Endpoint Is Not Assignable to Its ServiceType](MINEP012.md)
- [Documentation: Diagnostics](../../README.md)
