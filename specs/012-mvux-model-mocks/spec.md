# 012 — MVUX Model Mocks

**Status:** Implemented (2026-08-20) — all three phases; see [progress.md](progress.md) for what shipped, logged deviations, and follow-ups
**Area:** `Uno.Extensions.Reactive`, `Uno.Extensions.Reactive.Generator`
**Related:** [001 — MVUX SingleValueFeed](../001-mvux-single-value-feed/spec.md), [002 — UpdateFeed compaction](../002-mvux-updatefeed-compaction/spec.md)

---

## 1. Summary

Let an app author instantiate a **real** generated MVUX ViewModel whose feeds, list-feeds, states and commands are pinned to chosen states (`Loading`, `Undefined`, `Empty`, `Value`, `Error`, `Refreshing`) without constructing the Model, its services, or its dependency graph.

Two deliverables:

1. **A runtime state vocabulary** (`Uno.Extensions.Reactive.Mocks`) — factories that produce an `IFeed<T>` / `IListFeed<T>` / `IAsyncCommand` pinned to a chosen state. Usable today against hand-written Models; no generator involvement.
2. **A generator addition** — per Model, emit a `{Vm}Mocks` bundle plus a model-free constructor and a `{Vm}.CreateMock(...)` factory on the already-generated bindable ViewModel, so the mocked instance **is** the real ViewModel type.

The result at the call site:

```csharp
var vm = RecipesViewModel.CreateMock(m =>
{
    m.Recipes = MockListFeed.Loading<Recipe>();
    m.Profile = MockFeed.Error<Profile>(new TimeoutException());
    m.Save    = MockCommand.Disabled();
});

var page = new RecipesPage { DataContext = vm };
```

---

## 2. Motivation

Today, putting a page into a non-happy feed state requires faking the service the Model consumes. That approach cannot express several states at all:

| State | Reachable via a fake service? |
| --- | --- |
| Data present (`Some`) | Yes |
| Empty (`None`) | Yes |
| Error | Yes (throw) |
| **Loading, indefinitely** | Only with a never-completing `Task` — leaks, and hangs test runs |
| **Refreshing** (stale data + progress) | No |
| **Undefined** (pre-first-emission) | No |
| **Transient error over stale data** | No |
| Per-feed state, independently, on one Model | No — a service fake is per-service, not per-feed |

Additionally, service fakes require the whole DI graph to be stood up, which makes designer previews, screenshot/gallery harnesses, and UI tests of visual states expensive to write.

---

## 3. Goals

- G1. Pin any feed/list-feed/state member of a generated ViewModel to a chosen message state.
- G2. Pin any generated command to `Idle` / `Disabled` / `Executing`.
- G3. Produce an instance assignable to the **real** generated ViewModel type, so `x:Bind`, `d:DataType`, typed C# markup and navigation DI substitution keep working.
- G4. Require **no** Model instance and **no** service graph for a fully-mocked ViewModel.
- G5. Opt-in — apps that do not ask for mocks get byte-identical generated output.
- G6. Additive only. No breaking change to any existing public API or generated shape.

## 4. Non-goals

- NG1. Replacing service-level fakes for behavioral/integration tests. This targets *presentation state*, not logic.
- NG2. Mocking arbitrary non-MVUX view models.
- NG3. A designer-time (`d:DataContext`) XAML story. Out of scope for v1; the factory is callable from a preview head, which is enough.
- NG4. Recording/verifying command invocations (assertion helpers). Deferred; see §11.
- NG5. Setting the real `Refresh` axis (see §10.3).

---

## 5. Background — how the pipeline works today

Read this section before touching the generator. Every claim below is verified against the tree at the time of writing.

### 5.1 What the generator emits

`ViewModelGenTool_3` (`src/Uno.Extensions.Reactive.Generator/Bindables/ViewModelGenTool_3.cs`) walks every type matching `Model$` (or flagged `[ReactiveBindable]`) and emits, per Model:

- `{Model}.ViewModel.g.cs` — `partial class {Name}ViewModel : BindableViewModelBase` (`GenerateViewModel`, line 94)
- `{Model}.Reactive.g.cs` — a partial of the Model itself adding `IAsyncDisposable`, `ISourceContextAware`, `IModel<TVm>`
- `{Model}.FeedDependencies.g.cs` — when applicable
- `{Assembly}.ReactiveViewModelMappings.g.cs` — `Model type → bindable VM type` dictionary

