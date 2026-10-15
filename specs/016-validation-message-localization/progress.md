# 016 — Progress

**Status:** Implemented, pending review.

## Decisions

| Topic | Decision |
| --- | --- |
| Where to localize | Only in the MVUX `Validate`; `Uno.Extensions.Validation` passes keys as-is (v1 localized DataAnnotations in the validation layer, walked back — kept on branch `dev/xygu/20261005/spec-016-validation-localization-v1`) |
| MVUX API shape | Optional `IStringLocalizer? localizer = null` as the last parameter of every `Validate` overload; localization only in the base overload |
| Services in `ValidationContext` | Not in scope (reverted with v1) |
| Fluent member-name fix | In this spec |
| Runtime culture switch | Out of scope |
| Format arguments | Not supported on localized messages; use `Localizer[key, args]` in the validator |

## Plan

- [x] Create `src/Uno.Extensions.Validation.Tests` (net10.0 MSTest), added to `Uno.Extensions.sln` (under `src`) and `Uno.Extensions-packageonly.slnf`
- [x] §4.1 contract tests: DataAnnotations and Fluent keys passed as-is
- [x] §4.2 `FluentValidator` keeps `PropertyName` in `MemberNames` (red/fix/green: 1 test red before the fix)
- [x] §4.3 `State.Validate` overloads with a trailing optional localizer (reference `Microsoft.Extensions.Localization.Abstractions` from Reactive) + `Given_StateWithValidation` cases
- [x] §9 docs (Validation overview, MVUX validation, Localization cross-link)
- [ ] §7 manual verification: thread-pool culture + `ResourceLoaderStringLocalizer` on Windows (packaged/unpackaged), Skia desktop, WASM, Android, iOS
- [ ] TestHarness `Ext/Localization` page with a validated field (§8 runtime/manual)
- [ ] Release build of `Uno.Extensions-packageonly.slnf` warning-free

## Review

- v1 localized DataAnnotations in `Uno.Extensions.Validation` (`UseLocalizedDataAnnotations`, attribute-by-attribute validation with format arguments and display names) and gave the app services to the `ValidationContext`. Walked back to keep a single localization entry point in MVUX and the validation layer free of localization; the history was rewritten, v1 is kept on branch `dev/xygu/20261005/spec-016-validation-localization-v1`.
- `ResourceLoaderStringLocalizer` returns the key with `.` replaced by `/` when a key is not found (its `.` → `/` retry), so the MVUX localization keeps the original result on `ResourceNotFound` rather than using the not-found value — otherwise a plain message such as a DataAnnotations default would be altered. Covered by `When_KeyNotFound_Then_KeyUsedAsMessage` (key with a `.`).
- Adding overloads made a throw-only lambda ambiguous (`CS0121`, one existing test uses an explicit return type) and `cref`s ambiguous (`CS0419`, full signatures). Documented in §4.3 and the MVUX page.
- Tests: `Uno.Extensions.Validation.Tests` 5/5; `Given_StateWithValidation` 26/26. Full `Uno.Extensions.Reactive.Tests`: 1497 passed; `Given_BindableViewModel_Validation.When_RecordMemberSetFromView_Then_ValidationDelivered` (spec 015) fails intermittently under the full parallel run and passes alone; it fails the same way on the spec-015 tip (`e2a4d3fdf`, before this spec): pre-existing.
- `dotnet build Uno.Extensions-packageonly.slnf` fails at restore in this environment (SDK 11 preview, `MSB4181` with SDK resolver errors), unrelated to the change; touched projects were Release-built individually instead.
