# 008 — Progress

## Plan

- [x] Root cause pinned (see `spec.md`). Ruled out the "HR resets the Frame's `Region.Instance`"
      hypothesis (confirmed against Uno's `ClientHotReloadProcessor`: only `Frame.Content` is
      swapped; the Frame + its region survive). Real cause: `FrameNavigator._content` left
      pointing at the old, detached page after the out-of-band `Frame.Content` swap, then
      `CheckLoadedAsync` hangs waiting on it.
- [x] Deterministic RED reproduction —
      `src/Uno.Extensions.Navigation.UI.Tests/Given_TabNavigation_ActiveTabHotReload.cs`
      (`When_NavigateBackToHotReloadedActiveTab_Then_TabReloads`). No dev-server; simulates the
      exact HR effect (page swap + `MarkReplacedByHotReload` + cascade). RED on main: back-nav
      hangs at `post-hr:B->A`. Verified on the `net9.0-desktop` skia head.
- [x] Fix — `FrameNavigator.CheckLoadedAsync` re-syncs `_content` to the Frame's live `Content`
      before the host-aware load wait, so a stale cached view (invalidated by an out-of-band HR
      content swap) can't hang the navigation.
- [x] GREEN — the reproduction passes with the fix; isolation runs confirmed the page swap is the
      trigger (region-flag effects alone pass).
- [x] Regression — `Given_TabNavigation` (3) pass with the fix.
      `Given_ChainedGetDataAsync` / `Given_NavigatorStartup` / `Given_RouteNotifier` produce
      **identical** pass/fail counts with vs. without the fix on this head (residual failures are
      the headless environment's rendering/timeout sensitivity, not the change — verified by a
      main-baseline run).
- [x] Release build of the Navigation package: `0 Error(s)`; no warning originates from the
      changed file. (Full `Uno.Extensions-packageonly.slnf` Release build hit an environment
      restore quirk — "Invalid framework identifier ''" — with the platform-disable flag set on
      this machine; orthogonal to the change.)

## How to run the reproduction locally (skia-desktop)

```powershell
dotnet build src/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests.csproj `
  -c Debug -p:UnoTargetFrameworkOverride=net9.0-desktop -p:GeneratePackageOnBuild=false

cd src/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests/bin/Uno.Extensions.RuntimeTests/Debug/net9.0-desktop
$env:UNO_RUNTIME_TESTS_RUN_TESTS = '{"Filter": {"Value": "Given_TabNavigation_ActiveTabHotReload"}, "Attempts": 1}'
$env:UNO_RUNTIME_TESTS_OUTPUT_PATH = "results.xml"
dotnet Uno.Extensions.RuntimeTests.dll
```

RED on main (hang, `failed="1"`), GREEN with the fix (`passed="1"`). The desktop head runs the
non-hot-reload runtime suite; this test needs no dev-server.

## Findings along the way

- Uno HR replaces a hosted `Page` by setting `Frame.Content = newPage` **directly** (SwapViews),
  which does not raise `Frame.Navigated`; navigator-private caches like `FrameNavigator._content`
  therefore go stale silently.
- `EnsureLoadedWhileHostAttached` (spec 006) guards the *host* leaving the tree, but not the
  *awaited element* being a stale/detached reference — so it waited forever here.
- The field-report "silent no-op / missing FrameNavigator log line" is the user-visible face of a
  navigation that hangs inside `CheckLoadedAsync`, before the child-forward stage that emits that
  log.

## Open / follow-ups

- A faithful **real-HR** variant (per-tab distinct page fixtures, `HotReloadHelper`, in
  `Given_TabBar_HotReload`) could be added for the hot-reload CI stage. Deferred: the deterministic
  test already guards the exact defect and runs in the standard (non-HR) runtime stage without
  dev-server flakiness.