`MainModel` produces `MainViewModel`. Member mapping is in `GetMembers` (line 287):

| Model member | Generated VM member | Emitter |
| --- | --- | --- |
| `IListFeed<T> X` | `IListFeed<T> X { get; private set; }` | `BindableListFromListFeedProperty` |
| `IFeed<IImmutableList<T>> X` | list bindable | `BindableListFromFeedOfList*` |
| `IFeed<TRecord> X` (record w/ default ctor) | `Bindable{TRecord} X { get; private set; }` | `BindableFromFeedProperty` |
| `IFeed<T> X` (scalar) | `T X { get; set; }` + `Bindable<T> _x` backing | `PropertyFromFeedProperty` |
| `IState<T> X` | same as `IFeed<T>` | (`PropertyFromFeedProperty` handles both) |
| `ValueTask Foo(...)` | `IAsyncCommand Foo { get; private set; }` | `CommandFromMethod` |
| anything else | forwarded 1:1 | `MappedField` / `MappedProperty` / `MappedMethod` |

Field-declared feeds map through the `*Field` variants identically.

### 5.2 The `??=` seam — the core enabler

Every feed-derived member's initializer is **null-guarded**, because hot reload re-runs them:

```csharp
// BindableListFromListFeedProperty.cs:36
if (Recipes is null) { var src = (IListFeed<Recipe>)model.Recipes ?? throw ...; Recipes = BindableHelper.CreateBindableList(nameof(Recipes), ctx.GetOrCreateListState(src)); }

// BindableFromFeedProperty.cs:34
Profile ??= new BindableProfile(base.Property<Profile>(nameof(Profile), (IFeed<Profile>)model.Profile ?? throw ...));

// PropertyFromFeedProperty.cs:33
_search ??= new Bindable<string>(base.Property<string>(nameof(Search), (IFeed<string>)model.Search ?? throw ...));
```

**If the member is already non-null when these lines run, `model.<Member>` is never evaluated.** That is the entire mocking mechanism: pre-seed, then let the existing (unmodified) initializers no-op.

Commands are the exception — `CommandFromMethod.GetInitialization()` (line 224) emits a **plain assignment**:

```csharp
Save = new AsyncCommand(nameof(Save), new CommandConfig[] { ... }, Command.DefaultErrorHandler, ctx);
```

This is deliberate: hot reload must re-create commands. So commands need a `??` rather than a `??=` (see §7.3).

### 5.3 `BindableViewModelBase` requirements

- `protected BindableViewModelBase()` (`BindableViewModelBase.cs:46`) calls `InitializeHotReload()`, which registers the instance in a static weak list.
- `protected BindablePropertyInfo<T> Property<T>(string, IFeed<T>)` (line 80) **throws** unless `SourceContext.Find(this)` is already set. A mock ctor must therefore call `SourceContext.Set(this, ctx)` (or `GetOrCreate(this)`) *before* seeding members.
- `RegisterDisposable(IAsyncDisposable)` (line 56); `DisposeAsync` (line 228) disposes them.
- `SourceContext` is itself `IAsyncDisposable` (`SourceContext.cs:22`), so a mock ctor can register the context it created.

### 5.4 Feed state → `FeedView` visual state

`FeedViewVisualStateSelector.GetVisualState` (`src/Uno.Extensions.Reactive.UI/View/FeedViewVisualStateSelector.cs:31`) maps **changed axes** to states:

| Axis | Value | Visual state |
| --- | --- | --- |
| Data | `OptionType` | `Undefined` / `None` / `Some` |
| Error | set | `Error`, else `NoError` |
| Progress | `true` **and** Refresh axis unset (or `RefreshingState is Loading`) | `Indeterminate`, else `NoProgress` |

`FeedView.Subscription.Enumerate` (`FeedView.Subscription.cs:60`) calls `SetIsLoading(false)` only on a **non-transient** message. Consequence: **a stub that emits one transient message and then completes leaves the view in `Indeterminate` / `IsExecuting == true` permanently** — the desired "stuck loading" mock, with no dangling `TaskCompletionSource`.

