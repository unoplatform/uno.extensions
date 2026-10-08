# 017 — Validation of MVUX command parameters on execution

**Status:** In progress — see [progress.md](progress.md)
**Area:** `Uno.Extensions.Reactive` (command builder, `AsyncCommand`, `State.Validate`), `Uno.Extensions.Reactive.Generator` (analyzer), `Uno.Extensions.Core` / `Uno.Extensions.Validation` (`IValidator` location)
**Related:** [spec 015](../015-mvux-validation-indei/spec.md) (validation axis, `State.Validate`, INDEI) — §7 Q2 "explicit trigger on submit"; [spec 016](../016-validation-message-localization/spec.md) (localized `Validate` overloads)

## 1. Problem
Spec 015 validates a state **on every data change**. Forms commonly need the opposite: validate **when the user submits**, show the errors, and keep them until the next submit (not flicker while the user is typing). Today this requires hand-writing the validation in the command and pushing `m.Validation(...)` on the state, and nothing prevents the command from running with an invalid value.

## 2. Goals / non-goals
**Goals**
- A validation step on the command builder, between `Given`/`When` and `Then`:
  ```csharp
  public IAsyncCommand Submit => Command.Create(b => b
      .Given(Person)
      .When(person => person.IsComplete)          // CanExecute, unchanged
      .Validation(Validator, Localizer)           // runs on each execution
      .Then(async (person, ct) => await _service.Save(person, ct)));
  ```
