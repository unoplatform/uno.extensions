# 016 — Progress

**Status:** Draft. Spec written, awaiting review of §7 open questions before implementation.

## Decisions on §7 open questions

| Q | Decision |
| --- | --- |
| 1 Runtime culture switch | _pending_ (proposed: out of scope) |
| 2 Opt-in vs automatic | _pending_ (proposed: explicit `UseLocalizedDataAnnotations()`) |
| 3 Default messages | _pending_ (proposed: key convention only) |
| 4 Package placement | _pending_ (proposed: `Uno.Extensions.Validation`) |
| 5 Fluent member-name fix | _pending_ (proposed: in this spec) |
| 6 Localizer parameter type (MVUX overloads) | _pending_ (proposed: `IStringLocalizer`) |
| 7 Format arguments on key overloads | _pending_ (proposed: not in v1) |

## Plan

- [ ] Create `src/Uno.Extensions.Validation.Tests` (net10.0 MSTest, discovered by package CI)
- [ ] §4.1 `Validator` passes `IServiceProvider` into the `ValidationContext` it creates (red/fix/green)
- [ ] §4.4 `FluentValidator` keeps `PropertyName` in `MemberNames` (red/fix/green)
- [ ] §4.2 `DataAnnotationsLocalizationOptions` + `UseLocalizedDataAnnotations` (reference `Microsoft.Extensions.Localization.Abstractions`)
- [ ] §4.2 attribute-by-attribute validation with localized messages and format arguments; BCL parity tests
- [ ] §4.3 display-name localization and `DefaultMessageKeyFormat`
- [ ] §4.5 four `State.Validate` overloads (reference `Microsoft.Extensions.Localization.Abstractions` from Reactive) + `Given_StateWithValidation` cases
- [ ] `Given_StateWithValidation` case with `IValidator` + fake localizer
- [ ] §10 docs (Validation overview, MVUX validation, Localization cross-link)
- [ ] §8 manual verification: thread-pool culture + `ResourceLoaderStringLocalizer` on Windows (packaged/unpackaged), Skia desktop, WASM, Android, iOS
- [ ] Release build of `Uno.Extensions-packageonly.slnf` warning-free

## Review

_Not started._
