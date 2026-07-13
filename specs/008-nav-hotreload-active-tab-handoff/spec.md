# 008 — Navigating back to a hot-reloaded *active* tab hangs (silent no-op)

**Status:** Fix implemented, red→green verified on skia-desktop
**Affects:** `Uno.Extensions.Navigation.UI` (`FrameNavigator`)
**Follows:** [006-nested-nav-reload-recovery](../006-nested-nav-reload-recovery/spec.md),
[004-selector-navigator-hr-false-pending](../004-selector-navigator-hr-false-pending/spec.md)

## Problem

In a `TabBar`/`NavigationView` + sibling `Region.Navigator="Visibility"` content-region shell
(nested default route), after a **Hot Reload that replaces the currently-visible tab's
page/view-model** (a metadata/rude edit), navigating **away and back to that tab no longer
works** — the tab selection is applied but the content region never navigates the tab's page.
Only the tab that was **visible at the moment of HR** is affected; the other tabs keep working,
and the state never recovers on repeated user taps (only a full reload fixes it). Observed on
WASM (field report `nav-hotreload-active-tab-handoff.md`).

Field-report log signature — a working tab navigation logs a **pair**:

```
…TabBarNavigator: CoreNavigateAsync - Region '' has no children … (route: 'Menu')
…FrameNavigator:  CoreNavigateAsync - Region 'Menu' has no children … (route: '')
```

The broken navigation logs **only the first line** — the tab's `FrameNavigator` never runs,
with no exception and no page re-render.

## Root cause (code-verified + reproduced)

Confirmed against Uno's `ClientHotReloadProcessor`: when a `Page` hosted inside a
`Region.Attached` `Frame` is hot-reloaded, Uno's `SwapViews` sets **`Frame.Content = newPage`
directly** (bypassing `Frame.Navigate`), detaching the old page. The `Frame` itself, and its
`Region.Instance` (the `NavigationRegion`), survive intact — only the hosted page instance is
replaced.

But `FrameNavigator` caches the current page in a private `_content` field (also exposed as
`CurrentView`). The out-of-band `Frame.Content` swap does **not** raise `Frame.Navigated`, so
`FrameNavigator._content` is left pointing at the **old, now-detached page**.

On the next navigation back to that tab, the parent `PanelVisiblityNavigator` re-shows the tab's
`FrameView` and forwards the route into the tab's `FrameNavigator`. `Navigator.CoreNavigateAsync`
first calls `EnsureChildRegionsAreLoaded()` → `FrameNavigator.CheckLoadedAsync()`:

```csharp
protected override Task CheckLoadedAsync()
    => _content is not null ? _content.EnsureLoadedWhileHostAttached(Region.View) : Task.CompletedTask;
```

`EnsureLoadedWhileHostAttached` (spec 006) waits for `_content` to load, giving up only if the
**host** (`Region.View` = the `Frame`) leaves the tree. Here `_content` is the **detached old
page** — it will never load — and the host `Frame` **stays attached**, so the wait never
completes. The navigation hangs inside `CheckLoadedAsync`, **before** reaching the child-forward
stage, so the tab's `FrameNavigator` never logs "has no children" and never dispatches: exactly
the missing-second-log-line signature. From the user's perspective the tab tap does nothing.

Why only the active-at-HR tab: only the visible tab's page was swapped by HR, so only that tab's
`FrameNavigator._content` is stale. Inactive tabs' `FrameView`s were untouched.

Why it never recovers on user nav: nothing refreshes `_content` between attempts — every
back-navigation re-hits the same stale wait.

## Fix

`FrameNavigator.CheckLoadedAsync` must wait on the Frame's **live** content, not a cached
reference that an out-of-band swap can invalidate:

```csharp
protected override Task CheckLoadedAsync()
{
    // The Frame's content can be swapped out-of-band: Hot Reload's ReplaceViewInstance sets
    // Frame.Content = newPage directly, detaching the old page. Our cached _content would then
    // still point at that old, now-detached page. Waiting on it via EnsureLoadedWhileHostAttached
    // never completes — a detached element never loads, and the Frame host stays attached so the
    // host-aware wait never gives up — hanging the navigation before it reaches child forwarding.
    // The Frame's live Content is the authoritative current view, so re-sync to it before waiting.
    if (Control?.Content is FrameworkElement liveContent && !ReferenceEquals(liveContent, _content))
    {
        _content = liveContent;
    }

    return _content is not null ? _content.EnsureLoadedWhileHostAttached(Region.View) : Task.CompletedTask;
}
```

This is a no-op in the normal flow (by the time `CheckLoadedAsync` runs, `ExecuteRequestAsync`
has already set `_content` to the target page, so `_content == Control.Content`). It is corrective
only when the content changed out-of-band (HR). Re-syncing also keeps `CurrentView` fresh for any
subsequent use in the same navigation.

### Why not "keep `_content` in sync during HR"?

HR does not call into the navigation framework — it mutates `Frame.Content` directly and has no
handle on `FrameNavigator`'s private field. The navigator must therefore treat `Frame.Content` as
authoritative at the point of use. `EnsureLoadedWhileHostAttached` already guards the *host*
detaching (spec 006); this guards the *cached view* being invalidated.

## Verification

Deterministic runtime test `Given_TabNavigation_ActiveTabHotReload`
(`When_NavigateBackToHotReloadedActiveTab_Then_TabReloads`) — no dev-server. Uses the
`TabbedMainPage` shape (NavigationView selector + `Region.Navigator="Visibility"` content grid +
nested default route; NavigationView and TabBar are both `SelectorNavigator`s). It:

1. boots the shell (TabA default/visible), sanity-checks TabA→TabB→TabA;
2. simulates the active-tab HR effect on TabA, reproducing exactly what Uno's HR pipeline does:
   `Frame.Content = new TabAPage()` (SwapViews) + `MarkReplacedByHotReload()`
   (`NavigationVisibilityUpdateHandler.RestoreState`) + `ScheduleCascadeForAllContexts()`;
3. navigates TabA→TabB→TabA and asserts the back-navigation returns (a hard timeout turns the
   hang into a diagnostic failure) and TabA is re-rendered (visible + `TabAPage`, TabB collapsed).

**RED on main** (`net9.0-desktop` skia head): step `post-hr:B->A` — `NavigateRouteAsync('TabA')`
does not return within 10s (hang). **GREEN with the fix.** Isolation runs confirmed the trigger is
the page swap: the region-flag effects alone (`MarkReplacedByHotReload` + cascade, no swap) pass.

Regression: `Given_TabNavigation` (3 tests) pass with the fix; `Given_ChainedGetDataAsync`,
`Given_NavigatorStartup`, `Given_RouteNotifier` produce **identical** pass/fail counts with and
without the fix on this head (their residual failures are this headless environment's
rendering/timeout sensitivity, not related to the change).

## Notes

- The `_content = Control.Content` refresh already exists in one `NavigateForwardAsync` branch
  (the "same page type" else-branch); the bug's fast path (`segments.Length == 0`, "already at
  this route") skips it, and `CheckLoadedAsync` runs regardless — so `CheckLoadedAsync` is the
  correct, general choke point to guard.
- Related but distinct from spec 006 (host tree re-grafted mid-forward → park/resume) and spec 004
  (selector's intentional-null misclassified). This is a stale-cached-view wait, orthogonal to
  both.
