---
uid: Uno.Extensions.Mvux.Testing
---

# Previewing and testing MVUX states

The `Uno.Extensions.Reactive.Mocks` namespace provides factories that produce an `IFeed<T>`, `IListFeed<T>`, or `IAsyncCommand` pinned to a chosen state (`Loading`, `Undefined`, `Empty`, `Value`, `Error`, `Refreshing`). Use them to put a page into a specific visual state — including states that cannot be reached by faking a service, such as an indefinite loading state or stale data with a refresh in progress — for screenshot harnesses, state galleries, and UI tests.

Unlike a feed backed by a never-completing task, a mocked feed emits its configured state once and then **completes**, so it pins the view in that state without leaking pending work or hanging test runs.

## Mocking feeds

```csharp
using Uno.Extensions.Reactive.Mocks;

public partial record MainModel
{
    public IFeed<WeatherInfo> CurrentWeather => MockFeed.Loading<WeatherInfo>();
}
```

The available states:

| Factory | Resulting state |
| --- | --- |
| `MockFeed.Undefined<T>()` | No data emitted yet (`Undefined` visual state) |
| `MockFeed.Loading<T>()` | Loading, indefinitely (`Indeterminate` progress) |
| `MockFeed.Empty<T>()` | No result (`None`) |
| `MockFeed.Value<T>(value)` | Data present (`Some`) |
| `MockFeed.Error<T>(exception)` | Error |
| `MockFeed.Refreshing<T>(staleValue)` | Stale data present with an indeterminate progress |
| `MockFeed.Message<T>(configure)` | Any single message, configured through a `MessageBuilder<T>` |
| `MockFeed.Script<T>(steps)` | A scripted sequence of states, each applied after a delay |

For example, a scripted feed that loads for two seconds, shows a value, then fails:

```csharp
var feed = MockFeed.Script<int>(
    (TimeSpan.Zero, m => m.IsTransient(true)),                      // loading…
    (TimeSpan.FromSeconds(2), m => m.Data(42).IsTransient(false)),  // …value…
    (TimeSpan.FromSeconds(2), m => m.Error(new TimeoutException())));  // …error
```

> [!NOTE]
> `MockFeed.Refreshing` pins the *message* state faithfully — data present with an indeterminate progress — but does not set the internal refresh axis, which cannot be produced outside of a refreshable source feed. Consequently the `FeedView`'s **default template** shows its progress overlay (hiding the data presenters) exactly as it does for `Loading`; the stale data remains available to custom templates through `FeedViewState.Data`.

> [!NOTE]
> `MockFeed.Undefined` walks through `None` before pinning `Undefined`, so the data change is observable by state selectors. A subscriber that attaches *after* the messages were processed (e.g. a `FeedView` bound to an already-created mocked view model) may not observe an explicit transition — which is still correct, because a `FeedView`'s initial presentation already is its `UndefinedTemplate`.

> [!IMPORTANT]
> A `FeedView` can only bind to a feed-shaped member. When a Model exposes an `IFeed<T>` of a scalar type (e.g. `IFeed<string>`), the generated bindable view model exposes it as a plain property, not a feed — a `FeedView` bound to it silently renders nothing. This is a property of the MVUX generator, not of the mocks.

## Mocking list feeds

`MockListFeed` mirrors `MockFeed` for `IListFeed<T>`:

```csharp
public IListFeed<Recipe> Recipes => MockListFeed.Value(SampleData.Recipe1, SampleData.Recipe2);
```

Two states deserve a note:

- `MockListFeed.Empty<T>()` produces `None` — the "no result" state, the way real list feeds report an empty result.
- `MockListFeed.EmptyList<T>()` produces a *present but empty* list (`Some([])`). Real list feeds normalize an empty list to `None`, so this state is only producible by a mock; use it to validate how a view renders an empty collection when the data axis is set.

Consistently with real list feeds, `MockListFeed.Value(...)` with no items produces `None`.

## Mocking commands

`MockCommand` pins an `IAsyncCommand` to a state:

```csharp
public IAsyncCommand Save => MockCommand.Disabled();
```

| Factory | Resulting state |
| --- | --- |
| `MockCommand.Idle()` | Enabled, does nothing when invoked |
| `MockCommand.Disabled()` | `CanExecute` is `false` |
| `MockCommand.Executing()` | `IsExecuting` is `true`, indefinitely |
| `MockCommand.Callback(onExecute)` | Invokes the given callback when executed |

> [!NOTE]
> Like the real `AsyncCommand`, a programmatic `Execute` on a non-executable mock is a no-op. The `Callback` delegate must be synchronous — exceptions propagate raw to the invoker, and an `async` lambda would be an unobservable async-void.

