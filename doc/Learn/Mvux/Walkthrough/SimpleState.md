---
uid: Uno.Extensions.Mvux.HowToSimpleState
---

# MVUX Simple State

Capture and persist mutable data with `IState<T>` while exposing async commands from the model.

## Read and write hall occupancy

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

Records keep the DTO immutable so every change produces a fresh instance.

## Expose state and a save command

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

`State.Async` ties the state lifecycle to the model owner. Public async methods become generated commands.

## Bind the hall input to the state

```xml
<Page ...>
    <StackPanel>
        <TextBlock Text="How many people are currently in the hall?" />
        <TextBox DataContext="{Binding HallCrowdedness}"
                 Text="{Binding NumberOfPeopleInHall, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
    </StackPanel>
</Page>
```

MVUX adapters clone the record when `NumberOfPeopleInHall` changes so updates remain immutable.

## Trigger the generated save command

```xml
<Button Content="Save" Command="{Binding Save}" />
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

The view model exposes `HallCrowdedness` for binding and an async `Save` command that forwards to the model.

## Resources

- MVUX state basics: (xref:Uno.Extensions.Mvux.States)
- List state guide: (xref:Uno.Extensions.Mvux.ListStates)
- Sample app: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/WeddingHallApp
