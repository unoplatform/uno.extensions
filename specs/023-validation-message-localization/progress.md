# 023 — Progress

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
- Tests: `Uno.Extensions.Validation.Tests` 5/5; `Given_StateWithValidation` 26/26. Full `Uno.Extensions.Reactive.Tests`: 1497 passed; `Given_BindableViewModel_Validation.When_RecordMemberSetFromView_Then_ValidationDelivered` (spec 022) fails intermittently under the full parallel run and passes alone; it fails the same way on the spec 022 tip (`e2a4d3fdf`, before this spec). Root-caused by the 022 review panel (M1): a race of the test with the first push of the state to the view model, fixed in the test on #3203.
- `dotnet build Uno.Extensions-packageonly.slnf` fails at restore in this environment (SDK 11 preview, `MSB4181` with SDK resolver errors), unrelated to the change; touched projects were Release-built individually instead.

## Review panel (20261020)

Findings that belong to this PR (the panel reviewed #3203, #3204 and #3222 together, see spec 022 progress):

| Finding | Resolution |
| --- | --- |
| L2 The base `Validate` took a `Func<…>` while its siblings take `AsyncFunc` | Base overload is `AsyncFunc<T, IEnumerable<ValidationResult>>` (changed on #3203). |
| L4 FluentValidation results now carry `MemberNames` | Listed in the Extensions 8.0 upgrade notes (`doc/Learn/UpdatingExtensions.md`). |
| L18 `SetValidationAsync` takes no localizer | Not added: the caller holds the results, it can localize them before the call. |
| L20 The reflog / force-push lesson is unrelated to the feature | Kept in `specs/lessons.md`: it is a correction made while working on this spec, the place AGENTS.md assigns to such lessons. |
| M2 Narrow verification | Full `Uno.Extensions.Reactive.Tests` (1563 passed, 19 skipped), `Uno.Extensions.Validation.Tests` (5/5), `Uno.HotTesting.Reactive.Tests` (40/40), Release. |

## Review panel on the PR (20261027, kazo0)

| Finding | Resolution |
| --- | --- |
| [medium] A throwing localizer is reported as a failure of the validator, and drops the whole run | `ValidationLocalizer` keeps the result as produced when the localizer throws, and logs it naming the localizer (Error the first time, then Debug). `When_LocalizerThrows_Then_ResultKeptAsIs_And_LoggedAsErrorOnlyOnce`, red with the catch disabled. The pass-through branches are covered by `When_LocalizedResultsHaveNoMessage_Then_KeptAsIs`, `When_LocalizedValidatorReturnsNull_Then_NoResults` and `When_ResultIsNull_Then_KeptAsIs`. |
| [low] Localized results lose their runtime type; a list allocated on every run | Documented (XML remarks, MVUX page, §4.3): a resolved result is a new plain `ValidationResult`, resolve the messages of derived results in the validator. Results are returned as is when no message is resolved. `When_NoMessageResolved_Then_ResultsNotCopied`. |
| [medium] The fake localizer can't tell "not found" from "found" | Its not-found value replaces `.` by `/`, like `ResourceLoaderStringLocalizer`. Using the value regardless of `ResourceNotFound` makes 3 tests fail, including `When_KeyNotFound_Then_KeyUsedAsMessage`. |
| [medium] The overview advises resource keys to every `IValidator` user | It says that only the MVUX `Validate` resolves them, and shows how to resolve them in an MVVM view model. |
| [low] The ambiguity note misses lambdas which only return `null` / `default` | Widened in the MVUX page, §4.3 and the XML remarks of both overloads. |
| [low] Stale comment on the `Uno.Extensions.Validation` reference of `Reactive.Tests`; projects missing in `Uno.Extensions.Reactive.slnf` | Comment fixed; `Uno.Extensions.Validation` and `Uno.Extensions.Hosting` added to the filter (the `MockingApp` projects were already missing on `main`). |
| [low] Spec number 016 taken on `main` | Renumbered 023. |

Tests (20261029, Release): `Uno.Extensions.Reactive.Tests` 1589 passed, 19 skipped, 1 failed (`Given_PaginatedListFeed.When_Async_Then_FlagIsLoading`, the known timeout under a full parallel run; passes alone); `Uno.Extensions.Validation.Tests` 5/5.
