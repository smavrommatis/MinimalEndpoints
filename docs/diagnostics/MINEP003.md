# MINEP003: ServiceType Interface Missing Entry Point Method

## Diagnostic ID

`MINEP003`

## Severity

Error

## Description

When an endpoint specifies a `ServiceType` property, the interface must contain the entry point method that will be called. This error occurs when the interface is missing that method.

## Message

> The ServiceType '{0}' specified for endpoint '{1}' does not contain the entry point method '{2}'. Add the method to the interface or change the ServiceType property.

## Cause

This error occurs when:
1. `ServiceType` is specified on a MapMethods attribute
2. The interface specified doesn't declare the entry point method
3. The method name doesn't match (including the custom `EntryPoint` if specified)

## How to Fix

### Option 1: Add Method to Interface

Add the missing method to the interface:

**❌ Incorrect:**
```csharp
// Interface missing HandleAsync
public interface IGetUsersEndpoint
{
    // ❌ No HandleAsync method
}

[MapGet("/users", ServiceType = typeof(IGetUsersEndpoint))]
public class GetUsersEndpoint : IGetUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}
```

**✅ Correct:**
```csharp
// Interface has HandleAsync
public interface IGetUsersEndpoint
{
    Task<IResult> HandleAsync();  // ✅ Method declared
}

[MapGet("/users", ServiceType = typeof(IGetUsersEndpoint))]
public class GetUsersEndpoint : IGetUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### Option 2: Remove ServiceType Property

If you don't need interface-based registration, remove the `ServiceType` property:

```csharp
[MapGet("/users")]  // ✅ No ServiceType
public class GetUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### Option 3: Match Custom Entry Point

If using a custom entry point, ensure the interface matches:

```csharp
public interface IProcessEndpoint
{
    Task<IResult> ProcessRequest();  // ✅ Matches EntryPoint
}

[MapPost("/process", EntryPoint = "ProcessRequest", ServiceType = typeof(IProcessEndpoint))]
public class ProcessEndpoint : IProcessEndpoint
{
    public Task<IResult> ProcessRequest()
    {
        return Task.FromResult(Results.Ok());
    }
}
```

## Why This Error Exists

When you register an endpoint as an interface using `ServiceType`:
1. The generated code calls the method through the interface
2. The interface MUST declare the entry point method
3. This ensures type safety and prevents runtime errors

## How ServiceType Works

**Generated Registration:**
```csharp
// With ServiceType
services.AddScoped<IGetUsersEndpoint, GetUsersEndpoint>();

// Without ServiceType
services.AddScoped<GetUsersEndpoint>();
```

**Generated Handler:**
```csharp
// With ServiceType - calls through interface
static Task<IResult> Handler([FromServices] IGetUsersEndpoint endpoint)
{
    return endpoint.HandleAsync();  // Must be in interface!
}

// Without ServiceType - calls concrete class
static Task<IResult> Handler([FromServices] GetUsersEndpoint endpoint)
{
    return endpoint.HandleAsync();  // Can be in class only
}
```

## Examples

### ❌ Incorrect - Missing Method

```csharp
public interface IUserService
{
    // ❌ Missing HandleAsync
    void SomeOtherMethod();
}

[MapGet("/users", ServiceType = typeof(IUserService))]
public class GetUsersEndpoint : IUserService
{
    public void SomeOtherMethod() { }

    public Task<IResult> HandleAsync()  // ❌ Not in interface
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### ❌ Incorrect - Wrong Method Name

```csharp
public interface IUserService
{
    Task<IResult> Execute();  // ❌ Wrong name
}

[MapGet("/users", ServiceType = typeof(IUserService))]
public class GetUsersEndpoint : IUserService
{
    public Task<IResult> Execute() { ... }

    public Task<IResult> HandleAsync()  // Entry point not in interface
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### ✅ Correct - Method in Interface

```csharp
public interface IGetUsersEndpoint
{
    Task<IResult> HandleAsync();  // ✅ Correct
}

[MapGet("/users", ServiceType = typeof(IGetUsersEndpoint))]
public class GetUsersEndpoint : IGetUsersEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### ✅ Correct - Custom Entry Point

```csharp
public interface IProcessEndpoint
{
    Task<IResult> Process();  // ✅ Matches EntryPoint
}

[MapPost("/process", EntryPoint = "Process", ServiceType = typeof(IProcessEndpoint))]
public class ProcessEndpoint : IProcessEndpoint
{
    public Task<IResult> Process()
    {
        return Task.FromResult(Results.Ok());
    }
}
```

### ✅ Correct - With IConfigurableEndpoint

```csharp
public interface IGetUsersEndpoint
{
    Task<IResult> HandleAsync();
}

[MapGet("/users", ServiceType = typeof(IGetUsersEndpoint))]
public class GetUsersEndpoint : IGetUsersEndpoint, IConfigurableEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok());
    }

    // Configure is called on concrete class, not interface
    public static void Configure(IApplicationBuilder app, RouteHandlerBuilder endpoint)
    {
        endpoint.WithTags("Users");
    }
}
```

## Common Scenarios

### Scenario 1: Testable Endpoints

Use interfaces for better testability:

```csharp
public interface IGetUserEndpoint
{
    Task<IResult> HandleAsync(int id);
}

[MapGet("/users/{id}", ServiceType = typeof(IGetUserEndpoint))]
public class GetUserEndpoint : IGetUserEndpoint
{
    private readonly IUserRepository _repository;

    public GetUserEndpoint(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<IResult> HandleAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user != null ? Results.Ok(user) : Results.NotFound();
    }
}

// In tests, mock the interface
var mockEndpoint = new Mock<IGetUserEndpoint>();
```

### Scenario 2: Shared Interfaces

Share endpoint interfaces across projects:

```csharp
// Shared.Contracts project
public interface IHealthCheckEndpoint
{
    Task<IResult> HandleAsync();
}

// API project
[MapGet("/health", ServiceType = typeof(IHealthCheckEndpoint))]
public class HealthCheckEndpoint : IHealthCheckEndpoint
{
    public Task<IResult> HandleAsync()
    {
        return Task.FromResult(Results.Ok(new { status = "healthy" }));
    }
}

// Consumer project
public class HealthMonitor
{
    private readonly IHealthCheckEndpoint _healthCheck;

    public HealthMonitor(IHealthCheckEndpoint healthCheck)
    {
        _healthCheck = healthCheck;
    }
}
```

## See Also

- [MINEP001: Endpoint Missing Entry Point Method](MINEP001.md)
- [MINEP002: Multiple MapMethods Attributes](MINEP002.md)
- [Documentation: Service Interfaces](../README.md#service-interface)
- [Examples: ServiceType with IConfigurableEndpoint](../EXAMPLES.md#servicetype-with-iconfigurableendpoint)