`ValueFeed<T>` (`src/Uno.Extensions.Reactive/Sources/ValueFeed.cs`) is the existing internal precedent for a one-message-then-complete feed.

### 5.5 How navigation resolves a ViewModel

`MappedRouteResolver.cs:25` makes `RouteInfo.ViewModel` the **bindable** type (`RecipesViewModel`, not `RecipesModel`). `ControlNavigator.CreateViewModel` then does `services.GetService(mapping.ViewModel)` (`ControlNavigator.cs:425`) before falling back to reflection.

Therefore registering `services.AddTransient(_ => RecipesViewModel.CreateMock(...))` in a preview/test host is sufficient to make navigation hand a mocked VM to the page — **provided `CreateMock` returns the real VM type**. This is why G3 is a hard requirement and why a parallel `{Model}MockViewModel` hierarchy was rejected.

---

## 6. Design — Part 1: the runtime mock vocabulary

New folder `src/Uno.Extensions.Reactive/Mocks/`, namespace `Uno.Extensions.Reactive.Mocks`.

### 6.1 Why in the core package, not a new one

- Generated `CreateMock` code must compile unconditionally; a separate package could be absent from the consumer's references.
- `Uno.Extensions.Reactive` is `net9.0` only (`src/tfms-non-ui.props`) and is consumed by every platform head as-is, so no multi-targeting work is needed.
- `Uno.Extensions.Reactive.Testing` is **not** a candidate host: it references `Microsoft.NET.Test.Sdk`, `MSTest.*` and `FluentAssertions`, which must not flow into an app head.
- Cost: ~5 small AOT-safe types in the shipping package. Mitigated by a distinct namespace (the project sets `ImplicitUsings=false`, so nothing leaks into IntelliSense without an explicit `using`).

### 6.2 API surface

```csharp
namespace Uno.Extensions.Reactive.Mocks;

/// <summary>Feeds pinned to a fixed message state, for previews and UI tests.</summary>
public static class MockFeed
{
    public static IFeed<T> Undefined<T>();
    public static IFeed<T> Loading<T>();
    public static IFeed<T> Empty<T>();
    public static IFeed<T> Value<T>(T value);
    public static IFeed<T> Error<T>(Exception error);
    public static IFeed<T> Refreshing<T>(T staleValue);

    /// <summary>A feed pinned to an arbitrary message.</summary>
    public static IFeed<T> Message<T>(Action<MessageBuilder<T>> configure);

    /// <summary>A feed that walks a scripted sequence of states, then completes.</summary>
    public static IFeed<T> Script<T>(params (TimeSpan after, Action<MessageBuilder<T>> step)[] steps);
}

public static class MockListFeed
{
    public static IListFeed<T> Undefined<T>();
    public static IListFeed<T> Loading<T>();
    /// <summary>Emits <c>Option.None</c> — the "no result" state.</summary>
    public static IListFeed<T> Empty<T>();
    /// <summary>Emits <c>Some(empty list)</c> — distinct from <see cref="Empty{T}"/>.</summary>
    public static IListFeed<T> EmptyList<T>();
    public static IListFeed<T> Value<T>(params T[] items);
    public static IListFeed<T> Value<T>(IImmutableList<T> items);
    public static IListFeed<T> Error<T>(Exception error);
    public static IListFeed<T> Refreshing<T>(params T[] staleItems);
}

public static class MockCommand
{
    public static IAsyncCommand Idle();
    public static IAsyncCommand Disabled();
    public static IAsyncCommand Executing();
    public static IAsyncCommand Callback(Action<object?> onExecute, bool canExecute = true);
}

/// <summary>State selector, for data-driving a mock from a picker.</summary>
public enum MockFeedState { Undefined, Loading, Empty, Value, Error, Refreshing }
```

### 6.3 Implementation

All of `Feed.Create<T>(Func<CancellationToken, IAsyncEnumerable<Message<T>>>)` (`Core/Feed.cs:48`), `Message<T>.Initial`, `MessageBuilder<T>` (public readonly struct), `.Data(...)`, `.Error(...)` (`MessageAxisExtensions.cs:198`) and `.IsTransient(...)` (line 219) are public. The whole vocabulary reduces to one private helper:

