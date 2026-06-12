# MINEP999: MinimalEndpoints Source Generator Failed

## Diagnostic ID

`MINEP999`

## Severity

Error

## Description

The MinimalEndpoints **source generator** (not an analyzer) hit an unexpected exception while producing the generated extension methods. Rather than letting the build surface the opaque, detail-free **CS8785** ("generator failed to generate source"), the generator catches the failure and reports it as this diagnostic with the exception type and message, so the problem is actionable.

When this fires, **no `MinimalEndpointExtensions` is generated** — your `AddMinimalEndpoints()` / `UseMinimalEndpoints()` calls will not resolve until the underlying issue is fixed.

## Message

> The MinimalEndpoints source generator failed unexpectedly and produced no output: {0}. This is a bug in the generator — please report it with the details above.

## Cause

An unhandled exception in the generator's output step. This indicates a bug in MinimalEndpoints (valid user code should never trigger it — malformed/ambiguous code is handled gracefully and reported via MINEP001–MINEP008).

## How to Fix

1. Note the exception type and message from the build output / Error List.
2. See **[Debugging the generator](../CONTRIBUTING.md#debugging-the-generator)** to capture more detail (dump the generated files, attach a debugger).
3. Open an issue with a minimal repro and the reported details.

## Notes

This is a generator-reported diagnostic; it is intentionally **not** part of analyzer release tracking (`AnalyzerReleases.*.md`), which only covers `DiagnosticAnalyzer`-reported rules.
