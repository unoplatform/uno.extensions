# 015 — MVUX validation via `INotifyDataErrorInfo` on the generated ViewModel

**Status:** Implemented (v1) with the proposed defaults for §7, pending review — see [progress.md](progress.md)
**Area:** `Uno.Extensions.Reactive` (message axis, state hook, `BindableViewModelBase` / `Bindable<T>`), `Uno.Extensions.Reactive.Generator` (collision diagnostics only)
**Related:** `Uno.Extensions.Validation` (`IValidator`) — optional producer, no hard dependency; spec 013 (mocking can drive the new axis)

## 1. Problem

MVUX has no validation story. `Uno.Extensions.Validation` (`IValidator.ValidateAsync(object) → IEnumerable<ValidationResult>`) is MVVM-shaped: it *reads* `INotifyDataErrorInfo` from the validated object (`Validator.cs:32`) but nothing *implements* it. The generated MVUX ViewModel (`BindableViewModelBase`, generated `Bindable<TRecord>` subclasses) implements only `INotifyPropertyChanged` — so controls/templates that rely on `INotifyDataErrorInfo` (INDEI) get nothing, and apps fall back to hand-written `IFeed<string?> FirstNameError` boilerplate per field.

## 2. Goals / non-goals

**Goals**
- The generated intermediary VM (the object the view data-binds to) implements `System.ComponentModel.INotifyDataErrorInfo`.
- Validation results are **produced inside MVUX** and travel **on the state's own messages** (a new message axis) — **no `IFeed<ValidationResult>` is exposed to or authored by the app**. The VM reroutes them to INDEI.
- Results addressed to members of a record-typed state (`ValidationResult.MemberNames = ["FirstName"]`) surface on the bindable that owns the bound leaf (`BindablePerson.GetErrors("FirstName")`), so `{Binding Person.FirstName}` works.
- Validation never blocks a value: invalid input still flows into the state; results only annotate it.
- No dependency from `Uno.Extensions.Reactive` on `Uno.Extensions.Validation` (`ValidationResult` is BCL — `System.ComponentModel.DataAnnotations`).

**Non-goals (v1)**
- Rendering errors in controls. WinUI 3 controls do not visualise INDEI natively; display is via app templates/bindings to `HasErrors`/`GetErrors` (see Risks).
- `IListState` item-level validation.
- Command `CanExecute` gating (apps can bind to `HasErrors`; built-in gating is a follow-up).
- Changing `FeedView` or the `Error` axis semantics.

## 3. Why a new axis (not the `Error` axis, not a feed)

| Channel | Why not |
| --- | --- |
| `MessageAxis.Error` | Exception semantics: flips `FeedView` into its error template and hides the value. Validation must keep the value displayed. |
| A companion `IFeed<IImmutableList<ValidationResult>>` | Explicitly rejected: per-field boilerplate in the model, a second subscription per property, results can desync from the value they describe. |
| **New `Validation` axis on the state's messages** | Results are atomically paired with the data version they validate; the VM already consumes every `Message<T>` of every property (`BindableViewModelBase.CreateProperty → ViewModelToView.OnMessage`) and today just ignores non-`Data` changes. |

## 4. Design

### 4.1 Core — `ValidationAxis`
- `MessageAxes.Validation` (public const) and `MessageAxis.Validation` → `MessageAxis<IImmutableList<ValidationResult>>`, modelled on `MessageAxis.Error` (`Core/Axes/MessageAxis.cs:28`).
  - `Aggregate`: concatenation (for combined sources).
  - Equality: sequence equality on (`ErrorMessage`, `MemberNames`) so identical re-validations don't raise `ErrorsChanged`.
  - Not transient: results stay until replaced.
- Builder/entry helpers mirroring `Error(...)` in `MessageAxisExtensions.cs`:
  - `TBuilder Validation<TBuilder>(this TBuilder, IEnumerable<ValidationResult>? results)` (null/empty = clear)
  - `IImmutableList<ValidationResult> GetValidation(this IMessageEntry)` + `MessageEntry<T>.Validation` property.
- **Propagation:** the axis must **not** leak through projections (`Select`, `SelectAsync`, `Combine`, …) — a derived `FullName` must not show `Person`'s errors. Operators that forward parent axes must drop `Validation`.
  - *Implemented:* all operators forwarded every parent axis (`MessageManager`, `CombineFeedHelper`, `DynamicParentMessage`), so the axis is flagged with a new internal `MessageAxis.IsLocal`. `MessageManager` (and `MessageBuilder`) ignore parent values of local axes, `CombineFeedHelper` skips them (`DynamicParentMessage` goes through a `MessageManager`).
  - Exception: the `UpdateFeed` inside a state opts in (`forwardLocalAxes: true`), as its parent is the same logical feed (on hot-reload the source of the old state is the new state). A local value replaces the parent one (no aggregation).

