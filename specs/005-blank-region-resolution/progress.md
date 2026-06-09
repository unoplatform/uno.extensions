# 005 — Progress

## Plan

- [x] **Spec written** (`spec.md`) — root cause + event-driven fix design.
- [x] **Failing test (RED)** — `src/Uno.Extensions.Navigation.UI.Tests/Given_RegionResolutionResilience.cs`.
  Confirmed RED on `main` AND on no-fix+final-test (stash); GREEN with the fix. Built/ran via the skia-desktop
  head (`-p:UnoTargetFrameworkOverride=net9.0-desktop`, `xvfb-run`).
- [x] **Fix** — `Regions/NavigationRegion.cs`:
  - [x] `IsLoadedButUnresolved` predicate.
  - [x] Fields: `_resolveWatching`, `_resolveAttempts`, const `MaxResolveAttempts = 30`.
  - [x] `HandleLoaded`: `if (IsLoadedButUnresolved) { StartResolveWatch(); return; }` before commit; `StopResolveWatch()` on commit.
  - [x] `StartResolveWatch()` / `StopResolveWatch()` — subscribe/unsubscribe `View.LayoutUpdated`, guarded.
  - [x] `OnResolveLayoutUpdated` — non-mutating reachability probe; on reachable → stop + deferred `HandleLoading` via dispatcher (`ResolveAndLoadAsync`); on cap → stop + single Warning + `DescribeAncestry`.
  - [x] `ViewUnloaded`: `StopResolveWatch()` + reset attempts.
  - [x] Demote `AssignParent` "Unable to find service provider" Warning → Trace.
- [x] **Reuse** — `FrameworkElementExtensions.GetDispatcher` `private` → `internal`.
- [x] **Test passes (GREEN)** — `passed=1`.
- [ ] **Release contract build** — `Uno.Extensions-packageonly.slnf -c Release` zero warnings (in progress).
- [ ] **Regression** — full non-HR runtime suite (`!_HotReload`) + HR suite.
- [ ] **Review panel** — `/review-panel` on the diff.
- [ ] **Real Release-WASM app confirmation** — patched extensions (`-c Debug` → cache override); run the real
      blank-prone flow in Release; confirm no blank + zero "Unable to find service provider" warnings.
- [ ] **Docs/lessons** — update `specs/lessons.md` if corrections arise.

## Red/green evidence

| Build | Test variant | Result |
| --- | --- | --- |
| `main`, no fix | original (no forced layout) | RED (`failed=1`) |
| no fix (stashed) | final (forces a layout pass) | RED (`failed=1`) |
| fix | final | GREEN (`passed=1`) |

The defect is build-independent logic: the test forces the exact orphan path the WASM logs show and recovers
only with the fix — no reliance on release timing. The final test forces a layout pass (`InvalidateMeasure` +
`UpdateLayout`) to mirror the layout a real hot-reload re-graft produces, which is what `LayoutUpdated` watches.

## Decisions

- **Event-driven over dispatcher poll** (user request 2026-06-09): retry is triggered by `LayoutUpdated` (quiet at
  idle; the repo's `EnsureElementLoaded` already relies on it as the "element settled" signal), plus leaving
  `Loaded`/`Loading` subscribed so re-parent recovers for free. The dispatcher is used only for one safe
  deferred-commit hop, never as a retry loop.
- **Build-agnostic repro**: the defect is unconditional logic; the test forces the exact logged failure path, so
  it reproduces in Debug — no reliance on release timing.

## Review notes

_(to be filled in after implementation + review)_
