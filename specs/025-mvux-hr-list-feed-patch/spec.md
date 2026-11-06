# Spec 025: MVUX hot-patch must swap list feed properties too

**Status:** Implemented
**Issue:** [#3295](https://github.com/unoplatform/uno.extensions/issues/3295) (related; the access violation it reports is not reproduced, see below)
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

## Not resolved: the access violation of #3295

No run crashed (8 applications of the edit, default load context, secondary app process). The issue's host
loads the app into a collectible `AssemblyLoadContext` (7 crashes in 9 runs), which this lane does not cover.
A reflection dump of the view model after the delta shows what EnC did, which are leads for a runtime-level
fault, not a confirmed cause:

- the protected `ViewModel(Model)` ctor kept its token but its signature now reads `(Model#1)`;
- its closure class `<>c__DisplayClass10_0` is reused: field `model` is still declared as `Model` and holds a `Model#1`;
- the pre-edit async lambda `<.ctor>b__0` is mapped onto a different lambda, and struct-typed fields
  (`ValueTuple<...>`, `CancellationToken`) are EnC-added to its existing state machine.
