# MINEP014: Group Cannot Be Applied Because of Its Shape

## Diagnostic ID

`MINEP014`

## Severity

Warning

## Description

An endpoint references a group that *is* decorated with `[MapGroup]`, but the group has an unsupported shape (abstract, open generic, nested in an open generic, file-local, or below `internal`). Such a group is never mapped, so the endpoint is registered **without** the group's route prefix and configuration.

## Message

> Endpoint '{0}' references group '{1}', but '{1}' has an unsupported shape ({2}) and is not mapped. '{0}' is therefore registered without the group's route prefix and configuration.

## Cause

The group passes the `[MapGroup]` check (so `MINEP005` does not apply) but is skipped by the generator's shape gate, exactly as `MINEP008` would flag the group itself. Because the group is never discovered, the endpoint's prefix/configuration silently disappear. This warning is reported on the *endpoint* (whose route changes); the group separately reports `MINEP008` where applicable.

## How to Fix

### Give the group a supported shape

**❌ Incorrect:**
```csharp
[MapGroup("/api")]
public abstract class ApiGroup { }   // abstract -> never mapped

[MapGet("/items", Group = typeof(ApiGroup))]
public class GetItemsEndpoint { public IResult Handle() => Results.Ok(); }
// Maps at "/items", NOT "/api/items"
```

**✅ Correct:**
```csharp
[MapGroup("/api")]
public class ApiGroup { }            // concrete, non-generic, at least internal

[MapGet("/items", Group = typeof(ApiGroup))]
public class GetItemsEndpoint { public IResult Handle() => Results.Ok(); }
// Maps at "/api/items"
```

A group must be a non-abstract, non-generic, non-file-local class that is accessible (at least `internal`) and not nested in an open generic type.

## See Also

- [MINEP005: Invalid Group Type](MINEP005.md)
- [MINEP008: Unsupported Shape](MINEP008.md)
- [Documentation: Diagnostics](../../README.md)
