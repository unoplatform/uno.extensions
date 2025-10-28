---
uid: Uno.Extensions.Mvux.Overview
---

# MVUX Overview

Model-View-Update-eXtended (**MVUX**) applies unidirectional data flow to XAML apps while keeping data binding. Use it when immutable state, async data, and generated view models are preferred over hand-written `INotifyPropertyChanged`.

## Quick Facts
- **Problem**: MVVM view models become noisy when tracking loading, errors, and refresh logic.
- **Solution**: MVUX models expose immutable state via `IFeed` (output) and `IState` (input); source generators emit bindable view models.
- **When to choose**: UI needs declarative async pipelines, clear loading/error signaling, or reactive refresh triggered by app logic.
- **Key benefit**: View stays bound to generated properties while model updates stay pure and deterministic.

## Core Building Blocks
- **Model** (`partial` class or record with suffix `Model`): defines data projection through feeds/states; treated as the single source of truth.
- **ViewModel** (generated): bridges data binding; each public `IFeed`/`IState` becomes a bindable property or command.
- **Update logic**: triggered by feed refresh, command invocation, or state mutations; always returns new immutable data.
- **Feed lifecycle**: `Loading → Data → Error/NoData`; exposed through helpers like `FeedView`.

## Weather Sample at a Glance
- `WeatherModel` publishes `CurrentWeather : IFeed<WeatherInfo>` that queries a weather service.
- `Feed.Async` combines cancellation support and status metadata (loading, data, error).
- `FeedView` binds to the generated view model and chooses a template based on feed status (loading spinner, data view, error message).
- User actions (`Refresh` command) and `IState<string> City` updates schedule new feed executions without manual dispatcher work.

```csharp
public partial class WeatherModel
{
    readonly IWeatherService WeatherService;

    public IFeed<WeatherInfo> CurrentWeather =>
        Feed.Async(ct => WeatherService.GetCurrentWeather(ct));

    public IState<string> City => State<string>.Empty(this);
}
```

## MVVM vs MVUX
- MVVM view models mutate observable properties; MVUX models produce immutable snapshots.
- Commands in MVVM often manipulate multiple boolean flags; MVUX feeds surface status automatically.
- MVUX generated view models keep data binding, reducing hand-written `INotifyPropertyChanged` implementations.

## Typical Workflow
- **Model**: declare feeds/states, prefer records for immutability, keep side effects inside feed factories.
- **View**: bind to generated properties, use `FeedView` templates for loading/error/data, wire states with two-way bindings when input is required.
- **Services**: implement async-friendly APIs; feeds expect cancellation tokens and exception propagation for error reporting.

## Implementation Checklist
- Add the MVUX packages and analyzers to your project.
- Create at least one `partial` `*Model` with public `IFeed`/`IState` members.
- Run a build to generate the companion view model.
- Bind XAML to generated properties; optionally customize templates via `FeedView`.
- Use `Select`, `SelectAsync`, or `WithError` extensions to compose feeds and combine states.

## Resources
- [How to set up an MVUX project](xref:Uno.Extensions.Mvux.HowToMvuxProject)
- [Feeds documentation](xref:Uno.Extensions.Mvux.Feeds)
- [States documentation](xref:Uno.Extensions.Mvux.States)
- Weather app sample: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/WeatherApp