```csharp
private static IFeed<T> Pin<T>(Action<MessageBuilder<T>> configure)
    => Feed.Create<T>(ct => Emit(configure, ct));

private static async IAsyncEnumerable<Message<T>> Emit<T>(
    Action<MessageBuilder<T>> configure,
    [EnumeratorCancellation] CancellationToken ct)
{
    var builder = Message<T>.Initial.With();
    configure(builder);
    yield return builder;
}
```

State mapping:

| Factory | Builder calls | Resulting FeedView states |
| --- | --- | --- |
| `Undefined<T>()` | `.Data(Option<T>.Undefined())` | `Undefined` |
| `Loading<T>()` | `.IsTransient(true)` | `Indeterminate` (+ `IsExecuting` stays `true`) |
| `Empty<T>()` | `.Data(Option<T>.None())` | `None` |
| `Value<T>(v)` | `.Data(v)` | `Some` |
| `Error<T>(e)` | `.Error(e)` | `Error` |
| `Refreshing<T>(v)` | `.Data(v).IsTransient(true)` | `Some` + `Indeterminate` |

`MockListFeed` delegates to `MockFeed` over `IImmutableList<T>` and adapts with the public `ListFeed.AsListFeed(...)` / `ToListFeed(...)` extensions (`Core/ListFeed.Extensions.cs:206`).

`MockCommand` implements `IAsyncCommand` (`ICommand` + `INotifyPropertyChanged` + `Uno.Toolkit.ILoadable`) directly — a ~40-line internal class. Do **not** try to build a real `AsyncCommand`; it requires a `SourceContext` and a `CommandConfig`.

> ⚠️ `MessageBuilder` only reports axes that actually **changed** relative to `Message<T>.Initial`. `Message<T>.Initial` already has an unset Data axis, so `Undefined<T>()` may produce an empty change set and apply no visual state. Verify this in a test; if confirmed, `Undefined<T>()` must emit two messages (a `Some`/`None` message followed by an `Undefined` one) or set the axis explicitly. **Resolve this before implementing §6.2.**

---

## 7. Design — Part 2: the generator

### 7.1 Opt-in

New assembly-level attribute in `src/Uno.Extensions.Reactive/Config/`, modeled on `ImplicitBindablesAttribute`:

```csharp
namespace Uno.Extensions.Reactive.Config;

/// <summary>Enables generation of mock factories for bindable view models.</summary>
[AttributeUsage(AttributeTargets.Assembly)]
public class GenerateModelMocksAttribute : Attribute
{
    public bool IsEnabled { get; init; } = true;
    /// <summary>FullName patterns a Model must match. Defaults to all generated models.</summary>
    public string[] Patterns { get; } = { ".*" };

    public GenerateModelMocksAttribute() { }
    public GenerateModelMocksAttribute(params string[] patterns) => Patterns = patterns;
}
```

Per-type override reuses the existing `[ReactiveBindable]` pattern if a per-type opt-out proves necessary; not required for v1.

Read it in `BindableGenerationContext` alongside `ImplicitBindablesAttribute`, expose `bool IsMockGenerationEnabled(INamedTypeSymbol type)`.

### 7.2 New `IMappedMember` member

```csharp
internal interface IMappedMember
{
    string Name { get; }
    string? GetBackingField();
    string GetDeclaration();
    string? GetInitialization();

    /// <summary>
    /// The type of the mock override for this member, or null if the member is not mockable.
    /// E.g. 'global::Uno.Extensions.Reactive.IListFeed&lt;Recipe&gt;'.
    /// </summary>
    string? GetMockPropertyType();

    /// <summary>
    /// Code initializing the member from <paramref name="mocks"/>, guarded so it is a no-op
    /// when the override is null. Emitted BEFORE <see cref="GetInitialization"/>.
    /// </summary>
    string? GetMockInitialization(string mocks);
}
```

Default implementations return `null` for `MappedField`, `MappedProperty`, `MappedMethod` (non-mockable — they forward directly to the model and have no state to pin).

Each feed emitter's `GetMockInitialization` is its existing `GetInitialization` with the `model.<Name>` expression swapped for `<mocks>.<Name>` and the null-guard inverted. Example for `BindableListFromListFeedProperty`:

