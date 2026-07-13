# Uno.Extensions.Navigation — navigating back to a hot-reloaded *active* tab silently no-ops

## TL;DR

In a TabBar + `Region.Navigator="Visibility"` content-region setup, after a **Hot Reload
that replaces the currently-visible tab's page/view-model (a metadata/rude edit)**, navigating
**away and back to that tab no longer works** — the tab selection is applied but the content
region never navigates the tab's page. Only the hot-reloaded (active-at-HR) tab is affected;
the other tabs keep working. This reproduces on a build of `Uno.Extensions.Navigation`
**7.3.0-dev.97**, i.e. *with* the spec-006 `EnsureLoadedWhileHostAttached` host-aware wait and
the spec-004 pending-failed-request retry already present — so those fixes do not cover this
case. Target platform where observed: **WebAssembly** (desktop unverified — please check both).

## Environment where observed

- `Uno.Extensions.Navigation.WinUI` **7.3.0-dev.97** (contains `EnsureLoadedWhileHostAttached`
  / spec 006 and `RememberPendingFailedRequest` / spec 004).
- .NET 10 WASM, Uno 6.7.x.
- Host is a **design-time preview tool**: a WASM app that compiles a user app to a single
  assembly, loads it into a child `AssemblyLoadContext` in-process, and pushes Hot Reload
  deltas (XAML + C#, including new/changed types) into the running previewed app. The
  navigation framework assemblies are the host's (forwarded), not shipped with the previewed
  assembly. The bug is in the navigation framework behavior under Hot Reload, not host-specific.

## Minimal repro shape

Shell page — TabBar region + an **empty** content Grid using the Visibility navigator, as
siblings:

```xml
<Grid uen:Region.Attached="True">
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>

    <!-- content: pages are created here on demand, one FrameView per named route -->
    <Grid Grid.Row="0"
          uen:Region.Attached="True"
          uen:Region.Navigator="Visibility" />

    <utu:TabBar Grid.Row="1" uen:Region.Attached="True" Style="...">
        <utu:TabBarItem uen:Region.Name="Menu"   Content="Menu" />
        <utu:TabBarItem uen:Region.Name="Cart"   Content="Cart" />
        <utu:TabBarItem uen:Region.Name="Orders" Content="Orders" />
        <utu:TabBarItem uen:Region.Name="Favorites" Content="Favorites" />
    </utu:TabBar>
</Grid>
```

Routes — nested, first child `IsDefault`:

```csharp
routes.Register(new RouteMap("Main", View: views.FindByViewModel<MainModel>(), IsDefault: true,
    Nested:
    [
        new RouteMap("Menu",   View: views.FindByView<MenuPage>(), IsDefault: true),
        new RouteMap("Cart",   View: views.FindByView<CartPage>()),
        new RouteMap("Orders", View: views.FindByView<OrdersPage>()),
        new RouteMap("Favorites", View: views.FindByView<FavoritesPage>()),
    ]));
```

Each nested `View` is a `Page`, so the Visibility navigator wraps each in a `FrameView`.

### Steps

1. Launch. `Menu` is the default/visible tab; its `FrameView` + inner `Frame` are created and
   registered. Navigate Menu→Cart→Menu a few times: **works** (see log pairs below).
2. While **Menu is the visible tab**, apply a Hot Reload that is a *metadata/rude* edit of the
   Menu tab, not just a XAML tweak — in the observed case, three deltas in sequence:
   - add a new C# file introducing new record types,
   - change `MenuModel` from an empty `record` to one with properties (new members/shape),
   - replace `MenuPage.xaml` placeholder with real content (lists/ItemsRepeater).
   During HR the route resolver transiently logs `Could not resolve type for path 'MenuModel'`
   and `'__KeyEqualityProvider'` (types being rebuilt).
3. After HR: navigate Menu→Cart (**works**), then Cart→Menu (**broken** — nothing happens).
   Repeatable; other tabs (Cart/Orders/Favorites) still navigate fine. State does not recover
   on repeated attempts; only a full reload fixes it.

## Observed behavior — log signature

Working navigation to a tab produces a **pair**:

```
[App] ...TabBarNavigator: CoreNavigateAsync - Region '' has no children to forward request to (route: 'Menu', view loaded: True)
[App] ...FrameNavigator:  CoreNavigateAsync - Region 'Menu' has no children to forward request to (route: '', view loaded: True)
```

Broken navigation to the hot-reloaded tab produces **only the first line** — the
`FrameNavigator` for that region never runs, and there is **no exception and no page
re-render**:

```
[App] ...TabBarNavigator: CoreNavigateAsync - Region '' has no children to forward request to (route: 'Menu', view loaded: True)
   << missing: FrameNavigator ... Region 'Menu' >>
```

Interpretation: the TabBar SelectorNavigator selects the item and the sibling content
(PanelVisibility) region is supposed to forward the nested route into the tab's `FrameView`
→ inner `Frame` → page. Post-HR, that forward into the affected tab's region "has nothing to
dispatch into."

## Ruled out

- **Not a stale dependency / version pin.** The running Extensions (7.3.0-dev.97) already has
  the spec-006 wait and spec-004 retry.
- **Not the page content per se.** The reloaded page rendered fine immediately after HR (its
  bindings evaluated). A separate invalid binding in the sample (`Visibility="{Binding <string>}"`)
  logs `BindingExpression` errors but is descendant-level and does not affect region forwarding;
  navigation to the *same* tab worked before the HR in the same session.
- **Not all tabs.** Only the tab that was **visible at the moment of HR** breaks.

## Mechanism hypothesis (where to look)

Files:
- `src/Uno.Extensions.Navigation.UI/Navigators/PanelVisiblityNavigator.cs`
  — `Show()` does `Region.Children.Clear()`, `FindByPath(path)` (matches a visual child whose
  `GetName() == path`), returns `null` for `FrameView`-wrapped pages, then
  `controlToShow.ReassignRegionParent()`. `HandlePanelChildren()` seeds routes from existing
  named children on load.
- `src/Uno.Extensions.Navigation.UI/Navigators/ControlNavigator.cs`
  — `ControlNavigator<TControl>.ExecuteRequestAsync`: on a `null` `Show()` result where
  `RenderView` is a `Page` and `CurrentView is FrameView fv`, it
  `await fv.EnsureLoadedWhileHostAttached(Region.View)` then returns `Route.Empty`, relying on
  the downstream child-forwarding stage to dispatch the nested route into the FrameView's inner
  `Frame` region. Otherwise it `RememberPendingFailedRequest(request)`.
- `FrameView.EnsureLoadedWhileHostAttached` (spec 006 host-aware wait).
- The pending-retry handler (spec 004) that re-issues parked requests — appears to fire on HR
  deltas, **not** on user-initiated navigation.

Hypothesis: when HR replaces the **active** tab's view/model (metadata edit), the tab's
`FrameView` (or its inner `Frame`'s `NavigationRegion`) is detached/replaced/not-yet-reloaded.
On the next Cart→Menu:
1. `Show("Menu")` finds (or recreates) the FrameView and returns `null`;
2. `EnsureLoadedWhileHostAttached` either **gives up** (host/region view detached during the
   reload window) or completes but the **inner Frame region is not re-registered as a child** of
   the content region;
3. so the forward has no child region to dispatch into → no `FrameNavigator`, silent no-op;
4. the request is parked as a pending-failed request, but nothing re-issues it on a subsequent
   **user** tab tap (only on the next HR delta), so every later Menu tap repeats the failure.

Other tabs are fine because their FrameViews were never hot-reloaded and stayed loaded +
registered.

## Suggested investigation steps

1. Reproduce at the framework level using the existing HR-nav test infra —
   `src/Uno.Extensions.Navigation.UI.Tests/Given_HotReloadNavigation.cs`,
   `Pages/HotReloadRegionPage.cs`, `Given_TabNavigation.cs`, `Pages/TabNav/TabbedMainPage.cs` —
   extended to the **TabBar + PanelVisibility + nested-default-route** shape above, and simulate
   a **metadata/rude** HR of the *active* tab's view+model (new type, changed model shape), not a
   XAML-only delta.
2. Right after the simulated HR, inspect: is the affected tab still a visual child of the content
   Grid, still named (`GetName()` == route), and is its inner `Frame`'s `NavigationRegion` still a
   child of the content region? Then drive a navigate-away-and-back and log which branch of
   `ExecuteRequestAsync` is taken and whether `EnsureLoadedWhileHostAttached` completes or gives up.
3. Pin the exact failure mode among: (a) `EnsureLoadedWhileHostAttached` gives up → parked, never
   retried on user nav; (b) `FindByPath` returns null → recreate path fails to register inner
   region in time; (c) inner Frame region not re-added as child after `ReassignRegionParent`.
4. Check whether it's specifically the `IsDefault` tab or **any** tab that was visible during HR
   (build the repro with a non-default tab active at HR time).
5. Confirm desktop vs WASM (observed on WASM).

## Suggested fix directions (validate against the repro)

- After an HR that replaces a visible region's view, ensure the PanelVisibility child **and** its
  inner `Frame` region are re-registered so a later user navigation forwards correctly (revisit
  `Region.Children.Clear()` + `ReassignRegionParent()` interaction with a reloaded child).
- Allow a parked pending-failed request to be re-attempted on the **next navigation request** to
  that region (not only on HR deltas), or clear it so a fresh user navigation isn't short-circuited.
- If `EnsureLoadedWhileHostAttached` gives up because the host view briefly detaches during the
  reload, ensure the request resumes on region **re-attach** for the visibility/tab case (the
  spec-006 comment says it should be "resumed on re-attach" — verify that resume path exists for
  a sibling TabBar-driven content region).

## Regression test to add

A test in `Uno.Extensions.Navigation.UI.Tests` that: builds the TabBar+PanelVisibility+nested
shape, navigates to a non-default tab and back (assert page loads), simulates a metadata HR of
the **active** tab's view+model, then asserts navigate-away-and-back to that tab still loads the
page (i.e. the inner FrameNavigator runs).

## Open questions to answer while reproducing

- Default tab only, or any tab visible during HR?
- WASM only, or desktop too?
- Does `EnsureLoadedWhileHostAttached` complete or time out in this case?
- Is the inner `Frame` region present-but-unregistered, or is the `FrameView` itself
  recreated/renamed by HR?