- On each execution: validate the parameter, publish the results on the `Validation` axis of the `Given` state (so they reach the view through spec 015's INDEI), and abort the execution (`Then` not invoked) if there is any error.
- Results are **not** cleared when the state changes: they are replaced on the next execution.
- Use an `Uno.Extensions.Validation.IValidator` directly (`.Validation(validator)`, and `State.Validate(validator)`), without a reference between Reactive and Validation and without a bridge package.

**Non-goals**
- Commands generated from model methods (no attribute / DataAnnotations on the method parameters).
- Validating the view's `CommandParameter` (no state to publish the results on).
- Multiple parameters: the builder has a single `Given`. `Given(Feed.Combine(a, b))` is a feed, see §4.4. A per-parameter shape (`.Given(a).Validation(..).And(b).Validation(..)`, one validation slot per source state) is a possible follow-up.
- Telling "aborted by validation" apart in `ExecutionCompleted`.

## 3. `IValidator` without cross-references
- The public non-generic `IValidator` moves from `Uno.Extensions.Validation` to `Uno.Extensions.Core` (both Reactive and Validation already reference Core), **namespace unchanged** (`Uno.Extensions.Validation`). It only uses BCL types (`System.ComponentModel.DataAnnotations` is part of net10.0), so Core gets no new dependency.
- `Uno.Extensions.Validation` declares `[assembly: TypeForwardedTo(typeof(IValidator))]`: binaries compiled against the previous location keep working; no source break (same namespace, Core is a transitive dependency of Validation).
- The internal `IValidator<T>`, `Validator`, DI registration stay in Validation.
- Rejected: implicit conversion (impossible from an interface, CS0552); a delegate overload matching `ValidateAsync`'s shape (public `object` parameter, signature coupled to `IValidator` anyway); a bridge package; source-generated overloads; reflection (AOT).
- **API call-out:** moving a public type between assemblies is an API change even with the type forward; to be listed in the PR.
- **Upgrade:** the forward only covers an upgraded Validation. An older `Uno.Extensions.Validation` with a newer `Uno.Extensions.Core` (e.g. explicit package versions where only Reactive is upgraded) declares `IValidator` twice (`CS0433`). Packages must be upgraded together (as the Uno.Sdk does): release note + docs (Validation overview).

## 4. Design

### 4.1 Builder surface (`CommandBuilderExtensions`, additive)
Extension methods (not new interface members, so implementers of the public builder interfaces are not broken) on both `ICommandBuilder<T>` (after `Given`, or `Command.Create<T>`) and `IConditionalCommandBuilder<T>` (after `When`). They return `IConditionalCommandBuilder<T>`: only `Then` can follow. Overloads mirror `State.Validate` (spec 016), each with a trailing optional localizer:

```csharp
IConditionalCommandBuilder<T> Validation<T>(this ICommandBuilder<T> builder, Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator, IStringLocalizer? localizer = null);
IConditionalCommandBuilder<T> Validation<T>(this ICommandBuilder<T> builder, AsyncFunc<T, string?> validator, IStringLocalizer? localizer = null);  // single rule
IConditionalCommandBuilder<T> Validation<T>(this ICommandBuilder<T> builder, AsyncFunc<T, bool> isValid, string error, IStringLocalizer? localizer = null); // predicate
IConditionalCommandBuilder<T> Validation<T>(this ICommandBuilder<T> builder, IValidator validator, IStringLocalizer? localizer = null) where T : notnull;
// + the same 4 on IConditionalCommandBuilder<T>
```
- Guards when the command is built: null validator → `ArgumentNullException`; null/empty `error` → `ArgumentException`. A builder not created by `Command` → `NotSupportedException`.
- Calling `Validation` twice on the same chain replaces the validation (the last one wins), like `State.Validate`. Documented in the XML docs.
- Returning `IConditionalCommandBuilder<T>` (only `Then`) keeps the surface minimal, but permanently rules out `.Validation(...).When(...)` (that public interface cannot get new members).
- With an `IValidator`, a `null` parameter (e.g. a view parameter not bound yet) has nothing to validate and is considered as valid (documented publicly).
- The builder interfaces are covariant: the extensions work on `object` internally (`IValidatingCommandBuilder`), so `ICommandBuilder<Base>` backed by a builder of a derived type is supported.
- Localization, result shapes of the single-rule/predicate overloads: identical to spec 016 §4.3 (shared helper).
- `State.Validate<T>(this IState<T>, IValidator, IStringLocalizer? = null) where T : notnull` is added with the same adaptation (`ValidateAsync(value, null, ct)`).
- Naming: `State.Validate` (a state is validated continuously) vs builder `.Validation` (a step of the command).

### 4.2 Execution
`SubCommand.TryExecute` decides synchronously whether an execution starts (`CanExecute`), then runs the action in a `Task.Run`. The builder composes the validation into the action given to `Then` (no change to `CommandConfig` / `AsyncCommand`, which are shared with the generated commands), so it runs inside that task, on the coerced parameter (the value `Then` receives):
1. Run the validator (command's cancellation token, i.e. cancelled when the command is disposed). Messages localized as in spec 016; null results (`ValidationResult.Success`) ignored.
   - A validator exception (including an `OperationCanceledException` which is not the command's cancellation, e.g. an `HttpClient` timeout) is wrapped in an `InvalidOperationException` (without the value) so the execution faults instead of being silently cancelled.
2. Publish the results on the parameter, **unconditionally** (no stale check: on-demand results describe the submitted value and stay until the next execution even if the value is edited meanwhile):
   - a state created by the State factories: through `IValidationTarget.PublishValidationAsync`, the single internal entry point of `StateImpl`, also used by its own validator. It does not publish if already cancelled, aborts the wait when cancelled (or when the context of the state is disposed), and when the update is applied asynchronously (busy update pipeline) resumes asynchronously, so the action never runs under the lock of the `UpdateFeed`. It is not generic, so it also works when the parameter is seen as a state of a base type (covariance);
   - another `IState<T>`: `UpdateMessageAsync(m => m.Validation(results))`.
3. Any result: return without invoking `Then`; the execution completes **without error** (`ExecutionCompleted.Error == null`, indistinguishable from a successful execution, documented). Logged at Debug (count only).
4. Check the cancellation token before invoking `Then`: a validator which ignores it must not start the action after the command has been disposed.

Consequences:
- `IsExecuting` is `true` while validating, so a double-submit is blocked like during the execution itself. A validator that hangs keeps the command executing for that parameter: documented (honor the token, apply a timeout).
- Validation errors do not affect `CanExecute`: the user can always retry, which is what re-validates.
- A validator exception faults the execution (reported to the error handler of the command like any `Then` failure); previous results are kept.
- A successful validation clears the results (empty list).
- The results are a (volatile) update of the state: like the edits of the user, they are dropped when the source of the state produces a new value (e.g. refresh of a `State.Async`, re-emission of `State.FromFeed`, hot-reload). Documented and tested.
- Errors reported by commands no longer include the value of the parameter (the `ToString` of a form record exposed passwords in logs), only its type. This applies to all commands.

### 4.3 Coexistence with `State.Validate` — last writer wins
Both write the same axis (through the same `StateImpl` entry point). A command validation (valid or not) replaces the results of the on-change validator of the state, and its next run (on the next data change) replaces the ones of the command; an on-change validation which completes after the execution of the command also replaces them. Documented; no merge/slots in v1.

### 4.4 `Given` is not a state — `FEED2003`
With a `Given` feed which is not a state (e.g. `Feed.Async`, `Feed.Combine`), or without `Given` (parameter from the view), there is nowhere to publish the results: the command still validates and aborts on errors, the results are only logged at Warning (count, no message: messages may contain user input).
The rule is the same at runtime and in the analyzer: the parameter is a state (runtime `IState<>`).
A new analyzer reports **`FEED2003` (Warning)** on such `.Validation(...)` calls:
- walks back the fluent chain (`When(...)` → `Given(arg)`) and reports if the type of `arg` is not an `IState<>`, or if the chain reaches the parameter of the lambda given to `Command.Create` without any `Given`;
- conversions of `arg` are unwrapped, including explicit casts (`Given((IFeed<T>)MyState)` is not reported). A property typed `IFeed<T>` which returns a state cannot be known statically and is reported: type it as `IState<T>`;
- receivers it cannot follow (builder stored in a local/field) are not reported (no false positives).
Under `TreatWarningsAsErrors` this is an error, which is intended for a misuse.

## 5. Public API delta (additive)
- `Uno.Extensions.Reactive.CommandBuilderExtensions` — 8 `Validation` overloads (§4.1).
- `State.Validate<T>(IState<T>, IValidator, IStringLocalizer?)`.
- `IValidator` moved to `Uno.Extensions.Core` (+ type forward in Validation).
- Analyzer rule `FEED2003`.
- Internal only: `IValidationTarget` (implemented by `StateImpl<T>`), `IValidatingCommandBuilder`.
- Behaviour: errors reported by commands contain the type of the parameter instead of its value; null validation results are ignored on the axis.

## 6. Performance / platform
- No `.Validation`: one null check per execution.
- With `.Validation`: one validator run + one state update (and message rebuild) per execution, on the background task of the command; no UI-thread work, no locks (WASM-safe). When the update pipeline is busy, one forced yield before the action.
- Each execution adds a volatile update to the `UpdateFeed` of the state. They are compacted when the source of the state completes (e.g. `State.Value`); with a source which never completes (hot-reload `HotSwapFeed`, `State.FromFeed` over an infinite feed) they accumulate until the source emits, like the edits of a two-way binding.

## 7. Tests
- `Given_CommandValidation` (`Uno.Extensions.Reactive.Tests`): invalid → not executed, results on the state, completed without error; valid → executed, results cleared; data change after a failed execution keeps the results; re-execution replaces them; validator throws → error handler, previous results kept; `IsExecuting` during validation; disposal cancels the validation; `When` false → not validated; single-rule/predicate/`IValidator` overloads; localization; guards; non-state `Given` → aborts, nothing published; last writer wins with `State.Validate`.
- `Given_StateWithValidation`: `Validate(IValidator)` plain and localized.
- `Uno.Extensions.Validation.Tests`: `IValidator` lives in Core and the forward from Validation resolves.
- `Given_CommandValidationDiagnostic`: `FEED2003` reported for a feed / no `Given`, not for a state, also after `When`.
- Release build of `Uno.Extensions-packageonly.slnf` warning-free.

## 8. Docs
- `doc/Learn/Mvux/Advanced/Validation.md`: "Validating when a command is executed".
- `doc/Learn/Mvux/Advanced/Commands.md`: short section + link.
- `doc/Learn/Validation/ValidationOverview.md`: `IValidator` usable directly with MVUX.
- `doc/Reference/Reactive/rules.md`: `FEED2003`.
- Playground validation page: a submit command validated on execution.