```csharp
public string? GetMockInitialization(string mocks)
    => $@"
        if ({mocks}?.{_property.Name} is {{}} __mock{_property.Name})
        {{
            {_property.Name} = {NS.Bindings}.BindableHelper.CreateBindableList(
                nameof({_property.Name}),
                {N.Ctor.Ctx}.GetOrCreateListState(__mock{_property.Name}));
        }}";
```

Mock property types:

| Emitter | `GetMockPropertyType()` |
| --- | --- |
| `BindableListFromListFeed{Field,Property}` | `IListFeed<T>` |
| `BindableListFromFeedOfList{Field,Property}` | `IFeed<IImmutableList<T>>` |
| `BindableFromFeed{Field,Property}` | `IFeed<TRecord>` |
| `PropertyFromFeed{Field,Property}` | `IFeed<T>` |
| `CommandFromMethod` | `IAsyncCommand` |

### 7.3 Command initializer change

`CommandFromMethod.GetInitialization()` changes from

```csharp
Save = new AsyncCommand(...);
```

to

```csharp
Save = __reactiveMocks?.Save ?? new AsyncCommand(...);
```

`??` short-circuits, so the real `AsyncCommand` is never constructed when mocked. `__reactiveMocks` is a field on the VM, so this also holds in the hot-reload re-initialization path (`__Reactive_BindableInitializeForUpdatedModel`). When mock generation is disabled for the assembly, emit the original plain assignment so output is unchanged (G5).

### 7.4 The mocks bundle

New file per Model: `{Model}.Mocks.g.cs`.

```csharp
[global::System.CodeDom.Compiler.GeneratedCode(...)]
public partial class RecipesViewModelMocks           // : BaseViewModelMocks when the Model has a base Model
{
    public global::Uno.Extensions.Reactive.IListFeed<Recipe>? Recipes { get; set; }
    public global::Uno.Extensions.Reactive.IFeed<Profile>?    Profile { get; set; }
    public global::Uno.Extensions.Reactive.IFeed<string>?     Search  { get; set; }
    public global::Uno.Extensions.Reactive.IAsyncCommand?     Save    { get; set; }
}
```

Declared `partial` so app authors can add convenience helpers. The class mirrors the VM inheritance chain, so a derived VM's mocks bundle is assignable to the base VM's mock constructor.

### 7.5 The model-free constructor and factory

Added to `GenerateViewModel` (`ViewModelGenTool_3.cs:94`), emitted only when mock generation is enabled:

```csharp
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
protected RecipesViewModelMocks? __reactiveMocks;

/// <summary>Creates an instance whose members are pinned to the configured mock states.</summary>
public static RecipesViewModel CreateMock(global::System.Action<RecipesViewModelMocks>? configure = null)
{
    var mocks = new RecipesViewModelMocks();
    configure?.Invoke(mocks);
    return new RecipesViewModel(mocks);
}

protected RecipesViewModel(RecipesViewModelMocks __mocks)
    : base(registerForHotReload: false)                      // see §7.6; ': base(__mocks)' when hasBaseType
{
    __reactiveMocks = __mocks;

    var ctx = global::Uno.Extensions.Reactive.Core.SourceContext.GetOrCreate(this);

    // #if !hasBaseType
    global::Uno.Extensions.Reactive.Core.SourceContext.Set(this, ctx);
    base.RegisterDisposable(ctx);
    __reactiveModel = null;
    // #endif

    // 1. seed from mocks
    <members.Select(m => m.GetMockInitialization("__mocks"))>

    // 2. default every still-unset member to Undefined — never touches a model
    <members.Select(m => m.GetMockDefaultInitialization())>
}
```

Key properties of this shape:

- **No model instance is required.** The `SourceContext` is rooted on the ViewModel rather than the Model. Contrast the normal ctor, which does `SourceContext.GetOrCreate(model)`.
- No `RuntimeHelpers.GetUninitializedObject`, no `new Model(null!, null!)`, no reflection — AOT- and trimming-safe.
- `base.RegisterDisposable(ctx)` ties context lifetime to `DisposeAsync`, matching the non-mock path where the model is registered.
- Unmocked members fall back to `MockFeed.Undefined<T>()` (step 2) rather than reading a model, so there is no NRE surface for a partially-configured mock.
- `Model` returns `null` on a mock instance (`Unsafe.As<T>(null)`). Documented, not guarded — a property getter that throws would break the XAML binding engine.

