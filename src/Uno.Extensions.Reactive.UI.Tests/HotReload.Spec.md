# MVUX + Hot Reload Test Spec

Living spec for the MVUX hot-reload tests in
`Uno.Extensions.Reactive.WinUI.Tests.Given_HotReload`.
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
| **Multiple properties remove/re-add** | Several Feed/ListFeed/State properties removed and re-added together | Closer to real-world scenario (e.g. Chefs HomeModel). |
| **Feed↔State conversion** | Property type changes between `IFeed<T>` and `IState<T>` | Source generator must regenerate the ViewModel with the new property type. |
| **Feed/State syntax round-trip** | Multiple type/syntax changes across sequential HR deltas | Validates that repeated conversions don't corrupt generator state. |
| **State value update** | Lambda body value change inside a State property | Tests whether MVUX re-evaluates after a value-only HR change. |
| **AsyncEnumerable Feed** | `Feed.AsyncEnumerable` syntax with remove/re-add | Validates HR with a different Feed factory method. |
| **Feed.Async↔AsyncEnumerable syntax** | Switch between `Feed.Async` and `Feed.AsyncEnumerable` | Validates syntax change across different Feed factory methods. |

## 4. Scenario catalog

### 4.1 Baseline — method body update (implemented)

- **ID**: `When_UpdateMvuxFeedSource_Then_NewViewModelReflectsUpdate`
- **Goal**: Prove an MVUX Feed picks up a method body change via HR.
- **Model**: `MvuxHotReloadModel` with `IFeed<string> CurrentValue` calling
  `MvuxHotReloadTarget.GetValue()`.
- **HR change**: Flip `MvuxHotReloadTarget.GetValue()` from `"original"` to
  `"updated"`.
- **Assertion**: Original ViewModel's bound TextBlock updates to `"updated"`;
  new ViewModel instance also shows `"updated"`.
- **Status**: Failing — MVUX feeds don't re-evaluate when a called method's
  body changes via HR. The Feed lambda itself is unchanged so the pipeline has
  no signal to refresh.

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
- **Goal**: Same as 4.2 but for `IListFeed<string>` bound to a ListView
  (#2906).
- **Model**: `MvuxHotReloadListFeedRemoveModel` with
  `IListFeed<string> Items`.
- **HR change**: Two sequential deltas — remove, then re-add.
- **Assertion**: New ViewModel after re-add populates ListView with 3 items.
- **Status**: Passing.

### 4.4 State property remove/re-add (implemented)

- **ID**: `When_RemoveAndReAddStateProperty_Then_BindingsWork`
- **Goal**: Same pattern as Feed remove/re-add but for `IState<T>` (#2906).
- **Model**: `MvuxHotReloadStateRemoveModel` with
  `IState<string> CurrentValue`.
- **HR change**: Two sequential deltas — remove, then re-add.
- **Assertion**: New ViewModel after re-add has a working `CurrentValue`
  binding showing `"stateful"`.
- **Status**: Passing.

### 4.5 Multiple properties remove/re-add (implemented)

- **ID**: `When_RemoveAndReAddMultipleProperties_Then_AllBindingsWork`
- **Goal**: Mirrors the real Chefs scenario from #2906 — multiple
  Feed and ListFeed properties removed and re-added in one HR cycle.
- **Model**: `MvuxHotReloadMultiModel` with `IFeed<string> Title` and
  `IListFeed<string> Items`.
- **HR change**: Two sequential deltas — remove both properties, then
  re-add both.
- **Assertion**: New ViewModel after re-add has both `Title` and `Items`
  bindings working.
- **Status**: Passing.

### 4.6 Feed to State conversion (implemented)

- **ID**: `When_ConvertFeedToState_Then_BindingsWork`
- **Goal**: Prove that changing a property from `IFeed<T>` to `IState<T>`
  via HR correctly regenerates the ViewModel and restores bindings.
- **Model**: `MvuxHotReloadFeedToStateModel` starting with
  `IFeed<string> CurrentValue`.
- **HR change**: Single delta — convert property from `IFeed` + `Feed.Async`
  to `IState` + `State.Async`.
- **Assertion**: New ViewModel after conversion has a working binding.
- **Status**: Passing.

### 4.7 Feed/State syntax change (implemented)

- **ID**: `When_ChangeFeedSyntax_Then_BindingsWork`
- **Goal**: Prove that changing between Feed and State syntax across
  multiple HR cycles works. Feed.Async → State.Async → Feed.Async.
- **Model**: `MvuxHotReloadSyntaxChangeModel` starting with
  `IFeed<string> CurrentValue => Feed.Async(...)`.
- **HR change**: Two deltas — first converts to `IState` + `State.Async`,
  second converts back to `IFeed` + `Feed.Async` (same value throughout).
- **Assertion**: Each conversion produces a working binding; validates
  that repeated type conversions don't corrupt generator state.
- **Status**: Passing.

### 4.8 State value update (implemented)

- **ID**: `When_UpdateStateValue_Then_NewViewModelReflectsUpdate`
- **Goal**: Validate that changing the value inside a State lambda
  (`"stateful"` → `"updated"`) is reflected in both original and new
  ViewModel instances.
- **Model**: `MvuxHotReloadStateRemoveModel` (reuses State remove model).
- **HR change**: Single delta — change lambda return value.
- **Assertion**: Original VM and new VM both show `"updated"`.
- **Status**: Failing — lambda body value changes do not trigger Feed/State
  re-evaluation (same limitation as 4.1).

### 4.9 AsyncEnumerable Feed remove/re-add (implemented)

- **ID**: `When_AsyncEnumerableFeed_RemoveAndReAdd_Then_BindingsWork`
- **Goal**: Prove remove/re-add works with `Feed.AsyncEnumerable` syntax
  (different Feed factory method from `Feed.Async`).
- **Model**: `MvuxHotReloadAsyncEnumerableModel` with
  `IFeed<string> CurrentValue => Feed.AsyncEnumerable(GetValues)`.
- **HR change**: Two sequential deltas — remove property, then re-add.
- **Assertion**: New ViewModel after re-add has a working binding
  showing `"enumerable"`.
- **Status**: Passing.

### 4.10 Feed.Async ↔ AsyncEnumerable syntax change (implemented)

- **ID**: `When_ChangeFeedAsyncToAsyncEnumerable_Then_BindingsWork`
- **Goal**: Prove that switching between `Feed.AsyncEnumerable` and
  `Feed.Async` syntax via HR works in both directions.
- **Model**: `MvuxHotReloadAsyncEnumerableModel` starting with
  `Feed.AsyncEnumerable(GetValues)`.
- **HR change**: Two deltas — first converts to `Feed.Async(...)`,
  second converts back to `Feed.AsyncEnumerable(GetValues)`.
- **Assertion**: Each conversion produces a working binding.
- **Status**: Passing.

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
| `MvuxHotReloadStateRemoveModel.cs` | Model for State remove/re-add (#2906) |
| `MvuxHotReloadMultiModel.cs` | Model for multi-property remove/re-add (#2906) |
| `MvuxHotReloadFeedToStateModel.cs` | Model for Feed→State conversion |
| `MvuxHotReloadSyntaxChangeModel.cs` | Model for Feed↔State syntax changes |
| `MvuxHotReloadAsyncEnumerableModel.cs` | Model for AsyncEnumerable Feed scenarios |
