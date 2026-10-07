# 016 — Localized validation messages

**Status:** Implemented, pending review — see [progress.md](progress.md)
**Area:** `Uno.Extensions.Reactive` (MVUX `State.Validate`), `Uno.Extensions.Validation.Fluent` (member names), docs for Validation, MVUX and Localization
**Related:** [spec 015](../015-mvux-validation-indei/spec.md) (MVUX validation / INDEI) — consumer of the messages; `Uno.Extensions.Localization.WinUI` (`IStringLocalizer` → `ResourceLoaderStringLocalizer`, `.resw`)

## 1. Problem
Validation errors reach the view as `ValidationResult.ErrorMessage` strings (spec 015 INDEI). Apps using `UseLocalization` cannot localize them cleanly:

| Producer | Today | Gap |
| --- | --- | --- |
| DataAnnotations via `IValidator` | `TryValidateObject`, messages as written in `ErrorMessage` | `ErrorMessageResourceType` needs static-property resx classes, not `.resw` |
| FluentValidation via `IValidator` | `LanguageManager` defaults or `WithMessage(...)` | `PropertyName` is dropped from `MemberNames`, so errors don't route to the field (spec 015 §4.3) |
| Custom `Validate` delegate (MVUX) | App builds `ValidationResult` | Localizing means injecting `IStringLocalizer` and resolving each message by hand; single-rule checks need a hand-built `ValidationResult` list |

## 2. Goals / non-goals
**Goals**
- **One** localization entry point: the MVUX `State.Validate`, for every validator (custom delegates and `IValidator`, i.e. DataAnnotations and FluentValidation).
- `Uno.Extensions.Validation` stays localization-free: a resource key used as message comes out as-is (§4.1). No `Microsoft.Extensions.Localization*` dependency there.
- Fluent results keep `MemberNames` so messages land on the right field.
- Single-rule MVUX validators without hand-building `ValidationResult` lists.
- No dependency from `Uno.Extensions.Reactive` on `Uno.Extensions.Validation` or `Uno.Extensions.Localization*`; only `Microsoft.Extensions.Localization.Abstractions`.

**Non-goals**
- Format arguments in localized messages (DataAnnotations `{0}` display name, `{1}` length, ...): resolved messages are not formatted (§4.3).
- Localized display names (`[Display(Name)]`) and localized DataAnnotations default messages.
- Giving the app `IServiceProvider` to the `ValidationContext` created by `IValidator` (custom attributes localizing themselves).
- Re-localizing already-produced errors on a **runtime** culture switch (Localization requires restart and exposes no change signal).
- Changing FluentValidation's own localization (`LanguageManager`).

## 3. Where to localize
| Option | Verdict |
| --- | --- |
| Key + args on a `ValidationResult` subclass, localized by the bindable/template at display time | Rejected: Reactive has no DI / `IStringLocalizer` access; every INDEI consumer reads `ErrorMessage`; only pays off with runtime culture switching, which Localization doesn't support. |
| In each producer: `UseLocalizedDataAnnotations` in `Uno.Extensions.Validation` (attribute-by-attribute re-implementation of `TryValidateObject`, localized messages and display names with format arguments) | Rejected after implementation (v1, kept on branch `dev/xygu/20261005/spec-016-validation-localization-v1`): a second localization path beside MVUX, a Localization dependency in the pure validation layer, and a re-implementation of the BCL validation order to maintain. The cost is losing format arguments and display names for DataAnnotations. |
| **In the MVUX `Validate`, the validator's messages being resource keys** | Chosen: a single entry point for every validator; the validation layer is unchanged; messages are final strings on the axis, so spec-015 equality/dedup is unchanged. The validator runs on the thread pool, whose `CurrentUICulture` = `DefaultThreadCurrentUICulture` set by `LocalizationService.ApplyCurrentCulture` (verify, §7). |

## 4. Design

### 4.1 Validation layer: keys as-is (`Uno.Extensions.Validation`)
- No change: `[Required(ErrorMessage = "Validation_Required")]` gives the message `Validation_Required`. DataAnnotations format the `ErrorMessage` with `string.Format` (display name, attribute arguments), which leaves a key without `{`/`}` unchanged — documented: keys must not contain braces.
- FluentValidation: `.WithMessage("Validation_Required")` is reported as-is (its placeholders are `{PropertyName}`-style, absent from a key).
- Covered by contract tests (`Given_Validator`, `Given_FluentValidator`) so a future change does not silently start altering keys.

### 4.2 Fluent member names (`Uno.Extensions.Validation.Fluent`)
- `new ValidationResult(x.ErrorMessage, string.IsNullOrEmpty(x.PropertyName) ? [] : [x.PropertyName])`. Bug fix, required for spec-015 routing; red/fix/green test.

