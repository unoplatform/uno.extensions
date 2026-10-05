# 016 — Localized validation messages

**Status:** Draft — proposal, pending review
**Area:** `Uno.Extensions.Validation` (DataAnnotations `Validator`), `Uno.Extensions.Validation.Fluent` (member names), `Uno.Extensions.Reactive` (MVUX `Validate` overloads), docs for MVUX and Localization
**Related:** [spec 015](../015-mvux-validation-indei/spec.md) (MVUX validation / INDEI) — consumer of the messages; `Uno.Extensions.Localization.WinUI` (`IStringLocalizer` → `ResourceLoaderStringLocalizer`, `.resw`)

## 1. Problem
Validation errors reach the view as `ValidationResult.ErrorMessage` strings (MVVM `IValidator` users and MVUX `Validate`/INDEI alike). Apps using `UseLocalization` cannot localize them cleanly:

| Producer | Today | Gap |
| --- | --- | --- |
| DataAnnotations via `IValidator` (`Validator.cs`) | `TryValidateObject` with default messages | Default messages English-only; `ErrorMessageResourceType` needs static-property resx classes, not `.resw`; `ValidationContext` has no service provider, so custom attributes can't get `IStringLocalizer` |
| `[Display(Name=…)]` (`{0}` in messages) | Literal name | Same resx-vs-resw mismatch |
| FluentValidation (`FluentValidator.cs`) | Messages via FluentValidation `LanguageManager` (`CurrentUICulture`) or `WithMessage(...)` | Localization OK via DI; but `PropertyName` is dropped from `MemberNames`, so errors don't route to the field (spec 015 §4.3) |
| Custom `Validate` delegate (MVUX) | App builds `ValidationResult` | Works by injecting `IStringLocalizer` in the model ctor — undocumented; every single-rule check needs a hand-built `ValidationResult` list |

## 2. Goals / non-goals
**Goals**
- DataAnnotations messages and display names resolved through the app's `IStringLocalizer` (`.resw`) with correct format arguments, with no attribute changes beyond using a resource key as `ErrorMessage`/`Name`.
- Optional: an app that never calls `UseLocalization` keeps today's behaviour exactly.
- `ValidationContext` created by `Validator` exposes the app `IServiceProvider` (custom attributes / `IValidatableObject` can localize themselves).
- Fluent results keep `MemberNames` so localized messages land on the right field.
- Single-rule MVUX validators, plain or localized, without hand-building `ValidationResult` lists (§4.5).
- No dependency from `Uno.Extensions.Reactive` on `Uno.Extensions.Validation` or `Uno.Extensions.Localization*`; only `Microsoft.Extensions.Localization.Abstractions` (§4.5). MVUX API change is additive only.

**Non-goals (v1)**
- Re-localizing already-produced errors on a **runtime** culture switch (Localization requires restart and exposes no change signal; see §7 Q1).
- Storing message keys/arguments on the `Validation` axis or localizing at display time (§3).
- Shipping translated default DataAnnotations messages for every culture (apps provide their own `.resw`; see §7 Q3).
- Changing FluentValidation's own localization (`LanguageManager`).

## 3. Why localize at the producer (not on the axis / at display time)
| Option | Verdict |
| --- | --- |
| Key + args on `ValidationResult` subclass, localized by the bindable/template | Rejected: Reactive has no DI / `IStringLocalizer` access (services only reach models via ctor); every INDEI consumer reads `ErrorMessage`; only pays off with runtime culture switching, which Localization doesn't support. |
| **Localize when the validator runs** | Chosen: messages are final strings, spec-015 equality/dedup unchanged, works for MVVM and MVUX identically. The validator runs on the thread pool, whose `CurrentUICulture` = `DefaultThreadCurrentUICulture` set by `LocalizationService.ApplyCurrentCulture` (verify, §8). |

## 4. Design

### 4.1 `Validator` gets the service provider into `ValidationContext`
- `context ??= new ValidationContext(instance, _services, items: null);` (`Validator.cs:29`). Additive; an app-supplied context is untouched.
- Enables `validationContext.GetService<IStringLocalizer>()` in custom `ValidationAttribute.IsValid(object, ValidationContext)` / `IValidatableObject.Validate`.

### 4.2 Localized DataAnnotations (`Uno.Extensions.Validation`)
Prior art: ASP.NET Core `MvcDataAnnotationsLocalizationOptions.DataAnnotationLocalizerProvider` + attribute adapters — `ErrorMessage` is treated as a resource key and formatted with attribute-specific arguments.