## Generated mock factories

Opting in with the assembly-level `GenerateModelMocks` attribute makes the MVUX generator emit, for each generated bindable view model, a `{Vm}Mocks` bundle and a `CreateMock` factory:

```csharp
[assembly: Uno.Extensions.Reactive.Config.GenerateModelMocks]
```

`CreateMock` produces an instance of the **real** generated view model type — so `x:Bind`, `d:DataType`, typed C# markup, and navigation view-model resolution keep working — without constructing the Model, its services, or its dependency graph:

```csharp
var vm = RecipesViewModel.CreateMock(m =>
{
    m.Recipes = MockListFeed.Loading<Recipe>();
    m.Profile = MockFeed.Error<Profile>(new TimeoutException());
    m.Save    = MockCommand.Disabled();
});

var page = new RecipesPage { DataContext = vm };
```

Key behaviors:

- **No model, no services.** The Model is never constructed; `vm.Model` is `null` on a mocked instance.
- **Safe defaults.** Feed members left unconfigured are pinned in the `Undefined` state; commands default to an enabled no-op.
- **Plain members are unavailable.** Non-feed members (plain properties, methods) forward to the Model and therefore throw on a mocked instance. Only bind feed, state, and command members of a mocked view model — a `{x:Bind}` to a plain model property will fail at runtime.
- **Mocked states are read-only.** Assigning an `IFeed<T>` to a state member pins its display value but drops two-way binding writes silently. To keep the member editable, assign a real state instead: `m.Search = State.Value(this, () => "initial")`.
- **Hot reload is bypassed.** A mocked instance is not registered for hot reload, so editing the Model source does not clobber a live mocked view model (and, conversely, edits are not reflected while previewing with mocks).
- **Opt-in only.** Without the attribute, the generated output is unchanged. The attribute optionally takes regex patterns (matched unanchored against the model's full name) restricting which models get mock factories: `[assembly: GenerateModelMocks("MainModel$", "Details.*")]`. Consider gating the attribute with `#if DEBUG` if mocks should not ship in release builds.
- **Inheritance.** A derived model's mocks bundle derives from its base model's bundle, so base members are configurable from the derived `CreateMock`. Every model in the base chain must be mock-enabled, otherwise the generator reports `FEED3001` and skips. Beware the inverse exclusion: if a pattern covers a base model but not a derived one, `DerivedViewModel.CreateMock(...)` still compiles — C# resolves it to the inherited base factory, which returns a *base* view-model instance.
- **Dispose replaced mocks.** A mocked view model owns feed subscriptions; when re-creating mocks in a loop (e.g. a state-gallery picker), dispose the previous instance (`await previousVm.DisposeAsync()`) before assigning the new one — especially on WebAssembly, where peak allocations permanently grow the heap.

To make navigation resolve a mocked view model (e.g. in a preview head or a UI test host), register it in DI — navigation prefers services over reflection when creating view models:

```csharp
.ConfigureServices(s => s.AddTransient(_ => RecipesViewModel.CreateMock(m =>
{
    m.Recipes = MockListFeed.Error<Recipe>(new HttpRequestException());
})));
```

## Driving a state gallery

`MockFeedState` enumerates the states so a picker can data-drive a mock:

```csharp
void Apply(MockFeedState state) => Root.DataContext = RecipesViewModel.CreateMock(m =>
    m.Recipes = state switch
    {
        MockFeedState.Undefined  => MockListFeed.Undefined<Recipe>(),
        MockFeedState.Loading    => MockListFeed.Loading<Recipe>(),
        MockFeedState.Empty      => MockListFeed.Empty<Recipe>(),
        MockFeedState.Error      => MockListFeed.Error<Recipe>(new TimeoutException()),
        MockFeedState.Refreshing => MockListFeed.Refreshing(SampleData.Recipes),
        _                        => MockListFeed.Value(SampleData.Recipes),
    });
```

> [!IMPORTANT]
> When a gallery swaps states on a **reused** `FeedView`, visual-state residue can linger: a pinned loading state leaves the progress group in `Indeterminate`, and a subsequent source whose messages never touch the progress axis will not transition out of it. Re-inflate the view per selection instead — e.g. host it in a `ContentControl` and re-assign its `ContentTemplate` together with the new `Content`. And dispose the replaced view model, as noted above.

A complete, runnable gallery — every state with its code side by side, both for raw mock feeds on a `FeedView` and for `CreateMock` on a generated view model — is available in the repository's Playground sample: `samples/Playground/Playground/Views/MvuxMocksPage.xaml` (route `MvuxMocks`, reachable from the Home page).