### 4.3 MVUX `Validate` overloads (`Uno.Extensions.Reactive`, `State.Extensions.cs`)
Every overload takes an optional localizer as its **last** parameter:
```csharp
// 1. Base (spec 015 §4.2): the only place where localization is handled.
IState<T> Validate<T>(this IState<T> state,
    Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator,
    IStringLocalizer? localizer = null);
// 2. Single rule: returns the error (message, or key when localized); null/empty = valid.
IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, string?> validator, IStringLocalizer? localizer = null);
// 3. Predicate (true = valid, same polarity as FluentValidation `Must`) + error (message, or key when localized).
IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, bool> isValid, string error, IStringLocalizer? localizer = null);
```
```csharp
public partial record PersonModel(IStringLocalizer Localizer, IValidator Validator)
{
    public IState<string> Name => State.Value(this, () => string.Empty)
        .Validate(async (name, ct) => !string.IsNullOrWhiteSpace(name), "Validation_NameRequired", Localizer);

    public IState<Person> Person => State.Value(this, () => new Person())
        .Validate((person, ct) => Validator.ValidateAsync(person, null, ct), Localizer);
}
```
- **Composition:** 2 and 3 only adapt their delegate into the base shape and forward `localizer`, so cancellation, stale-result discarding, exception handling (logged, previous results kept) and idempotent registration are inherited unchanged.
- **Localization (base, `localizer` not null):** the validator is wrapped; the `ErrorMessage` of each result is a resource key.
  - Found → `new ValidationResult(localized.Value, result.MemberNames)`.
  - Not found (`ResourceNotFound`), null/empty message, or null entry → the result as produced. The not-found value is not used: `ResourceLoaderStringLocalizer` returns the key with `.` replaced by `/` after its retry, which would alter plain (non-key) messages.
  - Results are materialized eagerly, so the localizer runs once per result, on the validator thread (current culture, §3) — not each time the axis is enumerated, possibly on the UI thread.
  - Resolved messages are **not** formatted: a `{0}` in a resource is displayed as-is. Messages with arguments: no localizer on `Validate`, `Localizer[key, args]` in the validator.
  - Not resolved at registration: model property getters can run before the culture is applied, and a cached string would never follow it.
- **`localizer` null:** the validator is used unchanged (spec 015 behaviour).
- **Result shape (2, 3):** valid → no results (shared empty array); invalid → one `ValidationResult` with empty `MemberNames`, i.e. property-level per spec 015 §4.3.
- **Guards:** null delegate → `ArgumentNullException`; null/empty `error` → `ArgumentException`, thrown at registration. A null `localizer` is valid.
- **Overload resolution caveat:** a lambda whose body only throws converts to both the base delegate and overload 2 (`CS0121`). Lambdas that return a value are unambiguous. Documented: give a throw-only lambda an explicit return type. `cref`s to `Validate` name the full signature (`CS0419`).
- **Compatibility:** spec 015 has not shipped, so adding the optional parameter to the base overload is not a break.
- **Dependency:** `Uno.Extensions.Reactive` references `Microsoft.Extensions.Localization.Abstractions` (abstractions only; precedent: `Uno.Extensions.Navigation`). The localizer is passed explicitly (model ctor injection), as Reactive has no DI access.

## 5. Public API delta (additive vs `main`)
- `Uno.Extensions.Reactive`: three `State.Validate<T>` overloads (§4.3) — the base one is spec 015's, with the trailing `IStringLocalizer? localizer = null`.
- Behaviour: Fluent results carry `MemberNames`.
- New package reference: `Microsoft.Extensions.Localization.Abstractions` on `Uno.Extensions.Reactive`.
- `Uno.Extensions.Validation`: unchanged.

## 6. Performance / platform
- No localizer: zero change.
- With a localizer: one lookup and one list per validation run (i.e. per data change under MVUX `Validate`), plus one `ValidationResult` per localized message. Runs on the validator's thread; no UI-thread dependency, no locks (WASM-safe).

## 7. Risks / verify
- `ResourceLoaderStringLocalizer` returns the selected culture's strings from a **thread-pool** thread on Windows (packaged + unpackaged), Skia desktop, WASM, Android, iOS.
- `CurrentUICulture` on thread-pool threads equals the selected culture after `LocalizationService.ApplyCurrentCulture` (it sets `DefaultThreadCurrentUICulture`).

## 8. Tests
- **`src/Uno.Extensions.Validation.Tests`** (new, net10.0 MSTest, discovered by `**/*.Tests.dll`):
  - `Given_Validator`: DataAnnotations keys passed as-is, including attributes with format arguments (`StringLength`, `Range`).
  - `Given_FluentValidator`: `MemberNames` contains `PropertyName` (red first); object-level failure keeps empty `MemberNames`; `WithMessage` key passed as-is.
- **`Given_StateWithValidation`** (fake localizer): overloads 2/3 plain and localized; key not found keeps the message (key containing `.`); localizer read at validation time; list results localized with `MemberNames` kept and unfound keys untouched; localizer called once per result; braces not formatted; null localizer leaves messages unchanged; argument guards; re-registration replaces the base validator; `IValidator` + DataAnnotations keys localized on the axis (integration guard).
- **Runtime/manual:** TestHarness `Ext/Localization` page with a validated field, switch culture + restart, verify on Windows + Skia desktop + WASM (§7).
- Release build of `Uno.Extensions-packageonly.slnf` warning-free.

## 9. Docs
- `doc/Learn/Validation/ValidationOverview.md`: "Validation messages" — reported as written, use keys and localize in MVUX; no braces in keys; Fluent `MemberNames`.
- `doc/Learn/Mvux/Advanced/Validation.md`: single-rule overloads + "Localizing messages" (trailing localizer, `IValidator`, not-found, no formatting, restart caveat).
- `doc/Learn/Localization/LocalizationOverview.md`: cross-link to the MVUX section.
