# MINEP015: Endpoint Has a Malformed Map Attribute

## Diagnostic ID

`MINEP015`

## Severity

Error

## Description

An endpoint's Map attribute cannot produce a routable endpoint because its route pattern is `null` or, for `[MapMethods]`, its HTTP-method set is empty (or contains only `null` entries). The generator declines such endpoints, so they are never registered.

## Message

> Endpoint '{0}' cannot be mapped because {1}, so it is skipped. Fix the Map attribute to map the endpoint.

Where `{1}` is one of:

- `its route pattern is null`
- `its [MapMethods] attribute specifies no HTTP methods`

## Cause

The Map attribute was given an argument that yields nothing to route:

- A `null` route pattern (e.g. `[MapGet(null)]`). Emitting it would map the endpoint to the empty route `""`, which is almost never intended.
- An empty or all-`null` HTTP-method array on `[MapMethods]` (e.g. `[MapMethods("/items", new string[0])]`). A `MapMethods` call with no verbs produces no usable route.

## How to Fix

### Provide a valid pattern and at least one HTTP method

**❌ Incorrect:**
```csharp
[MapMethods("/items", new string[0])]   // no HTTP methods
public class ItemsEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

**✅ Correct:**
```csharp
[MapMethods("/items", new[] { "GET", "POST" })]
public class ItemsEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

For a single verb, prefer a verb-specific attribute such as `[MapGet]` or `[MapPost]`:

```csharp
[MapGet("/items")]
public class ItemsEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

## See Also

- [MINEP002: Multiple Map Attributes Detected](MINEP002.md)
- [Documentation: Diagnostics](../../README.md)