### 7.6 Hot-reload opt-out

`BindableViewModelBase()` unconditionally calls `InitializeHotReload()` (`BindableViewModelBase.HotReload.cs:84`), which registers the instance in the static `_instances` list. `HotPatch` then selects instances by `bindable.IsInstanceOfType(inst)` (line 52) — a mock VM **is** an instance of the real bindable type, so it would be picked up, and `__Reactive_CreateModelInstance` would attempt to build a real Model from DI/ctor args and assign it to `__reactiveModel`.

Fix — add an additive protected ctor overload to `BindableViewModelBase`:

```csharp
/// <param name="registerForHotReload">
/// False for mock instances, which have no model to reload.
/// </param>
protected BindableViewModelBase(bool registerForHotReload)
{
    _propertyChanged = new(this, h => h.Invoke, isCoalescable: false, schedulersProvider: _dispatcher.FindDispatcher);
    _dispatcher.TryResolve();
    if (registerForHotReload) { InitializeHotReload(); }
}

protected BindableViewModelBase() : this(registerForHotReload: true) { }
```

Purely additive; the existing parameterless ctor keeps its behavior.

---

## 8. Design — Part 3: wiring a mocked page

Three supported entry points, in ascending order of integration:

**Direct instantiation** (unit/UI test, screenshot harness):

```csharp
var page = new RecipesPage { DataContext = RecipesViewModel.CreateMock(m => m.Recipes = MockListFeed.Loading<Recipe>()) };
```

**Navigation host substitution** (preview head, `Given_*` UI test) — works because `RouteInfo.ViewModel` is the bindable type (`MappedRouteResolver.cs:25`) and `CreateViewModel` prefers `services.GetService` (`ControlNavigator.cs:425`):

```csharp
.UseNavigation(ReactiveViewModelMappings.ViewModelMappings, RegisterRoutes)
.ConfigureServices(s => s.AddTransient(_ => RecipesViewModel.CreateMock(m =>
{
    m.Recipes = MockListFeed.Error<Recipe>(new HttpRequestException());
})));
```

**State gallery** — because the bundle is plain settable properties, a picker can rebuild the VM per selection:

```csharp
void Apply(MockFeedState state) => Root.DataContext = RecipesViewModel.CreateMock(m =>
    m.Recipes = state switch
    {
        MockFeedState.Loading    => MockListFeed.Loading<Recipe>(),
        MockFeedState.Empty      => MockListFeed.Empty<Recipe>(),
        MockFeedState.Error      => MockListFeed.Error<Recipe>(new TimeoutException()),
        MockFeedState.Refreshing => MockListFeed.Refreshing(SampleData.Recipes),
        _                        => MockListFeed.Value(SampleData.Recipes),
    });
```

---

## 9. Files touched

| File | Change |
| --- | --- |
| `src/Uno.Extensions.Reactive/Mocks/MockFeed.cs` | **new** |
| `src/Uno.Extensions.Reactive/Mocks/MockListFeed.cs` | **new** |
| `src/Uno.Extensions.Reactive/Mocks/MockCommand.cs` | **new** (+ internal `MockAsyncCommand`) |
| `src/Uno.Extensions.Reactive/Mocks/MockFeedState.cs` | **new** |
| `src/Uno.Extensions.Reactive/Config/GenerateModelMocksAttribute.cs` | **new** |
| `src/Uno.Extensions.Reactive/Presentation/Bindings/BindableViewModelBase.cs` | add `protected BindableViewModelBase(bool)` |
| `src/Uno.Extensions.Reactive.Generator/Bindables/IMappedMember.cs` | add `GetMockPropertyType`, `GetMockInitialization` |
| `.../MappedMembers/BindableListFromListFeed{Field,Property}.cs` | implement the two new members |
| `.../MappedMembers/BindableListFromFeedOfList{Field,Property}.cs` | idem |
| `.../MappedMembers/BindableFromFeed{Field,Property}.cs` | idem |
| `.../MappedMembers/PropertyFromFeed{Field,Property}.cs` | idem |
| `.../MappedMembers/CommandFromMethod.cs` | idem + `??` in `GetInitialization` |
| `.../MappedMembers/{MappedField,MappedProperty,MappedMethod}.cs` | return `null` from both |
| `.../Bindables/BindableGenerationContext.cs` | read `GenerateModelMocksAttribute` |
| `.../Bindables/ViewModelGenTool_3.cs` | emit `{Model}.Mocks.g.cs`, mock ctor, `CreateMock` |
| `.../Rules.Feeds.cs` | new diagnostic (see §10.5) |
| `src/Uno.Extensions.Reactive.Tests/Mocks/Given_MockFeed.cs` etc. | **new** |
| `src/Uno.Extensions.Reactive.UI.Tests/Generator/Given_ModelMocks.cs` | **new** |
| `doc/Learn/Mvux/Testing.md` + `doc/Learn/Mvux/toc.yml` | **new** page |

