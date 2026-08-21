# 012 — MVUX Model Mocks — Progress

Tracker for [spec.md](spec.md). Mark items as they land; add a Review section at the end.

**Status:** All phases complete and committed (7 commits on `dev/sb/mvux-model-mocks-spec`, through `a6ab1fdb5`).
**Recommended entry point:** the "In-flight (handoff)" section below, then the Follow-ups list.

---

## In-flight (handoff, 2026-08-21)

In-flight change (committed just before this note, unfinished): `samples/Playground/Playground/Models/MockRecipesModel.cs`
adds a scalar feed member — `public IFeed<Person> Person => Feed.Async<Person>(async (ct) => new());`
(2 lines, compiles; `Person` already exists in `Models/Person.cs`).

**Why (inferred, unconfirmed):** the mock-enabled model only had a list feed (`Recipes`) and a command
(`Save`), so the generated `MockRecipesViewModelMocks` bundle / `CreateMock` gallery section never exercises
a **scalar** `MockFeed` override through the generated path. Adding `Person` gives `CreateMock` a scalar
member to pin.

**What's missing to finish it:**

1. Nothing consumes the new member yet — `Views/MvuxMocksPage.xaml{,.cs}` has no `Person` section.
   Extend the page's `CreateMock` section (or add a state entry) pinning `Person` via `MockFeed.*`
   (e.g. `Value`, `Loading`, `Error`) with the code snippet shown alongside, matching the existing entries.
2. Re-verify interactively on the desktop (Skia) head like the rest of the gallery
   (`dotnet run` on the Playground desktop head; route `MvuxMocks` from Home).
3. Fold the page work into a `feat(playground): pin a scalar feed via CreateMock in the mocks gallery`
   commit (squash with the stub commit on rebase) — or **revert the stub commit** if the scalar path is
   judged already covered by the raw `MockFeed.Script` section.

**Open decision:** keep-and-finish vs drop. The raw-vocabulary section of the gallery already shows scalar
`MockFeed` states on a `FeedView`; the only gap is scalar-through-`CreateMock`. Confirm with Steve if unclear.

---

## Phase 0 — de-risk

- [x] Write a failing-first test proving whether `Message<T>.Initial.With().Data(Option<T>.Undefined())`
      produces a Data change (spec §10.2 / §14.1). This decides the shape of `MockFeed.Undefined<T>()`
      and must be answered by a test, not by reading code.
      → Answered: **no change is registered** (`Given_MockFeed.When_DataSetToUndefinedOnInitial_Then_BuilderFlagsNoChange`).

## Phase 1 — runtime vocabulary (`Uno.Extensions.Reactive/Mocks/`)

- [x] `MockFeed`
- [x] `MockListFeed`
- [x] `MockCommand` (+ internal `MockAsyncCommand : IAsyncCommand`)
- [x] `MockFeedState`
- [x] Unit tests in `src/Uno.Extensions.Reactive.Tests/Mocks/` (spec §11)
- [x] `doc/Learn/Mvux/Testing.md` — vocabulary section + TOC entry (+ cross-link from `FeedView.md` with the §10.1 scalar-feed caveat)
- [x] Release build of `Uno.Extensions.Reactive.csproj`: zero warnings (full `packageonly.slnf` left to CI)

## Phase 2 — generator

- [x] `GenerateModelMocksAttribute` + `BindableGenerationContext.IsMockGenerationEnabled`
- [x] `BindableViewModelBase(bool registerForHotReload)` (additive overload)
- [x] `IMappedMember.GetMockPropertyType` / `GetMockInitialization`
- [x] Implement both on all mapped-member emitters (null for `MappedField`/`MappedProperty`/`MappedMethod`)
- [x] ~~`CommandFromMethod` — `??` initializer~~ → not needed: the mock ctor never runs `GetInitialization()`,
      and mocked instances are not hot-reload-registered, so no path re-creates a command on a mock (see decisions)