- **Opt-in surface** (hosting convention):
  ```csharp
  .UseValidation(configure: b => b.UseLocalizedDataAnnotations())            // default provider
  .UseValidation(configure: b => b.UseLocalizedDataAnnotations(o =>
        o.LocalizerProvider = (modelType, sp) => sp.GetRequiredService<IStringLocalizer>()))
  ```
  `DataAnnotationsLocalizationOptions` (public record/class): `Func<Type, IServiceProvider, IStringLocalizer?> LocalizerProvider` (default: resolve non-generic `IStringLocalizer` from DI, null if absent), `string? DefaultMessageKeyFormat` (§4.3). Bound via `IOptions<>`.
- **Dependency:** `Uno.Extensions.Validation` references `Microsoft.Extensions.Localization.Abstractions` (already in `src/Directory.Packages.props`, net10.0, non-UI). **No** reference to `Uno.Extensions.Localization.WinUI`; the localizer is optional.
- **Mechanism:** when a localizer is available, `Validator` stops calling `TryValidateObject` and validates **attribute by attribute** (same semantics: property attributes incl. `validateAllProperties: true`, then class-level attributes, then `IValidatableObject` only if no prior errors — mirror `System.ComponentModel.DataAnnotations.Validator` order):
  - For each failing attribute: if `ErrorMessage` is set and `ErrorMessageResourceType`/`ResourceName` are **not**, message = `localizer[attribute.ErrorMessage, args]`; if the key is not found (`ResourceNotFound`), fall back to the attribute's own `FormatErrorMessage(displayName)` (today's text).
  - Attributes using `ErrorMessageResourceType` keep their existing behaviour (explicit wins).
  - `MemberNames` = the property name, as today.
  - **Never mutate attribute instances** (they are cached/shared by `TypeDescriptor`; not thread-safe).
- **Why `ErrorMessage` is the key, not `ErrorMessageResourceName`:**
  | Reason | Detail |
  | --- | --- |
  | Not a standalone key | BCL `ValidationAttribute` throws `InvalidOperationException` when formatting if `ErrorMessageResourceName` is set without `ErrorMessageResourceType` (or vice versa), or together with `ErrorMessage`. |
  | `.resw` has no static type | `ErrorMessageResourceType.<ResourceName>` is read by reflection and must be a static `string` property — the `.resx` designer shape. `.resw`/PRI is reached via `ResourceLoader` / injected `IStringLocalizer`; there is no generated class to point at. |
  | Hand-written wrapper is a poor fit | One static property per key; a static service locator (`ResourceLoader.GetForViewIndependentUse()`) bypassing the DI localizer; property-name reflection (trimming/AOT). Still supported as-is ("explicit wins" above), not the recommended path. |
  | Opt-out must stay safe | Name-only attributes would throw on the BCL path (`TryValidateObject` without opt-in, or any other consumer validating the same model). With `ErrorMessage` as key, the worst case without opt-in is the key text shown as the message. |
  | Prior art | ASP.NET Core's `DataAnnotationLocalizerProvider` uses `ErrorMessage` as the localizer key for the same reasons. |
