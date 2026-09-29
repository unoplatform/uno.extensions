# Progress — HR refresh of the active route's view model (#3142)

Branch: `dev/mara/fix-routes-hr` (no commit/push in this session — per request)

## Plan

- [x] Read issue #3142, the downstream consumer report that surfaced it, and the existing HR machinery
      (`NavigationRouteUpdateHandler`, `FrameNavigator` rehook, `Given_FrameContentRehook`,
      spec 003, `HotReload.Spec.md`)
- [x] Write spec (`spec.md` in this folder)
- [x] RED: harness-free UI test `Given_ActiveRouteVmRefresh` (VM delta → rebuilt; view-only delta →
      preserved) — fails on current code
- [x] RED: runtime HR test `Given_HotReload.When_ActiveRouteViewModelUpdated_Then_ActiveRegionVmReinstantiated`
      + ctor-seeded property on `HotReloadRegionVm`
- [x] FIX: `ControlNavigator.RefreshActiveRouteViewModelAsync` (internal virtual + generic override)
- [x] FIX: `NavigationRouteUpdateHandler` — thread `updatedTypes` into `ScheduleCascade`, add
      `CollectUpdatedViewModels` + `RefreshUpdatedActiveRoutesFromRoot`
- [x] Unit tests for `CollectUpdatedViewModels` in `Given_NavigationRouteUpdateHandler`
- [x] GREEN: build + run the new tests (and the neighboring HR/rehook tests) locally
- [x] Update living spec `src/Uno.Extensions.Navigation.UI.Tests/HotReload.Spec.md`
- [x] Baseline-diff the neighboring navigation suites (stash fix → rebuild → rerun) to attribute
      any failures

## Review

- Verified locally on Skia desktop (net9.0-desktop runtime-test host):
  - `Given_ActiveRouteVmRefresh` — RED before fix (`When_ActiveRouteViewModelInDelta_...` timed
    out: nothing re-instantiated the VM), GREEN after; the view-only control test passed in both
    states.
  - `Given_NavigationRouteUpdateHandler` — 11/11 green (incl. 5 new `CollectUpdatedViewModels`
    cases: null, unregistered, registered VM, registered view-only, MVUX mapped-model).
  - `Given_FrameContentRehook` — 4/4 green (state-preservation behavior unchanged).
  - Release build of `Uno.Extensions.Navigation.WinUI.csproj`: 0 errors.
  - `Given_HotReload` secondary-app tests require the dev-server HR harness (not available in
    this environment — the RC connection has no listener); the new test compiles and follows the
    existing harness patterns and runs in CI's `stage-build-runtimetests-skia-hotreload.yml`
    (filter `_HotReload` matches `Given_HotReload`).
- Neighboring suites: `Given_NavigationBoundary`, `Given_Navigation_VmCreationFailure`,
  `Given_TabNavigation`, `Given_NestedNavReloadRecovery` green. `Given_ChainedGetDataAsync` (8),
  `Given_NavigatorStartup` (1), `Given_Region` (1), `Given_RouteNotifier` (1) failed identically
  on the UNMODIFIED baseline (stash → rebuild → rerun) — pre-existing/environmental, not caused
  by this change; none of them exercise any HR entry point.
- Design decision worth keeping in mind: the refresh triggers on **view-model** membership only —
  view deltas are owned by Uno's element walk, and including them would regress the
  state-preservation behavior pinned by `Given_FrameContentRehook`.