- [x] `{Model}.Mocks.g.cs` emission
- [x] Mock ctor + `CreateMock` in `ViewModelGenTool_3.GenerateViewModel`
- [x] Diagnostics `FEED3001` (base model not mock-enabled → skip) and `FEED3002` (pattern matched nothing, spec §10.5)
- [x] Generator tests (`Given_ModelMocks`, `Given_ModelMocks_HotReload`) — G5 verified by a **byte-compare of all
      136 emitted files** before/after the generator change with the attribute absent (0 differences, clean rebuild),
      plus an in-suite gating test (`When_ModelNotMatchingPatterns_Then_NoMockCodeGenerated`)
- [x] UI tests: `Given_FeedView_Mocks` in `Uno.Extensions.Reactive.UI.Tests` (run by the runtime-test stages);
      hot-reload guard covered deterministically in the unit suite (`When_MockedVm_Then_HotPatchDoesNotReplaceMocks`)
- [x] Docs — generated-mocks section in `doc/Learn/Mvux/Testing.md`
- [x] Release build of `Uno.Extensions.Reactive` + generator: zero warnings from changed files
      (runtime tests on the Skia head are left to CI — the WinUI test head is not runnable via `dotnet test`)

## Phase 3 — samples (optional)

- [x] State-gallery page in `samples/Playground` exercising every `MockFeedState`:
      `Views/MvuxMocksPage` (route `MvuxMocks`, button on Home) with two sections —
      raw `MockListFeed`/`MockFeed.Script` on a `FeedView`, and `CreateMock` on the generated
      `MockRecipesViewModel` (`Models/MockRecipesModel.cs`, `[assembly: GenerateModelMocks("MockRecipesModel$")]`).
      Each state shows the exact mocking code alongside the live view. Verified interactively on the
      desktop (Skia) head: all seven direct states + script walk + mocked/real VM swaps + disabled
      `Save` via `MockCommand.Executing()`.
      Notes: each state re-inflates its host template — a reused `FeedView` keeps visual-state residue
      (e.g. a pinned progress state) across source swaps; `ErrorTemplate`/`ProgressTemplate` receive the
      `Exception` / progress `bool` as DataContext (not the `FeedViewState`).
      Also fixed (pre-existing, unrelated to mocks): the Playground booted to a black screen because the
      first navigation resolved an empty route — `initialViewModel: typeof(HomeViewModel)` did not produce
      a Home navigation and no shell route was `IsDefault`; marked `Home` as `IsDefault: true`.

---

## Decisions log

