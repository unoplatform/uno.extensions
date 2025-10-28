---
uid: Uno.Extensions.Mvux.HowToSimpleState
---

# MVUX Simple State

Short guide for modeling mutable app data with `IState<T>` and exposing commands from an MVUX model.

## TL;DR
- States keep MVUX data mutable (via snapshot replacement) while feeds remain read-only.
- Define a service that reads/writes your entity; expose it through `State.Async` on the model.
- MVUX generates bindable properties and async commands (method names) on the companion view model.

## 1. Service Contract
```csharp
namespace TheFancyWeddingHall;

public partial record HallCrowdedness(int NumberOfPeopleInHall);

public interface IHallCrowdednessService
{
    ValueTask<HallCrowdedness> GetHallCrowdednessAsync(CancellationToken ct);
    ValueTask SetHallCrowdednessAsync(HallCrowdedness crowdedness, CancellationToken ct);
}

public class HallCrowdednessService : IHallCrowdednessService
{
    int _numberOfPeopleInHall = 5;

    public async ValueTask<HallCrowdedness> GetHallCrowdednessAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
        return new HallCrowdedness(_numberOfPeopleInHall);
    }

    public async ValueTask SetHallCrowdednessAsync(HallCrowdedness crowdedness, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
        _numberOfPeopleInHall = crowdedness.NumberOfPeopleInHall;
    }
}
```
- `HallCrowdedness` is a record so updates create new instances.

## 2. Model with State + Command
```csharp
public partial record HallCrowdednessModel(IHallCrowdednessService Service)
{
    public IState<HallCrowdedness> HallCrowdedness =>
        State.Async(this, Service.GetHallCrowdednessAsync);

    public async ValueTask Save(CancellationToken ct)
    {
        var current = await HallCrowdedness;
        if (current is null)
        {
            return;
        }

        await Service.SetHallCrowdednessAsync(current, ct);
    }
}
```
- `State.Async(owner, loader)` ties the state lifetime to the model.
- Public async methods become MVUX commands (e.g., `Save` → `IAsyncCommand Save` on the view model).

## 3. Bind the View
```xml
<Page ...>
    <StackPanel>
        <TextBlock Text="How many people are currently in the hall?" />
        <TextBox DataContext="{Binding HallCrowdedness}"
                 Text="{Binding NumberOfPeopleInHall, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
        <Button Content="Save" Command="{Binding Save}" />
    </StackPanel>
</Page>
```
```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        DataContext = new HallCrowdednessViewModel(new HallCrowdednessService());
    }
}
```
- Binding to `HallCrowdedness` uses MVUX adapters to clone the record when `NumberOfPeopleInHall` changes.
- The generated `Save` command propagates to the model’s `Save` method.

## Tips
- Both `IState<T>` and `IListState<T>` are awaitable; use `await HallCrowdedness` inside the model to access the latest snapshot.
- Commands receive a `CancellationToken` automatically; honor it in long-running operations.

## Related Material
- Project setup: (xref:Uno.Extensions.Mvux.HowToMvuxProject)
- State fundamentals: (xref:Uno.Extensions.Mvux.States)
- List states: (xref:Uno.Extensions.Mvux.ListStates)
- Sample app: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/WeddingHallApp
