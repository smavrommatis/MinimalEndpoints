# MINEP008: Endpoint or Group Class Has an Unsupported Shape

## Diagnostic ID

`MINEP008`

## Severity

Warning

## Description

A class marked with a MinimalEndpoints attribute (`[MapGet]`, `[MapPost]`, …, or `[MapGroup]`) cannot be mapped because its *shape* prevents the source generator from referencing and instantiating it from the generated code. The class is skipped (no endpoint/group is generated for it), and this warning explains why.

The generated registration and mapping code lives in the same assembly as your endpoint and must be able to:

1. Name the type in a generated method/registration, and
2. Resolve it from dependency injection.

The following shapes break one of those requirements and are therefore skipped:

- **Open generic types** (e.g. `class ListEndpoint<T>`) — the type parameter is unbound in the generated `services.Add*<...>()` registration, and the sanitized method name would contain `<`/`>`.
- **File-local types** (`file class ...`) — not referenceable outside their own file.
- **Types whose effective accessibility is below `internal`** — e.g. a `private`/`protected`/`private protected` nested class. (`internal` and `public` are fine; generated code is in the same assembly.)

> Abstract classes are also skipped by discovery, but intentionally **do not** report MINEP008 — an abstract base endpoint/group is a legitimate pattern that is simply never mapped.

## Message

> Type '{0}' is marked with a MinimalEndpoints attribute but cannot be mapped because it is {1}. Endpoint and group classes must be non-generic, accessible (at least internal), non-file-local classes.

## How to Fix

Make the endpoint/group a non-generic, at-least-`internal`, non-file-local class.

```csharp
// ❌ Open generic — cannot be mapped
[MapGet("/items")]
public class ListEndpoint<T>
{
    public IResult Handle() => Results.Ok();
}

// ✅ Concrete, non-generic
[MapGet("/items")]
public class ListItemsEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

```csharp
// ❌ Private nested — not referenceable from generated code
public class Container
{
    [MapGet("/x")]
    private class Inner { public IResult Handle() => Results.Ok(); }
}

// ✅ At least internal, not nested-private
[MapGet("/x")]
internal class XEndpoint
{
    public IResult Handle() => Results.Ok();
}
```

```csharp
// ❌ File-local — unreferenceable from the generated file
[MapGet("/x")]
file class Hidden { public IResult Handle() => Results.Ok(); }

// ✅ Internal (or public)
[MapGet("/x")]
internal class XEndpoint { public IResult Handle() => Results.Ok(); }
```

## Notes

This is a warning, not an error: the surrounding code still compiles, but the affected class is silently *not* mapped. Address it (or remove the attribute) so the endpoint/group is generated.