Note the generator is `netstandard2.0` and **must stay so** — do not use C# features unavailable to the analyzer host.

---

## 10. Edge cases and constraints

### 10.1 Scalar feeds are invisible to `FeedView`
`GetMembers` only produces an `IFeed`-shaped VM member when the value type is a record with an accessible default ctor (`ViewModelGenTool_3.cs:311`); `IFeed<string>` becomes a plain `string` property. A `FeedView` bound to it silently renders nothing, because `FeedView.SourceProperty` is `typeof(object)` and `OnSourceChanged` does `args.NewValue as ISignal<IMessage>` (`FeedView.cs:17-21`). Mocking does not change this — call it out in the docs so users do not mistake it for a mock bug.

### 10.2 `Message<T>.Initial` change detection
See the warning in §6.3. Must be resolved by a test before the vocabulary is considered done.

### 10.3 The `Refresh` axis is internal
`RefreshAxis` (`Core/Axes/RefreshAxis.cs:10`) and `RefreshToken` are `internal`, so a true refresh state cannot be scripted from user code. Not a problem: with the Refresh axis unset, `FeedViewVisualStateSelector` takes the `"Indeterminate"` branch, which is the intended visual. `MockFeed.Refreshing` is therefore *visually* faithful but not axis-faithful. Document it.

### 10.4 Hot reload
Covered by §7.6. A UI test must assert that editing a Model source file does not clobber a live mocked VM.

### 10.5 Diagnostic for a mock on a non-generated model
If `CreateMock` is requested for a type the generator does not otherwise process, emit a diagnostic rather than silently skipping. Add to `Rules.Feeds.cs` following the existing `FEED2001`/`FEED2002` shape.

### 10.6 Nullability
`src/Directory.Build.props` sets `Nullable=enable` and `TreatWarningsAsErrors=true`. The mocks bundle properties are nullable by design; the seeding code uses `is {} x` patterns rather than `!`, per AGENTS.md §1.

### 10.7 AOT / trimming
`Uno.Extensions.Reactive` sets `IsAotCompatible=true`. Nothing in this design uses reflection. Keep it that way — do not add a reflection-based fallback for constructing models.

---

## 11. Test plan

Per `AGENTS.md` → *Minimum Test Additions Per PR* (source-generator change + new public API).

### Unit — `src/Uno.Extensions.Reactive.Tests/Mocks/`
`Given_MockFeed`, `Given_MockListFeed`, `Given_MockCommand`. Use the existing `FeedTests` base + `FeedRecorder` from `Uno.Extensions.Reactive.Testing`.

- `When_Undefined_Then_DataAxisIsUndefined` — **write this one first**; it resolves §10.2.
- `When_Loading_Then_MessageIsTransient_And_FeedCompletes`
- `When_Empty_Then_DataIsNone`
- `When_Value_Then_DataIsSome`
- `When_Error_Then_ErrorAxisIsSet_And_DataUnset`
- `When_Refreshing_Then_DataIsSome_And_IsTransient`
- `When_Script_Then_MessagesEmittedInOrder`
- `When_Subscribed_Twice_Then_SameStateReplayed`
- `MockListFeed.Empty` vs `EmptyList` produce `None` vs `Some([])`
- `MockCommand.Disabled_Then_CanExecuteIsFalse`; `Executing_Then_IsExecutingIsTrue`

### Generator — `src/Uno.Extensions.Reactive.UI.Tests/Generator/Given_ModelMocks.cs`
Follow `Given_BasicViewModel_Then_Generate` / `Given_VMWithCommands`.

