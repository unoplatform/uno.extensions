# 013 — Version and decision history

Rebuilt after the loss of the ACO workspace (`devid-feat-uno-extensions-architecture`, destroyed together with the unpushed `dev/devid/spec-013-mvux-mocking` branch). Merge sources:

- local **recovery** files (rebuilt from the transcripts) — they carried the open "context scope" question and the reference to commit `cd4c9ad`;
- **David's VS Code files** (attached 08-23 18:22) — `spec.md`/`impl.md` were the v1 state (`8d589d9`, never reloaded), `archi.md` was the most recent state (v4, post-`2618def`);
- the full Telegram transcript of the discussion.

---

## Phase 0 — Framing (Sat 08-22, evening)

**Context.** Goal set by David: mocking helpers for UI previews (Hot Design) and for testing apps that consume feeds (simulate feed states, not test the feeds themselves). Two existing POCs: PR **#3148** (Nick, spec 009 — XAML only, POCO/JSON envelope coerced at `FeedView.Source`) and PR **#3147** (Steve, spec 012 — `Mocks` vocabulary plus a `{Vm}Mocks`/`CreateMock` generator). David's three-level vision: (1) static in XAML, (2) per-feed mock structures on a view-model, (3) "complete model" helpers.

**Discussion and decisions:**