### 4.2 Producer — how results get onto the state (MVUX side)

Two entry points, both writing the axis through the existing `StateImpl.UpdateMessageAsync` pipeline:

1. **Manual (always available):**
   ```csharp
   await Person.UpdateMessageAsync(m => m.Validation(results), ct);
   ```
2. **Declarative validator hook on the state (recommended surface):**
   ```csharp
   public IState<Person> Person => State.Value(this, () => new Person())
       .Validate((person, ct) => _validator.ValidateAsync(person, null, ct));
   ```
   - Registers a validator **on the `StateImpl` itself** and returns the same instance (the VM requires a `StateImpl<T>` — `BindableViewModelBase.Property(string, IState<T>)` throws for custom implementations, so a wrapping state is not an option).
   - Idempotent: model property getters are re-evaluated, so re-registration replaces, never stacks.
   - After every `Data` change: cancel the in-flight validation, run the validator off the UI thread, then write `Validation` **only if the data is still the version that was validated** (reference/version check) — stale results are discarded.
   - Validator exceptions are logged and do not fault the state (not surfaced as `Error` axis). Previous results are kept.
   - The validator runs only when the state has a value: `None` clears the results, `Undefined` keeps them.
   - `IValidator` fits the delegate directly (`_validator.ValidateAsync`), so no bridge package is required. A convenience `Validate(this IState<T>, IValidator)` overload is an open question (§7 Q3).

### 4.3 Consumer — the VM reroutes to INDEI

**`BindableViewModelBase : INotifyDataErrorInfo`** (top-level model properties)
- In `ViewModelToView.OnMessage`, add an **independent** branch: `if (msg.Changes.Contains(MessageAxis.Validation))` → capture results, dispatch to UI thread (same coalescing pattern as `UpdateValue`). This branch must **not** be subject to the `BindingSource` re-entrancy check that filters `Data`: results for a value the user just typed must still reach the view.
- On the UI thread, route each result:
  - `MemberNames` empty, or equal to the property name → property-level: `GetErrors("Person")`.
  - `MemberNames = ["FirstName"]` / dotted `"Address.Street"` → forwarded to the sub-bindable (§ below).
- Store: lazily-allocated `Dictionary<string, IImmutableList<ValidationResult>>`, UI-thread only (no locks; WASM-safe).
- Raise `ErrorsChanged(propertyName)` per changed key; raise `PropertyChanged(nameof(HasErrors))` only when `HasErrors` flips.
- `HasErrors` aggregates own entries + all sub-bindables.

