# 015 — Progress

## Decisions on §7 open questions (v1 — spec's proposed defaults)

| Q | Decision |
| --- | --- |
| 1 Producer surface | `State.Validate(IState<T>, validator)` registers on the `StateImpl` and returns it (mutating, idempotent: last registration wins, loop started once). |
| 2 When to validate | Every `Data` change of the state, including the initial value. Explicit/"touched" triggers are a follow-up; the manual `SetValidationAsync(results)` covers explicit validation today. |
| 3 `IValidator` convenience | Not added (`_validator.ValidateAsync` fits the delegate directly). |
| 4 Name collisions | INDEI members implemented explicitly on both bases + a public `HasErrors` for bindings, so only `HasErrors` can collide. A model/record member named `HasErrors` hides the public one (generated code already suppresses CS0108); new `FEED1001` (Info, so it does not break `TreatWarningsAsErrors` apps) reports it. |
| 5 Read-only feeds | States only. The axis is `IsLocal`: operators never forward it. |
| 6 `IListState` items | Deferred. |

Additional behaviour choices:

- Validator runs only when the state has a value (`Option.Some`, including `Some(null)`: the validator gets `null`). `None` clears the results; `Undefined` keeps the previous ones.
- Validator exceptions are logged (type only, no message) and keep the previous results.
- Stale check compares the state's current data with the validated value (`EqualityComparer<T>.Default`) **inside** the update, so it is atomic with the state's own update pipeline.
- Inside the state's own `UpdateFeed` the axis still flows from parent to child (needed for hot-reload `HotSwap`, where the old state's parent is the new state). A local value replaces the parent one (no aggregation).
- Hot-reload: when a state is hot-swapped with the state of the updated model, it stops its own validation loop (the updated state owns validation, its results reach the old state through its source). Otherwise the previous validator keeps running and can overwrite the results of the updated one.

## Checklist

- [x] Spec read; operators' axis-forwarding verified (`MessageManager`, `CombineFeedHelper`, `DynamicParentMessage` all forward every non-Data axis → `IsLocal` needed).
- [x] Core: `MessageAxes.Validation`, `ValidationAxis` (`IsLocal`, content equality, empty → unset, local replaces parent), `MessageAxis.Validation`.
- [x] Core: `Validation(...)` builder ext, `GetValidation(...)`, `MessageEntry<T>.Validation`.
- [x] Operators: drop `IsLocal` axes in `MessageManager` (opt-in forward for `UpdateFeed`), `MessageBuilder.Get`, `CombineFeedHelper`.
- [x] Producer: `StateImpl.SetValidator` + `State.Validate`.
- [x] Consumer: `BindableViewModelBase : INotifyDataErrorInfo`, routing by `MemberNames`.
- [x] Consumer: `Bindable<T> : INotifyDataErrorInfo` (generated record bindables), nested routing.
- [x] Generator: `FEED1001` collision diagnostic + `AnalyzerReleases.Unshipped.md`.
- [x] Tests: `Given_ValidationAxis`, `Given_StateWithValidation`, `Given_BindableViewModel_Validation`.
- [x] Docs: `doc/Learn/Mvux/Advanced/Validation.md` + TOC + cross-link from `doc/Learn/Validation/ValidationOverview.md`.
- [x] Full `Uno.Extensions.Reactive.Tests` run green.
- [ ] Release build of `Uno.Extensions-packageonly.slnf` warning-free — **not verified locally**: solution restore fails on `Uno.Extensions.Maui.WinUI.Runtime.Skia` with `NETSDK1241: The TargetFramework property is empty` (the local `DebugPlatforms.props` disables every platform this project targets), before any build. Built instead in Release, 0 warnings: `Reactive`, `Reactive.WinUI`, `Reactive.WinUI.Markup`, `Reactive.Messaging`, `Reactive.Generator`, `Reactive.Tests`, `HotTesting.Reactive.Tests` (`Uno.Extensions.Reactive` is `net10.0` only by design — `tfms-non-ui.props`).
- [x] Hot-reload: `Given_HotReload.When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed` (unit-test HR harness). Red without the `StopValidation()` on hot-swap (5/5 runs), green with it.
- [x] UI / runtime tests (two-way `TextBox` in a real Uno host), run on Windows desktop with the recipe in `specs/lessons.md`:
  - `Reactive.UI.Tests/Given_Validation` (2): `ErrorsChanged` / `HasErrors` raised on the UI thread for a validated string state, and errors of a record member reported by the generated bindable of the record. 2/2 passed (5 runs).
  - `Reactive.UI.Tests/Given_HotReload.When_UpdateValidatorInModel_Then_ErrorsFromUpdatedValidator`: only the updated validator publishes results, on the live view model and on one created after the delta. The whole `Given_HotReload` class passed (15/15).
  - Mutation checks: with `BindableValidationErrors.Update` as a no-op all 3 tests fail; without the `StopValidation()` on hot-swap the hot-reload test fails.

## Review