- My first proposal (build on 3147 plus a markup extension and a catalog) was reframed by David: start from **his** design — `MessageEntry` for layer 1 (no magic envelope, no JSON to dynamic) and the **hot-reload SwapFeed** for layer 2 (full control, a fully malleable system; layer 3 then becomes helpers on top).
- Feasibility verified in the code: `MessageEntry<T>`/`IMessageEntry` are public; `MessageEntry.Empty` forces the Data axis, which kills the "Undefined" canary (spec 012 §10.2); `HotSwapFeed`/`IHotSwapState`/`StateImpl` are an existing seam (only caller: hot reload); gate `HotReloadSupport.State`; commands are not swap-backed (gap identified).
- View-model construction: neither "real view-model through DI" nor "constructor without a model" — instead a **real view-model plus a real model**, services **null-injected**, proven safe by **dependency analysis at code generation time** (David's "option 2"). Rationale: MVUX feeds are lazy arrow getters (the service is captured in a closure and only touched at enumeration); the blocking case is an **eager service access in the constructor**. "We do not control how our users use our architecture", so the analysis and the diagnostics are mandatory.
- Cross-assembly: the generator only sees the metadata of a model living in another assembly, accepted at that stage as *mocking equals the model assembly* (the `MyModel.Empty`/`Create(<feeds>)` contracts are only available there).
- **Draft 1** written in the repository (never committed, overwritten by the v1 pivot): tier 1 was a `FeedMock` DTO plus a `{mvux:Mock}` markup extension; tier 2 was `{Vm}Mocks`/`CreateMock` generated in the model assembly; D1 to D4 were still open.

## v1 — commit `8d589d9` (Sun 08-23 10:42) — the "external generation" pivot and checkpoint

**Discussion (08-23 morning):** while reviewing, David realises that mocking has to be **consumable from the outside** (a test project referencing the app), so we cannot inject the code into the view-model or the model; the MVUX generator adds **hidden hooks** (following the hot-reload model) and the mocking generator takes control from the outside. His dump: a `RecipeModelMock` record with `required init` plus `Empty`, `Create()`/`Create(steps)` (null-inject plus `SetModel`), and `SetModel` close to `__Reactive_UpdateModel`. My verifications added:

- `__Reactive_UpdateModel` cannot be reused as is (it reassigns `__reactiveModel`, and `Unsafe.As` on a foreign type is undefined behaviour), so a **dedicated hidden method** is needed (confirmed by David, point 3).
- **Derived feeds must survive** ("that is the whole concept"), which led to the anchor discovery: feeds are cached by `AttachedProperty.GetOrCreate` with a stable identity, so we **wrap in `HotSwapFeed` at the model-feed cache level**; derivations compose on the wrapper, so the swap travels through the business logic and `SetModel` becomes a set of typed swaps with **no `dynamic`**.
- **Dependency attributes** emitted by the analysis and also hand-declarable (David's `[FeedShape(...)]` idea, renamed to `[FeedDependency]`/`[CtorDependency]`) — required because the external generator has no syntax trees.
- **Constructor instrumentation** (David's idea): a direct service access in the constructor makes `Create` require that service as a parameter.
- Non-AOT accepted (point 4): mocking is dynamic injection, for development and test only.
- D1 to D7 logged; D3 (facade) and D4 (dedicated flag) decided by David.

**Checkpoint requested by David**: branch `dev/devid/spec-013-mvux-mocking` from `main@32faf32`, commit `8d589d9` (3 documents, 293 lines).

## v2 — commit `292fb5f` (10:47) — David review

- **Derived members are overridable**: `{Model}Mock.StepsCount` is nullable like `Save` — left unset, the real derivation runs over the swapped inputs; set, it is replaced (useful for tests).
- **Tier 1 without `FeedMock`**: "I do not see the point of a FeedMock at that place", so `FeedView.Source` takes an **author-declared non-generic `MessageEntry`** (the framework concept itself).
- **XAML examples required** and added (pinned loading, inline POCO, error/empty/undefined as resources, state picker).
- **Natural evolution contract**: replacing the instance in `Source` **pushes** the entry into the existing wrapper (axis diff against the previous entry); transiting through a loading state is forbidden, and recreating the wrapper is tolerated only when no visible reset occurs.
- Removed the "v1/v2" references from the documents ("we are writing this spec right now, there is no existing version").

## v3 — commit `2618def` (11:08) — David review

- **Custom axes are first-class**: one strength of MVUX is axis extensibility, hence an `Axes` collection (`AxisValue`) plus `Set(MessageAxis, value)` in code; the XAML identifier is resolved against core and registered axes, and an unknown identifier raises a diagnostic; custom axes take part in the wrapper diff.
- **A JsonConverter example** was requested (JSON to target object, type passed through `ConverterParameter` because the dependency property is `object`) and added.
- My two deductions at the time — `MessageEntry` as a `DependencyObject` in `.UI` (to bind inside `Data`) and a push on dependency-property mutation — **were reverted in v4**.

## v4 — David revisions in VS Code (commit `cd4c9ad`, lost; content is his attached `archi.md`)

David answered my question "are you fine with this split?" by editing the architecture directly:

- **`MessageEntry` stays a plain CLR object in Core** — deliberately **not** a `DependencyObject` (no UI property-system complexity in the message model).
- **The entry is not observable**: mutating `Data`/`Error`/`IsProgress`/`Axes` after assignment pushes nothing; **replacing the instance** is the unit of change.
- The JSON converter **is no longer a deliverable**: it is an **application-owned** illustration attached at `FeedView.Source` that must return `IMessageEntry`; the spec neither defines nor implements a converter.
- Explicit constraint: **tiers 2 and 3 are strongly typed end to end** — never a `MessageEntry` or an untyped envelope in their contracts; the generator emits "external, generic and strongly typed" types.
- `AxisValue.Axis` is typed `string` (the XAML identifier); the typed `MessageAxis` instance goes through `Set(...)` in code.
- (His attached `spec.md`/`impl.md` were the v1 `8d589d9` state, never reloaded in VS Code; only the architecture carried v4, so the restored spec and implementation documents carry these decisions back.)

## v5 — last exchange before the loss (never committed) — OPEN question

- David: the **view-model** scope of tier 2 may be accidental; the real boundary would be the **context** that owns states and subscriptions (**`SourceContext`**, to be verified in the source).
- Target syntax: `using (MockingService.Enable()) { var model = new MyModel(...); }`
- Semantics to be settled by a **spike** (P0-e): ambient `AsyncLocal` versus global, nested scopes, concurrency, eager versus lazy context, survival of contexts after `Dispose`, interaction with the mockable flag (D4).
- **No answer accepted until verified in the source and reviewed by David.**

Then came the **loss of the ACO workspace** (node destroyed, branch never pushed — commits `8d589d9`, `292fb5f`, `2618def`, `cd4c9ad` lost). Rebuild and restore into this folder (spawn `ext-mvux-mock`, 08-23 evening).

## v6 — David decision (Sun 08-24) — scoped activation SETTLED

- **`using (MockingService.Enable())` is certain**, it is no longer an open question: it is what turns mocking on.
- Granularity is the caller's choice: a test assembly that wants mocking at large opens the scope in its **assembly init**; otherwise one scope per test.
- **Reason: `HotSwapFeed` has a cost.** Activation happens on demand only — *"we do not want to inject that feed into ALL the feeds of a live app"*. Outside a scope there is no wrap and the raw feed is cached exactly as today.
- The spike (P0-e) only had to settle the **mechanism** (owning context, eager versus lazy, `AsyncLocal` versus a carried token, nesting, concurrency, survival after `Dispose`, wiring to the D4 flag), not the API shape.
- Propagated to the three documents: spec §13 plus G9, R7 and D10; architecture §1/§6/§7; implementation §1/§2.2/§6/§7/§8/§9.

## v7 — David decision (Sun 08-24, evening) — per-context gate and reflection swap

- **Question settled ("where does the mockable flag live?")**: source investigation requested by David.
  - Code findings: hot reload wraps in `StateImpl.cs:74-77` (`EffectiveHotReload.HasFlag(State)` then `new HotSwapFeed<T>`), so the gate is a **global static**, `FeedConfiguration.EffectiveHotReload`; the swap driver is **already reflection-based** over `IHotSwapState<T>` (`BindableViewModelBase.HotReload.cs:457-467`). `SourceContext` already carries `AsyncLocal<SourceContext> Current`, per-owner contexts (`GetOrCreate`), an eager `PreConfigure`/`Set` seam, and one `IStateStore States` per context.
  - **David decision**: *"flag on the SourceContext (`IsMockingActive`) plus reflection for the swap, fail-hard"*. The `FeedConfiguration.Mockable` static (D4) is dropped: the **context** is what needs the information (D12). No home-grown `AsyncLocal`. Strict reflection swap (D11).
  - AOT point (David): a two-assembly split forces reflection anyway (generating `{Model}Mock` next to the `Model` would leave the mock assembly hollow), so core reflection is accepted and the mocking path stays development and test only, non-AOT (D7/NG2).
- The "P0-e spike" (scope mechanism) is **resolved** and is no longer a spike: it rides on `SourceContext`.
- Propagated: spec §13/§10/§5/§4 plus D4 (superseded), D11 and D12; architecture §0/§1/§2.1/§5/§6/§7; implementation §1/§2.2/§3/§6/§7/§8.

## v8 — tier 2 and 3 implementation (Tue 08-25)

Landed on `dev/devid/spec-013-mvux-mocking` (pushed to staging PR #1), after the spec (`b029713cb`):

- **Core substrate** (`62af49aa0`): `SourceContext.IsMockingActive` (inherited per-context bit, no separate static, no home-grown `AsyncLocal`), `EnableMocking()` ambient scope, wrap gate in the `StateImpl` constructor (`|| context.IsMockingActive`). Reflection swap through `IHotSwapState<T>`.
- **Fail-hard fix (D11)** (`4b68b7e93`): `StateImpl<T>` ALWAYS implements `IHotSwapState<T>`, hence the addition of `IHotSwapState<T>.CanHotSwap` (`=> _hotSwap is not null`); `MockModel` throws on `!CanHotSwap` (a missing test, never executed at first, was hiding that bug — fixed).
- **MVUX generator pass** (`4d02e6553`): `FeedDependency` classification (service-dependent `OnParameter`, derived `OnFeed`, plain independent) plus constructor instrumentation `CtorDependency(Eager=true)`; opt-in `[assembly: EnableFeedMocking]`, byte-identical output when absent. View-model seam `__Mock_SetCommand` (commands, R2; no `__Mock_Create` since public constructors plus the ambient scope are enough, D12).
- **Second assembly `Uno.Extensions.Reactive.Mocking`** (`4b68b7e93`, `b6caeee03`, `6384a4d48`): `MockingService.Enable()`, `MockModel.SwapFeed`/`SwapListFeed` (fail-hard), the `MockFeed`/`MockListFeed` vocabulary (Value, Empty, EmptyList, Undefined, Loading, Error, Refreshing), and `MockCommand` (Idle, Disabled, Executing, Callback).
- **Consumer generator `Uno.Extensions.Reactive.Mocking.Generator`** (`4b68b7e93`, `6384a4d48`): reads the metadata (referenced assemblies plus the current compilation) and emits `record {Model}Mock` (required inputs, optional derived members and commands), `Empty`, `Create()`/`Create(inputs)`/`Create(mock)` (null-inject), and `SetModel` (typed swaps plus `__Mock_SetCommand`).
- **Fixture app `Uno.Extensions.Reactive.Tests.MockingApp`**: a real model with the opt-in, to exercise the true two-assembly flow (generators do not chain inside a single compilation).

**Tests (actually executed):** Given_MockingActivation 4/4, Given_MockingRuntime 4/4, Given_GeneratedMock 4/4 (Create plus SetModel driving a real view-model to a mocked feed; live re-swap; `Create()` Empty to None; command override), Tests.Generator 80/80 (byte-identical output preserved), Given_HotReload 8/8 (Core unchanged).

**Remaining (outside the tier 2 and 3 core, to be planned with David):**

- `MockFeed.Message`/`Script` (they depend on the #3147 vocabulary, not merged).
- Diagnostics `FEED3201` to `FEED3203` and `MOCK0001` (the analysis is in place, the diagnostics are not emitted).
- Documentation `doc/Learn/Mvux/Testing.md` and `FeedView.md` (§9), tier 1 (on hold).
- Upstream delivery: ABO outbox to PR #3165 (after David review).

## v9 — reconciliation with #3149 (FeedMock merged) and David review (Tue 08-25)

Rebase on `main` (PR #3154 / issue #3149 merged): **the mocked-feed vocabulary already exists** in a dedicated assembly, `Uno.HotTesting.Reactive` (`FeedMock`/`ListFeedMock`, namespace and assembly `Uno.HotTesting.Reactive`, spec 009). My `Uno.Extensions.Reactive.Mocking` duplicated it, so it was **removed**. Naming and namespace decisions following David's review on staging PR #1:

- **A single assembly, `Uno.HotTesting.Reactive`**: the whole mocking runtime lives there (the existing `FeedMock`/`ListFeedMock` plus the tier 2 and 3 additions). `Uno.Extensions.Reactive.Mocking` deleted.
- **`<Thing>Mock` naming** (suffix, consistent with `FeedMock`): `MockFeed` to `FeedMock` (reused), `MockListFeed` to `ListFeedMock` (reused), `MockCommand` to **`CommandMock`** (added). The public surface of `FeedMock`/`ListFeedMock` is locked by `Given_PublicApi` (7 primitives: Empty, Error, Loading, Message, Refreshing, Undefined, Value) and is reused as is (my `EmptyList` was dropped, `Empty` as None is enough).
- **Swap engine inside `MockingService`** (the `MockModel` type was dropped as confusing): `MockingService.Enable()` plus `MockingService.SwapFeed<T>`/`SwapListFeed<T>` (public, `EditorBrowsable(Never)`, fail-hard). The swap is **strongly typed and generated** (no runtime reflection), so it is **AOT-safe** and the assembly keeps `IsAotCompatible=true`.
- **Consumer generator renamed to `Uno.HotTesting.Reactive.Generator`** (analyzer and tool of the `Uno.HotTesting.Reactive` package); emission uses **raw string literals** (consistent with the rest of the code generation); it emits `FeedMock`/`ListFeedMock`/`CommandMock` plus `MockingService.Swap` and `__Mock_SetCommand`.
- **MVUX instrumentation emitted BY DEFAULT** (no longer opt-in): the `FeedDependency`/`CtorDependency` attributes and the `__Mock_SetCommand` seam are always emitted; the **runtime** (`MockingService.Enable()`) decides activation. Opt-out is available through `[assembly: EnableFeedMocking(IsEnabled = false)]`, which restores byte-identical MVUX output. This matches the on-by-default, opt-out model of the other MVUX attributes.

**Tests after the refactor (green):** Given_MockingActivation 4/4, Given_MockingRuntime 4/4, Given_GeneratedMock 4/4, Tests.Generator 80/80, `Uno.HotTesting.Reactive.Tests` 22/22 (the existing FeedMock is not regressed).

## v10 — David review (comment 31): AsyncLocal out of Core

David feedback on `SourceContext`: *"if we need an AsyncLocal for mocking, putting it in the SourceContext brings nothing, we should keep it in the MockingService"*. Correct — the ambient activation state is a **mocking** concern, not a Core one.

- **`MockingService` (in `Uno.HotTesting.Reactive`) owns the ambient `AsyncLocal<bool>`** plus `Enable()`.
- **`SourceContext` (Core) only keeps** the instance bit `IsMockingActive` plus a **seam**, `internal static Func<bool>? IsMockingActiveProbe`. When a root context is created, `IsMockingActive = IsMockingActiveProbe?.Invoke() ?? false`; a child inherits from its parent. `MockingService` registers the probe in its static constructor.
- **Live application**: `MockingService` is never touched, so the probe is null, `IsMockingActive` is always false and the cost is zero (G9/R7 preserved). The mechanism is still D12 (per-context bit captured at construction, surviving a lazy subscription after the scope is disposed).

Tests unchanged and green: Given_MockingActivation 4/4, Given_MockingRuntime 4/4, Given_GeneratedMock 4/4, Tests.Generator 80/80, Uno.HotTesting.Reactive.Tests 22/22.

## v11 — documentation, sample and factory naming polish (Tue 08-25)

- **Documentation**: `doc/Reference/Reactive/testing.md` (the #3149 page about hand-written `FeedMock`) extended with the generated tier 2 and 3 layer: the `MockingService.Enable()` scope, `record {Model}Mock` (required inputs, optional derived members and commands), `{Vm}Mock.Create(...)`, `vm.SetModel(...)`, `CommandMock`, derived-survives, one-liners and named catalogs (tier 3), and the `[assembly: EnableFeedMocking(IsEnabled = false)]` opt-out. The "no generator" sentence from #3149 is updated.
- **Sample**: `RecipeCatalog` (a tier 3 named catalog: Loading, Empty, Basic, Failed) in the test project, plus the test `When_CatalogEntry_Then_PinnedState` (Given_GeneratedMock 5/5).
- **Naming polish (consumer generator)**: the generated factory class moves from `{Model}MockExtensions` to **`{Vm}Mock`** (`RecipeViewModelMock.Create(...)`), which reads cleanly and stays close to the intent of spec §7 and §8 (a literal `{Vm}.Create` is impossible cross-assembly). `Empty` moved **onto the record** (`RecipeModelMock.Empty`) so it composes with `with`. Spec §7 and §8 aligned with the real API.

Tests: MockingActivation 4/4, MockingRuntime 4/4, GeneratedMock 5/5, Uno.HotTesting.Reactive.Tests 22/22, Tests.Generator 80/80.

## v12 — David review (post-discussion on staging PR #1, Fri 08-28)

Six pieces of feedback from David on the PR, all applied:

1. **The generated `{Vm}Mock` is `partial`** — the app extends the factory class with its named catalogs (tier 3) in its own file, in the same namespace.
2. **`SetModel` renamed to `SetMock`** (facade rename).
3. **`Create` only takes the record** — the denormalized `Create(input...)` overloads are removed. What remains is `Create()` (equal to `{Model}Mock.Empty`) and `Create({Model}Mock)`.
4. **`{Model}Mock.Empty`** confirmed (on the record, every input empty) plus the example `vm.SetMock(RecipeModelMock.Empty with { Steps = ListFeedMock.Loading<Step>() })`.
5. **Activation scope moved INSIDE `Create`** — `Create` opens `MockingService.Enable()` around construction, so user code no longer opens a scope (the mockable bit captured on the context survives later `SetMock` calls and lazy subscriptions, D12).
6. **Command mocking deferred to vNext** — the consumer generator no longer emits a command member nor the `__Mock_SetCommand` wiring; the MVUX seam stays available for that future work. `CommandMock` (the vocabulary) stays in the assembly, not wired up.

Propagated: documentation `doc/Reference/Reactive/testing.md`, spec §7/§8/§10/§13, architecture §2.2/§5/§6, and the `RecipeViewModelMock` partial sample. Tests: MockingActivation 4/4, MockingRuntime 4/4, GeneratedMock 4/4, Tests.Generator 80/80, Uno.HotTesting.Reactive.Tests 22/22.

---

## Final decision register

| # | Decision | Version |
| --- | --- | --- |
| D1 | Tier 1 is an author-declared non-generic `MessageEntry` **in Core**, a plain CLR object (not a `DependencyObject`), **not observable**, with core axes as direct properties and custom axes through `Axes`/`Set`; replacing the instance pushes into the existing wrapper (natural evolution, no loading flash) | v2 to v4 |
| D2 | Commands go through a `??` seam (no swap analog) | v1 |
| D3 | A facade (`SetModel` and generated setters) sits in front of the hooks; `HotSwapFeed` and the handles stay non-public | v1 |
| D4 | ~~Dedicated mockable flag in `FeedConfiguration`~~ **replaced in v7** by the per-context gate `SourceContext.IsMockingActive` (D12) | v1 to v7 |
| D5 | Mocking code generation is **external** (consumer project); the MVUX generator only does analysis, attributes and hidden hooks | v1 |
| D6 | The swap is anchored at the **model-feed cache**, so derived feeds survive (non-negotiable); derived members remain individually overridable | v1 and v2 |
| D7 | The non-AOT nature of the mocking path is accepted (development and test only) | v1 |
| D8 | Converters are application-owned illustrations at `FeedView.Source` (returning `IMessageEntry`); the feature implements none | v4 |
| D9 | Tiers 2 and 3 are strictly typed; the tier 1 object is confined to tier 1 | v4 |
| D10 | **Scoped activation**: `using (MockingService.Enable())` — never an app-wide switch; an assembly init can cover a whole run. Outside a scope there is **no wrap** (`HotSwapFeed` has a cost and is forbidden in a live app). Only the internal mechanism was left to the P0-e spike | v6 |
| D11 | **Fail-hard reflection swap**: reuse the hot-reload driver (`BindableViewModelBase.HotReload`, iterating `IHotSwapState<T>`); the MVUX generator emits **no `__Mock_Swap_{Member}`**, only metadata plus the null-inject constructor and command seam. **Difference from hot reload: a member that cannot be swapped throws** (mocking is strict, not best-effort) | v7 |
| D12 | **The mockable gate is the per-context bit `SourceContext.IsMockingActive`**, read in the `StateImpl` constructor **instead of** the global `EffectiveHotReload` static, so only contexts under a scope wrap and everything else pays nothing (G9/R7 by construction). No separate static and no home-grown `AsyncLocal` (we reuse `AsyncLocal<SourceContext> Current`). **Core reflection accepted over strict AOT**: the two-assembly split forces reflection anyway, and the mocking path is development and test only, non-AOT (NG2/D7) | v7 |
