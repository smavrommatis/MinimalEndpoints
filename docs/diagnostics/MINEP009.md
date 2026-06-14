# MINEP009: Referenced Group Is Not Scanned

## Diagnostic ID

`MINEP009`

## Severity

Warning

## Description

An endpoint's `Group` (or a group's `ParentGroup`) refers to a `[MapGroup]` type defined in a
**referenced compiled assembly** that cross-assembly scanning does not cover. Because that assembly is
not scanned, the group is never discovered, so it cannot be composed — the endpoint is silently mapped
**without** the group's route prefix and configuration.

This fires when the referencing endpoint/group is in your project but the group it points to lives in
another assembly **and** either:

- the host has no `[assembly: ScanReferencedEndpoints]`, or
- the host opts in with a **targeted** list (`[assembly: ScanReferencedEndpoints(typeof(...))]`) that
  does not include the group's assembly.

## Message

> '{0}' references group '{1}' from referenced assembly '{2}', which is not scanned for endpoints. The group's route prefix and configuration will not be applied — '{0}' is mapped without the group. Add [assembly: ScanReferencedEndpoints] to the host (optionally targeting it with [assembly: ScanReferencedEndpoints(typeof(...))]).

## How to Fix

### Option 1: Opt in to cross-assembly scanning

Add the opt-in attribute once on the host assembly so referenced groups are discovered:

```csharp
[assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints]
```

Or target only the assembly that defines the group:

```csharp
// any public type from the group's assembly works as the marker
[assembly: MinimalEndpoints.Annotations.ScanReferencedEndpoints(typeof(MyCompany.Api.SomeEndpoint))]
```

### Option 2: Move the group into this project

If the group does not need to live in a separate assembly, declare it in the same project as the
endpoint — same-project groups are always discovered (no opt-in needed).

### Option 3: Drop the group reference

If you did not intend to use the group, remove the `Group`/`ParentGroup` argument and put any prefix
directly in the route pattern.

## Notes

- This fires only for a **public** referenced group whose assembly isn't scanned. A non-public group
  can't be named from your code at all (the `typeof(...)` itself is a CS0122 compiler error), so the
  compiler already reports that case and MINEP009 stays quiet.
- Only assemblies the host references **directly** are scanned; purely transitive references are not.
- A non-public `ServiceType` on a referenced endpoint is ignored (the concrete class is registered), not
  a cause of this diagnostic.
- Discovery stays entirely at compile time; enabling scanning adds no runtime reflection.

## See Also

- [MINEP005: Invalid Endpoint Group Type](MINEP005.md)
- [MINEP006: Cyclic Group Hierarchy](MINEP006.md)
- [Documentation: Endpoints in a Referenced Library](../../README.md#endpoints-in-a-referenced-library)
