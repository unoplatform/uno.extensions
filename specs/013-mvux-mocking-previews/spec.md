# 013 — MVUX Mocking & Previews

**Status:** Draft — restored after workspace loss (branch `dev/devid/spec-013-mvux-mocking`, last known commits `8d589d9` → `292fb5f` → `2618def` → `cd4c9ad`, all lost with the ACO node; see `history.md`)
**Area:** `Uno.Extensions.Reactive` (attributes + hooks), `Uno.Extensions.Reactive.UI` (tier-1 bridge), **new package `Uno.Extensions.Reactive.Mocking`** (typed vocabulary + facade + its own generator)
**Prior art (POCs):** #3148 / spec 009, #3147 / spec 012
**Primary consumers:** app **test projects** (referencing the app), and Uno **Hot Design** *MVUX State Previews*

---

## 1. Problem

Driving a page or a `FeedView` into a **non-happy feed state** (*Loading forever*, *Error*, *Empty/None*, *Refreshing*, *Undefined*) requires faking the service the Model consumes. Expensive, and several states are **unreachable** via service fakes (indefinite loading, refreshing, undefined, per-feed independence). Two audiences, and **two distinct needs that must stay separate**:

- **Previews (Hot Design):** declare a feed state with no view model, ideally in XAML — a small UI authoring convenience.
- **App testing:** pin each feed of a **real VM** to a chosen state — *testing the app that consumes feeds, not the feeds* — without standing up DI, through a **strongly typed mocking engine**. **Crucially: mocking must be consumable from the OUTSIDE (a test project that references the app), not injected into the app's own source.**

## 2. Core principle — real VM, real Model, business logic survives

We always instantiate the **real ViewModel wrapping the real Model** (null-injected services). Mocking replaces **inputs** (service-dependent feeds), never **logic**: a derived feed such as

```csharp
public IFeed<int> StepsCount => Steps.Select(steps => steps.Count);   // business logic
```

**MUST keep computing over the mocked `Steps`.** That is the whole point of building a real VM+Model. Achieved by anchoring the swap at the **Model-feed level** (the feed-identity cache), so every composition (`Select`, `Where`, …) observes the swapped source. Live re-swap drives state transitions.

## 3. The three tiers (layers of one system)

