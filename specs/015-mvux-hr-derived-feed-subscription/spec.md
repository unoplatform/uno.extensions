# Spec 015: MVUX hot-patch must re-key shared subscriptions before initializing new members

**Status:** Implemented
**Issue:** [#3226](https://github.com/unoplatform/uno.extensions/issues/3226) — `[HR][MVUX] After a Hot Reload edit that adds a feed to the model, commands update a model the UI no longer shows`
**Files touched:**

- `src/Uno.Extensions.Reactive.Generator/Bindables/ViewModelGenTool_3.cs` (and `_2`)
- `src/Uno.Extensions.Reactive/Presentation/Bindings/BindableViewModelBase.HotReload.cs`
- `src/Uno.Extensions.Reactive.Tests/IntegrationTests/Given_HotReload.cs`
- `src/Uno.Extensions.Reactive.UI.Tests/Given_HotReload.cs` + `MvuxHotReloadAddDerivedFeedModel.cs`

## Problem

`__Reactive_UpdateModel` (generated) ran `__Reactive_BindableInitializeForUpdatedModel` **before**
`__Reactive_TryPatchBindableProperties`. Initializing a member added by the delta from the updated
model — e.g. `IsGoalReached => Count.Select(...)` — subscribes to the updated model's `Count` state,
which registers a `FeedSubscription` keyed by that state in the shared `StateStore`.

`TryPatchBindablePropertyGeneric` then called `States.SetSubscription(updated, sharedSubscription)`
*before* `sharedSubscription.HotSwap(updated)`. `SetSubscription` throws when the key already holds a
different subscription; the per-property `catch` logged a warning and the hot-swap never ran. The
`Count` bindable kept enumerating the previous model's state while the rebuilt `Increment` command
updated the new one: "the command runs, `Count` stays 0".

The issue's "feeds are created only once (`??=`)" framing is not the cause — the `??=` is by design;
the shared subscription is what is meant to be re-pointed.

## Fix

1. **Generator**: patch first, then initialize. The shared subscriptions are re-keyed onto the updated
   model's feeds before anything initialized from that model can subscribe to them, so every consumer
   (bindables, derived feeds, feed-parameter commands) lands on the one subscription that later deltas
   will find via `GetOrCreateSubscription(previous)`.
2. **Runtime**: in `TryPatchBindablePropertyGeneric`, hot-swap (and cache) before `SetSubscription`, and
   downgrade a key conflict to a warning. The bindable follows the updated model for this delta even if
   something foreign already subscribed to the new feed; only the next delta's re-keying is affected.

## Verification

- `Given_HotReload.When_AddDerivedFeedInModel_Then_StateBindableFollowsUpdatedModel` (unit): red on
  `main` at the "bindable follows updated state" wait; green with the runtime half alone and with the
  generator half alone (each verified separately); both kept. It also drives the state to 10 to prove
  the added derived feed is live, then applies a second delta (`_v2`) to prove the shared subscription
  is still found on the next generation.
- `Given_HotReload.When_AddDerivedFeedToModelWithStateAndCommand_Then_CommandUpdatesBoundState`
  (runtime, real HR delta) — see `src/Uno.Extensions.Reactive.UI.Tests/HotReload.Spec.md` §4.13.
