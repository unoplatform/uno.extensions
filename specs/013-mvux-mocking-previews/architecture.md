# 013 — Architecture

Grounded in the current tree. File refs relative to repo root. (Restored after workspace loss — line references to be re-verified against a fresh clone.)

## 0. Existing seams reused

| Concern | Existing type / seam | Location |
| --- | --- | --- |
| State carrier bound by templates | `MessageEntry<T>` (public), `IMessageEntry` | `Core/MessageEntry.cs`, `Core/IMessageEntry.cs` |
| One-message-then-complete feed | `ValueFeed<T>` (internal) | `Sources/ValueFeed.cs` |
| Runtime feed substitution | `HotSwapFeed<T>.Set(feed)` (internal) | `Operators/HotSwapFeed.cs` |
| **Feed identity cache (per property)** | `AttachedProperty.GetOrCreate(owner/delegate, factory)` | `Core/Feed.cs` (all factories), `Core/Internal/AttachedProperty*` |
| Per-state swap seam | `IHotSwapState<T>.HotSwap` → `_hotSwap.Set` | `Core/Internal/StateImpl.cs:95` |
| Swap gate (precedent) | `EffectiveHotReload.HasFlag(State)` | `StateImpl.cs:74`, `Config/HotReloadSupport.cs` |
| HR model replacement (inspiration) | `HotPatch` → `__Reactive_CreateModelInstance` → `__Reactive_UpdateModel` → `__Reactive_BindableInitializeForUpdatedModel` | `Presentation/Bindings/BindableViewModelBase.HotReload.cs`, `ViewModelGenTool_3.cs:202` |
| VM ctor wraps real Model | `{Vm}(params) : this(new Model(params))` | `ViewModelGenTool_3.cs:128` |
| Visual state from axes | `FeedViewVisualStateSelector.GetVisualState` | `UI/View/FeedViewVisualStateSelector.cs:31` |

## 1. The swap anchor — why derived feeds survive (D6)

Every feed factory caches its instance via `AttachedProperty.GetOrCreate` keyed on the provider delegate (stable when lambdas capture only `this` — the MVUX norm). A derived feed `StepsCount => Steps.Select(...)` is itself a cached `SelectFeed(sourceFeed, selector)` **composed on the instance returned by `Steps`**.

**Anchor:** under the mockable flag, the feed returned for a Model feed-property is wrapped in a `HotSwapFeed<T>` **at this cache level** (stable identity preserved — the wrapper is what gets cached). Consequences:

- `Model.Steps` returns the wrapper → the VM state subscribes to it → **swap propagates to the VM member**;
- `StepsCount`'s `SelectFeed` composes on the same wrapper → **swap propagates through business logic** (live: a re-swap re-emits through `Select`);
- no `dynamic`, no duck-typed re-init needed for feeds: **`SetModel` = a series of typed swaps** on hidden handles. (The HR `dynamic` path stays untouched, HR-only.)

Identity risk (R6): lambdas capturing locals/params produce fresh delegate targets → unstable cache key. This pre-exists mocking (same constraint for state persistence); P0 canary + doc.

## 2. Split of responsibilities

### 2.1 MVUX generator (Model's assembly — analysis + attributes + hidden hooks)

**a) Dependency analysis** (Roslyn, source available):
- per feed/command member: walk initializer/getter body; **lambda/anonymous/local-function bodies = deferred boundary**; eager remainder binding to a ctor param (or param-assigned field) → `ServiceDependent(param)`; reference to another feed member → `DerivedFrom(member)`; else `Independent`.
- **ctor instrumentation**: walk ctor bodies (incl. field/property initializers, primary-ctor captures used eagerly); any eager service dereference → the ctor is **unsafe under null-inject for that parameter**.

**b) Emitted metadata attributes** (defined in core so they survive as metadata; also **hand-declarable** on the Model — explicit declarations override/merge with analysis):

