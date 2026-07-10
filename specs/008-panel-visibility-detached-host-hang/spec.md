# 008 — PanelVisiblityNavigator hangs on detached host; NavigationView container crash swallows default-tab clicks

## Symptom (as reported)

In a hosted-preview environment (an ALC-loaded app whose visual tree is re-grafted into the
host's chrome during bootstrap), a shell app with a NavigationView/TabBar + visibility content
region starts on its `IsDefault` tab. Navigating to the other tabs works, but selecting the
default tab again does nothing: the selection indicator moves, the content region never
switches back, and no `Uno.Extensions.Navigation` log line is emitted for the attempt.

Field evidence (host-side feedback bundle): after the app binary reloads, the initial
default-route navigation never completes (no leaf `CoreNavigateAsync` log for the default
route), four `NavigationRegion.AssignParent - Unable to find service provider for root
navigator` warnings fire, and every subsequent navigation is logged EXCEPT requests targeting
the default route, which vanish without a trace while the NavigationView shows the default
item as selected.

## Two distinct defects

### Defect A (this repo — fixed): PanelVisiblityNavigator's non-host-aware load wait

`PanelVisiblityNavigator.CheckLoadedAsync` awaited `CurrentlyVisibleControl.EnsureLoaded()`
with no timeout and no host-attachment awareness. When the hosting tree detaches while the
initial navigation is cascading into the default tab's FrameView (the spec 006 re-graft seam,
one level deeper), the FrameView can never load and the whole navigation pipeline dangles
forever — `PostNavigateAsync` (the visibility flip) never runs, the route never completes, and
`InitializeNavigationAsync`'s task never resolves.

Spec 006 made `FrameNavigator` and `ContentControlNavigator` use the host-aware
`EnsureLoadedWhileHostAttached(Region.View)`; `PanelVisiblityNavigator` was missed.

**Fix**: `PanelVisiblityNavigator.CheckLoadedAsync` now uses
`EnsureLoadedWhileHostAttached(Region.View)`. When the panel leaves the tree the wait gives
up, the undeliverable part of the request is parked by the child-forwarding stage (spec 006)
and resumed when the region re-attaches.

**Red/green test**:
`Given_NavViewShellNavigation.When_TreeDetachedDuringStartup_Then_InitialNavigationDoesNotHang`
— red before the fix (engine-level 5-minute timeout → restructured into a 15s completion
assertion), green after.

### Defect B (Uno.UI — pinpointed, NOT fixed here): NavigationView stale-container crash

With Defect A fixed, the park/resume machinery correctly delivers the default tab after a
re-graft (`wait-home-initial` phase passes). The remaining failure is inside **Uno.UI's
NavigationView**:

```text
System.ArgumentOutOfRangeException: Index was out of range.
   at System.Collections.Generic.List`1.get_Item(Int32 index)
   at Microsoft.UI.Xaml.Controls.NavigationView.GetIndexPathForContainer(NavigationViewItemBase nvib)
   at Microsoft.UI.Xaml.Controls.NavigationView.GetRecommendedTransitionDirection(DependencyObject prev, DependencyObject next)
   at Microsoft.UI.Xaml.Controls.NavigationView.ChangeSelection(Object prevItem, Object nextItem)
   at Microsoft.UI.Xaml.Controls.NavigationView.OnSelectedItemPropertyChanged(...)
```

The first selection change that involves an item container realized BEFORE the re-graft
crashes: `ItemsRepeater.GetElementIndex(container)` returns `-1` for the stale container and
`GetIndexPathForContainer` indexes `m_topDataProvider.GetPrimaryItems()[-1]` (and the
`path.Insert(0, parentIR.GetElementIndex(...))` variants) without a bounds check
(`src/Uno.UI/UI/Xaml/Controls/NavigationView/NavigationView.cs`, `GetIndexPathForContainer`).

Because the crash fires inside `OnSelectedItemPropertyChanged` BEFORE `SelectionChanged` is
raised, `SelectorNavigator` is never notified: **the user's click produces no navigation and
no log**, while the `SelectedItem` DP (already set) moves the selection indicator. Only
transitions involving the pre-re-graft container (i.e. the default tab's) crash — clicks on
tabs whose containers were realized after the re-graft work normally. This asymmetry matches
the field symptom exactly.

A Nav-library-side workaround was considered (also subscribing `NavigationView.ItemInvoked`,
which fires before the selection is applied) but rejected here: it would only cover physical
clicks (not programmatic selection), double-fires on healthy clicks unless deduped, and papers
over a control defect. The control needs a `containerIndex >= 0` guard.

**Reproducing tests (intentionally red until the Uno.UI fix ships)**:
- `Given_NavViewShellNavigation.When_TreeRegraftDuringStartup_Then_DefaultTabStillReachable`
- `Given_NavViewShellNavigation.When_ContentMovedToNewHostDuringStartup_Then_DefaultTabStillReachable`

Both fail with `Uno.UI NavigationView container crash during phase 'select-menu'`.

## Non-regression coverage added

- `When_NestedShell_SelectOtherThenDefault_Then_DefaultContentShown` — scaffolded-shell layout
  (content grid nested inside `NavigationView.Content`), select-away/select-default via the
  control (the user-gesture path, previously untested — existing tests drove tab
  FrameNavigators programmatically).
- `When_NestedShell_CycleAllTabsThenDefault_Then_DefaultContentShown` — full tab cycle.
- `When_SiblingShell_SelectOtherThenDefault_Then_DefaultContentShown` — sibling layout.

All three pass before and after the Defect A fix.

## Verification notes

- Full `!_HotReload` runtime suite, with-fix vs without-fix (same machine, net9.0-desktop):
  the ONLY failures unique to the with-fix run are the two intentionally-red Defect B tests.
  All other failures (ComboBox mouse-injection tests, the `Given_ChainedGetDataAsync` suite,
  `When_DefaultRouteConfigured_Then_NavigationSucceeds`, `When_NavigateBack_Then_RouteChanged_Has_Route`,
  and notably spec 006's `When_ContentDetachedDuringInitialNavigation_Then_RouteResumesOnReattach`)
  fail identically WITHOUT the fix — pre-existing local/suite-context issues, not regressions.
- **Seam determinism caveat**: the detach seam (hook the default tab's inner `Frame.Loaded`,
  detach `Window.Content`) is deterministic when the test class runs in isolation (hang test
  red twice consecutively pre-fix, green post-fix) but can land after the cascade completes
  when the full suite runs first, making the seam tests pass vacuously. Spec 006's own test
  shows the same suite-context sensitivity. Follow-up: gate the assertions on a "detach landed
  mid-cascade" precondition (initTask incomplete at detach time) or drive the seam from a
  page-construction hook.

## Related

- spec 004 (selector null-Show phantom pending), spec 006 (park/resume dropped navigation).
- Host-side history: the consuming preview host closed its own workaround PR for the
  AssignParent/sentinel race unmerged; `NavigationRegion.AssignParent` still gives up
  permanently when `FindServiceProvider()` fails (no retry when the provider becomes
  available later). Not addressed in this spec; noted for follow-up.
