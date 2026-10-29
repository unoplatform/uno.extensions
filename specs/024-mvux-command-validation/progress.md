# 024 — Progress

Stacked PR: `main` ← #3203 (spec 022) ← #3204 (spec 023) ← this branch (`dev/xygu/20261007/spec-017-command-validation`).

## Decisions

| Topic | Decision |
| --- | --- |
| Validator type | `IValidator` moved to Core (type forward in Validation); delegate overloads mirror spec 023. |
| Naming | `State.Validate` / builder `.Validation`. |
| Coexistence with `State.Validate` | Last validation started wins (documented; versioned publications, spec 022 review panel). |
| `Given` not a state / no `Given` | Validate + abort, results not published (logged), `FEED2003` warning at build. |
| Scope | Builder only, single `Given`. |

## Plan

- [x] Spec (this folder)
- [x] Move `IValidator` to Core + type forward
- [x] Shared validation helpers + `State.Validate(IValidator)`
- [x] `CommandBuilderExtensions.Validation`, composed in the command builder (initially in `AsyncCommand`)
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
| Medium: no trace of an aborted execution | Logged (count only), at Information since the 20261020 panel |
| Medium: `CommandConfig.Validate` hook not needed | Validation composed in the builder; `CommandConfig` / `AsyncCommand.SubCommand` unchanged vs base |
| Medium: two writers of the axis through different doors | Single internal entry point `StateImpl.PublishValidationAsync` (`IValidationTarget`), used by the state validator, commands and the public `SetValidationAsync` (spec 022 review) |
| Medium: results dropped when the source of the state emits | Fixed by the spec 022 validation update (20261020 panel): results kept until the next execution, tested (`State.Async` refresh) |
| Medium: lockstep upgrade (`CS0433`) | Spec §3 + Validation overview; to be called out in the PR |
| Medium: `Task.Delay(50)` negative assertions | Replaced with synchronous checks (`ExecutionStarted` count, `IsExecuting`) or synchronous continuations |
| Medium: missing tests | Added: `Validation` twice, covariant builder, view parameter at runtime, null view parameter with `IValidator` |
| Medium: duplicated test helpers | `_Utils/ValidationTestHelper.cs` (`WaitFor`, `WaitForAsync`, `TestLocalizer`, `ValidatedPerson`), `Generator/GeneratorTestHelper.cs` |
| Low: "is a state" decided 3 ways | Runtime: non-generic `IValidationTarget` (covariance) or `IState<T>`; analyzer unwraps explicit casts; test |
| Low: null parameter with `IValidator` | Kept valid (nothing to validate), documented publicly and tested |
| Low: docs | Last-wins chaining, hung validators, aborted vs successful completion, `CS0121` for null-only lambdas, on-change validation completing after the command |
| Low: misc | `IValidatingCommandBuilder` in its own file; `FEED2003` title aligned; `Given_IValidatorLocation` test renamed/strengthened; duplicated localized `IValidator` test merged; explicit assertion after the last wait |

## Review panel (20261020)

Seven-agent panel on the whole stack (#3203, #3204, #3222). Findings that belong to this PR (see spec 022 progress for the others):

| Finding | Resolution |
| --- | --- |
| M3 Nothing enforces upgrading Validation and Core together (`CS0433`) | Declared as a breaking change: upgrade notes (`doc/Learn/UpdatingExtensions.md`, Extensions 8.0) and PR description. A bridge package was considered and not chosen. |
| M5 A slow command validation could overwrite newer results | The version of the results is taken before running the validator; older results are dropped (`When_ResultsPublishedWhileValidating_Then_ResultsOfExecutionDiscarded`). |
| M7 `FEED2003` reported a feed property returning a state | The analyzer follows properties, fields and locals declared in the same file to their value; unknown values are not reported (`When_GivenStateDeclaredAsFeed_Then_NoDiagnostic`). |
| M8 Duplicated publication fallback | `ValidationHelper.GetPublisher`, shared by commands and `SetValidationAsync`; the version counter is global. |
| M9 An aborted execution looks like "Submit does nothing" | Logged at Information; documented with a "Diagnosing validation" section (L8). |
| M10 The original error of a command validator reached the Error log | Wrapped without it (its type only); details logged at Debug, like the validator of a state. |
| M13 Docs vs code (`Validate(IValidator)` with null), stale spec text | Documented; specs 015, 016 and 017 updated. |
| L2 / L3 Base overloads took a `Func<…>`; `IValidator` overloads required `notnull` | `AsyncFunc<T, IEnumerable<ValidationResult>>`; constraint removed (API reviewed). |
| L4 Command errors no longer include the parameter | Upgrade notes. |
| L7 The warning for a non-state parameter logged only a count | Also names the members in error; docs say what is logged. |
| L13 / L14 Adapter allocations, redundant null filter | Adapters and localization produce immutable lists, published as is. |
| L15 Duplicated poll loop | Shared `WaitForAsync`. |
| L17 Tests build `StateImpl` directly | Kept: same pattern as the rest of `Reactive.Tests`, behavior asserted through the public API of commands. |
| L19 "Only place" of localization | Specs and lessons reworded. |
| Command results after a source refresh | Kept until the next execution (consequence of the spec 022 fix), test updated. |
