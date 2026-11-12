# Spec 025: MVUX hot-patch must swap list feed properties too

**Status:** Implemented
**Issue:** [#3295](https://github.com/unoplatform/uno.extensions/issues/3295) (the access violation it reports is covered by spec 026)
**Files touched:**

- `src/Uno.Extensions.Reactive/Presentation/Bindings/BindableViewModelBase.HotReload.cs`
- `src/Uno.Extensions.Reactive.UI.Tests/Given_HotReload.cs` + `MvuxHotReloadCommandParametersModel.cs`

## Cause

`__Reactive_TryPatchBindableProperties` reads each property of the previous and updated models through
`GetAsFeed`, which only recognized `IFeed<>`. `IListFeed<T>` does not implement `IFeed<>`, so every
`IListFeed<T>` / `IListState<T>` property was skipped ("was and is not a feed property").

`ListState<T>.Empty(this)` and `ListState<T>.Value(this, ...)` are cached per model instance. After a
hot-patch, the commands are rebuilt against the updated model and write to its list state, while the list
bindable stays on the previous model's list state. The view never shows the items the command adds. This
happens on a live view model, and on a view model created after the delta, because it self-patches
(`HotReloadService.TryHotPatch`) onto a second instance of the updated model.

This predates the 7.4 baseline of the issue. It is not the regression the issue reports.

## Fix

`GetAsFeed` maps a list feed property to the feed of list its bindable reads, then the existing
`TryPatchBindablePropertyGeneric<IImmutableList<T>>` path swaps it like any state:

- a list state (`ListStateImpl<T>`) is read by its bindable through the state it is a view of
  (`Implementation`, cf. `FeedToListFeedAdapter`);
- any other list feed is read as `listFeed.AsFeed()`, cached per list feed (cf. `SourceContext.GetOrCreateListState`).

No feed tracking (spec 015, cause 2) is needed: list states are keyed by caller name and line, not by lambda.

## Verification

Runtime test `When_CommandGetsParametersBoundToStates_Then_NewViewModelUsesUpdatedCommand` hot reloads
`Add(CancellationToken)` into `Add(string, int, enum, DateOnly, CancellationToken)`, which is the edit in the issue.
Run locally on Windows (desktop head, retries off, see `specs/lessons.md`):

- red on `main`: the recreated view model's command completes without error, but its list stays empty;
- green with the fix; also green with `ListState.Value(this, () => ...)` instead of `ListState.Empty(this)`;
- all 16 `Reactive.WinUI.Tests.Given_HotReload` tests green.

Runtime test `When_UpdateListFeedSource_Then_LiveViewModelListUpdates` covers a list feed that is not a list state
(`ListFeed.Async(...)`): red without the list feed branch of `GetAsFeed` (the live list stays on the original items),
green with it; all 18 `Given_HotReload` tests green.

## The access violation of #3295

That crash is a different cause, worked around in spec 026 (dotnet/roslyn#86338).
