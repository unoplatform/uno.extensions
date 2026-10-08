# 017 — Progress

Stacked PR: `main` ← #3203 (spec 015) ← #3204 (spec 016) ← this branch (`dev/xygu/20261007/spec-017-command-validation`).

## Decisions
| Topic | Decision |
| --- | --- |
| Validator type | `IValidator` moved to Core (type forward in Validation); delegate overloads mirror spec 016. |
| Naming | `State.Validate` / builder `.Validation`. |
| Coexistence with `State.Validate` | Last writer wins (documented). |
| `Given` not a state / no `Given` | Validate + abort, results not published (logged), `FEED2003` warning at build. |
| Scope | Builder only, single `Given`. |

## Plan
- [x] Spec (this folder)
- [x] Move `IValidator` to Core + type forward
- [x] Shared validation helpers + `State.Validate(IValidator)`
- [x] `CommandBuilderExtensions.Validation` + execution in `AsyncCommand`
- [x] `FEED2003` analyzer
- [x] Tests
- [x] Docs + Playground sample
- [x] Release build warning-free, tests green
- [x] Address review panel findings (below)

## Verification
- Release build of every project of `Uno.Extensions-packageonly.slnf`, built one by one (the solution-level restore fails with `MSB4181` on SDK 11 preview, also on the base commit): 51/52 clean, no warnings. `Uno.Extensions.Maui.WinUI.Runtime.Skia` has no target framework locally (iOS/Android disabled by `DebugPlatforms.props`).
- `Uno.Extensions.Reactive.Tests` (`Given_CommandValidation`, `Given_CommandValidationDiagnostic`, `Given_StateWithValidation`, `Given_AsyncCommand`, `Given_CompositeCommand`, `Given_ValidationDiagnostic`): 76 passed. `Uno.Extensions.Validation.Tests`: 8 passed.
- `FEED2003` verified end-to-end: it fired on the test project itself (which loads the generator as analyzer) for the two intentional non-state cases.

## Review panel (7 lenses)
Verdict: fix-first (security fix-before-merge, skeptic fix-first, others approve-with-changes). Addressed:

| Finding | Resolution |
| --- | --- |
| High: `Then` could start after disposal when the validator ignores `ct` | Cancellation checked before the action; red/green tests with a validator ignoring `ct` |
| Medium: command errors exposed the `ToString` of the parameter (form data, passwords) in logs | All commands report the parameter type only; red/green test (`Given_AsyncCommand`) |
| Medium: `ValidationResult.Success` (null) on the axis (phantom error, NRE in axis equality) | Ignored by the axis builder (all writers) and the localization; red/green test |
| Medium: command could hang on a disposed `Given` state | Not reproduced (execution completes); regression test kept. Publication now honors the token and the context of the state |
| Medium: action could run inline under the `UpdateFeed` lock | `StateImpl.PublishValidationAsync` resumes asynchronously when the update is applied asynchronously |
| Medium: an `OperationCanceledException` of the validator itself silently aborted | Validator errors wrapped (`InvalidOperationException`, no value) so the execution faults; test |
| Medium: no trace of an aborted execution | Debug log (count only) |
| Medium: `CommandConfig.Validate` hook not needed | Validation composed in the builder; `CommandConfig` / `AsyncCommand.SubCommand` unchanged vs base |
| Medium: two writers of the axis through different doors | Single internal entry point `StateImpl.PublishValidationAsync` (`IValidationTarget`), used by the state validator and commands |
| Medium: results dropped when the source of the state emits | Documented (docs, XML docs, spec) and tested (`State.Async` refresh) |
| Medium: lockstep upgrade (`CS0433`) | Spec §3 + Validation overview; to be called out in the PR |
| Medium: `Task.Delay(50)` negative assertions | Replaced with synchronous checks (`ExecutionStarted` count, `IsExecuting`) or synchronous continuations |
| Medium: missing tests | Added: `Validation` twice, covariant builder, view parameter at runtime, null view parameter with `IValidator` |
| Medium: duplicated test helpers | `_Utils/ValidationTestHelper.cs` (`WaitFor`, `WaitForAsync`, `TestLocalizer`, `ValidatedPerson`), `Generator/GeneratorTestHelper.cs` |
| Low: "is a state" decided 3 ways | Runtime: non-generic `IValidationTarget` (covariance) or `IState<T>`; analyzer unwraps explicit casts; test |
| Low: null parameter with `IValidator` | Kept valid (nothing to validate), documented publicly and tested |
| Low: docs | Last-wins chaining, hung validators, aborted vs successful completion, `CS0121` for null-only lambdas, on-change validation completing after the command |
| Low: misc | `IValidatingCommandBuilder` in its own file; `FEED2003` title aligned; `Given_IValidatorLocation` test renamed/strengthened; duplicated localized `IValidator` test merged; explicit assertion after the last wait |
