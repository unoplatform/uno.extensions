# 006 — Progress

## Plan

- [x] Spec written (`spec.md`) — in-vivo root cause + park-and-resume design.
- [x] Failing test (RED) — `src/Uno.Extensions.Navigation.UI.Tests/Given_NestedNavReloadRecovery.cs`.
      Deterministic: detaches the window content inside the created Frame's `Loaded` (which
      always precedes the route delivery — the frame loads empty), re-attaches after the init
      task settles, asserts the initial route reaches the frame. RED on main: final wait times
      out, frame permanently empty (skia-desktop head, `UnoTargetFrameworkOverride=net9.0-desktop`).
- [x] Fix — three silent drop sites converted to park-and-resume:
  - `Navigator.ForwardToChildrenAsync` (extracted from `CoreNavigateAsync`): children emptied
    mid-forward (the in-vivo kill at the `NestedNavigatorsAsync` dispatcher hop) → park
    `_pendingChildRequest`; resumed by a child region's `HandleLoaded` via
    `TryResumePendingChildRequestAsync` (re-enters only the forwarding stage — a full
    `NavigateAsync` would re-run Show() and re-create the FrameView).
  - `Navigator.RegionNavigateAsync` + `ControlNavigator.ControlNavigateAsync`: `Region.Services`
    null because the region detached mid-request (the test's kill variant — the wrap can land a
    tick later and hit the child's own pipeline instead) → park `_pendingDetachedRequest`;
    resumed by the region's own `HandleLoaded` via `TryResumeDetachedRequestAsync` (nothing was
    executed for the request, so full `NavigateAsync` re-entry is safe).
  - `NavigationRegion.HandleLoaded`: on (re-)attach, resume own detached request, then ask the
    parent to resume a parked child-bound request. Both fire-and-forget with fully guarded
    bodies (async void chain; WASM-fatal otherwise).
- [x] Test passes (GREEN) — formal sequence on the skia-desktop head:
      RED with main's `src/Uno.Extensions.Navigation.UI` + the test (`failed="1"` — init task
      hangs/drops, frame stays empty), GREEN with the fix (`passed="1"`). Commit `563c9c51e`.
- [ ] Regression — navigation runtime suite subset (`Given_NavigatorStartup`, non-HR nav tests).
- [ ] Release build zero-warnings check on the Navigation package.

## Additional kill heads found while driving the test to green

- The deterministic detach also exposed: (c) `await fv.EnsureLoaded()` (FrameView waits in
  `ExecuteRequestAsync` / `InitializeCurrentView` / `CheckLoadedAsync` overrides) **hangs
  forever** when the awaited view detaches before completing — the wait is timeout-less, and
  `Loaded` is raised child-first on managed targets, so a re-graft landing inside the loaded
  batch can detach the parent FrameView before its `Loaded` ever fires. Fixed with
  `EnsureLoadedWhileHostAttached` (gives up when the region's view leaves the tree; the request
  then falls through to the park sites). In vivo this head manifested as the silent drop rather
  than a hang only because the already-arranged FrameView completed the wait via its retained
  size.
- Test-topology gotcha (documented in HotReload.Spec.md, re-learned): descending from `""` to an
  `IsDefault` leaf requires children to already be attached — the root navigator's
  `RegionCanNavigate` is false for an unmapped empty route and never creates the FrameView. The
  test must pass `initialRoute:` explicitly, like the production apps do.

## Findings along the way

- The kill has multiple heads with identical symptoms (silent, "successful" response, blank
  shell): (a) parent forwarding sees zero children; (b) child's `Region.Services` resolves null
  after detach. The RED test deterministically produces (b); the in-vivo capture (Studio Live,
  instrumented timeline) shows (a). Both are fixed by parking; both resumes anchor on the
  region's `HandleLoaded` re-attach — the hook the in-vivo timeline shows firing reliably
  (`wasUnloaded=True` re-commit) on every re-graft.
- The pre-existing `wasUnloaded` re-cascade in `HandleLoaded` cannot recover this scenario:
  it requires `parentNav.Route.Base.Length > 0`, but in the FrameView pattern the parent
  navigator's route is `Route.Empty` by design.
