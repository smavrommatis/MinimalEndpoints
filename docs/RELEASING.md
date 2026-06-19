# Releasing MinimalEndpoints

This is the canonical release runbook. Follow it exactly. It is written to be executable by both a
human maintainer and an automated assistant (e.g. Claude Code) — but **an assistant must never run the
tagging/push step (Step 8) without explicit human confirmation**, because that step publishes to
NuGet and cannot be undone.

Releases are **tag-triggered**: pushing a `vX.Y.Z` tag runs
[`.github/workflows/release.yml`](../.github/workflows/release.yml), which re-builds and tests the
tagged commit, packs deterministically, validates that the package version matches the tag and the
package layout is complete, and publishes to nuget.org via OIDC Trusted Publishing. **You never push
packages by hand.**

## Single source of truth

| What | Where | Notes |
|------|-------|-------|
| Package version | `src/MinimalEndpoints/MinimalEndpoints.csproj` → `<Version>` | The **only** place the version is defined. The git tag must be `v` + this value (tag `v1.4.0` ⇄ `<Version>1.4.0`). The Release workflow fails if they disagree. |
| Unreleased diagnostics | `src/MinimalEndpoints.CodeGeneration/AnalyzerReleases.Unshipped.md` | New analyzer rules land here during development; they "ship" at release (Step 2). |
| Shipped diagnostics | `src/MinimalEndpoints.CodeGeneration/AnalyzerReleases.Shipped.md` | Append-only; one `## Release X.Y.Z` section per release. |
| Changelog | `CHANGELOG.md` → `## [Unreleased]` | Keep-a-Changelog format; entries accumulate here during development. |
| Benchmark numbers | `README.md` (`## 📊 Performance`) and `docs/PERFORMANCE.md` | Refreshed from a local benchmark run (Step 1). The raw `BenchmarkDotNet.Artifacts/` output is gitignored — only the curated tables are committed. |
| Version-stamped docs | `SECURITY.md`, `docs/MIGRATION.md`, `docs/TROUBLESHOOTING.md`, `docs/ARCHITECTURE.md` | Carry a human-written version/date that must be bumped each release (Step 4). These were missed in 1.2.0/1.3.0 and went stale — do not skip them. |

## Pre-flight

1. On `main`, working tree clean (`git status`), up to date with origin.
2. CI is green on the commit you are about to release.
3. Decide the new version per [SemVer](https://semver.org): breaking → major, new feature/diagnostic →
   minor, bug-fix only → patch. Call it `X.Y.Z` below.

## The ceremony

Do all of Steps 1–6 first, then commit once (Step 7), then tag (Step 8). Everything below goes into a
**single commit** titled `Release X.Y.Z`.

### Step 1 — Refresh benchmark numbers (when generator/analyzer code changed this cycle)

BenchmarkDotNet requires Release; run the full suite on the machine you're releasing from (numbers are
machine-dependent — see [`benchmarks/README.md`](../benchmarks/README.md)):

```bash
dotnet run -c Release --project benchmarks/MinimalEndpoints.CodeGeneration.Benchmarks
```

Results print to the console and to `BenchmarkDotNet.Artifacts/results/` (the
`*-report-github.md` files are GitHub-flavored Markdown tables). Copy the refreshed tables into:

- `README.md` → `## 📊 Performance` — the **Analyzer Performance** and **Code Generation Performance**
  tables, and update the prose figures beneath them (the "~0.33 ms … ~9 ms" style summary).
- `docs/PERFORMANCE.md` → the **Analyzer Performance**, **Code Generation Performance**,
  **Cross-Assembly Scanning**, and **Build Time Impact** tables, plus their bullet summaries.

Do **not** commit `BenchmarkDotNet.Artifacts/` (gitignored). If no perf-relevant code changed this
cycle, you may skip this step and leave the existing numbers — but say so in the PR/commit.

### Step 2 — Ship the unshipped diagnostics

If `AnalyzerReleases.Unshipped.md` has any rows under `### New Rules`:

1. In `AnalyzerReleases.Shipped.md`, add a new section at the end:

   ```
   ## Release X.Y.Z

   ### New Rules

   Rule ID | Category | Severity | Notes
   --------|----------|----------|-------
   MINEPNNN | MinimalEndpoints | Error | EndpointsAnalyzer
   ```

   (Copy the exact rows from Unshipped.md.)
