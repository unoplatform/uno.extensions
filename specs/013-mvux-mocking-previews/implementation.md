# 013 — Implementation

Concrete surfaces, touch-list, phasing, tests. Names bikesheddable; semantics fixed per `spec.md`. (Restored after workspace loss.)

## 1. Packages & where things live

| Piece | Package | Notes |
| --- | --- | --- |
| Dependency attributes | `Uno.Extensions.Reactive` (core) | must survive as metadata in the app assembly |
| Mockable gate + HotSwap wrap at feed cache | core | **`SourceContext.IsMockingActive`** (new per-context bit, D12) read in `StateImpl` ctor; wrap wired at the `AttachedProperty`/factory cache |
| Authorable `MessageEntry` + `AxisValue` (plain CLR) + internal `MessageEntryFeed` | core | tier-1, AOT-safe, **not** a `DependencyObject` |
| `FeedView.Source` coercion bridge | `Uno.Extensions.Reactive.UI` | tier-1 |
| Analysis + hidden hooks emission | `Uno.Extensions.Reactive.Generator` | on Model & VM partials, on by default (opt-out) |
| Mock vocabulary (`FeedMock`/`ListFeedMock`/`CommandMock`/`FeedMockState`) | **`Uno.HotTesting.Reactive`** (new) | referenced by test/preview projects only |
| Mocking generator (`{Model}Mock`, `Create`, `SetMock`) | `Uno.HotTesting.Reactive` (analyzer asset) | runs in consumer project, reads app metadata |
| Reflection swap driver (reused, fail-hard) | core | reuse hot-reload's `IHotSwapState<T>` iteration; **throw on un-swappable member** (D11) |
| `MockingService.Enable()` activation scope | `Uno.HotTesting.Reactive` | frozen name; sets `SourceContext.IsMockingActive` on the ambient/pre-seeded context (§6) |

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

### 2.2 Mockable gate + swap anchor
- **`SourceContext.IsMockingActive`** (per-context bit, D12 — distinct from `HotReload`, no global static, no bespoke `AsyncLocal`) — **set by the activation scope (§6), off by default**; context not mockable → no wrap, so a live app pays nothing (spec G9/R7). Read at wrap time in `StateImpl` ctor **instead of** `FeedConfiguration.EffectiveHotReload`.
- When the owning context is mockable: feed factories wrap the cached instance in `HotSwapFeed<T>` (the wrapper IS the cached value → stable identity; derivations compose on the wrapper). Minimal wiring: wrap inside `AttachedProperty.GetOrCreate` call sites in `Core/Feed.cs` / `Core/ListFeed.cs` factories (one helper reading the context bit).
- **Swap = reflection over the context's `IHotSwapState<T>` members** (D11), reusing the hot-reload driver (`BindableViewModelBase.HotReload`), **fail-hard**: a mocked member that cannot be swapped throws (no silent skip — the hot-reload delta).

