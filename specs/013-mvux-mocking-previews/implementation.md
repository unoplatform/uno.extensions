# 013 — Implementation

Concrete surfaces, touch-list, phasing, tests. Names bikesheddable; semantics fixed per `spec.md`. (Restored after workspace loss.)

## 1. Packages & where things live

| Piece | Package | Notes |
| --- | --- | --- |
| Dependency attributes | `Uno.Extensions.Reactive` (core) | must survive as metadata in the app assembly |
| Mockable flag + HotSwap wrap at feed cache | core | `FeedConfiguration.Mockable` (new), wired in `AttachedProperty`/factories |
| Authorable `MessageEntry` + `AxisValue` (plain CLR) + internal `MessageEntryFeed` | core | tier-1, AOT-safe, **not** a `DependencyObject` |
| `FeedView.Source` coercion bridge | `Uno.Extensions.Reactive.UI` | tier-1 |
| Analysis + hidden hooks emission | `Uno.Extensions.Reactive.Generator` | on Model & VM partials, opt-in only |
| Mock vocabulary (`MockFeed`/`MockListFeed`/`MockCommand`/`MockFeedState`) | **`Uno.Extensions.Reactive.Mocking`** (new) | referenced by test/preview projects only |
| Mocking generator (`{Model}Mock`, `Create`, `SetModel`) | `Uno.Extensions.Reactive.Mocking` (analyzer asset) | runs in consumer project, reads app metadata |
| `MockingService.Enable()` activation scope | `Uno.Extensions.Reactive.Mocking` | frozen name; the only way to turn the wrap on (§6) |

## 2. Core (`Uno.Extensions.Reactive`)