- Fixture model with one of each mapped member kind.
- `When_MockGenerationDisabled_Then_OutputUnchanged` — byte-compare against the current generated output (**G5 guard**).
- `When_CreateMock_Then_ReturnsRealViewModelType`
- `When_CreateMock_Then_ModelIsNotConstructed` — fixture model with a ctor that throws.
- `When_MemberNotMocked_Then_DefaultsToUndefined`
- `When_CommandMocked_Then_RealAsyncCommandNotConstructed`
- Inheritance: derived model + base model, mocks bundle assignable through the chain.
- **Clean-rebuild check** — Roslyn caches generator output; a stale cache masks regressions (AGENTS.md §11).

### UI — `src/Uno.Extensions.Reactive.UI.Tests/Given_FeedView_Mocks.cs`
`[RunsOnUIThread]`, using `UnitTestsUIContentHelper.CurrentTestWindow` — **never `new Window()`** (AGENTS.md, *Windows in UI / runtime tests*).

- Each `MockFeedState` drives the expected visual state (`Undefined`/`None`/`Some`/`Error`/`Indeterminate`).
- `When_Loading_Then_FeedViewIsExecutingStaysTrue`
- `When_MockedVm_Then_HotReloadDoesNotReplaceMocks` (covers §10.4)
- `When_RegisteredInDi_Then_NavigationResolvesMockedVm`

### Verification
- `dotnet build Uno.Extensions-packageonly.slnf -c Release` — zero warnings.
- `dotnet test Uno.Extensions-packageonly.slnf -c Release --filter "FullyQualifiedName!~UI.Tests"`
- Runtime tests via the Skia head for the UI-level cases.

---

## 12. Documentation

- New page `doc/Learn/Mvux/Testing.md` — "Previewing and testing MVUX states", registered in the Mvux TOC.
- Cross-link from `doc/Learn/Mvux/FeedView.md`, including the §10.1 scalar-feed caveat.
- XML docs on every new public member (`GenerateDocumentationFile=true`).

---

## 13. Phased task breakdown

Phases 1 and 2 are independently shippable. **Phase 1 delivers real value on its own** — it works today against hand-written mock Models with zero generator work — and is the recommended starting point.

### Phase 1 — runtime vocabulary
- [ ] Resolve §10.2 with a failing-first test for `Undefined<T>()`
- [ ] `MockFeed`, `MockListFeed`, `MockCommand`, `MockFeedState`
- [ ] Unit tests (§11)
- [ ] `doc/Learn/Mvux/Testing.md` — vocabulary section

### Phase 2 — generator
- [ ] `GenerateModelMocksAttribute` + `BindableGenerationContext` wiring
- [ ] `BindableViewModelBase(bool registerForHotReload)`
- [ ] `IMappedMember.GetMockPropertyType` / `GetMockInitialization` + all 11 implementations
- [ ] `CommandFromMethod` `??` initializer (gated on opt-in)
- [ ] `{Model}.Mocks.g.cs` emission
- [ ] Mock ctor + `CreateMock` in `GenerateViewModel`
- [ ] Diagnostic (§10.5)
- [ ] Generator tests incl. the disabled-output byte-compare and clean-rebuild check
- [ ] UI tests (§11)
- [ ] Docs — generated-mocks section

### Phase 3 — samples (optional)
- [ ] A state-gallery page in `samples/Playground` exercising every `MockFeedState`

---

## 14. Open questions

1. **§10.2** — does `Message<T>.Initial.With().Data(Option<T>.Undefined())` register a change? Blocks the shape of `Undefined<T>()`. *Answer with a test, not by reading code.*
2. Should `MockCommand` record invocations for assertion? Deferred (NG4) — but decide before the API ships, since adding a recording surface later is additive while changing `IAsyncCommand` identity is not.
3. Should `CreateMock` have an overload taking a real Model, for partial mocks over a live model? Cheap to add (the normal ctor already handles it — pass mocks *and* a model, seeding wins). Not required for the stated use case; add only if a consumer asks.
4. Is `Patterns` on `GenerateModelMocksAttribute` worth having in v1, or is a plain `IsEnabled` bool sufficient?