**`Bindable<T> : INotifyDataErrorInfo`** (generated record bindables, e.g. `BindablePerson`)
- WinUI queries INDEI on the **source object of the leaf** of a binding path; for `{Binding Person.FirstName}` that is `BindablePerson`, so it must implement INDEI too.
- `BindablePropertyInfo<T>` gains an internal error-subscription channel alongside `Subscribe(Action<T>)`; the parent pushes the member-scoped slice, the child applies the same store/raise logic, recursing for nested records via `Property<TProperty>(...)`.
- `GetErrors(null or "")` returns the entity-level errors (matching WinUI's "whole object" convention). On the VM there is no entity-level error, so it returns an empty list.
- A result targeting a record-typed member (`["Address"]`) is exposed on the owner (`GetErrors("Address")`) and as entity-level error of the sub-bindable.
- Results received before a sub-bindable subscribes are re-routed when it subscribes (the base `Bindable<T>` ctor subscribes to its owner before the generated ctor creates the sub-bindables).

**Generator**
- No emission change needed: generated VMs and record bindables derive from the two bases above.
- New diagnostic when a model/record already declares `HasErrors`, `GetErrors` or `ErrorsChanged` → base uses explicit interface implementation and the public `HasErrors` is not exposed for that type (§7 Q4).
  - *Implemented:* both bases implement the three INDEI members explicitly, and expose a public `HasErrors` for bindings. Only `HasErrors` can therefore collide: the generated member hides it (generated code already disables warnings), INDEI keeps working, and `FEED1001` (Info, to not break `TreatWarningsAsErrors` apps) is reported.

### 4.4 Data flow
```
user types → Bindable.SetValue → StateImpl.UpdateMessageAsync (Data, BindingSource)
          → validator hook (async, cancel-previous) → UpdateMessageAsync (Validation)
          → VM OnMessage [Validation changed] → UI thread → route by MemberNames
          → ErrorsChanged("FirstName") on BindablePerson / HasErrors on VM
```

## 5. Public API delta (all additive)
- `MessageAxes.Validation`, `MessageAxis.Validation`
- `MessageAxisExtensions.Validation(...)`, `GetValidation(...)`, `MessageEntry<T>.Validation`
- `State.Validate<T>(this IState<T>, Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>>)`
- `BindableViewModelBase` and `Bindable<T>` now implement `INotifyDataErrorInfo` (`HasErrors`, `GetErrors`, `ErrorsChanged` — `EventHandler<DataErrorsChangedEventArgs>`, compliant with the events rule), explicitly, plus a public `bool HasErrors` on both.
- Docs: new `doc/Learn/Mvux/Advanced/Validation.md` + TOC, cross-link from `doc/Learn/Validation/ValidationOverview.md`.

Not breaking: interfaces are added to types marked `EditorBrowsable(Advanced/Never)` and intended for generated code only; the collision case is handled by §4.3 Generator.

## 6. Performance / platform
- Zero cost when unused: one `Changes.Contains(axis)` check per message; no store allocation until a result arrives (except a small sub-bindable registry for VMs/bindables that have record-typed members).
- UI-thread-only store; dispatch via the existing `_dispatcher` — no locks (WASM).
- Validator runs off the UI thread with cancellation honoured.

## 7. Open questions for review
1. **Producer surface:** `Validate` mutating the cached `StateImpl` vs an overload on `State.Value(this, init, validate:)` / `State.FromFeed(...)`. The former composes with every state factory; the latter avoids a mutating extension.
2. **When to validate:** every data change (proposed) vs only after first user edit ("touched") vs explicit `ValidateAsync()` (e.g. on Save). Likely need both "on change" and an explicit trigger that validates untouched fields on submit.
3. **`IValidator` convenience:** add `Validate(IState<T>, IValidator)`? Where — Reactive (dependency on Validation, rejected), Validation (dependency on Reactive), or a new thin `Uno.Extensions.Reactive.Validation` package?
4. **Name collisions** with `HasErrors`/`GetErrors`/`ErrorsChanged` on the model: explicit impl + diagnostic (proposed) vs generator-prefixed names.
5. **Read-only feeds** (`IFeed<T>` model properties): reroute an upstream-set `Validation` axis too, or restrict to `IState<T>`? (Proposed: restrict to states in v1, given §4.1 propagation rule.)
6. **`IListState<T>`** item-level errors — defer to follow-up?

## 8. Risks
- **Rendering:** WinUI 3 (Windows) dropped built-in INDEI visuals; Uno Skia behaviour needs verification. Value of v1 is the contract (templates, Toolkit, `HasErrors` binding, programmatic access). Verify on Windows + Skia desktop + WASM before merge.
- **Axis forwarding in operators:** if operators forward all parent axes today, dropping `Validation` needs a per-axis rule (a flag on `MessageAxis`, e.g. `IsLocal`), which is itself a small core change.
- **Stale results** if the version check in §4.2 is wrong — covered by tests below.
- **Hot reload:** `BindableViewModelBase.HotReload.cs` swaps feeds/models; the error store must be cleared/re-subscribed on swap. *Implemented:* the error store is driven only by the messages of the (kept) state, so it follows the swap; the hot-swapped state stops its own validator, the updated state's validator owns validation.

## 9. Tests
- `Uno.Extensions.Reactive.Tests`
  - `Given_ValidationAxis`: set/clear, aggregate, equality (no duplicate change on identical results), not propagated through `Select`/`Combine`.
  - `Given_StateWithValidation`: runs on data change; cancels previous run; discards stale result; validator exception logged, state not faulted; idempotent registration; completes with the state / honours dispose.
  - `Given_BindableViewModel_Validation`: `ErrorsChanged` + `HasErrors` flip for a primitive state; routing by `MemberNames` into generated record bindable; nested record path; results still delivered when the change originated from the binding (`BindingSource`); no allocation/no event when the axis is never set.
- Generator: fixture with `HasErrors` collision → diagnostic; clean-rebuild check.
- `Uno.Extensions.Reactive.UI.Tests` / RuntimeTests: two-way bound `TextBox` → `ErrorsChanged` observed on UI thread; hot-reload swap clears/restores errors.
- Release build of `Uno.Extensions-packageonly.slnf` warning-free.
