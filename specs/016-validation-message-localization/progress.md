# 016 — Progress

**Status:** Implemented (v1) with the proposed defaults for §7, pending review.

## Decisions on §7 open questions

| Q | Decision |
| --- | --- |
| 1 Runtime culture switch | Adopted default: out of scope |
| 2 Opt-in vs automatic | Adopted default: explicit `UseLocalizedDataAnnotations()` |
| 3 Default messages | Adopted default: key convention only (`DefaultMessageKeyFormat`) |
| 4 Package placement | Adopted default: `Uno.Extensions.Validation` |
| 5 Fluent member-name fix | Adopted default: in this spec |
| 6 Localizer parameter type (MVUX overloads) | Adopted default: `IStringLocalizer` |
| 7 Format arguments on key overloads | Adopted default: not in v1 |

## Plan

- [x] Create `src/Uno.Extensions.Validation.Tests` (net10.0 MSTest), added to `Uno.Extensions.sln` (under `src`) and `Uno.Extensions-packageonly.slnf`
- [x] §4.1 `Validator` passes `IServiceProvider` into the `ValidationContext` it creates (red/fix/green: 2 tests red before the fix)
- [x] §4.4 `FluentValidator` keeps `PropertyName` in `MemberNames` (red/fix/green: 1 test red before the fix)
- [x] §4.2 `DataAnnotationsLocalizationOptions` + `UseLocalizedDataAnnotations` (reference `Microsoft.Extensions.Localization.Abstractions`)
- [x] §4.2 attribute-by-attribute validation with localized messages and format arguments; BCL parity tests
- [x] §4.3 display-name localization and `DefaultMessageKeyFormat`
- [x] §4.5 four `State.Validate` overloads (reference `Microsoft.Extensions.Localization.Abstractions` from Reactive) + `Given_StateWithValidation` cases
- [x] `Given_StateWithValidation` case with `IValidator` + fake localizer
- [x] §10 docs (Validation overview, MVUX validation, Localization cross-link)
- [ ] §8 manual verification: thread-pool culture + `ResourceLoaderStringLocalizer` on Windows (packaged/unpackaged), Skia desktop, WASM, Android, iOS
- [ ] TestHarness `Ext/Localization` page with a validated field (§9 runtime/manual)
- [ ] Release build of `Uno.Extensions-packageonly.slnf` warning-free

## Review

- BCL probe (scratch script) drove three spec corrections: `ValidationContext` ignores `[DisplayName]` (§4.3); class-level attributes are gated on property errors, not only `IValidatableObject` (§4.2); `ResourceLoaderStringLocalizer` drops arguments on its `.` → `/` key retry, so the validator formats messages itself (§4.2).
- A parity case added late (property whose type has class-level attributes) was red: `TypeDescriptor` merges the type's attributes into the property's, the BCL store removes them. Fixed by mirroring that filter.
- Localization only replaces a message when the attribute produced its own `FormatErrorMessage(displayName)`, so attributes overriding `IsValid` with a custom message are left untouched. A `FormatException` (bad translation) falls back to the attribute message instead of reaching `Validator`'s catch-all and losing the error list.
- Adding the MVUX overloads made a throw-only lambda ambiguous (`CS0121`, one existing test updated with an explicit return type, not removed) and two `cref`s ambiguous (`CS0419`, now full signatures). Documented in §4.5 and the MVUX page.
- `UseLocalizedDataAnnotations` lives in `DataAnnotationsValidationBuilderExtensions`: `ValidationBuilderExtensions` already exists in the `Uno.Extensions` namespace (Fluent assembly).
- Tests: `Uno.Extensions.Validation.Tests` 28/28 (services reach attributes and `IValidatableObject` on both the BCL and the localized path); `Given_StateWithValidation` 22/22 (8 new). Full `Uno.Extensions.Reactive.Tests`: 1492 passed, 1 failed (`Given_PaginatedListFeed.When_Async_Then_FlagIsLoading`), which fails the same way on `HEAD` without these changes and on `main` (1438 passed, same single failure), and passes when run alone: pre-existing, timing-dependent under a full parallel run.
- `dotnet build Uno.Extensions-packageonly.slnf` fails at restore in this environment (SDK 11 preview, `MSB4181` with SDK resolver errors), unrelated to the change; touched projects were Release-built individually instead.
