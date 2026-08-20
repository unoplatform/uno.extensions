# 012 — MVUX Model Mocks — Progress

Tracker for [spec.md](spec.md). Mark items as they land; add a Review section at the end.

**Status:** Not started
**Recommended entry point:** Phase 1 — it ships value on its own with zero generator work.

---

## Phase 0 — de-risk

- [ ] Write a failing-first test proving whether `Message<T>.Initial.With().Data(Option<T>.Undefined())`
      produces a Data change (spec §10.2 / §14.1). This decides the shape of `MockFeed.Undefined<T>()`
      and must be answered by a test, not by reading code.

## Phase 1 — runtime vocabulary (`Uno.Extensions.Reactive/Mocks/`)

- [ ] `MockFeed`
- [ ] `MockListFeed`
- [ ] `MockCommand` (+ internal `MockAsyncCommand : IAsyncCommand`)
- [ ] `MockFeedState`
- [ ] Unit tests in `src/Uno.Extensions.Reactive.Tests/Mocks/` (spec §11)
- [ ] `doc/Learn/Mvux/Testing.md` — vocabulary section + TOC entry
- [ ] Release build of `Uno.Extensions-packageonly.slnf`: zero warnings

## Phase 2 — generator

- [ ] `GenerateModelMocksAttribute` + `BindableGenerationContext.IsMockGenerationEnabled`
- [ ] `BindableViewModelBase(bool registerForHotReload)` (additive overload)
- [ ] `IMappedMember.GetMockPropertyType` / `GetMockInitialization`
- [ ] Implement both on all 11 mapped-member emitters (null for `MappedField`/`MappedProperty`/`MappedMethod`)
- [ ] `CommandFromMethod` — `??` initializer, gated on opt-in
- [ ] `{Model}.Mocks.g.cs` emission
- [ ] Mock ctor + `CreateMock` in `ViewModelGenTool_3.GenerateViewModel`
- [ ] Diagnostic for mock-on-non-generated-model (spec §10.5)
- [ ] Generator tests — including the **disabled-output byte-compare** (G5) and a **clean rebuild**
      (Roslyn caches generator output; a stale cache hides regressions)
- [ ] UI tests in `Uno.Extensions.Reactive.UI.Tests` (spec §11) — use
      `UnitTestsUIContentHelper.CurrentTestWindow`, never `new Window()`
- [ ] Docs — generated-mocks section
- [ ] Release build: zero warnings; runtime tests run on the Skia head

## Phase 3 — samples (optional)

- [ ] State-gallery page in `samples/Playground` exercising every `MockFeedState`

---

## Decisions log

| Date | Decision | Rationale |
| --- | --- | --- |
| | | |

## Review

_To be completed when the work lands: what shipped, what deviated from the spec and why,
what follow-up issues were opened._