2. Reset `AnalyzerReleases.Unshipped.md` back to just its two header comment lines (remove the
   `### New Rules` section and its rows).

The RS2000/RS2001/RS2002 release-tracking analyzers enforce that every supported diagnostic appears in
these files, so a mismatch fails the build. (`MINEP999` is generator-reported, intentionally excluded —
do not add it here.)

### Step 3 — Roll the changelog

In `CHANGELOG.md`:

1. Rename the `## [Unreleased]` heading to `## [X.Y.Z] - YYYY-MM-DD` (today's date).
2. Add a fresh empty `## [Unreleased]` heading above it.
3. Update the link-reference footer at the bottom (compare-URL form):

   ```
   [Unreleased]: https://github.com/smavrommatis/MinimalEndpoints/compare/vX.Y.Z...HEAD
   [X.Y.Z]: https://github.com/smavrommatis/MinimalEndpoints/compare/vPREV...vX.Y.Z
   ```

### Step 4 — Bump the version-stamped docs

Update every human-written version/date reference to the new version (these are the ones that went
stale in past releases — grep for the previous version to catch them all:
`git grep -n "X.Y-1.Z\|<previous version>"`):

- `SECURITY.md` — the **Supported Versions** table row (e.g. `1.3.x` → `1.4.x`) and the "Last Updated" date.
- `docs/MIGRATION.md` — the "Currently on stable release (X.Y.Z)" line.
- `docs/TROUBLESHOOTING.md` — the sample `dotnet list package` output version and the footer date.
- `docs/ARCHITECTURE.md` — the footer `Version:` / `Last Updated:` stamp.
- If **new diagnostics** shipped this cycle, bump the diagnostic-range references ("MINEP001–MINEPNNN")
  in `docs/ARCHITECTURE.md`, `docs/CONTRIBUTING.md`, `docs/diagnostics/MINEP999.md`, and `README.md`,
  and ensure each new `MINEPNNN.md` is created and listed in `MinimalEndpoints.slnx`.
- `README.md` — the NuGet version badge tracks nuget.org automatically; bump only hardcoded version
  mentions.

### Step 5 — Bump the package version

In `src/MinimalEndpoints/MinimalEndpoints.csproj`, set `<Version>X.Y.Z</Version>`.

### Step 6 — Build and test locally (the gate)

```bash
dotnet build MinimalEndpoints.slnx --configuration Release
dotnet test  MinimalEndpoints.slnx --configuration Release --no-build
```

Both must be clean before you commit. (CI re-runs this with the ≥70% merged coverage gate and the
OS/TFM matrix; the Release workflow re-runs build+test on the tagged commit too.)

### Step 7 — One commit

Stage everything from Steps 1–5 and commit with exactly this message:

```bash
git add -A
git commit -m "Release X.Y.Z"
```

One commit per release, message `Release X.Y.Z` (matches the existing history). Do **not** add a
`Co-Authored-By` trailer.

### Step 8 — Tag and push (human-confirmed; this publishes)

```bash
git push origin main
git tag vX.Y.Z
git push origin vX.Y.Z
```

Pushing the tag triggers [`release.yml`](../.github/workflows/release.yml). It will refuse to publish if
`artifacts/Blackeye.MinimalEndpoints.X.Y.Z.nupkg` is absent (i.e. if `<Version>` ≠ the tag) or if the
package layout is incomplete (missing analyzer DLLs, README, or any of `lib/net8.0|net9.0|net10.0`).

### Step 9 — Verify the release

1. Watch the **Release** workflow run to green.
2. Confirm `Blackeye.MinimalEndpoints X.Y.Z` (and its `.snupkg`) appears on
   [nuget.org](https://www.nuget.org/packages/Blackeye.MinimalEndpoints). Publishing uses
   `--skip-duplicate`, so re-running the workflow is safe.
3. Optionally create a GitHub Release from the tag with the new CHANGELOG section as the notes.

## Notes for automated assistants

- The version is defined **only** in `src/MinimalEndpoints/MinimalEndpoints.csproj`. Never invent a
  second source of truth.
- Perform Steps 1–7 autonomously, but **stop and ask for explicit confirmation before Step 8** — tagging
  publishes to a public registry irreversibly.
- Benchmark numbers are machine-dependent; only refresh them from a run on the release machine, and never
  fabricate figures. If you cannot run the suite, leave the numbers and say so.
- Never commit until `dotnet build` + `dotnet test` are both green (Step 6).
