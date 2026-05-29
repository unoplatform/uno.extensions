# 004 — Progress

Tracking: collectible-ALC late region-attachment navigation race (downstream collectible-ALC WASM host)
Spec: [spec.md](./spec.md)

## Plan

- [x] Step 0 — repro-first: added `NavigationRegion.AttachDelayForTests` seam + the
  `When_ChildRegionAttachesLate_Then_DefaultTabStillRenders` test in `Given_TabBar_HotReload`.
  Confirmed it FAILS on current code (red): the late attachment stalls the initial cascade and
  `SetupTwoTabAppAsync` times out at `WaitForRouteAsync` (`Route='<null>'`) — the exact
  empty-children drop, confirming the mechanism.
- [x] Fix 1 — discriminated, bounded, cancellation-aware child-attach wait in
  `Navigator.EnsureChildRegionsAreLoaded` (waits only when a `Region.Attached` descendant is
  pending — `HasUnattachedRegionDescendant`); added `NavigationConstants`.
- [~] Fix 2 — DEFERRED (see Decision below). Not implemented.
- [x] Fix 3 — `ShowReturnsNullOnSuccess` hook on `ControlNavigator`, override `true` in
  `SelectorNavigator`; selector's intentional null no longer warns or parks a pending request.
- [ ] Verify — new test green; full `Given_TabBar_HotReload` + `Given_NavigationHotReload`
  suites green; Release build zero warnings.
- [ ] Cross-reference test in `HotReload.Spec.md` §5.5.

## Decision — Fix 2 (fresh-load self-heal) deferred

The approved plan included Fix 2 as optional defense-in-depth. On closer analysis it was dropped:

- The production drop the bundles show happens with an **empty** route (`route: ''`), which
  Fix 2's `!request.Route.IsEmpty()` guard would not match — so it would not even cover the
  primary case.
- A blind deferred dispatcher-retry cannot outrace child attachment any better than Fix 1's
  bounded wait; if attachment exceeds the budget, the retry fails identically. The simpler lever
  for slower hosts is the `ChildRegionAttachWaitBudget` constant.
- The existing `NavigationRegion.HandleLoaded` re-cascade already recovers the HR re-load case.

Per AGENTS §1 (Simplicity First / Minimal Impact): Fix 1 addresses the root cause directly;
Fix 2 added per-region retry state without covering a case Fix 1 doesn't. If a future bundle
shows attachment beyond the budget, raise the budget rather than add the retry.

## Notes

- Local `artifacts/uno.extensions` is at the same commit as upstream main (`4b4941cf`); files
  byte-identical. Not a regression vs main — the shipping HR navigation code has the bug.
- Skia secondary-app harness does NOT reproduce the WASM-ALC timing naturally (existing
  TabBar/NavigationView HR tests load the default tab fine), hence the explicit delay seam.

## Results (Skia desktop runtime-test host, net9.0-desktop)

- **Red → green (the fix):** `Given_TabBar_HotReload.When_ChildRegionAttachesLate_Then_DefaultTabStillRenders`
  - Red on pristine code — the late attachment stalls the initial cascade and
    `SetupTwoTabAppAsync` times out at `WaitForRouteAsync` (`Route='<null>'`).
  - Green after Fix 1 — the default tab (TabOne, IsDefault) renders. ✅
- **No regression (selector + cascade):** `Given_TabNavigation` — 3/3 pass on the fixed build
  (`When_NavigateToSiblingTab`, `When_NavigateToAlreadyVisitedTab`, `When_NavigateToNonTabRoute`).
  These exercise the SelectorNavigator (Fix 3) and the tab/frame cascade. ✅
- **Release build:** `Uno.Extensions.Navigation.WinUI.csproj -c Release` — zero warnings in any
  changed file (Navigator / ControlNavigator / SelectorNavigator / NavigationRegion /
  NavigationConstants). ✅
- **Environmental flakes (NOT this change), confirmed:**
  - `Given_NavigatorStartup.When_DefaultRouteConfigured_Then_NavigationSucceeds` fails on the
    fixed build — but **also fails identically on pristine (stashed) code** (verified via
    stash → rebuild → run). It uses `new Window()`, whose un-composited content never fires
    `Loaded`, so the initial nav never lands (the hazard documented in `HotReload.Spec.md §2`).
    Pre-existing host flake, not a regression.
  - The HR-based suites (`Given_TabBar_HotReload` HR cases, `Given_NavigationHotReload`) hit
    `TimeoutException: Timeout while waiting for metadata update` from the HR **dev-server**
    (Roslyn workspace churn in this environment). Independent of this change — the fix touches
    no HR-helper/dev-server code, and the new regression test deliberately avoids the dev-server
    via the `AttachDelayForTests` seam.

## Verification method note

The Skia secondary-app harness does not naturally reproduce the WASM-ALC late-attachment timing,
so the deterministic `AttachDelayForTests` seam is the regression guard. The decisive "is this my
regression?" question for the `new Window()` startup failure was answered by re-running it on a
pristine checkout (`git stash`) — same failure — proving it environmental.