| Date | Decision | Rationale |
| --- | --- | --- |
| 2026-08-20 | `MockFeed.Undefined<T>()` constructs its message via the internal `Message<T>` ctor with an explicitly-flagged Data change | §10.2 confirmed by test: the builder's `AreEquals` early-out swallows `Data(Undefined)` on `Initial`. The single-message internal-ctor shape avoids the two-message (`None` → `Undefined`) alternative, which would flash the None state to subscribers. In-assembly internals are fair game (`ValueFeed` precedent). |
| 2026-08-20 | `MockListFeed` forwards messages through a private pass-through `IListFeed<T>` adapter instead of `AsListFeed()` | `FeedToListFeedAdapter` coerces `Some(empty list)` to `None`, which would make `EmptyList` indistinguishable from `Empty`. Safe: `BindableEnumerable.UpdateSubProperties` computes its own collection change-set when a message carries none. |
| 2026-08-20 | `MockListFeed.Value(...)` / `Refreshing(...)` with no items normalize to `None` | Matches real list-feed semantics everywhere else in MVUX; `EmptyList()` is the explicit opt-in for `Some([])`. |
| 2026-08-20 | `MockCommand.Executing()` reports `CanExecute == false` | Matches `AsyncCommand`, which refuses execution while already executing for the parameter. |
| 2026-08-20 | No `__reactiveMocks` field and no `??` in `CommandFromMethod.GetInitialization()` (deviates from spec §7.3/§7.5) | The mock ctor initializes every member itself via `GetMockInitialization` (which folds spec §7.5's "seed" and "default" steps into one `mocks.X ?? Undefined` expression) and never calls `GetInitialization()`. The only other re-init path is hot reload, from which mocked instances are excluded (§7.6). The `??` would therefore be dead code — and dropping it keeps the emitted output for existing members byte-identical even in mock-enabled assemblies. |
| 2026-08-20 | Mock override for `IFeed<TCollection>`-of-list members is `IListFeed<TItem>` (deviates from spec §7.2 table's `IFeed<IImmutableList<T>>`) | Lets `MockListFeed.*` factories be used directly for every list-shaped member, and works for any `TCollection` (arrays, `ImmutableArray<T>`, …) which `IFeed<IImmutableList<T>>` would not. |
| 2026-08-20 | §10.5 realized as two warnings: `FEED3001` (model mock-enabled but a base model in the chain is not → mock skipped) and `FEED3002` (explicit pattern matches no generated model) | The mock ctor chains through base VM mock ctors, so a non-mock-enabled base makes generation impossible; both silent-skip cases now surface. |
| 2026-08-20 | §10.4 hot-reload guard tested at the unit level (`Given_ModelMocks_HotReload`) by invoking `BindableViewModelBase.HotPatch` directly | Deterministic (no secondary app, no file edits); the test also patches a regular instance to prove it is not vacuous. |
| 2026-08-20 | `MockFeed.Undefined` emits **two messages** (None → Undefined) instead of a single explicitly-flagged one, and constructs a fresh feed per call (no `Feed.Create` delegate caching) | Review finding: the explicit change flag is recomputed away by the state pipeline (`MessageManager` compares values) — the path every `CreateMock` member takes — and even the two-message shape cannot flag the change on *replay* to late subscribers (replay diffs against `Initial`; Undefined == Undefined). The two-message shape gives early subscribers an explicit transition; late subscribers keep the FeedView's initial presentation, which *is* the Undefined template. Tests assert the achievable contract (data converges to Undefined; view settles not-loading/no-data). Per-call instances prevent two same-typed unconfigured members from sharing one backing state. |
| 2026-08-20 | Plain (non-feed) members throw on a mocked instance — documented, not guarded | `MappedProperty`/`MappedMethod` forward to the null `Model`. Guarding them would change emission for all mock-enabled assemblies; v1 documents the limitation (docs + generated XML summary) and pins the behavior with `When_PlainMemberOnMockedVm_Then_Throws`. Null-tolerant forwarders are a candidate follow-up. |
| 2026-08-20 | `MockCommand` `Execute` is a no-op when not executable | Review finding: matches `AsyncCommand`, which refuses execution through its sub-command gate. |
| 2026-08-20 | Hot Design recorded as a consumer of the generated names (spec §10.8); `When_MockLoading_Then_PinnedStateSurvivesReparenting` added to the FeedView UI tests | Required by uno.hotdesign's *MVUX State Previews* spec (its review panel): the designer discovers `CreateMock`/`{Vm}Mocks` by reflection, so the names are frozen contract; the re-parenting test is the stage-0 verification gate for the preview-group activation path (subscribe → unload → resubscribe to a completed mock). |
| 2026-08-20 | `MockListFeed.Value(items, SelectionInfo)` overload added — pinned selection support | The selection axis is `internal` but the `Selected(...)` builder extension is public, and `BindableListFeed` applies incoming selection-axis changes to `ICollectionView.CurrentItem` without the `Selection` operator — verified by a runtime test. Spec 012 was silent on selection; unlike the Refresh axis (§10.3), it is expressible, so it gets first-class support rather than a documented limitation. Empty list ignores the selection (normalizes to `None`). |
| 2026-08-20 | Command coverage completed through the bound-control layer | `Callback` invocation through a mocked VM (unit) and `Executing`/`Disabled`/default-Idle driving a `Command`-bound `Button.IsEnabled` (runtime) — the designer-visible command behaviors. |

## Review

**Shipped (2026-08-20):**

- Phase 1 — runtime vocabulary: `MockFeed`, `MockListFeed`, `MockCommand` (+ internal `MockAsyncCommand`),
  `MockFeedState` in `Uno.Extensions.Reactive/Mocks/`, with 24 unit tests in `Uno.Extensions.Reactive.Tests/Mocks/`.
- Phase 2 — generator: `GenerateModelMocksAttribute` opt-in, `BindableViewModelBase(bool registerForHotReload)`,
  `IMappedMember.GetMockPropertyType/GetMockInitialization` on all emitters, `{Model}.Mocks.g.cs` bundle,
  mock ctor + `CreateMock` on generated VMs, diagnostics `FEED3001`/`FEED3002`, 11 generator/HR tests
  (`Given_ModelMocks*`), 7 FeedView UI tests (`Given_FeedView_Mocks`), docs (`doc/Learn/Mvux/Testing.md` + TOC
  + `FeedView.md` cross-link).

**Deviations from spec:** see the decisions log — no `__reactiveMocks` field / no `CommandFromMethod` `??`
(dead code given §7.6), `IListFeed<TItem>` as the mock override type for feed-of-list members, §10.5 as two
diagnostics, §10.4 tested at unit level via direct `HotPatch` invocation.

**Verification:** §10.2 answered by test (no change flagged by the builder → explicit change in `Undefined`);
G5 verified by byte-comparing all 136 generated files with the attribute absent (0 diffs, clean rebuild);
full unit suite green; `Uno.Extensions.Reactive` + generator Release builds clean.

**Review panel (2026-08-20):** all seven reviewer lenses ran pre-commit. Fixed as a result: the Undefined
state-pipeline gap (two-message shape + per-call feed instances + E2E `CreateMock`→`FeedView` runtime tests),
plain-member NRE pinned + documented, `MockCommand.Execute` gating, null-arg guards on the public vocabulary,
field-declared-feed fixtures + value-flow tests for every emitter kind, `FEED3001/3002` documented in
`doc/Reference/Reactive/rules.md` (help links now resolve) with `FEED3002` located at the attribute,
per-property XML docs on the generated bundle, `Rules.Feeds.cs` section ordering, `IsMockSupported` memoization,
Script cancellation + disposal tests, and doc caveats (read-only mocked states + escape hatch, dispose-replaced-
mocks/WASM, `#if DEBUG` gating, base-factory shadowing on derived exclusion, unanchored-regex semantics).

**Follow-ups:**

- `When_RegisteredInDi_Then_NavigationResolvesMockedVm` (spec §11) needs a navigation-capable UI test host
  (TestHarness); not covered by `Uno.Extensions.Reactive.UI.Tests`.
- `MockCommand` invocation recording (spec §14.2) still open; the API ships without it (additive later).
- Null-tolerant forwarders for plain members on mock-enabled assemblies (would remove the documented NRE).
- Committed golden-file guard for G5 (currently: reflection sweep in-suite + out-of-band 136-file byte-compare).
- Regex timeout / invalid-pattern diagnostic for `GenerateModelMocks` patterns (same exposure as the
  pre-existing `ImplicitBindables` path).
- API evolution rule for the mock vocabulary (from contract review): evolve `MockFeed`/`MockListFeed`/
  `MockCommand` factories via **new overloads only** — adding an optional parameter to a shipped
  factory is binary-breaking, and `Script`'s named-tuple element names are source contract.
- `initialViewModel:` in `InitializeNavigationAsync` did not produce a first navigation in the
  Playground desktop head (empty route resolved, black screen until `Home` was marked `IsDefault`);
  possibly related to recent navigation view-model-creation changes — needs its own investigation.
