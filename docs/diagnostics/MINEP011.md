# MINEP011: Entry Point Parameter Uses an Unsupported Modifier

## Diagnostic ID

`MINEP011`

## Severity

Error

## Description

An entry point parameter uses a `ref`, `out`, `in`, or pointer modifier. ASP.NET Core cannot model-bind such a parameter, and the generated route handler delegate cannot reproduce the modifier, so the endpoint is not mapped.

## Message

> Parameter '{0}' of entry point '{1}' on endpoint '{2}' uses a 'ref', 'out', 'in', or pointer modifier. ASP.NET Core cannot model-bind such a parameter and the generated handler cannot pass it; remove the modifier.

## Cause

The selected entry point declares a by-reference (`ref`/`out`/`in`) or pointer parameter. Emitting it produces `CS1620` (for `ref`/`out`), `CS0214` (for a pointer), or — for `in` — silently changes binding to by-value. The generator therefore declines the endpoint.

## How to Fix

### Pass parameters by value

**❌ Incorrect:**
```csharp
[MapGet("/items/{id}")]
public class GetItemEndpoint
{
    public IResult Handle(ref int id) => Results.Ok(id);
}
```

**✅ Correct:**
```csharp
[MapGet("/items/{id}")]
public class GetItemEndpoint
{
    public IResult Handle(int id) => Results.Ok(id);
}
```

To return data, use the return value (e.g. an `IResult`) rather than an `out`/`ref` parameter.

## See Also

- [MINEP001: Missing Entry Point Method](MINEP001.md)
- [Documentation: Diagnostics](../../README.md)