- New tests (46): `Given_ValidationAxis` (13), `Given_StateWithValidation` (14), `Given_BindableViewModel_Validation` (11), `Given_BindableValidationErrors` (4), `Given_ValidationDiagnostic` (3, drives the generator in-process: the generator is now also referenced by `Reactive.Tests` under the `ReactiveGenerator` alias), `Given_HotReload.When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed` (1).
- Stability: the 45 `~Validation` tests run 15× in a row.
- Mutation check: with `IsLocal = false`, the 5 "not propagated" tests fail.
- `Uno.Extensions.Reactive.Tests`: all passed except the 19 pre-existing skipped. `Uno.HotTesting.Reactive.Tests`: 34 passed.
- ~~Known gap in "no breaking change": a hand-written `HasErrors` member in a user's `partial` of a generated view model hides `BindableViewModelBase.HasErrors` → `CS0108`.~~ Fixed after the second panel: `BindableViewModelBase` implements only `INotifyDataErrorInfo.HasErrors`, and the generator (all tool versions) adds the public `HasErrors` only when the model and the user's partial declare none (otherwise `FEED1001`).
- Follow-ups: explicit / "touched" validation trigger (Q2), `IValidator` bridge (Q3), `IListState` items (Q6), `CanExecute` gating, rendering verification on Windows / Skia / WASM.

## PR review (#3203)

| Finding | Resolution |
| --- | --- |
| `With` prefix not used in MVUX naming (`WithValidation`) | Renamed `Validate` (earlier revision of the PR). |
| `.Validation(() => Validator)` convenience | `Validate(IValidator)` overload added by spec 017 (#3222), with `IValidator` moved to `Uno.Extensions.Core`. |
| Provide `SetValidationAsync` like `ClearSelectionAsync` | `State.SetValidationAsync(results, ct)`; docs and spec use it for on-demand validation. |
| `Some(null)` treated as `None` (validator skipped, results cleared) | `Some(null)` is a value: the validator is invoked with `null` (`When_SomeNull_Then_ValidatorInvokedWithNull`). |
| `Validate(customState, null)` throws `NotSupportedException` | Arguments checked first: `ArgumentNullException` (`When_NullValidator_Then_ThrowsArgumentNull`, `When_NullState_Then_ThrowsArgumentNull`). |
| Code quality: empty `catch` for the cancellation | Commented: expected when the value changes or the validation stops. |
| Code quality: generic `catch` in `Validate` | Kept: the validator is user code and must never fault the state; `OperationCanceledException` is caught first, `Exception` is the logged fallback (AGENTS.md §8). |
| Code quality: `__reactiveBindableViewModel` can be `readonly` (test) | Kept: hand-written stand-in of the generated code, where the bindable assigns the field (`ViewModelGenTool_2`), like the other `Given_HotReload` stubs. |

## Review panel (20261020)

Seven-agent panel on the whole stack (#3203, #3204, #3222). Findings that belong to this PR:

| Finding | Resolution |
| --- | --- |
| H1 Results lost for good when the source re-emits an equal value (volatile update dropped, no `Data` change so no re-validation) | One validation update per state, replaced on each publication, kept across source messages and re-applied while the data matches (`When_SourceProducesSameValue_Then_ResultsKept`, red before). |
| H2 Runtime tests required by §9 missing | `Reactive.UI.Tests`: `Given_Validation` (two-way `TextBox`, record member) and `Given_HotReload.When_UpdateValidatorInModel_Then_ErrorsFromUpdatedValidator`; run on desktop, mutation-checked. |
| M1 `When_RecordMemberSetFromView_Then_ValidationDelivered` flaky | The first push of the state to the view model is asynchronous and overwrote the edit of the test (reproduced by delaying it). The test waits for it. The race itself is pre-existing on `main` (an edit from the view right after load is reverted): to fix in its own PR. |
| M4 One validation update per keystroke kept over a never-completing source | Single replaced update (cf. H1). |
| M5 "Last writer wins" was "last to complete wins" | Version taken when the validation starts; older results dropped (`When_ResultsOfEarlierVersionCompleteLast_Then_Discarded`). |
| M6 Stale-value check never exercised | `When_SourceProducesNewValue_Then_ResultsOfPreviousValueNotShown` (the check now runs on each source message). |
| M8 Different rules for custom `IState` | Documented: `Validate` needs a factory state (`NotSupportedException`), `SetValidationAsync` falls back to `UpdateMessageAsync` for other states. |
| M10 Validator failures logged without identity | Logs name the validator (user type + method) and the value type, message still at Debug only. |
| M11 `HasErrors` in a user partial → `CS0108` | Already listed in the PR breaking-change note and `Validation.md`. |
| M12 Validation stores allocated for list items | A sub-property provides validation only if its owner does (`When_PropertyWithoutValidation_Then_NoStoreAllocatedForSubBindables`). |
| L2 Base `Validate` took a `Func<…>` | Now `AsyncFunc<T, IEnumerable<ValidationResult>>`, like the other overloads (API reviewed). |
| L5 / L6 / L12 / L13 | Threading and cancellation documented; publish failures logged apart; validator + token swapped atomically; fewer allocations. |
| L9 Sub-bindable handlers never unsubscribed | Not changed: sub-bindables subscribe once, from the constructors of the generated bindables, so handlers cannot pile up. |
| L10 `ErrorsChanged` handler exception escapes | Not changed: same behavior as a throwing `PropertyChanged` handler. |
| L11 Results for an unknown member never displayed | Reported as entity-level errors of the bindable of the record. |
| L16 Narrating comments | Removed. |
| L17 Tests of the internal `BindableValidationErrors` | Kept: routing unit tests, the behavior is also covered end to end. |

Follow-ups (design, M14):

- "Local" axis forwarding is decided per operator (`MessageManager(forwardLocalAxes)` opted in by `UpdateFeed` only, `CombineFeedHelper`): model it once (e.g. on the identity-preserving feeds) so new operators don't have to decide.
- `StateImpl` holds the whole validation loop: extract it (e.g. a `StateValidation<T>` attached like `StateForEach`), keeping only the publication and the hot-swap stop in the state.