1. **Tier 1 — Static/XAML, no VM:** `FeedView.Source` accepts a declared **`MessageEntry`** — a new authorable non-generic entry in **Core**, a **plain CLR object (deliberately NOT a `DependencyObject`)**, XAML element syntax; core axes as direct convenience properties and **custom axes first-class** via an axis collection (MVUX's open axis model). **Replacing** the `Source` entry instance pushes the new entry through the existing wrapper feed: the stream evolves like a real feed (no re-subscribe, no loading flash). The entry itself is **not observable** — a new instance is the unit of change. No heuristic envelope, no parallel DTO, no converter deliverable (an application-owned converter at `FeedView.Source` is illustration only). Tier 1 is an **isolated UI convenience** and never leaks into tiers 2/3.
2. **Tier 2 — Externally-generated mocks over the real VM:** referencing `Uno.Extensions.Reactive.Mocking` in a **test/preview project** generates, per reachable Model: `record {Model}Mock` (required-init, compile-time completeness), `{Vm}.Create(...)` factories (null-inject + apply mock), and `SetModel(vm, mock)` which **swaps** each mocked feed at the Model-feed anchor. Commands via a `??` seam. Contracts remain `IFeed<T>` / `IListFeed<T>`, typed states and typed commands **end to end** — tier 2 never accepts `MessageEntry` or any untyped envelope.
3. **Tier 3 — Complete-model ergonomics:** `{Model}Mock.Empty`, `Create()` overloads whose **required parameters are exactly the service-dependent feeds**; **derived members are optional overrides** (unset → the real business logic runs over the mocked inputs; set → replaced — useful for tests); hand-extensible named catalogs (`BasicRecipe`, `RecipeWithSelection`…) for one-line preview binding. Strongly typed, no tier-1 abstractions.

## 4. Split of responsibilities

- **MVUX generator (runs in the Model's assembly, on the partial Model):**
  a. **Dependency analysis** of each feed/command member + **ctor instrumentation** (detect eager service access that would NRE under null-inject);
  b. emits results as **metadata attributes** (also hand-declarable by the author — explicit declarations win/merge);
  c. emits **hidden hooks** (HR-style, `EditorBrowsable(Never)`): per-feed typed swap handles on the Model partial + a dedicated mock-apply path on the VM (NOT `__Reactive_UpdateModel`, which reassigns `__reactiveModel`/INPC and is unsafe for this use).
- **Mocking generator (ships in `Uno.Extensions.Reactive.Mocking`, runs in the consuming test/preview project):** reads the app assembly **metadata** (types + attributes — no syntax trees needed), generates `{Model}Mock` records, `Create` factories and the `SetModel` facade as **new external, generic and strongly typed types/extensions** (no cross-assembly partial).

## 5. Goals / Non-goals

**Goals**
- G1. Pin any service-dependent feed / list-feed / state / command of a real generated VM.
- G2. **Derived feeds recompute over mocked inputs** (business logic survives); derived members remain individually overridable for tests.
- G3. Mock generation happens **in the consumer project** (test/preview), against app metadata.
- G4. Compile-time completeness (`required init`) and compile-time surfacing of eager-ctor constraints.
- G5. Opt-in; byte-identical MVUX output when disabled. Additive only.
- G6. Live re-swap to drive transitions.
- G7. Tier-1 XAML state declaration with no VM, including custom axes.
- G8. Tiers 2/3 **strongly typed end to end**.

**Non-goals**
- NG1. Behavioral/integration testing of services (this targets presentation state).
- NG2. **AOT/trim compliance of the mocking path.** Mocking is dynamic injection, dev/test-time only (JIT). Accepted and documented; never ships in a published app.
- NG3. Making arbitrary JSON graphs bindable on every platform (WinAppSDK dynamic-binding caveat).
- NG4. Command invocation recording/assertions (deferred).
- NG5. Making the tier-1 `MessageEntry` bindable or observable.
- NG6. Defining or implementing a JSON (or any) converter — converters are application-owned illustrations only.
- NG7. Reusing tier-1 untyped authoring objects or conversion helpers in tiers 2/3.

## 6. Frozen contracts (Hot Design + test code discover by name)

`{Model}Mock` record shape (required-init members, `Empty`, `with`-friendly), `{Vm}.Create(...)`, `SetModel`, the dependency attributes, hidden hook naming, and the `Uno.Extensions.Reactive.Mocking` namespace. Renames = breaking; additive evolution fine.

## 7. Risks

- R1. **Eager ctor service access** → NRE at construction, before any swap. Mitigation: ctor instrumentation (§4a) → attribute → `Create(...)` **requires** those services as parameters (or a diagnostic if unconstructible).
- R2. Commands are not swap-backed states → `??` seam in `CommandFromMethod` emission.
- R3. Undefined change-detection canary (spec 012 §10.2) → tier-1 `MessageEntry` route avoids it; tier-2 vocab must prove it by test (P0).
- R4. Refresh axis internal → `Refreshing` visually faithful, not axis-faithful. Documented.
- R5. Scalar `IFeed<T>` → plain generated property, invisible to `FeedView`. Documented.
- R6. Swap anchor identity: feed caching keys on stable delegate targets (lambdas capturing only `this`); exotic capture patterns may produce unstable identity → P0 canary + diagnostic.

## 8. Resolved decisions (log)

- D1 → tier-1 authoring = authorable non-generic `MessageEntry` **in Core** (the framework concept itself — no parallel DTO, plain XAML element syntax); **plain CLR object, deliberately not a `DependencyObject`**; **not observable** (replacing the instance is the unit of change); core axes as convenience properties, **custom axes** via `Axes` collection / `Set(MessageAxis, value)`; entry-instance replacement pushes through the existing wrapper (natural feed evolution, no loading flash).
- D2 → commands = `??` seam (no swap analog).
- D3 → **facade** (`SetModel` / generated setters) in front of hidden hooks; `HotSwapFeed`/handles stay non-public.
- D4 → dedicated **`FeedConfiguration` mockable flag** (decoupled from hot reload).
- D5 → mock codegen is **external** (consumer project); MVUX gen only analyzes + emits attributes & hidden hooks.
- D6 → swap anchored at **Model-feed cache level** so derivations survive (non-negotiable).
- D7 → AOT non-compliance of the mocking path accepted (dev/test only).
- D8 → converters (JSON or other) are **application-owned illustrations** attached at `FeedView.Source` and must return `IMessageEntry`; this feature defines and implements none.
- D9 → tiers 2/3 are **strongly typed end to end**; the tier-1 authoring object is confined to tier 1 and never appears in generated mock contracts.

## 9. Open question — context scope & ambient activation (UNRESOLVED)

Raised immediately before the workspace loss; **not** a resolved decision:

- Tier 2 may not fundamentally be VM-scoped. The actual boundary may be the feed subscription/state **context** (believed to be `SourceContext`; exact type/API must be verified in source).
- Investigate whether typed swaps can apply to **any** such feed context, not only a generated VM.
- Desired call-site shape:

```csharp
using (MockingService.Enable())
{
    var model = new MyModel(...);
}
```

- Required semantics to decide:
  - whether `Enable()` is ambient (`AsyncLocal`) or process-global;
  - nested scopes and restoration order;
  - concurrent model creation;
  - whether the context is created eagerly or lazily after the scope;
  - subscriptions created in the scope but living after `Dispose`;
  - whether `Dispose` disables only future capture while already-created contexts remain mockable;
  - how this ambient activation relates to the dedicated mockable configuration flag (D4).

No answer is considered accepted until verified against source and reviewed by David. A spike is scheduled in P0 (see `implementation.md` §6).