```csharp
[FeedDependency(nameof(RecipeModel.Steps), OnParameter = "svc")]          // service-dependent input
[FeedDependency(nameof(RecipeModel.StepsCount), OnFeed = nameof(Steps))] // derived — never required in mocks
[CtorDependency("svc", Eager = true, Members = new[] { "..." })]          // ctor NREs if svc is null
```

(Names to bikeshed; semantics fixed: *input vs derived vs independent*, plus *ctor-eager* flags.)

**c) Hidden hooks** (`EditorBrowsable(Never)`, emitted only under the opt-in flag):
- on the **Model partial**: typed per-feed swap handles — `__Mock_Swap_Steps(IListFeed<Step> feed)` → `hotSwapWrapper.Set(feed)`; no strings, no reflection;
- on the **VM partial**: `__Mock_Initialize()` (dedicated; NOT `__Reactive_UpdateModel` — must not reassign `__reactiveModel`, rebind INPC, nor let `Model`'s `Unsafe.As` see a foreign type) + command seam `Save = __mockCommands?.Save ?? new AsyncCommand(...)` (R2).

### 2.2 Mocking generator (ships in `Uno.Extensions.Reactive.Mocking`, runs in the test/preview project)

Reads the app assembly **metadata** (generated VM/Model types + the attributes above). No syntax trees needed → cross-assembly by construction. Emits **external, generic and strongly typed types/extensions** (partial injection impossible and not needed):

```csharp
public record RecipeModelMock
{
    public static RecipeModelMock Empty { get; } = new() { Steps = MockListFeed.Empty<Step>() };
    public required IListFeed<Step> Steps { get; init; }     // ServiceDependent input → required
    public IFeed<int>? StepsCount { get; init; }              // Derived → optional override; null = real business logic
    public IAsyncCommand? Save { get; init; }                 // command → optional; null = idle no-op
}
public static class RecipeViewModelMockExtensions
{
    public static RecipeViewModel Create();                                  // null-inject + SetModel(Empty)
    public static RecipeViewModel Create(IListFeed<Step> steps);             // required inputs as params
    public static void SetModel(this RecipeViewModel vm, RecipeModelMock mock); // typed swaps via hidden handles
}
```

- `Create()` constructs the **real VM** via `new {Vm}(default!, …)`; **compile-time guard**: if `[CtorDependency(Eager=true)]` names parameter `p`, `Create` **requires** a real/fake `p` argument (or the generator emits an error diagnostic if no safe overload is possible).
- `SetModel` may be called repeatedly (live transitions, G6); `with`-expressions on the record make variants cheap (`Empty with { Steps = … }`).
- `required init` on service-dependent inputs = compile-time completeness. **Derived members are optional overrides**: `null` (default) → the real derivation recomputes over the swapped inputs; non-null → that member's own wrapper is swapped too (the cache-level anchor wraps *every* feed property, derived included) — lets a test pin a derived value without caring about its inputs.
- **Tier 2 and tier 3 never accept `MessageEntry`, an untyped feed envelope, or any other tier-1 authoring abstraction. Their contracts remain `IFeed<T>`, `IListFeed<T>`, typed states and typed commands end to end.**

## 3. Tier 1 — declared `MessageEntry` as `FeedView.Source`

`FeedView.Source` accepts an **`IMessageEntry`** directly (core contract, unchanged). The authoring surface is a new **non-generic, authorable `MessageEntry` in Core**. It is a plain CLR object that XAML can instantiate, implementing `IMessageEntry`; it is **deliberately not a `DependencyObject`** and adds no UI property-system complexity to the message model:

```csharp
// Uno.Extensions.Reactive — authorable entry (XAML-friendly, plain CLR)
public sealed class MessageEntry : IMessageEntry
{
    public object? Data { get; set; }       // set → Some; explicitly null → None
    public bool IsUndefined { get; set; }   // true → Data axis Undefined (pre-first-emission)
    public object? Error { get; set; }      // Exception, or any value wrapped as exception (strings)
    public bool IsProgress { get; set; }    // true → transient message → Indeterminate

    // Extensibility — MVUX's strength is its open axis model; anything beyond
    // the core axes goes through the axis collection:
    public AxisValueCollection Axes { get; }            // XAML content collection
    public void Set(MessageAxis axis, object? value);   // code path (typed axis instance)
}

public sealed class AxisValue
{
    public string Axis { get; set; }    // axis identifier for XAML,
                                        // resolved against core + registered app axes
    public object? Value { get; set; }
}
```

**Custom axes are first-class.** The core axes (`Data`/`Error`/`Progress`) are just convenience properties; any other axis — built-in non-core (selection, pagination) or app-defined `MessageAxis<T>` — is expressible through `Axes`/`Set`, participates in the wrapper's **axis diff** like any core axis, and flows to `FeedViewState`/bindings exactly as it would from a real feed. Axis resolution from XAML uses the axis **identifier** (`MessageAxis.Identifier`); an unknown identifier is a diagnostic, not a silent drop.

`Data` may be a POCO or another value authored by the application. This convenience object is confined to tier 1; it is not a mocking vocabulary and does not participate in generated Model mocks.

### Coercion & evolution semantics

`FeedView.OnSourceChanged`:
1. `ISignal<IMessage>` (any feed/state) → passthrough, unchanged.
2. `IMessageEntry` → the view lazily creates **one entry-driven wrapper feed** (`MessageEntryFeed`, internal) and keeps it for the lifetime of the subscription.
3. anything else → today's behavior (ignored). No heuristic.

**Requirement — natural feed evolution.** When the `FeedView.Source` **instance changes** to another `IMessageEntry` (for example, a preview state picker updates its bound source), the new entry is **pushed into the existing wrapper**: the subscription is preserved and the message stream evolves entry-by-entry, each message's changes being the **axis diff against the previous entry**. The view must **not** transit through a spurious initial/loading state between two entries — the stream must look like the natural evolution of a real feed (`Loading → Some → Error → …`). Re-creating the wrapper is acceptable as an implementation detail **only if** no visible state reset occurs; the observable contract is the natural evolution.

**`MessageEntry` itself is not observable.** Mutating `Data`, `Error`, `IsProgress` or `Axes` after the entry has been assigned does not push a message; assign a new entry to `FeedView.Source` to represent the next state.

### XAML examples

```xml
xmlns:mvux="using:Uno.Extensions.Reactive.UI"
xmlns:reactive="using:Uno.Extensions.Reactive"

<!-- Pinned loading — no view model -->
<mvux:FeedView ProgressTemplate="{StaticResource Spinner}">
    <mvux:FeedView.Source>
        <reactive:MessageEntry IsProgress="True" />
    </mvux:FeedView.Source>
</mvux:FeedView>

<!-- Value state, inline POCO payload -->
<mvux:FeedView>
    <mvux:FeedView.Source>
        <reactive:MessageEntry>
            <reactive:MessageEntry.Data>
                <models:Recipe Title="Avocado toast" StepsCount="3" />
            </reactive:MessageEntry.Data>
        </reactive:MessageEntry>
    </mvux:FeedView.Source>
    <DataTemplate>
        <TextBlock Text="{Binding Data.Title}" />
    </DataTemplate>
</mvux:FeedView>

<!-- Error / empty / undefined, as resources -->
<Page.Resources>
    <reactive:MessageEntry x:Key="Failed"    Error="Timeout while loading the recipe" />
    <reactive:MessageEntry x:Key="NoResult"  Data="{x:Null}" />
    <reactive:MessageEntry x:Key="Undefined" IsUndefined="True" />
</Page.Resources>
<mvux:FeedView Source="{StaticResource Failed}" />

<!-- State picker driving natural evolution (preview head) -->
<mvux:FeedView Source="{Binding CurrentEntry}" />
<!-- CurrentEntry is an IMessageEntry property raising INPC: each assignment pushes
     the next entry through the SAME wrapper — Loading → Value with no re-subscribe,
     no loading flash between entries. -->

<!-- Custom axes: core axes are direct properties, anything else via Axes -->
<mvux:FeedView>
    <mvux:FeedView.Source>
        <reactive:MessageEntry>
            <reactive:MessageEntry.Data>
                <models:Recipe Title="Avocado toast" />
            </reactive:MessageEntry.Data>
            <reactive:MessageEntry.Axes>
                <reactive:AxisValue Axis="MyApp.Confidence" Value="0.87" />
            </reactive:MessageEntry.Axes>
        </reactive:MessageEntry>
    </mvux:FeedView.Source>
</mvux:FeedView>

<!-- Illustrative only: an app may attach its own converter at FeedView.Source.
     The converter must return MessageEntry; this spec does not define or implement it. -->
<mvux:FeedView Source="{Binding RecipeJson,
                                Converter={StaticResource AppJsonToMessageEntry}}" />
```

The `FeedView.Source` converter above is only an illustration of normal XAML composition. It is application-owned and is not an implementation deliverable of this proposal.

## 4. Tier 3 — complete-model helpers

Pure consumers of §2.2: named catalogs (`static RecipeViewModel BasicRecipe => Create(ListFeed.Value(...))`), selection posed via states (`vm.Selected.Set(1)`), gallery pickers over `MockFeedState`. Hand-written in the test/preview project, optionally scaffolded.

## 5. End-to-end flow

```
Test/preview project refs Uno.Extensions.Reactive.Mocking
  → its generator reads app metadata + [FeedDependency]/[CtorDependency]
  → emits {Model}Mock (required inputs only) + Create/SetModel
Create(steps)
  → new {Vm}(default!…)            // real VM + real Model (ctor-eager params required as args)
  → mockable flag ON → every Model feed-property cached as HotSwapFeed wrapper
  → SetModel(Empty with { Steps = steps })
      → vm.Model.__Mock_Swap_Steps(steps)   // typed, hidden
      → StepsCount (SelectFeed over wrapper) recomputes  ✔ business logic
  → FeedView renders pinned states; later SetModel(...) re-swaps live
```

## 6. Context-wide scope and ambient activation — UNRESOLVED

Raised in the last exchange before the workspace loss. VM scope may be accidental: the more general boundary may be the context owning States/subscriptions, believed to be `SourceContext` but **not yet source-verified**.

Desired creation scope:

```csharp
using (MockingService.Enable())
{
    var model = new RecipeModel(...);
}
```

A plausible design is that `Enable()` establishes an ambient capture scope; any feed context created inside it is tagged/configured as mockable. Disposing the scope would stop capture for future contexts while already-created contexts remain mockable for their lifetime. **This is only a hypothesis, not an accepted decision.**

The source review / spike must answer:

- exact context type and context-creation call;
- whether context creation is eager during Model/VM construction or lazy at first subscription;
- whether an `AsyncLocal` scope is sufficient across async construction;
- nested scope semantics;
- concurrent tests/model construction;
- subscription lifetime after scope disposal;
- whether activation attaches a mock registry/provider to a context;
- interaction with the separate mockable configuration flag (D4).

If the context is created lazily after the `using` block, the desired syntax cannot work without either eager context capture during construction or transferring an activation token onto the Model/context owner.

## 7. Constraints

- **Non-AOT/trim-safe by design** (D7): dev/test-time only; document that the Mocking package must never be referenced by a published app head.
- Tier 2/3 APIs remain generic and strongly typed; they do not depend on the non-generic tier-1 `MessageEntry`, source conversion, or an untyped feed abstraction.
- Tier 1 stays an isolated UI convenience.
- Frozen names (Hot Design + tests): `{Model}Mock`, `Empty`, `Create`, `SetModel`, attribute names, hidden hook prefixes.
- MVUX output byte-identical when opt-in flag absent.