### 2.1 Dependency attributes (emitted by MVUX gen AND hand-declarable; explicit wins/merges)
```csharp
namespace Uno.Extensions.Reactive.Config;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class FeedDependencyAttribute : Attribute
{
    public FeedDependencyAttribute(string member) { Member = member; }
    public string Member { get; }
    public string? OnParameter { get; init; }   // ctor param (service) feeding this member
    public string? OnFeed { get; init; }        // other feed member → derived
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class CtorDependencyAttribute : Attribute
{
    public CtorDependencyAttribute(string parameter) { Parameter = parameter; }
    public string Parameter { get; }
    public bool Eager { get; init; }            // true → NRE under null-inject; Create must require it
}
```
(David's `[FeedShape("Steps", ModelParameter=…)]` idea, renamed. Multiple per member allowed.)

### 2.2 Mockable flag + swap anchor
- `FeedConfiguration.Mockable` (flag, distinct from `HotReload`) — **driven by the activation scope (§6), off by default**; no scope → no wrap, so a live app pays nothing (spec G9/R7).
- When ON: feed factories wrap the cached instance in `HotSwapFeed<T>` (the wrapper IS the cached value → stable identity; derivations compose on the wrapper). Minimal wiring: wrap inside `AttachedProperty.GetOrCreate` call sites in `Core/Feed.cs` / `Core/ListFeed.cs` factories (one helper).

### 2.3 Tier-1 core surfaces
- `Feed.Value<T>` public factory (from #3148, additive).
- Authorable non-generic `MessageEntry : IMessageEntry` — **plain CLR object, not a `DependencyObject`, not observable**; settable `Data` / `IsUndefined` / `Error` / `IsProgress`; `Axes` (`AxisValueCollection` of `AxisValue { string Axis; object? Value }`) + `Set(MessageAxis, object?)` code path.
- Axis-identifier resolution against core + registered app axes; **unknown identifier → diagnostic**, never a silent drop.
- Internal `MessageEntryFeed` — entry-driven wrapper with `Push(IMessageEntry)`; each pushed entry emitted as the **axis diff** vs the previous one, **custom axes included**.

## 3. MVUX generator changes (`Uno.Extensions.Reactive.Generator`)

Opt-in: `[assembly: EnableFeedMocking]` (or MSBuild prop). When absent → byte-identical output.

1. **Analysis pass** (per Model): classify members `ServiceDependent(param) | DerivedFrom(feed) | Independent`; lambda/anonymous/local-function bodies = deferred boundary. **Ctor instrumentation**: walk ctor bodies + field/property initializers + primary-ctor eager captures → mark `CtorDependency(Eager=true)` per offending parameter. Hand-declared attributes override/merge (author is the escape hatch).
2. **Emit attributes** (§2.1) on the generated Model partial.
3. **Hidden hooks** (`EditorBrowsable(Never)`):
   - Model partial: `void __Mock_Swap_{Member}(IListFeed<T>/IFeed<T> feed)` per feed member → wrapper `.Set(feed)`; plus `bool __Mock_IsMockable` guard (flag on). Hooks retain concrete generic member types.
   - VM partial: `static {Vm} __Mock_Create(object?[] ctorArgs)` → `new {Vm}(…)` null-inject path (dedicated — NOT `__Reactive_UpdateModel`); command seam `Save = __mockCommands?.Save ?? new AsyncCommand(...)` + `__Mock_SetCommand(name, IAsyncCommand)`.
4. Diagnostics: `FEED3201` eager ctor access detected (info: `Create` will require the service), `FEED3202` unstable feed identity (capture pattern defeats caching), `FEED3203` explicit attribute contradicts analysis.

## 4. Mocking package (`Uno.Extensions.Reactive.Mocking`)

### 4.1 Runtime vocabulary (all generic and strongly typed)
```csharp
public static class MockFeed
{
    public static IFeed<T> Undefined<T>();
    public static IFeed<T> Loading<T>();       // transient → Indeterminate, IsExecuting stays true
    public static IFeed<T> Empty<T>();         // Option.None
    public static IFeed<T> Value<T>(T value);
    public static IFeed<T> Error<T>(Exception error);
    public static IFeed<T> Refreshing<T>(T staleValue);
    public static IFeed<T> Message<T>(Action<MessageBuilder<T>> configure);
    public static IFeed<T> Script<T>(params (TimeSpan after, Action<MessageBuilder<T>> step)[] steps); // from #3147
}
public static class MockListFeed
{
    // Typed list equivalents: Undefined, Loading, Empty (None), EmptyList (Some(empty)),
    // Value(params/list), Value(list, SelectionInfo), Error, Refreshing.
}
public static class MockCommand
{
    public static IAsyncCommand Idle();
    public static IAsyncCommand Disabled();
    public static IAsyncCommand Executing();
    public static IAsyncCommand Callback(Action<object?> onExecute, bool canExecute = true);
}
public enum MockFeedState { Undefined, Loading, Empty, Value, Error, Refreshing }
```
Built over public `Feed.Create` + `MessageBuilder` (vocabulary from #3147). **These APIs never accept the non-generic tier-1 `MessageEntry` or untyped envelopes.** Never referenced by a published app head (non-AOT, dev/test only — NG2/D7).

### 4.2 Generator (runs in the consumer/test project, metadata-driven)
For each Model/VM pair found in referenced assemblies with `__Mock_*` hooks + attributes:
```csharp
public record RecipeModelMock
{
    public static RecipeModelMock Empty { get; }              // ServiceDependent → MockFeed/MockListFeed.Empty
    public required IListFeed<Step> Steps { get; init; }      // exactly the ServiceDependent set
    public IFeed<int>? StepsCount { get; init; }               // Derived → optional override; null = real derivation
    public IAsyncCommand? Save { get; init; }                  // optional; default idle no-op
}
public static class RecipeViewModelMocking
{
    public static RecipeViewModel Create();                                       // null-inject + SetModel(Empty)
    public static RecipeViewModel Create(IListFeed<Step> steps);                  // per required input
    public static RecipeViewModel Create(IRecipeService svc, IListFeed<Step> steps); // when CtorDependency(Eager) → service required
    public static void SetModel(this RecipeViewModel vm, RecipeModelMock mock);   // typed __Mock_Swap_* calls
}
```
Rules:
- Required properties/parameters = the **ServiceDependent** input set.
- **Derived members: optional overrides** — `null` (default) → real derivation recomputes over swapped inputs; set → that member's wrapper is swapped too. Independent members: untouched.
- Commands optional; default is an idle no-op.
- `SetModel` callable repeatedly → live transitions (`vm.SetModel(mock with { Steps = ... })`).
- Concrete generic types preserved throughout; **no tier-1 type or conversion path is emitted**.
- Diagnostic `MOCK0001` when a VM is reachable but its assembly lacks hooks (opt-in missing).

## 5. UI (`Uno.Extensions.Reactive.UI`) — tier 1
- `FeedView.OnSourceChanged`: typed branch `IMessageEntry` → lazily create ONE `MessageEntryFeed` wrapper kept across `Source` changes; a subsequent `IMessageEntry` instance is **pushed** into the wrapper (subscription preserved, no state reset — natural-evolution contract, architecture §3). No heuristic.
- **Mutations of an already-assigned entry are not observed** (plain CLR, not observable); a new instance is the unit of change.
- XAML element syntax (`<reactive:MessageEntry IsProgress="True" />`, …) — examples in architecture §3.
- **No converter implementation.** A converter at `FeedView.Source` returning `IMessageEntry` appears in docs/samples as an application-owned illustration only (D8/NG6).

## 6. Scoped activation — API decided (D10), mechanism to spike

```csharp
namespace Uno.Extensions.Reactive.Mocking;

public static class MockingService
{
    public static IDisposable Enable();     // frozen name; disposal ends the scope
}
```

```csharp
// whole test run
[AssemblyInitialize] public static void Init(TestContext _) => _scope = MockingService.Enable();
[AssemblyCleanup]    public static void Cleanup()           => _scope.Dispose();

// or a single test
using (MockingService.Enable()) { var vm = RecipeViewModel.Create(MockListFeed.Value(steps)); }
```

**Non-negotiable constraint:** no activation scope → **no `HotSwapFeed` wrap at all**. The wrap is one indirection per feed; it may never be injected into the feeds of a live app (spec G9/R7). `FeedConfiguration.Mockable` (§2.2) is the internal gate the scope drives, not a switch app authors set.

Spike (P0-e) — establish the *mechanism*, the API shape is fixed:
- which context owns States/subscriptions (believed `SourceContext`) and where it is created;
- eager (Model/VM construction) vs lazy (first subscription) context creation — **if lazy, the scope must be captured on the Model/context owner at construction**, an `AsyncLocal` alone being gone by then;
- ambient propagation across async construction (`AsyncLocal` candidate) vs explicit token;
- nested scopes, deterministic restoration, test isolation under concurrency;
- survival of contexts/subscriptions created inside a disposed scope (expected: mockable for their own lifetime);
- exact wiring from the scope to `FeedConfiguration.Mockable` (D4).

## 7. Phasing

- **P0 — de-risk canaries (blocking):**
  a. `MessageEntry` wrapper → Undefined/None/Some/Error/Loading visual states (R3), and entry push → axis-diff evolution with no loading flash;
  b. wrap-at-cache: swap `Steps` → `StepsCount` (`Select`) re-emits (D6 — THE gate);
  c. null-inject construction on a lazy model; eager-ctor fixture NREs as predicted;
  d. feed-identity stability matrix (capture patterns) → informs FEED3202;
  e. `MockingService.Enable()` scope spike (§6) — mechanism only, API shape decided; must prove **no wrap when no scope is open**.
- **P1 — Tier 1** (core+UI): `Feed.Value`, authorable `MessageEntry` + `AxisValue` (custom axes), `MessageEntryFeed` + push semantics, `FeedView` bridge, documentation-only converter illustration. Ships alone.
- **P2 — Core mockable flag + wrap + hidden hooks + attributes + analysis** (MVUX gen).
- **P3 — Mocking package**: typed vocabulary + consumer generator (`{Model}Mock`/`Create`/`SetModel`).
- **P4 — Tier 3 catalogs + Hot Design checkpoint** (name freeze), docs.

## 8. Test plan

### Core
- Every typed `MockFeed`/`MockListFeed`/`MockCommand` state emits expected axes.
- Authorable entry maps to Data/Error/Progress/Undefined correctly; custom axes map and diff correctly.
- Consecutive entry instances produce correct core + custom axis diffs.
- Wrap identity (`AttachedProperty` returns the same wrapper); swap propagation through `Select`/`Where` and chained derived feeds; live re-swap.

### Generators
- Classification fixtures (lazy/eager/derived/independent; ctor bodies, field/property initializers, primary-ctor captures).
- Attribute emission; explicit-attribute override/merge; FEED3201–3203.
- Byte-identical output when opt-in absent; hooks hidden (`EditorBrowsable`) and typed (concrete generics).
- Consumer generation against a compiled fixture assembly; required-input set = ServiceDependent set; eager-ctor → required service parameter; MOCK0001; **no tier-1/untyped surface in tier-2/3 output**.

### Runtime / UI (Skia)
- Each pinned state renders; Loading keeps `IsExecuting`.
- Successive `Source` entries evolve without re-subscribe (no loading flash); **mutating an assigned entry does not emit** — assigning a replacement does.
- `SetModel` drives Loading → Value → Error live; derived member updates on-screen after an input swap (D6 end-to-end).
- Command states drive `Button.IsEnabled`; hot reload does not clobber a mocked VM/context.

### Scoped activation (with §6 spike)
- **No scope open → feeds are the raw instances** (no `HotSwapFeed` in the cache, no measurable overhead) — the G9 guard test.
- Assembly-init scope covers every test of the run; a per-test scope covers only its own.
- Nested `Enable()` scopes restore correctly; parallel tests do not leak mockability; async construction retains the intended scope; lazy first subscription after scope disposal has defined behavior; existing contexts remain deterministic after `Dispose`.

### Contract freeze
- Reflection-discovery test for `{Model}Mock`/`Empty`/`Create`/`SetModel`/attribute names (Hot Design contract).

## 9. Docs
- `doc/Learn/Mvux/Testing.md`: `MockingService.Enable()` scope (assembly-init vs per-test, and why it is never app-wide), typed vocabulary, `Create`/`SetModel`, derived-feeds-survive concept, eager-ctor guidance, non-AOT constraint, R4/R5 caveats.
- `doc/Learn/Mvux/FeedView.md`: tier-1 entry authoring + custom axes; converter shown only as an application-owned illustration at `FeedView.Source` (not a deliverable).
- `rules.md`: FEED3201–3203, MOCK0001.
