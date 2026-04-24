# Navigation + Hot Reload Test Spec

Living spec for the `Given_HotReload.cs` test class in `Uno.Extensions.Navigation.UI.Tests`.
Tracks what scenarios we want to cover, why each one is load-bearing, and the harness
constraints future tests must honor.

## 1. Goal

Prove that Uno Platform Hot Reload (C# + XAML) keeps working across the full surface area
of `Uno.Extensions.Navigation`. The Reactive HR test
(`Uno.Extensions.Reactive.UI.Tests.Given_HotReload.When_UpdateMethodBody_Then_MetadataUpdateReceived`)
only exercises a static target class; Navigation layers view caching, `FrameView` wrapping,
region lifecycle, selection-based regions, and dialog/flyout navigators on top — each is a
distinct failure surface for HR delta application.

Each scenario in this spec should:

1. Navigate into a specific navigation shape (Frame, Region, TabBar, etc.).
2. Apply a source change with `HotReloadHelper.UpdateSourceFile(...)` (or XAML equivalent
   once wired).
3. Trigger the code path that would surface a stale delta (re-navigate, back-navigate,
   re-show, re-select, rebind), and
4. Assert the new behavior is observed.

Missing any step yields a test that *looks* green but doesn't actually prove HR applied.

## 2. Harness & Constraints

These are non-negotiable — they were discovered by trial and are captured in the project
memory. New tests should reuse `SetupAppAsync` rather than re-derive them.

- **Secondary app window**: `new Window()` inside `[RunsInSecondaryApp]` produces an
  un-composited window whose `Loaded`/`Activate` events never fire, so initial navigation
  never runs (symptom: black secondary app). Always host in
  `UnitTestsUIContentHelper.CurrentTestWindow` and assign `window.Content = navigationRoot`.
  `SaveOriginalContent`/`RestoreOriginalContent` is the lifecycle.
- **Initial route must be explicit**: descending from `""` to an `IsDefault` leaf requires
  `Region.Children` to be populated (e.g. a nested `Region.Attached="True"` control). A bare
  root `ContentControl` has zero region children, and `Navigator.CoreNavigateAsync` returns
  early at that guard (`Navigator.cs:764`). Pass the target page name in `initialRoute:`.
  Same pattern as `Given_ChainedGetDataAsync.cs:74` and `Given_RouteNotifier.cs:56`.
- **Pages are wrapped in `FrameView`**: `ContentControlNavigator.Show`
  (`ContentControlNavigator.cs:42-54`) rewrites a `Page` view type to `FrameView` and
  returns a null path, so the root navigator's `Route` stays empty. The actual page lives
  at `((FrameView)root.Content).NavigationFrame.Content`, and the Frame's navigator
  (`FrameView.Navigator`) is what holds the current route. Use
  `WaitForFrameNavigatorAsync` + `ResolveCurrentPage<T>` helpers in `Given_HotReload.cs`.
- **Wait helpers, not `UIHelper.WaitFor`**: `UIHelper.DefaultTimeout` is 1 s without a
  debugger attached (`UIHelper.cs`). Use the `WaitForRouteAsync` helper with an explicit
  30 s timeout for navigation-state polls.
- **Timeouts set in `[TestInitialize]`**: `HotReloadHelper.DefaultWorkspaceTimeout = 300s`
  (Roslyn workspace load on a large solution is slow); `DefaultMetadataUpdateTimeout = 60s`
  (CI delta compile). Don't lower these.
- **`#if DEBUG` wrap**: HR is a debug-only runtime concern; release builds won't have the
  HR agent at all.
- **`ForceAssemblyLoading()` must reference both HR test classes by fully qualified name**
  in `App.xaml.cs` to disambiguate `Given_HotReload` between Reactive and Navigation
  namespaces. Add new types here too if they're only referenced via reflection.
- **Target files live alongside the test assembly**: `HotReloadHelper.UpdateSourceFile`
  takes a path relative to the running executable's working directory. Keep edit-target
  files (like `HotReloadTarget.cs`) in this project and reference with
  `"../../Uno.Extensions.Navigation.UI.Tests/<File>"` to match the Reactive pattern.

### 2.1 `SetupAppAsync` helper

`Given_HotReload.SetupAppAsync` boots an Uno host, mounts the navigation root in the test
window, runs `InitializeNavigationAsync`, and waits for both the `FrameView` navigator and
the requested initial route to land. It returns an `IAsyncDisposable` (`HotReloadTestApp`)
exposing `NavigationRoot` and `FrameNavigator`; disposal stops the host and restores
original window content (including when setup itself throws). New scenarios should
consume it:

```csharp
await using var app = await SetupAppAsync(
    registerViewsAndRoutes: (views, routes) => { /* scenario-specific */ },
    initialRoute: "StartingPage",
    ct);
```

If a scenario needs a root other than a bare `ContentControl` (e.g. a `Grid` with
`Region.Attached` children for region-based navigation), generalize `SetupAppAsync` to take
a `Func<FrameworkElement>` factory rather than forking the helper.

## 3. Test-host run command (Skia desktop)

```bash
# Build
"/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" \
  src/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests.csproj \
  -p:Configuration=Debug -p:UnoTargetFrameworkOverride=net9.0-desktop -restore

# Run (filter to a single test)
cd "src/Uno.Extensions.RuntimeTests/Uno.Extensions.RuntimeTests/bin/Uno.Extensions.RuntimeTests/Debug/net9.0-desktop"
UNO_RUNTIME_TESTS_RUN_TESTS='{"Filter":{"Value":"Given_HotReload.When_..."}}' \
UNO_RUNTIME_TESTS_OUTPUT_PATH='C:/temp/hr-result.xml' \
DOTNET_MODIFIABLE_ASSEMBLIES=debug \
dotnet Uno.Extensions.RuntimeTests.dll
```

`dotnet build` does **not** work for these WinUI projects (`UNOB0008`) — must use msbuild.
For Windows TFM, substitute `-p:UnoTargetFrameworkOverride=net9.0-windows10.0.19041`.

## 4. Hot-Reload categories we care about

Every scenario below should pick at least one of these change categories. The harder ones
(XAML, cached instances) are where HR most plausibly diverges from the happy path.

| Category | What changes | Notes |
| --- | --- | --- |
| **C# method body** | Return value / logic of an existing method, no signature change | Covered by baseline (Tool 1). Reliable across all targets per HR support matrix. |
| **C# field initializer** | Instance field default value | Applied on *new* instances only; existing instances keep the old value. Useful to prove view caching reuses instances. |
| **XAML DataTemplate** | Template body used by `ItemsRepeater`/`ListView`/etc. inside a nav-hosted page | XAML HR targets the in-memory tree; the Frame's cached page content may or may not re-apply it. |
| **XAML Style / Resource** | `App.xaml` resource, or a resource dictionary referenced by the page | Tests that HR cascades into nav-hosted pages even when they're not the visual root. |
| **`x:Bind` expression** | A simple bound property reference | x:Bind HR is limited to simple expressions and events; worth a scenario to prove the Frame's binding context isn't broken by wrapping. |
| **C# Markup `CSharpMarkup`** | Markup built programmatically | Same surface as XAML for HR purposes; skip unless we adopt C# Markup elsewhere. |

Changes *not* supported by HR (new members, signature changes, adding XAML files) are out
of scope — asserting they fail is someone else's test.

## 5. Scenario catalog

Each scenario block is: **ID**, **Goal**, **Layout**, **HR change**, **Trigger**,
**Assertion**, **Risk** (why this could plausibly break). `ID` becomes the test method
name: `When_<trigger>_Then_<assertion>`.

### 5.1 Baseline (implemented)

- **ID**: `When_NavigateAfterSourceUpdate_Then_NewPageReflectsUpdate`
- **Goal**: Hello-world. Prove the harness works end-to-end before adding complexity.
- **Layout**: Root `ContentControl` hosting a `FrameView`. Two pages
  (`HotReloadPageOne`, `HotReloadPageTwo`), each reading from a shared static
  `HotReloadTarget.GetValue()` in their constructor.
- **HR change**: Flip `HotReloadTarget.GetValue` body from `return "original"` to
  `return "updated"`.
- **Trigger**: Navigate to `HotReloadPageTwo` after the delta applies.
- **Assertion**: `HotReloadPageTwo.DisplayedValue == "updated"`.
- **Risk**: None beyond the harness itself — if this fails the infra is broken.
- **Status (2026-04-21)**: Passing end-to-end on Skia desktop. Previous metadata-update
  timeout no longer reproduces — dev-server log shows `Done applying IL Delta` and
  `Received a metadata update, continuing test`.

### 5.2 Route / ViewMap plumbing

- **ID**: `When_UpdateViewModel_Then_ReNavigationReflectsVmChange`
- **Goal**: HR of a `ViewModel` method body bound to a page via `ViewMap<Page, VM>()`.
- **Layout**: `ViewMap<HotReloadVmPage, HotReloadVm>()` + a `RouteMap` entry. The page
  reads its VM via `DataContext` (set by `InjectServicesAndSetDataContextAsync` in
  `FrameworkElementExtensions`); the VM's `DisplayedValue` is a property whose getter calls
  the HR'd helper method `GetDisplayedValue()`.
- **HR change**: Helper method in the VM returns a different value.
- **Trigger**: Navigate to sibling `HotReloadPageTwo` and back to `HotReloadVmPage`.
- **Assertion**: Bound value updates.
- **Risk**: ViewModels are registered by type via DI (`ViewMap.RegisterTypes` uses
  `AddTransient`); verify the scoped instance built post-HR picks up the updated code.
  Singleton-scoped VMs won't. Also: forward-navigating to a route already on the back
  stack did not trigger forward nav in `FrameNavigator` — use `NavigateBackAsync(this)`
  to return to the prior page.
- **Status (2026-04-21)**: Implemented and passing end-to-end on Skia desktop.

- **ID**: `When_UpdateRouteInitGate_Then_GatedRouteBecomesNavigable`
- **Goal**: HR on a method the `RouteMap.Init` delegate targets — simulates "a new route
  becoming navigable after HR". True `IRouteRegistry` mutation is not achievable via HR
  because `RouteResolver` snapshots `IRouteRegistry.Items` at construction
  (`RouteResolver.cs:40` — `Mappings.AddRange(maps.Flatten())`) and `edit-and-continue`
  cannot add new types. The gated-Init pattern is the closest honest analogue.
- **Layout**: Two pre-registered routes — `HotReloadPageOne` (default) and `"NewPage"`
  pointing at `HotReloadPageTwo`. The `"NewPage"` `RouteMap` has an `Init` delegate that
  calls `HotReloadRouteGate.IsAvailable()`; when false it rewrites the request to
  `request with { Route = request.Route with { Base = "HotReloadPageOne" } }`, when true
  it returns the request unchanged. `Navigator.cs:548-560` re-resolves the request after
  Init returns a different `Route.Base`.
- **HR change**: Flip `HotReloadRouteGate.IsAvailable()` from `return false` to
  `return true`.
- **Trigger**: `navigator.NavigateRouteAsync(this, "NewPage")` both before and after the
  HR delta.
- **Assertion**: Pre-HR — current page is **not** `HotReloadPageTwo` (Init redirected).
  Post-HR — current page **is** `HotReloadPageTwo` and the frame's `Route.Base == "NewPage"`.
- **Risk**: Init-redirect landing on the same route as the current page could be
  short-circuited to a no-op by the navigator; redirect target was chosen to be the
  initial route so the "stayed put" state is consistent either way. The test asserts on
  page type (`HotReloadPageTwo` absent/present) rather than on redirect semantics.
- **Status (2026-04-21)**: Implemented and passing end-to-end on Skia desktop.

- **ID**: `When_UpdateDataViewMapCtor_Then_ReNavigationUsesNewCtor`
- **Goal**: HR of a ctor body on a `DataViewMap<Page, VM, TData>`-bound VM.
- **Layout**: `DataViewMap<HotReloadDataPage, HotReloadDataVm, Entity>()`.
- **HR change**: The VM ctor transforms the injected `Entity` differently post-HR.
- **Trigger**: `NavigateDataAsync(this, entity)` twice.
- **Risk**: Navigation's data-injection path constructs the VM via the service provider;
  make sure HR applies to the delta'd ctor and not a cached compiled delegate.

### 5.3 Frame navigation

- **ID**: `When_UpdateThenBackNavigate_Then_BackPageReflectsUpdate`
- **Goal**: Validate back-stack handling. Frames keep previously-visited page instances
  alive (or re-materialize them depending on cache mode).
- **Layout**: Same as baseline but navigate A → B, apply HR, then back to A.
- **HR change**: Method body on page A.
- **Trigger**: `frameNav.NavigateBackAsync(this)`.
- **Assertion**: Page A either reflects the update (if the frame re-instantiates it) OR
  we document the Frame cache behavior in the test assertion. Answering this is the
  point.
- **Risk**: If Frame uses `NavigationCacheMode.Enabled`, the existing instance is reused
  and C# field-init changes won't apply. This is the canonical HR caveat; the test
  pins down the behavior.

- **ID**: `When_UpdateDuringForwardNavigationInFlight_Then_Completes`
- **Goal**: Race: apply HR while a navigation is mid-flight.
- **Risk**: Low priority; documents that mid-flight navigations don't crash when a
  metadata-update lands.

### 5.4 Region-based navigation (Panel, Grid, Stack)

Selection-based and visibility-based regions use `Region.Attached="True"` on a `Panel`.
Each child `Region.Name` is a selectable target. See docs:
*Reference / Navigation / Regions* and *Walkthrough / UsePanel*.

- **ID**: `When_SwitchRegionAfterUpdate_Then_NewlyShownRegionReflectsUpdate`
- **Goal**: HR on a sub-view selected via `Region.Name`.
- **Layout**: `HotReloadRegionPage` with nested `ContentGrid` (Region.Attached + Region.Navigator="Visibility"). `RegionOne` (IsDefault) and `RegionTwo` both map to `HotReloadRegionContentPage` with `HotReloadRegionVm` whose `DisplayedValue` property reads `HotReloadRegionTarget.GetValue()` on every access.
- **HR change**: `HotReloadRegionTarget.GetValue()` flip from "original" → "updated".
- **Trigger**: Switch regions via the ContentGrid's own `PanelVisiblityNavigator` (do NOT use the outer FrameNavigator — it rejects nested region routes at RegionCanNavigate and the bubble-up hangs the await).
- **Assertion**: After HR and re-show, RegionTwo's VM reflects "updated".
- **Risk**: PanelVisiblityNavigator reuses existing FrameViews, so the same VM instance is re-visible post-HR. The VM's property getter re-reads the HR'd method on each access — that's what makes the assertion work. If the VM cached the method return value, HR wouldn't be visible without re-instantiation.
- **Status (2026-04-22)**: Implemented and passing end-to-end on Skia desktop.

- **ID**: `When_UpdateStackPanelChildAfterHR_Then_NewlyShownChildReflectsUpdate`
- **Goal**: Same as above but with a `StackPanel` instead of `Grid`.
- **Risk**: `StackPanel` doesn't have rows/columns but the region mechanism is identical
  — mostly a smoke test that panel type doesn't matter.

- **ID**: `When_DescentFromEmptyRoot_Then_DefaultRegionChildLoadsAfterUpdate`
- **Goal**: Cover the `""` → `IsDefault` descent path that the baseline had to side-step.
- **Layout**: Root `ContentControl` with a nested `Region.Attached="True"` `ContentControl`
  (so `Region.Children` is populated and `CoreNavigateAsync` proceeds past the guard at
  `Navigator.cs:764`).
- **HR change**: Target method body read by the default leaf page.
- **Trigger**: Start with `initialRoute: ""`, verify descent; re-navigate.
- **Risk**: This path is subtly different from the baseline's explicit initial route and
  exercises the nested-region discovery code.

### 5.5 Tab navigation (`TabBar`)

Uses Uno Toolkit's `TabBar` + `TabBarItem` with `Region.Name` on each item. See docs:
*Walkthrough / UseTabBar* and *Chefs / TabBar Navigation*.

- **ID**: `When_SwitchTabAfterUpdate_Then_SelectedTabReflectsUpdate`
- **Goal**: Prove HR applies to tab content on first view after selection.
- **Layout**: `TabBar` with two `TabBarItem`s; each item's content is a distinct
  page/control. `uen:Region.Name` identifies them.
- **HR change**: Target read by the second tab's content's ctor.
- **Trigger**: Click/select tab 2 (`NavigateRouteAsync(this, "Tab2")`).
- **Assertion**: Tab 2's bound value reflects the update.
- **Risk**: TabBar may eagerly materialize tab content at startup — if so, HR won't pick
  up field-init changes. Use a method body change.

- **ID**: `When_UpdateWhileOnTabTwo_Then_SwitchBackToTabOneReflectsUpdate`
- **Goal**: Complementary direction to catch eager-instantiation caching.
- **Risk**: Same as above.

### 5.6 `NavigationView`

- **ID**: `When_SwitchNavMenuAfterUpdate_Then_SelectedItemReflectsUpdate`
- **Goal**: `NavigationView` is the third selection-based region control and behaves
  similarly to `TabBar` for HR purposes, but uses `NavigationViewItem`.
- **Layout**: `muxc:NavigationView` with `MenuItems` each tagged with `uen:Region.Name`.
  See `Walkthrough / UseNavigationView.md`.
- **HR change**: Method body on the control rendered by the second menu item.
- **Risk**: `SettingsItem` is auto-generated — its region name has to be set in code on
  `Loaded`. Scope keeps this test to `MenuItems`.

### 5.7 Dialogs (Flyout and Modal)

See docs: *HowTo / ShowDialog*. The view *type* decides dialog vs flyout:
`ContentDialog` → Modal, `Page` → Flyout. Both take the `!` qualifier / `Qualifiers.Dialog`.

- **ID**: `When_ShowFlyoutAfterUpdate_Then_FlyoutReflectsUpdateAndFrameStaysStable`
- **Goal**: HR on a `Flyout`-typed view, proving both (a) re-shown flyouts reflect the
  update and (b) the flyout lifecycle does not disturb the underlying Frame's navigation.
- **Layout**: `HotReloadPageOne` (uses `HotReloadTarget`) is the Frame's underlying page.
  `HotReloadFlyoutView : Flyout` is registered via `ViewMap<HotReloadFlyoutView>()` and a
  sibling `RouteMap("HotReloadFlyoutView", ...)`. The flyout reads
  `HotReloadFlyoutTarget.GetValue()` in its ctor; a static `HotReloadFlyoutView.Current`
  lets the test reach the live flyout without walking the popup layer.
- **HR change**: Flip `HotReloadFlyoutTarget.GetValue()` from `"original"` to `"updated"`.
- **Trigger**: `FrameNavigator.NavigateRouteAsync(this, "!HotReloadFlyoutView")`. The `!`
  qualifier + `IsDialogViewType` auto-detection (Flyout subclass) routes the request
  through `FlyoutNavigator`, which builds a fresh Flyout via `CreateInstance` and calls
  `ShowAt(placementTarget)` (Region.View / window content when the sender isn't a
  `FrameworkElement`). Close each show with `flyout.Hide()`; the `Closed` event clears
  `Current`.
- **Assertion**:
  - Pre-HR flyout's `DisplayedValue == "original"`.
  - After close: `FrameNavigator.Route.Base == "HotReloadPageOne"` and the underlying
    `HotReloadPageOne` instance is the same (by reference) as before the flyout opened.
  - Post-HR flyout is a **new** instance and its `DisplayedValue == "updated"`.
  - Across the full cycle: underlying page's `DisplayedValue` stays `"original"` and
    `FrameNavigator.Route.Base` stays pinned to `HotReloadPageOne`. HR on the flyout's
    target does not bleed into the underlying page or the Frame.
- **Risk**: Flyouts are reparented into a popup surface, not the main visual root. For C#
  HR on the Flyout subclass ctor this isn't an issue (each nav builds a new instance);
  for XAML HR on popup content it would be. `FlyoutNavigator.ExecuteRequestAsync` returns
  `route with { Path = null }` for non-injected flyout types so the Frame's route is
  untouched — but `FlyoutNavigator.Flyout_Closed` calls `NavigateBackAsync` on the
  scoped dialog region (not the Frame's region), so the outer Frame stays put.
- **Status (2026-04-23)**: Implemented and passing end-to-end on Skia desktop.

- **ID**: `When_ShowModalAfterUpdate_Then_ModalReflectsUpdateAndFrameStaysStable`
- **Goal**: HR on a modal (`ContentDialog`-typed) view, and prove the modal lifecycle
  doesn't disturb the Frame's underlying navigation.
- **Layout**: `HotReloadPageOne` is the Frame's underlying page (uses `HotReloadTarget`).
  `HotReloadModalDialog : ContentDialog` is registered via `ViewMap<HotReloadModalDialog>()`
  and a sibling `RouteMap("HotReloadModalDialog", ...)`. The modal reads
  `HotReloadModalTarget.GetValue()` in its ctor; a static `HotReloadModalDialog.Current`
  lets the test reach the live dialog without walking the popup layer.
- **HR change**: Flip `HotReloadModalTarget.GetValue()` from `"original"` to `"updated"`.
- **Trigger**: `FrameNavigator.NavigateRouteAsync(this, "!HotReloadModalDialog")`. The `!`
  qualifier routes through `ContentDialogNavigator` which builds a fresh dialog via
  `CreateInstance` and calls `ShowAsync` on it. Close each show with `dialog.Hide()`; the
  `Closed` event clears `Current`.
- **Assertion**:
  - Pre-HR modal's `DisplayedValue == "original"`.
  - After close: `FrameNavigator.Route.Base == "HotReloadPageOne"` and underlying
    `HotReloadPageOne` instance is the same (by reference) as before the modal opened.
  - Post-HR modal is a **new** instance and its `DisplayedValue == "updated"`.
  - Across the full cycle: the underlying page's `DisplayedValue` stays `"original"` and
    `FrameNavigator.Route.Base` stays pinned to `HotReloadPageOne`. HR on the modal's
    target does not bleed into the underlying page or the Frame.
- **Risk**: `ContentDialog` has its own lifecycle via `ShowAsync` (not a Frame push) — the
  navigator wraps this, so we want to prove that wrapper (a) picks up the HR delta on each
  new `CreateInstance` and (b) doesn't leak into the Frame's route/back-stack state.
  `ContentDialogNavigator.DisplayDialog` sets `dialog.XamlRoot = Window.Content!.XamlRoot`,
  which is satisfied by `UnitTestsUIContentHelper.CurrentTestWindow`; no extra harness work.
- **Status (2026-04-22)**: Implemented and passing end-to-end on Skia desktop.

### 5.8 Advanced / stretch scenarios

Lower priority; add once the core matrix above is green.

- **ID**: `When_XamlDataTemplateUpdated_Then_BoundListReflectsUpdate`
  XAML HR on a `DataTemplate` referenced by a nav-hosted list. Requires wiring up
  `HotReloadHelper` for `.xaml` edits (today we only edit `.cs`).
- **ID**: `When_ResourceDictionaryUpdated_Then_NavHostedPageReflectsNewStyle`
  XAML HR on `App.xaml` resources — does it cascade into a Frame-hosted page?
- **ID**: `When_DeepLinkAfterUpdate_Then_TargetPageReflectsUpdate`
  Deep-linking with query string + `FromQuery` conversion. HR the `FromQuery` delegate
  body. Verifies the nav pipeline doesn't cache compiled delegates.
- **ID**: `When_XBindUpdated_Then_BoundValueReflectsUpdate`
  `x:Bind` expression HR — limited to simple expressions per the support matrix, but
  worth one test to prove binding survives the `FrameView` wrap.
- **ID**: `When_NavigatorScopedServiceReplaced_Then_NewScopePicksItUp`
  HR on a service registered with `AddScopedInstance<T>` — does a new navigation scope
  resolve the updated type? Document behavior rather than enforce one.

## 6. Organizational notes

- One test method per scenario. Don't combine multiple HR edits into one test — a
  failure should point at exactly one surface.
- Keep each scenario's page/VM types as small, single-purpose classes in
  `Pages/` or a new `Scenarios/` subfolder if the count grows.
- Reuse `HotReloadTarget.cs` where a shared edit target is fine; fork it (e.g.
  `HotReloadTabTarget.cs`) when two concurrent scenarios would edit the same file and
  race.
- If a scenario depends on an edit target only used by that test, name the target to
  match the scenario (`HotReloadDialogTarget.cs`) so HR-file-churn doesn't bleed across
  tests.

## 7. Open questions

- **Dev-server workspace scope**: baseline currently fails at metadata-update delivery on
  Skia desktop. Unclear whether the dev-server's Roslyn workspace includes
  `Uno.Extensions.Navigation.UI.Tests`. If it only loads the host project and its direct
  references, HR edits to files in the tests project won't be processed. Investigate:
  compare `[DEV_SERVER]` log output between the working Reactive HR test and our nav
  test; check whether the tests project is referenced by `Uno.Extensions.RuntimeTests`
  (the HR host).
- **View caching defaults**: which navigators enable page caching by default
  (`ContentControlNavigator`, `FrameNavigator`, `TabBarNavigator`)? The answer shapes
  whether field-init changes are ever visible without a re-instantiation. Document per
  scenario.
- **XAML HR target-file path**: `HotReloadHelper.UpdateSourceFile` for `.xaml` files —
  confirm the mechanism before writing the first XAML-change scenario.