### 2.3 Tier-1 core surfaces
- `Feed.Value<T>` public factory (from #3148, additive).
- Authorable non-generic `MessageEntry : IMessageEntry` — **plain CLR object, not a `DependencyObject`, not observable**; settable `Data` / `IsUndefined` / `Error` / `IsProgress`; `Axes` (`AxisValueCollection` of `AxisValue { string Axis; object? Value }`) + `Set(MessageAxis, object?)` code path.
- Axis-identifier resolution against core + registered app axes; **unknown identifier → diagnostic**, never a silent drop.
- Internal `MessageEntryFeed` — entry-driven wrapper with `Push(IMessageEntry)`; each pushed entry emitted as the **axis diff** vs the previous one, **custom axes included**.

## 3. MVUX generator changes (`Uno.Extensions.Reactive.Generator`)

On by default (the runtime decides activation). Opt-out: `[assembly: EnableFeedMocking(IsEnabled = false)]` → byte-identical MVUX output.

1. **Analysis pass** (per Model): classify members `ServiceDependent(param) | DerivedFrom(feed) | Independent`; lambda/anonymous/local-function bodies = deferred boundary. **Ctor instrumentation**: walk ctor bodies + field/property initializers + primary-ctor eager captures → mark `CtorDependency(Eager=true)` per offending parameter. Hand-declared attributes override/merge (author is the escape hatch).
2. **Emit attributes** (§2.1) on the generated Model partial.
3. **Emitted seams** (`EditorBrowsable(Never)`) — only what reflection cannot synthesize:
   - Model partial: **no per-feed `__Mock_Swap_{Member}`** — swap is reflection over `IHotSwapState<T>` at runtime (D11). (The `HotSwapFeed` wrappers already expose the swap seam the reflection driver uses.)
   - VM partial: **no dedicated construction seam** — null-inject construction reuses the existing public constructors (`new {Vm}(default!, …)`); under an ambient `MockingService.Enable()` scope the `SourceContext` created at construction is mockable (D12), and the bit is captured on the context instance so a lazy first subscription after the scope is disposed still wraps. Commands have no `IHotSwapState<T>` and are unreachable by the reflection swap, so a **dedicated public `__Mock_SetCommand(string name, IAsyncCommand)`** seam (`EditorBrowsable(Never)`) reassigns the command property post-construction (R2). Fail-hard: an unknown command name throws (strict, like D11).
4. Diagnostics: `FEED3201` eager ctor access detected (info: `Create` will require the service), `FEED3202` unstable feed identity (capture pattern defeats caching), `FEED3203` explicit attribute contradicts analysis.

## 4. Mocking package (`Uno.HotTesting.Reactive`)

### 4.1 Runtime vocabulary (all generic and strongly typed)
```csharp
public static class FeedMock
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
public static class ListFeedMock
{
    // Typed list equivalents: Undefined, Loading, Empty (None), EmptyList (Some(empty)),
    // Value(params/list), Value(list, SelectionInfo), Error, Refreshing.
}
public static class CommandMock
{
    public static IAsyncCommand Idle();
    public static IAsyncCommand Disabled();
    public static IAsyncCommand Executing();
    public static IAsyncCommand Callback(Action<object?> onExecute, bool canExecute = true);
}
public enum FeedMockState { Undefined, Loading, Empty, Value, Error, Refreshing }
```
Built over public `Feed.Create` + `MessageBuilder` (vocabulary from #3147). **These APIs never accept the non-generic tier-1 `MessageEntry` or untyped envelopes.** Never referenced by a published app head (non-AOT, dev/test only — NG2/D7).

### 4.2 Generator (runs in the consumer/test project, metadata-driven)
For each Model/VM pair found in referenced assemblies with `__Mock_*` hooks + attributes:
```csharp
public record RecipeModelMock
{
    public static RecipeModelMock Empty { get; }              // ServiceDependent → FeedMock/ListFeedMock.Empty
    public required IListFeed<Step> Steps { get; init; }      // exactly the ServiceDependent set
    public IFeed<int>? StepsCount { get; init; }               // Derived → optional override; null = real derivation

    public static RecipeModelMock Empty { get; }               // every input pinned to its Empty state
}
public static partial class RecipeViewModelMock   // partial → user extends with named catalogs
{
    public static RecipeViewModel Create();                                      // = Create(RecipeModelMock.Empty)
    public static RecipeViewModel Create(RecipeModelMock mock);                 // opens MockingService.Enable() internally
    public static void SetMock(this RecipeViewModel vm, RecipeModelMock mock);  // typed swaps (fail-hard)
}
```
Rules:
- Required properties = the **ServiceDependent** input set; `Create` takes only the record (no denormalized per-input overloads).
- **Derived members: optional overrides** — `null` (default) → real derivation recomputes over swapped inputs; set → that member's wrapper is swapped too. Independent members: untouched.
- **Command mocking is deferred to vNext**: the record carries no command member and `SetMock` wires none (the MVUX `__Mock_SetCommand` seam stays available for that future work).
- `Create` opens the `MockingService.Enable()` scope around construction, so user code never opens it. `SetMock` callable repeatedly → live transitions (`vm.SetMock(RecipeModelMock.Empty with { Steps = ... })`).
- Concrete generic types preserved throughout; **no tier-1 type or conversion path is emitted**.
- Diagnostic `MOCK0001` when a VM is reachable but its assembly lacks hooks (opt-in missing).

## 5. UI (`Uno.Extensions.Reactive.UI`) — tier 1
- `FeedView.OnSourceChanged`: typed branch `IMessageEntry` → lazily create ONE `MessageEntryFeed` wrapper kept across `Source` changes; a subsequent `IMessageEntry` instance is **pushed** into the wrapper (subscription preserved, no state reset — natural-evolution contract, architecture §3). No heuristic.
- **Mutations of an already-assigned entry are not observed** (plain CLR, not observable); a new instance is the unit of change.
- XAML element syntax (`<reactive:MessageEntry IsProgress="True" />`, …) — examples in architecture §3.
- **No converter implementation.** A converter at `FeedView.Source` returning `IMessageEntry` appears in docs/samples as an application-owned illustration only (D8/NG6).

## 6. Scoped activation — API decided (D10), mechanism resolved (D12)

```csharp
namespace Uno.HotTesting.Reactive;

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
var vm = RecipeViewModelMock.Create(new RecipeModelMock { Steps = ListFeedMock.Value(steps) }); // Create opens the scope internally
```

**Non-negotiable constraint:** context not mockable → **no `HotSwapFeed` wrap at all**. The wrap is one indirection per feed; it may never be injected into the feeds of a live app (spec G9/R7). `SourceContext.IsMockingActive` (§2.2, D12) is the internal per-context gate the scope drives, not a switch app authors set.

Mechanism (resolved against source — `Core/Internal/SourceContext.cs`, D12):
- **Owner context = `SourceContext`** — already owns `States`/subscriptions, already ambient via `AsyncLocal<SourceContext> Current`, already per-owner via `GetOrCreate(owner)`, with an eager pre-seed seam `PreConfigure(type, ctx)` / `Set(owner, ctx)`. It gains `bool IsMockingActive`.
- **Eager vs lazy = solved by pre-seed**: `Create(...)` pre-seeds a mockable context on the VM/Model owner (`PreConfigure`/`Set`), so a lazy first subscription after the `using` block still wraps — the bit is on the context instance, not only on the ambient `AsyncLocal`.
- **Ambient propagation**: the existing `AsyncLocal<SourceContext> Current` carries mockability across async construction; no bespoke `AsyncLocal`.
- **Nested / concurrency / lifetime**: per-context-instance bit → concurrent tests don't leak; contexts created inside a scope stay mockable for their own lifetime after `Dispose`.
- **Wiring**: `StateImpl` ctor reads `context.IsMockingActive` (replaces the `EffectiveHotReload` read); swap is reflection over `IHotSwapState<T>` (D11).

## 7. Phasing

- **P0 — de-risk canaries (blocking):**
  a. (tier-1, on hold) `MessageEntry` wrapper visual states + push axis-diff — deferred with tier 1;
  b. wrap-at-cache via `SourceContext.IsMockingActive`: swap `Steps` → `StepsCount` (`Select`) re-emits (D6/D12 — THE gate). The hot-reload path already proves derivation-survives-swap; this canary re-verifies it under the per-context gate;
  c. null-inject construction on a lazy model; eager-ctor fixture NREs as predicted;
  d. feed-identity stability matrix (capture patterns) → informs FEED3202;
  e. `MockingService.Enable()` → `IsMockingActive` on the pre-seeded context: prove **no wrap when the context is not mockable**, and reflection swap is **fail-hard** on an un-swappable member (D11).
- **P1 — Tier 1** (core+UI): `Feed.Value`, authorable `MessageEntry` + `AxisValue` (custom axes), `MessageEntryFeed` + push semantics, `FeedView` bridge, documentation-only converter illustration. Ships alone.
- **P2 — Core: `SourceContext.IsMockingActive` + wrap gate in `StateImpl` + fail-hard reflection swap + attributes + analysis + `__Mock_SetCommand` seam** (MVUX gen). No per-feed swap hooks; no `__Mock_Create` (public ctors + ambient scope).
- **P3 — Mocking package**: typed vocabulary + consumer generator (`{Model}Mock`/`Create`/`SetMock`).
- **P4 — Tier 3 catalogs + Hot Design checkpoint** (name freeze), docs.

## 8. Test plan

### Core
- Every typed `FeedMock`/`ListFeedMock`/`CommandMock` state emits expected axes.
- Authorable entry maps to Data/Error/Progress/Undefined correctly; custom axes map and diff correctly.
- Consecutive entry instances produce correct core + custom axis diffs.
- Wrap identity (`AttachedProperty` returns the same wrapper); swap propagation through `Select`/`Where` and chained derived feeds; live re-swap.

### Generators
- Classification fixtures (lazy/eager/derived/independent; ctor bodies, field/property initializers, primary-ctor captures).
- Attribute emission; explicit-attribute override/merge; FEED3201–3203.
- Byte-identical output when opted out; hooks hidden (`EditorBrowsable`) and typed (concrete generics).
- Consumer generation against a compiled fixture assembly; required-input set = ServiceDependent set; eager-ctor → required service parameter; MOCK0001; **no tier-1/untyped surface in tier-2/3 output**.

### Runtime / UI (Skia)
- Each pinned state renders; Loading keeps `IsExecuting`.
- Successive `Source` entries evolve without re-subscribe (no loading flash); **mutating an assigned entry does not emit** — assigning a replacement does.
- `SetMock` drives Loading → Value → Error live; derived member updates on-screen after an input swap (D6 end-to-end).
- Command states drive `Button.IsEnabled`; hot reload does not clobber a mocked VM/context.

### Scoped activation (with §6 spike)
- **Context not mockable → feeds are the raw instances** (no `HotSwapFeed` in the cache, no measurable overhead) — the G9 guard test.
- **Fail-hard swap**: a mocked member with no `IHotSwapState<T>` throws (D11), asserted.
- Assembly-init scope covers every test of the run; a per-test scope covers only its own.
- Nested `Enable()` scopes restore correctly; parallel tests do not leak mockability; async construction retains the intended scope; lazy first subscription after scope disposal has defined behavior; existing contexts remain deterministic after `Dispose`.

### Contract freeze
- Reflection-discovery test for `{Model}Mock`/`Empty`/`Create`/`SetMock`/attribute names (Hot Design contract).

## 9. Docs
- `doc/Learn/Mvux/Testing.md`: `MockingService.Enable()` scope (assembly-init vs per-test, and why it is never app-wide), typed vocabulary, `Create`/`SetMock`, derived-feeds-survive concept, eager-ctor guidance, non-AOT constraint, R4/R5 caveats.
- `doc/Learn/Mvux/FeedView.md`: tier-1 entry authoring + custom axes; converter shown only as an application-owned illustration at `FeedView.Source` (not a deliverable).
- `rules.md`: FEED3201–3203, MOCK0001.
