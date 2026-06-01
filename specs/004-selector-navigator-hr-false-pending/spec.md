# SelectorNavigator — `Show()` Returns Null By Design, Must Not Be Treated As A Failed View Resolution

**Status:** In progress
**Affects:** `Uno.Extensions.Navigation.UI`, `Uno.Extensions.Navigation.Toolkit.UI`
**Follows:** [003-hot-reload-navigation-route-resolver](../003-hot-reload-navigation-route-resolver/spec.md)
**Issue:** unoplatform/studio.live#2245 — "[Navigation] Menu pages silently fail to resolve after Hot Reload — Show() returned null with no exception thrown"

## Problem

A Studio Live app generated with a bottom-`TabBar` shell (or a sidebar `NavigationView`
shell) renders its default tab, but after Hot Reload deltas are applied the menu items
stop navigating: clicking any tab logs

```
warn: Uno.Extensions.Navigation.Toolkit.Navigators.TabBarNavigator[0]
      ExecuteRequestAsync - Navigation to 'Reading' failed: Show() returned null.
      No matching view was found or created. ...
```

(`NavigationViewNavigator` on the sidebar variant) with no exception. The app sits on
whichever page it was already showing. The exported app — launched as a normal Uno app
with no hot-reload hosting — navigates correctly, so the warning is specific to the
hot-reload path.

## Root cause — the selector's intentional `null` is misclassified as a missing view

`TabBarNavigator` and `NavigationViewNavigator` both derive from
`SelectorNavigator<TControl>`. `SelectorNavigator.Show()` **always returns `null` by
design**:

```csharp
// SelectorNavigator.Show()
if (item != null && SelectedItem != item)
{
    SelectedItem = item;
}
// Don't return path, as we need for path to be passed down to children
return default;
```

It selects the matching `TabBarItem` / `NavigationViewItem` and returns `null` so the
route flows on to the **sibling** content region (the `Region.Navigator="Visibility"`
panel) that actually renders the page. `RegionCanNavigate` has already verified the item
exists before `Show()` is ever called, so a `null` result here is a *successful
delegation*, not a failed view resolution.

`ControlNavigator<TControl>.ExecuteRequestAsync` treats a `null` `Show()` result as a
failure unless one specific success case applies (spec 003, Fix 4):

```csharp
if (mapping?.RenderView is { } renderView && renderView.IsSubclassOf(typeof(Page))
    && CurrentView is FrameView fv) { ... return Route.Empty; }   // FrameView wrapper only
```

For a selector, `CurrentView` is the selected `TabBarItem` / `NavigationViewItem` — **never
a `FrameView`** — so this branch never matches. Every selector navigation therefore falls
through to the failure path, which:

1. logs the misleading `"Show() returned null. No matching view was found or created."`
   warning (the issue's smoking-gun line), and
2. calls `RememberPendingFailedRequest(request)`.

Step 2 is the damaging part. Spec 003's hot-reload self-heal (`Fix 3`) walks the live
region tree on **every** hot-reload delta and re-issues every `ControlNavigator` with a
pending failed request (`RetryPendingFailedRequestsFromRoot`). Because a selector records a
phantom pending request on *every* tab switch, each subsequent HR delta re-issues a stale
selector navigation. During a burst of deltas (e.g. the `LiveCharts` `PropertyValueBase`
deserialization storm observed in the bundle) the retry walk repeatedly re-navigates the
selector, thrashing the active tab — "navigation silently breaks after every Hot
Reload pass."

Spec 003 explicitly anticipated this collision for the `ContentControlNavigator` /
`PanelVisiblityNavigator` `FrameView` case ("the wrapper navigator would record a pending
retry for a route that is actually being handled by the inner `FrameNavigator`") and added
Fix 4 — but only for the `FrameView` shape. The identical collision on the
`SelectorNavigator` was not covered.

## Design

A navigator whose `Show()` legitimately returns `null` because the navigation is owned by
another region must advertise that, so `ExecuteRequestAsync` does not record a phantom
pending hot-reload retry or log a spurious warning.

### Fix — `ControlNavigator.IsNullShowResultExpected`

Add a virtual hook on the `ControlNavigator` base, defaulting to `false`:

```csharp
/// <summary>
/// When <c>true</c>, a <c>null</c> result from <c>Show()</c> is a successful
/// delegation rather than a failed view resolution ...
/// </summary>
protected virtual bool IsNullShowResultExpected => false;
```

`SelectorNavigator<TControl>` overrides it to `true`: it has already verified (in
`RegionCanNavigate`) that the requested item exists, selects it in `Show()`, and delegates
page rendering to the sibling content region.

`ControlNavigator<TControl>.ExecuteRequestAsync`, in the `executedPath is null` block,
after the existing `FrameView` branch, treats the expected-null case as success:

```csharp
if (IsNullShowResultExpected)
{
    // SelectorNavigator (TabBar / NavigationView) selected the item and delegates
    // the page to the sibling content region. Successful delegation, not a missing
    // view: clear any stale pending slot and do NOT record an HR retry.
    ClearPendingFailedRequest();
    return Route.Empty;
}
```

The route flow is unchanged — the failure path already returned `Route.Empty`, so `Trim`
still keeps the request route intact for the sibling/child regions. The *only* behavioral
differences are: (a) no false warning, and (b) `ClearPendingFailedRequest()` instead of
`RememberPendingFailedRequest()`, so the selector never enters the HR retry set.

### Why not change `SelectorNavigator.Show()` to return a non-null path?

`Show()` returns `null` deliberately so the route is passed down to children
(`// Don't return path, as we need for path to be passed down to children`). Returning the
path would make `CoreNavigateAsync` `Trim` it off the request and starve the sibling
content region. The fix keeps the existing return contract and only corrects the
*classification* of that `null` in the caller.

## Verification

### Framework regression test (red → green)

`Given_TabBar_HotReload.When_TabNavigated_Then_NoPendingFailedRequestRecorded`:

1. Boot the two-tab TabBar app (empty `Region.Navigator="Visibility"` content grid +
   `TabBarItem`s with `Region.Name`, page-typed routes — the exact scaffolded shape).
2. Navigate to `TabTwo`; wait for its content VM (confirms navigation succeeded).
3. Assert the `TabBarNavigator`'s `HasPendingFailedRequest` is `false`.

Without the fix the selector records a phantom pending request on the successful
navigation and the assertion fails (red). With the fix it passes (green). The existing
TabBar HR tests continue to pass because the route flow is unchanged.

### Product-level reproduction (studio.live)

`StudioLive.RuntimeTests.TabBarHotReloadNavigationRuntimeTests` is rewritten to scaffold
the **real** shell shape (empty Visibility region + separate page *types* created on
demand, as `TabBarShellStrategy` emits) instead of pre-instantiated named child panels, so
it exercises the on-demand `FrameView` + selector path that the production scaffolding
actually produces.

## How to apply

When a navigator returns `null` from `Show()` as a *contract* (it delegated the view to
another region) rather than as a *failure* (it could not resolve the view), the caller's
"null == failure" branch must distinguish the two. Conflating them poisons any retry/queue
keyed off the failure signal — here, spec 003's hot-reload retry walk re-issued a phantom
selector navigation on every delta. The `FrameView` wrapper and the `SelectorNavigator` are
two shapes of the same "intentionally null" contract; a third shape should reuse
`IsNullShowResultExpected`.
