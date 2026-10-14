# Spec 015: MVUX hot-patch must swap the subscription the bindable is actually attached to

**Status:** Implemented
**Issue:** [#3226](https://github.com/unoplatform/uno.extensions/issues/3226) — `[HR][MVUX] After a Hot Reload edit that adds a feed to the model, commands update a model the UI no longer shows`
**Files touched:**

- `src/Uno.Extensions.Reactive.Generator/Bindables/ViewModelGenTool_3.cs` (and `_2`)
- `src/Uno.Extensions.Reactive/Presentation/Bindings/BindableViewModelBase.cs`
- `src/Uno.Extensions.Reactive/Presentation/Bindings/BindableViewModelBase.HotReload.cs`
- `src/Uno.Extensions.Reactive.Tests/IntegrationTests/Given_HotReload.cs`
- `src/Uno.Extensions.Reactive.UI.Tests/Given_HotReload.cs` + `MvuxHotReloadAddDerivedFeedModel.cs`

## How a bindable follows a hot-patch

A bindable property created by `Property(name, feed)` enumerates the shared `FeedSubscription` keyed by
the feed it was created from. `TryPatchBindablePropertyGeneric(previous, updated)` makes it follow the
updated model by hot-swapping **the subscription keyed by `previous`**, then re-keying it under
`updated` so the next delta finds it again. Both causes below break that chain.

## Cause 1: a derived feed added by the delta subscribes before the patch (live view model)

The generated `__Reactive_UpdateModel` ran `__Reactive_BindableInitializeForUpdatedModel` **before**
`__Reactive_TryPatchBindableProperties`. Initializing a member added by the delta, such as
`IsGoalReached => Count.Select(...)`, subscribes to the updated model's `Count` state, which registers a
subscription keyed by that state. `SetSubscription(updated, shared)` then threw on the key conflict, the
per-property `catch` logged a warning, and the swap, which ran after it, never happened.

## Cause 2: the patcher re-reads a different feed than the bindable holds (recreated view model)

This is the scenario in the issue: the page is recreated after the delta. Its view model is built by the
EnC-updated constructor, which then self-patches (`HotReloadService.TryHotPatch`). The constructor's
`model.Count` and the patcher's reflection read of `Count` on the original model type do not run the
same getter. Each getter has its own `() => 0` lambda, and `State.Value` (like `Feed.Async`) caches per
lambda, so the same model instance yields two different states. The patcher swapped the subscription of
a state no bindable used; the bound `Count` stayed on the original state while the rebuilt command
updated the new one. Observed in the runtime test with a reflection dump: the bindable's state had an
untouched subscription, while an unrelated state's subscription had been swapped to the model's state.

## Fix

1. **Generator**: patch first, then initialize, so the shared subscription is re-keyed onto the updated
   feed before anything initialized from the updated model can subscribe to it.
2. **Runtime, track the bindable's feed**: `Property(name, feed)` records the feed in
   `_propertyFeedsCache` (only when state hot reload is enabled), and the patcher prefers that tracked
   feed over a fresh read of the previous model.
3. **Runtime, swap before re-keying**: hot-swap before `SetSubscription`. On a key conflict, log a warning
   and keep tracking `previous`, which still resolves to the shared subscription for the next delta.

## Verification

- Unit (`Uno.Extensions.Reactive.Tests`, `IntegrationTests/Given_HotReload.cs`):
  - `When_AddDerivedFeedInModel_Then_StateBindableFollowsUpdatedModel` (cause 1): red on `main`; green with
    the generator reorder alone, with the runtime swap reorder alone, and with both. It also drives the
    state to 10 to prove the added feed is live, then applies a second delta.
  - `When_ViewModelCreatedAfterDelta_Then_CommandsUpdateBoundState` (cause 2): a model whose first and
    later `Count` reads return distinct cached states, reproducing the EnC getter divergence. Red without
    feed tracking, green with it.
- Runtime (`Uno.Extensions.Reactive.UI.Tests`, real HR delta):
  `When_AddDerivedFeedToModelWithStateAndCommand_Then_CommandUpdatesBoundState` covers the live view model
  and a view model created after the delta. Run locally on Windows (desktop head, retries off):
  red on `main` (`expected "2", actual "0"` on the recreated view model), red with only the cause 1 fixes,
  green with the full fix. See `specs/lessons.md` for how to run the HR lane on Windows.
