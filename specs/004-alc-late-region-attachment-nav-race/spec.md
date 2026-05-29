# 004 — ALC late-region-attachment navigation race

Status: in progress
Predecessor: [003-hot-reload-navigation-route-resolver](../003-hot-reload-navigation-route-resolver/spec.md)
Tracking: collectible-ALC late region-attachment navigation race (downstream collectible-ALC WASM host)

## 1. Problem

When a host (e.g. a downstream IDE/preview tool) loads a generated Uno **WASM** app
inside a *collectible* `AssemblyLoadContext` and reloads it on each build / Hot-Reload
pass, menu/tab navigation silently breaks: clicking a `NavigationView` item or a
`TabBarItem` logs a warning and the page does **not** change. **No exception is thrown**,
so it presents as a frozen UI. The same app exported and run normally navigates fine.
The failure reproduces on **both** `NavigationView` and `TabBar` shells, implicating the
shared navigation pipeline rather than a single navigator.

Decisive log sequence captured on a **fresh** load (full ALC swap — *not* a C# HR delta):

```
AppAssemblyLoadContext disposed / GC-unloaded → new collectible ALC created
inner host built for App → AppLoaded
~900 ms later:
  NavigationViewNavigator ExecuteRequestAsync - Navigation to 'Dashboard' failed: Show() returned null...
  FrameNavigator CoreNavigateAsync - Region 'Dashboard' has no children to forward request to (route: '', view loaded: True)
HotReloadManager created successfully       <-- AFTER the failure
```

The failing navigation is the **initial cascade to the nested `IsDefault` route**, and the
HR subsystem is not even up yet, so the existing HR-delta-driven retry never runs.

## 2. Root cause

The initial cascade to the nested `IsDefault` route **races the attachment of the content
region's child `NavigationRegion`** under ALC hosting:

1. `PanelVisiblityNavigator.Show(path, PageType)` rewrites a `Page`-typed view to a
   `FrameView` and adds it to the panel.
2. The `FrameView`'s inner `Frame` becomes a child region that attaches to
   `Region.Children` **only when its `Loaded` event fires**
   (`NavigationRegion.HandleLoading → AssignParent → Parent.Children.Add(this)`).
   `Children` is a plain `List<IRegion>` with no change notification.
3. `Navigator.CoreNavigateAsync` calls `EnsureChildRegionsAreLoaded()` then bails at
   `if (Region.Children.Count == 0)` with "has no children to forward request to",
   dropping the navigation.
4. `EnsureChildRegionsAreLoaded` already mitigates ALC late-attachment but only pumps the
   dispatcher **up to 5 times with zero delay**. Under WASM ALC hosting the `Loaded`
   events need real elapsed time, so the pumps finish before the child attaches.
5. On a fresh load no HR metadata-update follows, so the HR-delta-driven retry
   (`NavigationRouteUpdateHandler`) never fires → it never self-heals. Re-clicking
   re-creates the `FrameView` and re-hits the race.

**Secondary (diagnostic noise):** `SelectorNavigator.Show()` intentionally always returns
`null` after selecting its item, but `ControlNavigator.ExecuteRequestAsync` classifies a
`null` return as failure (warn + park pending request) unless `CurrentView is FrameView`.
A selector's `CurrentView` is the `NavigationViewItem`/`TabBarItem`, so every selector
navigation emits the misleading `Show() returned null` warning and parks a spurious
pending request.

## 3. Design constraint

A genuine **leaf** page region also has `Children.Count == 0 && View.IsLoaded == true`,
identical to the race window. The fix must **not** add latency to leaf navigations — it
waits only when child regions are genuinely expected: the loaded `Region.View` subtree
contains a descendant with `Region.Attached == true` that has not yet attached.

## 4. Fix

1. **Discriminated, bounded, cancellation-aware wait** in
   `Navigator.EnsureChildRegionsAreLoaded`: when (and only when) a region-attached
   descendant is pending, poll with a real per-iteration delay up to a wall-clock budget.
   Constants centralized in `NavigationConstants`
   (`ChildRegionAttachWaitBudget`, `ChildRegionAttachPollInterval`). Honors the request's
   `CancellationToken`; `OperationCanceledException` degrades gracefully.
2. **One-shot fresh-load self-heal**: when the cascade still finds no children with a
   non-empty route, reuse the existing `RememberPendingFailedRequest` /
   `RetryPendingFailedRequestAsync` infra via a single dispatcher re-enqueue (guarded
   against re-scheduling so no retry loop, no double-navigation).
3. **Selector intentional-null hook**: `ControlNavigator.ShowReturnsNullOnSuccess`
   (`virtual`, default `false`), overridden `true` in `SelectorNavigator`; the null branch
   treats it as success (no warning, no parked request). Leaves the existing `FrameView`
   intentional-null path untouched.

## 5. Test

`Given_LateRegionAttachment` (`Uno.Extensions.Navigation.UI.Tests`, `[RunsInSecondaryApp]`)
makes the race deterministic via an internal `NavigationRegion.AttachDelayForTests` seam
that delays a child region's attachment by an interval **longer than the old zero-delay
pump window but shorter than the new budget**:
- Red on current code (default/target page never renders, "has no children" logged).
- Green after Fix 1.
- Covers a `NavigationView` and a `TabBar` shell.
Plus an assertion that selector navigation does not park a pending failed request (Fix 3).

## 6. Verification

Per `HotReload.Spec.md §3` (Skia desktop runtime-test host). Run the new test (red → green),
the full `Given_TabBar_HotReload` + `Given_NavigationHotReload` suites (no regression), and
`dotnet build Uno.Extensions-packageonly.slnf -c Release` (zero warnings).
