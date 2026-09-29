# 015 — Progress

## Decisions on §7 open questions (v1 — spec's proposed defaults)

| Q | Decision |
| --- | --- |
| 1 Producer surface | `State.Validate(IState<T>, validator)` registers on the `StateImpl` and returns it (mutating, idempotent: last registration wins, loop started once). |
| 2 When to validate | Every `Data` change of the state, including the initial value. Explicit/"touched" triggers are a follow-up; the manual `UpdateMessageAsync(m => m.Validation(...))` path covers explicit validation today. |
| 3 `IValidator` convenience | Not added (`_validator.ValidateAsync` fits the delegate directly). |
| 4 Name collisions | INDEI members implemented explicitly on both bases + a public `HasErrors` for bindings, so only `HasErrors` can collide. A model/record member named `HasErrors` hides the public one (generated code already suppresses CS0108); new `FEED1001` (Info, so it does not break `TreatWarningsAsErrors` apps) reports it. |
| 5 Read-only feeds | States only. The axis is `IsLocal`: operators never forward it. |
| 6 `IListState` items | Deferred. |

Additional behaviour choices:

- Validator runs only when the state has a value (`Option.Some` and non-null). `None` clears the results; `Undefined` keeps the previous ones.
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
- [ ] UI / runtime tests (two-way `TextBox` in a real Uno host) — not written/run: needs an Uno runtime-test head. Binding behaviour is covered with `FeedUITests` (test dispatcher).

## Review

- New tests (46): `Given_ValidationAxis` (13), `Given_StateWithValidation` (14), `Given_BindableViewModel_Validation` (11), `Given_BindableValidationErrors` (4), `Given_ValidationDiagnostic` (3, drives the generator in-process: the generator is now also referenced by `Reactive.Tests` under the `ReactiveGenerator` alias), `Given_HotReload.When_UpdateValidatorInModel_Then_OnlyUpdatedValidatorUsed` (1).
- Stability: the 45 `~Validation` tests run 15× in a row.
- Mutation check: with `IsLocal = false`, the 5 "not propagated" tests fail.
- `Uno.Extensions.Reactive.Tests`: all passed except the 19 pre-existing skipped. `Uno.HotTesting.Reactive.Tests`: 34 passed.
- Known gap in "no breaking change": a hand-written `HasErrors` member in a user's `partial` of a generated view model now hides `BindableViewModelBase.HasErrors` → `CS0108` in user code (an error with `TreatWarningsAsErrors`). `FEED1001` only covers members mirrored from the model/record. Documented in `Validation.md`.
- Follow-ups: explicit / "touched" validation trigger (Q2), `IValidator` bridge (Q3), `IListState` items (Q6), `CanExecute` gating, hot-reload runtime test, rendering verification on Windows / Skia / WASM.
