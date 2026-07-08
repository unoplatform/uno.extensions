# 006 — Initial navigation dies silently when child regions detach mid-forward

## Problem

An app can come up **permanently blank** when its hosting tree is re-grafted while the initial
navigation is in flight. Captured in vivo (WASM, ALC-hosted app whose content is re-hosted by
Hot Design moments after launch; instrumented timeline):

```
t=17     root region ready (navigationRoot ContentControl, ContentControlNavigator)
t=1268   FrameView's Frame created          ← initial "Main" navigation started
t=1647   Frame Loaded, FrameNavigator ready ← nested hop into the Frame pending
t=1900   ENTIRE tree Unloaded               ← host re-graft (Hot Design wrap), 250ms later
t=3034→5772  re-enter, regions re-attach and re-commit (wasUnloaded=True)
…        the Frame never navigates; the canvas stays blank for the whole session
```

## Root cause (all code-verified)

1. **The kill.** For a Page route the root `ContentControlNavigator` shows a `FrameView` and
   returns `Route.Empty` (by design — the page itself is the child Frame's job), then
   `Navigator.CoreNavigateAsync` forwards the request to child regions. Between the early
   `Region.Children.Count == 0` check and the child filter sits an async gap
   (`NestedNavigatorsAsync` dispatcher hop). When the hosting tree is detached inside that gap,
   `NavigationRegion.ViewUnloaded` sets `Parent = null`, which **removes the child region from
   `Region.Children`**. The filtered child array is then empty and
   `NavigateChildRegions(empty)` returns null — silently (that path has no logging), and the
   overall response is still a non-empty "success" (`Route.Empty` region response), so even
   `HostAsync`'s "Initial navigation returned null … blank screen" warning stays quiet.
2. **Recovery hook #1 can't fire.** On re-attach, `NavigationRegion.HandleLoaded` has a
   re-cascade for `wasUnloaded` regions — but it requires
   `parentNav.Route is { Base.Length: > 0 }`, and in the FrameView pattern the parent's
   `Route` is **Empty by design** (`UpdateRoute(Route.Empty)`). The condition is unsatisfiable
   at the root of the standard app shape.
3. **Recovery hook #2 doesn't apply.** The hot-reload retry (`RetryPendingFailedRequestAsync`)
   only re-issues requests that failed inside `Show()` — the child Frame never received a
   request to fail, so there is nothing to retry.

## Fix

Park-and-resume, mirroring the existing `_pendingFailedRequest` precedent:

- `Navigator`: when the child-forwarding stage finds no eligible child regions for a non-empty,
  non-back route, **park the request** on the navigator (`_pendingChildRequest`) instead of
  dropping it. The forwarding stage (nested navigators fetch → default-route application →
  child filter → `NavigateChildRegions`) is extracted into `ForwardToChildrenAsync` so it can
  be re-entered without re-running the region's own `Show()` (a full `NavigateAsync` re-entry
  would re-create the FrameView).
- `NavigationRegion.HandleLoaded`: when a region attaches and is routable, ask the parent's
  navigator to resume any parked request (`TryResumePendingChildRequestAsync`) — the exact
  hook the in-vivo timeline shows firing reliably on every re-graft (`wasUnloaded=True`
  re-commit). Resume clears the slot first (no loops), runs only the forwarding stage, and is
  fully guarded (invoked from an `async void` event chain).
- A newer forwarded request supersedes the slot (cleared on successful dispatch, overwritten on
  the next park).

Parking logs at Information (route Base only — no query/data, per the no-PII rule), resume
logs at Information; the silent filtered-empty path is thereby also observable.

## Tests

`Given_NestedNavReloadRecovery` (Navigation.UI.Tests): deterministically reproduces the kill —
hooks the created Frame's `Loaded`, detaches the window content inside the handler (which lands
in the gap between the early child check and the child filter, because the forwarding
continuation is queued behind the Loaded dispatch), re-attaches on the next tick, and asserts
the initial route reaches the Frame. RED on main (frame stays empty forever), GREEN with the
fix. Regression: `Given_NavigatorStartup`, navigation runtime suite.

## Notes

- Spec 005 number is held by the in-flight `dev/sb/root-sp` branch (blank-region resolution);
  that work targeted the region-orphaning family, which in-vivo turned out to be Hot Design
  preview thumbnails (anchor-free by construction). This spec addresses the actual blank.
- The same parked-request mechanism also covers the cold-start race documented in
  `CoreNavigateAsync` ("no children to forward" during HR cold-start) as a durable backstop to
  the existing bounded dispatcher-drain retry.
