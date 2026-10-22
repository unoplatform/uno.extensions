# MVUX selection lost on refresh and paging (#3258)

## Findings

With `ListFeed.PaginatedAsync(...).Selection(state)` shown in a `FeedView` with a `ListView`, selecting a row in the `ListView` and then refreshing the feed (`FeedView.Refresh`) cleared `ListView.SelectedItem`, while the selection state kept the item. That item was in the reloaded page, and the `ListView` instance was the same. Loading the next page lost the selection the same way, refresh or not.

Two causes, one in the operator and one in the view binding. Fixing only the first makes the `ListView` write `None` into the state instead.

### 1. `ListFeedSelection` re-applied a stale state value

A selection made in the `ListView` is a transient update of the list state, which `StateImpl` drops on the next source message (`StateImpl.cs:167`), so on any refresh or page. After that, the list's selection comes from the operator's own update, which holds the selection state's value. The state-to-list sync skips changes that came from the list (`ListFeedSelection.cs:133`), so that update still held the earlier value (`None`) and was re-applied (`ListFeedSelection.cs:207`): the list's selection became empty while the state kept the item.

### 2. `SelectionFacet` pushed the `ListView`'s reset deselection to the state

On a collection `Reset`, `Selector.ResetIndexIfNeeded()` sets `SelectedIndex = -1`. That reached `SelectionFacet` (`SelectionFacet.cs:250`, `:259`) and `SelectionService.PushToSource` (`SelectionService.cs:96`) as a user deselection, writing `None` into the state. On `main` it was hidden because cause 1 had already emptied the selection; it showed as soon as something kept the selection across a reset (a fix for cause 1 alone, or an app re-selecting the row after reloads).

## Plan

- [x] `ListFeedSelection`: when the state change came from the list, update the operator's selection value without re-applying it, so the next source message re-applies the item by key and no extra message is emitted.
- [x] `CollectionChangedFacet`: `IsRaisingReset` while a `Reset` is raised.
- [x] `SelectionFacet`: while it is set, don't push the `ListView`'s selection change to the state; restore the view's selection from the selection service after the reset.
- [x] `BindableListFeed` and `MessageAxis`: push an unset selection to the view as empty, unless an operator unset it, so a paginated feed without `.Selection()` doesn't show a row the list state no longer has, and adding an item to a `.Selection()` list state keeps the selected row.
- [x] Unit tests for refresh and next page, asserting no `None` is written to the state.
- [x] Runtime tests: `FeedView` + `ListView` from XAML, selection made with `SelectedIndex`; refresh, next page, and two guards.

## Out of scope

- The Windows path where the `ListView` reads selection through `ISelectionInfo`: not verified. `SelectionFacet.DeselectRange` is not guarded during a reset.
- Grouped lists: the reset flag is in the shared raise wrapper, but no test covers them.
- If the source publishes a new list before the `ListView`'s selection reaches the state (it is pushed asynchronously), the previous state value can be re-applied once.
- #2946 (a selection set by a command on a `ListState` not reaching the UI) was not investigated.

## Review

- `Given_ListFeedSelection`: the 2 new tests fail on `main` (selection empty instead of the item) and pass with the fix; 41/41 over five runs.
- Reactive unit tests: 1492 passed, 0 failed, 19 skipped.
- `Given_FeedView_Selection` (Skia desktop, Win32): the 2 scenario tests fail on `main` with `ListView.SelectedItem=<none>, state=I-007`; with the fix, 4/4 over three runs. The 2 guards (`ListFeed` without `.Selection()` refreshed; item added to a `.Selection()` list state) pass before and after.
- Reactive runtime slice (`Uno.Extensions.Reactive.WinUI.Tests & !HotReload`): 19 of 23 pass. The 4 failures fail on `main` on the same machine: 3 ComboBox selection tests every time, and `When_PreselectedItem_SelectedItems_ListView`, flaky.
- `Uno.Extensions.Reactive.WinUI.csproj` Release builds for `net10.0` and `net10.0-windows10.0.19041`.
- The issue's repro app with the worktree's `Uno.Extensions.Reactive.dll` and `Uno.Extensions.Reactive.UI.dll`: `ListView.SelectedItem=Item 7; model Selected=Item 7` after refresh and after the next page, 5/5 runs.
