# MVUX + Hot Reload Test Spec

Living spec for the MVUX hot-reload tests in
`Uno.Extensions.Reactive.UI.Tests.Given_HotReload`.
Tracks what scenarios we cover and harness constraints.

Related issue: https://github.com/unoplatform/uno.extensions/issues/2906

## 1. Goal

Prove that Hot Reload keeps working across the MVUX surface area:
Feed, ListFeed, State, and the source-generated ViewModels that wrap them.
The pre-existing Reactive HR test
(`When_UpdateMethodBody_Then_MetadataUpdateReceived`) only exercises a bare
static method body; MVUX layers source generation, bindable property wrappers,
and `SourceContext` lifecycle on top — each is a distinct failure surface.

Each scenario should:

1. Create a generated ViewModel, load it into the visual tree (required for
   `SourceContext`).
2. Verify the initial bound value.
3. Apply one or more source changes with `HotReloadHelper.UpdateSourceFile`.
4. Create a new ViewModel instance (or re-bind), and assert the change is
   reflected.

## 2. Harness & Constraints

- **`[RunsInSecondaryApp(ignoreIfNotSupported: true)]`**: Tests run in a hidden
  child process managed by the runtime test engine. The parent app window will
  appear idle during the run. Results are written to the output path.
- **`#if DEBUG` wrap**: HR is debug-only; the entire test class is wrapped.
- **`[RunsOnUIThread]`**: MVUX ViewModels need a `SourceContext` provided by
  the visual tree. All UI-touching tests must run on the UI thread.
- **`UIHelper.Load`**: Required to get the ViewModel into the visual tree so
  feeds activate and bindings resolve.
- **Separate edit-target files**: Each test scenario that edits a different
  source file must use its own dedicated file to avoid delta cache interference
  between tests. Name targets to match the scenario
  (`MvuxHotReloadTarget.cs`, `MvuxHotReloadFeedRemoveModel.cs`, etc.).
- **Timeouts set in `[TestInitialize]`**:
  `HotReloadHelper.DefaultWorkspaceTimeout = 300s` (Roslyn workspace load);
  `DefaultMetadataUpdateTimeout = 60s` (CI delta compile). Don't lower these.
- **File paths are relative to the runtime executable**: Use the pattern
  `"../../Uno.Extensions.Reactive.UI.Tests/<File>"` for `UpdateSourceFile`.

## 3. HR categories relevant to MVUX

| Category | What changes | Notes |
| --- | --- | --- |
| **C# method body** | Return value of a static method called by a Feed | Covered by baseline. Reliable across targets. |
| **Feed property remove/re-add** | Entire `IFeed<T>` property removed then restored | #2906 core scenario. Source generator must re-emit the ViewModel property. |
| **ListFeed property remove/re-add** | Entire `IListFeed<T>` property removed then restored | #2906 core scenario for collections. |
| **State property remove/re-add** | Entire `IState<T>` property removed then restored | Same pattern as Feed but with two-way binding surface. |
| **Feed source expression** | Lambda body inside `Feed.Async(...)` | Tests that the feed's async factory is re-evaluated post-HR. |
| **Multiple properties remove/re-add** | Several Feed/ListFeed/State properties removed and re-added together | Closer to real-world scenario (e.g. Chefs HomeModel). |

## 4. Scenario catalog

### 4.1 Baseline — method body update (implemented)

- **ID**: `When_UpdateMvuxFeedSource_Then_NewViewModelReflectsUpdate`
- **Goal**: Prove an MVUX Feed picks up a method body change via HR.
- **Model**: `MvuxHotReloadModel` with `IFeed<string> CurrentValue` calling
  `MvuxHotReloadTarget.GetValue()`.
- **HR change**: Flip `MvuxHotReloadTarget.GetValue()` from `"original"` to
  `"updated"`.
- **Assertion**: New ViewModel instance's bound TextBlock shows `"updated"`.
- **Status**: Passing.

### 4.2 Feed property remove/re-add (implemented)

- **ID**: `When_RemoveAndReAddFeedProperty_Then_BindingsWork`
- **Goal**: Prove that removing then re-adding a Feed property via HR
  correctly restores bindings (#2906).
- **Model**: `MvuxHotReloadFeedRemoveModel` with
  `IFeed<string> CurrentValue`.
- **HR change**: Two sequential deltas — remove the property line, then
  re-add it.
- **Assertion**: New ViewModel after re-add has a working `CurrentValue`
  binding showing `"hello"`.
- **Status**: Passing.

### 4.3 ListFeed property remove/re-add (implemented)

- **ID**: `When_RemoveAndReAddListFeedProperty_Then_BindingsWork`
- **Goal**: Same as 5.2 but for `IListFeed<string>` bound to a ListView
  (#2906).
- **Model**: `MvuxHotReloadListFeedRemoveModel` with
  `IListFeed<string> Items`.
- **HR change**: Two sequential deltas — remove, then re-add.
- **Assertion**: New ViewModel after re-add populates ListView with 3 items.
- **Status**: Passing.

### 4.4 State property remove/re-add (not yet implemented)

- **ID**: `When_RemoveAndReAddStateProperty_Then_TwoWayBindingWorks`
- **Goal**: Same pattern for `IState<T>` — verify two-way binding survives
  the remove/re-add cycle.
- **Risk**: State has write-back semantics via `UpdateAsync`; the generated
  ViewModel setter may not reconnect after re-add.

### 4.5 Multiple properties remove/re-add (not yet implemented)

- **ID**: `When_RemoveAndReAddMultipleProperties_Then_AllBindingsWork`
- **Goal**: Mirrors the real Chefs scenario from #2906 — multiple
  Feed/ListFeed/State properties removed and re-added in one HR cycle.
- **Risk**: Source generator diff may handle single-property changes
  differently from bulk changes.

### 4.6 Feed source expression update (not yet implemented)

- **ID**: `When_UpdateFeedAsyncLambda_Then_NewValueReflected`
- **Goal**: Change the lambda inside `Feed.Async(async ct => ...)` directly,
  rather than an external method body.
- **Risk**: The lambda may be captured in a compiler-generated closure class;
  HR of closure bodies has known limitations.

## 5. Organizational notes

- One test method per scenario.
- Each scenario's model/target files live in
  `src/Uno.Extensions.Reactive.UI.Tests/`.
- Name pattern: `MvuxHotReload<Scenario>Model.cs` for models,
  `MvuxHotReload<Scenario>Target.cs` for edit targets (when separate from the
  model).
- Reuse the `[TestInitialize] Setup()` method for timeout configuration.

## 6. Files

| File | Purpose |
| --- | --- |
| `Given_HotReload.cs` | Test class (shared with baseline reactive HR test) |
| `MvuxHotReloadTarget.cs` | Edit target for baseline Feed source update |
| `MvuxHotReloadModel.cs` | Model for baseline Feed source update |
| `MvuxHotReloadFeedRemoveModel.cs` | Model for Feed remove/re-add (#2906) |
| `MvuxHotReloadListFeedRemoveModel.cs` | Model for ListFeed remove/re-add (#2906) |
