# Hot-Reload Refresh of the Active Route's View Model

**Status:** Implemented (branch `dev/mara/fix-routes-hr`)
**Issue:** [#3142](https://github.com/unoplatform/uno.extensions/issues/3142) — `[HR][Navigation]
CascadeNewDefaultsFromRoot suppresses the refresh of the active route, so the displayed page is
never re-instantiated` (surfaced by a downstream hosted-app consumer)
**Affects:** `Uno.Extensions.Navigation.UI`
**Files touched:**

- `src/Uno.Extensions.Navigation.UI/NavigationRouteUpdateHandler.cs`
- `src/Uno.Extensions.Navigation.UI/Navigators/ControlNavigator.cs`

## Problem

A C# hot-reload delta that updates the view model of the **currently displayed** route never
re-instantiates that view model, so edits that only run at construction time (constructor bodies,
property initializers) stay invisible until a full rebuild:

```csharp
public ISeries[] DeploymentSeries { get; } = new ISeries[]
{
    new ColumnSeries<double> { ... },  // HR edit -> LineSeries<double>: invisible on the active page
};
```

Why every existing HR path declines to handle it:

- `NavigationRouteUpdateHandler.UpdateApplication` rebuilds the route table and
  `ShouldCascadeForUpdatedTypes` correctly classifies the delta as navigation-relevant — but
  `CascadeNewDefaultsFromRoot` suppresses the `IsDefault` dispatch because the descendant region is
  *already* on that route (`FindActiveDescendantNestedRoute`). That suppression is intentional and
  correct for its own case (don't yank the user's selection back to the default) — but it is the
  only walk that could have touched the region, and it never receives `updatedTypes`.
- `NavigationFrameContentUpdateHandler` → `FrameNavigator.RehookCurrentViewAfterHotReload` only acts
  when Uno's element-update walk **replaced** `Frame.Content`. A metadata-only edit to a plain C#
  view-model class replaces no element, so the re-hook early-outs on `ReferenceEquals`.
- `RetryPendingFailedRequestsFromRoot` only re-issues *failed* requests; a region that navigated
  successfully has none.

Net effect: the one page guaranteed not to refresh is the page the user is looking at.

## Design

Two situations were sharing one branch; they are now handled by two **orthogonal walks** instead of
entangling `updatedTypes` into the cascade's conflict check:

| Situation | Correct action | Owner |
|---|---|---|
| Cascade wants a *different* default than the region's active route | suppress — don't move the user | `CascadeNewDefaultsFromRoot` (unchanged) |
| The active route's own **view model** was just updated | re-create that view model in place | `RefreshUpdatedActiveRoutesFromRoot` (new) |

`ScheduleCascade` now receives `updatedTypes` and, in the same dispatcher-deferred lambda as the
cascade and retry walks, runs a third walk:

1. `CollectUpdatedViewModels(updatedTypes, resolver)` maps each updated type to the view-model type
   the (just-rebuilt) route table associates with it, via `RouteResolver.FindByViewModel`. Going
   through the resolver — instead of comparing raw types — makes the MVUX case work:
   `MappedRouteResolver.InternalFindByViewModel` translates a model type (`OverviewModel`, which is
   what the delta contains) to its generated bindable view model (which is what
   `RouteInfo.ViewModel` holds).
2. `RefreshUpdatedActiveRoutesFromRoot` walks the live region tree; every region whose navigator's
   active `Route.Base` resolves (`FindByPath`) to a mapping whose `ViewModel` is in that set gets
   `ControlNavigator.RefreshActiveRouteViewModelAsync()` — a new internal entry point that re-runs
   `InitializeCurrentView(request, route, mapping, refresh: true)`, i.e. the standard
   `CreateViewModel` + `InjectServicesAndSetDataContextAsync` pipeline, for the route the navigator
   is already on. No navigation is issued, so the user's selection can't move.

### Why "re-create the view model" and not "re-navigate the route"

Re-issuing the same route is a no-op end to end: `FrameNavigator.NavigateForwardAsync` computes
0 forward segments for the route it is already on, and its HR bypass only fires when
`SourcePageType` differs from the mapping's `RenderView` — which it doesn't for an in-place (EnC)
update. The refresh entry point sidesteps the pipeline's already-there short-circuits and does
exactly the missing piece.

### Why view types do NOT trigger the refresh

Updated **views** are already owned by Uno's element-update walk (materialized instances) and
`FrameElementMetadataUpdateHandler` + `RehookCurrentViewAfterHotReload` (unmaterialized ones).
Triggering the view-model rebuild on a view-only delta would discard un-persisted view-model state
on every XAML/code-behind page edit — the exact state-preservation behavior
`Given_FrameContentRehook.When_XamlOnlyDelta_Then_CopiedViewModelIsPreserved` pins.

### Scope / known limitations

- A view model **replaced** via `CreateNewOnMetadataUpdate` (rude edit) whose registration
  delegate body was not itself updated resolves to the old type in the rebuilt route table and is
  not matched — same pre-existing limitation as the route table itself (#3072). The demonstrated
  scenario (EnC in-place constructor/initializer edit, type identity preserved) is fully covered.
- `updatedTypes == null` (unknown delta) refreshes nothing — conservative, mirrors the
  pre-existing x:Uid-revert guard on the cascade.

## Verification

- `Given_NavigationRouteUpdateHandler` (unit): `CollectUpdatedViewModels` for null / unregistered /
  registered-VM / registered-view-only / MVUX-mapped-model deltas.
- `Given_ActiveRouteVmRefresh` (UI, no HR harness — same technique as `Given_FrameContentRehook`):
  drives `ScheduleCascadeForAllContextsIfRouteRelevant` with a synthetic delta against a live
  navigation tree; asserts the VM is rebuilt in place (view instance untouched) for a VM delta and
  preserved for a view-only delta. Red on main, green with the fix.
- `Given_HotReload.When_ActiveRouteViewModelUpdated_Then_ActiveRegionVmReinstantiated` (runtime,
  real HR delta, secondary app): navigates a visibility region to a non-default route, edits a
  constructor-seeded property initializer on the active route's VM file, asserts the VM is
  re-instantiated with the post-HR seed AND the selection is not yanked back to the `IsDefault`
  route.