- **Format arguments** (internal table, mirroring the attributes' own `FormatErrorMessage`): `{0}` display name for all; `StringLength` → `{1}` max, `{2}` min; `Range` → `{1}` min, `{2}` max; `MinLength`/`MaxLength`/`Length` → `{1}` length(s); `Compare` → `{1}` other property display name; `RegularExpression` → `{1}` pattern. Unknown/custom attributes → `{0}` only (they can localize themselves via §4.1).
- Metadata reflection cached per type (`ConcurrentDictionary<Type, …>`); no per-call LINQ allocations beyond results. AOT: same trimming annotations as the current `TryValidateObject` path.

### 4.3 Display names and default messages
- `[Display(Name = "Person_FirstName")]` without `ResourceType` → `localizer["Person_FirstName"]` if found, else the literal (then `[DisplayName]`, then property name). `Display.ResourceType` has the same static-property requirement as `ErrorMessageResourceType` (§4.2), so `Name` is used as the key.
- `ErrorMessage` not set → optional convention key via `DefaultMessageKeyFormat`, e.g. `"Validation_{0}"` → `Validation_Required`, `Validation_StringLength` (attribute type name minus `Attribute`). Null by default = today's English default (§7 Q3).
- Keys go through `ResourceLoaderStringLocalizer` unchanged, so its existing `.` → `/` fallback applies; docs recommend `_`-separated keys to avoid clashing with `x:Uid` property syntax.

### 4.4 Fluent member names (`Uno.Extensions.Validation.Fluent`)
- `new ValidationResult(x.ErrorMessage, string.IsNullOrEmpty(x.PropertyName) ? [] : [x.PropertyName])`. Behavioural fix (bug), required for spec-015 routing; red/fix/green test.
- Localization stays FluentValidation's: `LanguageManager` (culture from `CurrentUICulture`) or `.WithMessage(_ => localizer["Key"])` with `IStringLocalizer` injected into the `AbstractValidator<T>` ctor (resolved from DI — validators are registered there).

### 4.5 MVUX — single-rule `Validate` overloads (`Uno.Extensions.Reactive`)
Shortcuts over the existing `Validate(Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>>)` (spec 015 §4.2), in `State.Extensions.cs`:
```csharp
// 1. Delegate returns the error message; null/empty = valid.
IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, string?> validator);
// 2. Predicate + fixed message.
IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, bool> isValid, string errorMessage);
// 3. Delegate returns a resource key; null/empty = valid.
IState<T> Validate<T>(this IState<T> state, IStringLocalizer localizer, AsyncFunc<T, string?> validator);
// 4. Predicate + resource key.
IState<T> Validate<T>(this IState<T> state, IStringLocalizer localizer, AsyncFunc<T, bool> isValid, string errorMessageKey);
```
```csharp
public partial record PersonModel(IStringLocalizer Localizer)
{
    public IState<string> Name => State.Value(this, () => string.Empty)
        .Validate(Localizer, async (name, ct) => !string.IsNullOrWhiteSpace(name), "Validation_NameRequired");
}
```
- **Composition:** each overload wraps its delegate into the base one and calls it, so cancellation, stale-result discarding, exception handling (logged, previous results kept) and idempotent registration are inherited unchanged.
- **Result shape:** valid → no results (shared empty array, no allocation); invalid → one `ValidationResult` with empty `MemberNames`, i.e. property-level per spec 015 §4.3 (`GetErrors("Name")`; entity-level on a record sub-bindable). Member-scoped or multiple results → base overload.
- **Polarity:** `isValid` returns `true` when the value is valid (same as FluentValidation `Must`).
- **Localization (3, 4):** `localizer[key]` is resolved each time a failure is produced, on the validator thread, so the culture rules of §3 apply. Not resolved at registration: model property getters can run before the culture is applied, and a cached string would never follow it. Key not found → the key text (`IStringLocalizer` contract; `ResourceLoaderStringLocalizer` returns `name`). No format arguments in v1 (§7 Q7).
- **Guards:** null delegate/localizer → `ArgumentNullException`; null/empty `errorMessage`/`errorMessageKey` → `ArgumentException`, thrown at registration.
- **Dependency:** `Uno.Extensions.Reactive` references `Microsoft.Extensions.Localization.Abstractions` (abstractions only; precedent: `Uno.Extensions.Navigation`). No reference to `Uno.Extensions.Localization.WinUI` or `Uno.Extensions.Validation`. The localizer is passed explicitly (model ctor injection), as Reactive has no DI access (§3).
- **Docs:** "Localizing messages" in `doc/Learn/Mvux/Advanced/Validation.md` covers these overloads, the base overload with an injected localizer for multi-result/member-scoped/formatted cases, and the `IValidator` + `UseLocalizedDataAnnotations` path. State the restart caveat (link Localization overview).

## 5. Public API delta (additive)
- `Uno.Extensions.Validation`: `DataAnnotationsLocalizationOptions`; `IValidationBuilder UseLocalizedDataAnnotations(this IValidationBuilder, Action<DataAnnotationsLocalizationOptions>? configure = null)`.
- Behaviour: `Validator` passes `IServiceProvider` into the `ValidationContext` it creates; Fluent results carry `MemberNames`.
- `Uno.Extensions.Reactive`: four `State.Validate<T>` overloads (§4.5).
- New package reference: `Microsoft.Extensions.Localization.Abstractions` on `Uno.Extensions.Validation` and `Uno.Extensions.Reactive`.

## 6. Performance / platform
- Off by default: zero change for apps not opting in.
- Per-type attribute/metadata cache; localizer lookups only for failing attributes.
- Runs on the validator's thread (thread pool under MVUX `Validate`); no UI-thread dependency, no locks beyond the concurrent cache (WASM-safe).

## 7. Open questions (proposed defaults)
1. **Runtime culture switch:** re-validate states / refresh `IValidator` results when culture changes? *Proposed:* out of scope until Localization exposes a culture-changed signal (would need a public event on `ILocalizationService`, itself an API change); then MVUX could re-run `Validate` validators.
2. **Opt-in vs automatic** when `IStringLocalizer` is registered. *Proposed:* explicit `UseLocalizedDataAnnotations()` — automatic would silently change messages whose `ErrorMessage` happens to match a resource key.
3. **Default messages:** ship a key convention only (proposed), or ship a neutral `.resw`/resx of default messages in the package? *Proposed:* convention only; docs provide a copy-paste English `.resw` block.
4. **Package placement:** in `Uno.Extensions.Validation` (proposed, one new abstractions reference) vs a new `Uno.Extensions.Validation.Localization` package.
5. **Fluent member-name fix** — ship in this spec (proposed) or separately as a bug fix PR.
6. **Localizer parameter type** on the MVUX overloads: `IStringLocalizer` (proposed; needs the abstractions reference in Reactive) vs `Func<string, string>` (no dependency, less discoverable, loses `ResourceNotFound`).
7. **Format arguments** on the key overloads (e.g. `Func<T, object[]>`)? *Proposed:* not in v1 — overload 1 with `Localizer[key, args]` covers it.

## 8. Risks / verify
- `ResourceLoaderStringLocalizer` returns the selected culture's strings from a **thread-pool** thread on Windows (packaged + unpackaged), Skia desktop, WASM, Android, iOS.
- `CurrentUICulture` on thread-pool threads equals the selected culture after `LocalizationService.ApplyCurrentCulture` (it sets `DefaultThreadCurrentUICulture`) — note the `IOptionsMonitor.OnChange` path updates only Default* cultures.
- Re-implementing the DataAnnotations validation order must match `TryValidateObject` exactly (required-first, `IValidatableObject` short-circuit) — covered by parity tests against the BCL for unlocalized input.
- Trimming/AOT of reflection over attributes — reuse existing annotations; check `Uno.Extensions.Serialization.AotTests`-style warnings in Release build.

## 9. Tests
- **New project `src/Uno.Extensions.Validation.Tests`** (net10.0 MSTest, FluentAssertions; discovered by `**/*.Tests.dll`, non-empty for `TreatNoTestsAsError`); fake in-memory `IStringLocalizer`.
  - `Given_Validator`: context carries services (custom attribute resolves localizer); no-localizer path identical to BCL `TryValidateObject` (parity).
  - `Given_LocalizedDataAnnotations`: key found → localized; key missing → original message; `ErrorMessageResourceType` untouched; format args for `StringLength`/`Range`/`MinLength`/`Compare`/`RegularExpression`; display-name localization; `DefaultMessageKeyFormat`; `IValidatableObject` order; culture read from `CurrentUICulture`.
  - `Given_FluentValidator`: `MemberNames` contains `PropertyName` (red first); `WithMessage(localizer)` flows.
  - `Given_UseLocalizedDataAnnotations`: registration + options binding; no-op without `UseLocalization`.
- **Reactive** (`Given_StateWithValidation`, fake localizer):
  - one case using `IValidator` + localized DataAnnotations → localized `ErrorMessage` on the axis (integration guard);
  - per §4.5 overload: valid → results cleared; invalid → single result with the message/localized key and empty `MemberNames`; key missing → key text; localizer read at validation time (value changed between runs is picked up); argument guards; re-registration replaces the base validator (shared slot).
- **Runtime/manual:** TestHarness `Ext/Localization` page with a validated field, switch culture + restart, verify on Windows + Skia desktop + WASM (§8).
- Release build of `Uno.Extensions-packageonly.slnf` warning-free.

## 10. Docs
- `doc/Learn/Validation/ValidationOverview.md`: "Localizing validation messages" (DataAnnotations keys, display names, Fluent).
- `doc/Learn/Mvux/Advanced/Validation.md`: §4.5 overloads + "Localizing messages" section + restart caveat.
- `doc/Learn/Localization/LocalizationOverview.md`: cross-link.
