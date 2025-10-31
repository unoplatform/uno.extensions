---
uid: Uno.Extensions.Mvux.SimpleState.HowTo
---
# How to asynchronously load, display, and manipulate data from and to a service with MVUX

**Goal:** load a single value from an async service into an MVUX `IState<T>` and show it in the UI.

**Dependencies**

* `Uno.Extensions`
* `Uno.Extensions.Reactive`
* Your project created with MVUX template (any recent Uno.Extensions MVUX app is fine).

**Model**

```csharp
// HallCrowdedness.cs
namespace TheFancyWeddingHall;

public partial record HallCrowdedness(int NumberOfPeopleInHall);

public interface IHallCrowdednessService
{
    ValueTask<HallCrowdedness> GetHallCrowdednessAsync(CancellationToken ct);
}

public class HallCrowdednessService : IHallCrowdednessService
{
    private int _numberOfPeopleInHall = 5;

    public async ValueTask<HallCrowdedness> GetHallCrowdednessAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), ct); // simulate server
        return new HallCrowdedness(_numberOfPeopleInHall);
    }
}
```

```csharp
// HallCrowdednessModel.cs
using Uno.Extensions.Reactive;

namespace TheFancyWeddingHall;

public partial record HallCrowdednessModel(IHallCrowdednessService Service)
{
    // owner = "this"
    public IState<HallCrowdedness> HallCrowdedness =>
        State.Async(this, Service.GetHallCrowdednessAsync);
}
```

**View**

```xml
<!-- MainPage.xaml -->
<Page
    x:Class="TheFancyWeddingHall.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <StackPanel Spacing="12">
        <TextBlock Text="How many people are currently in the hall?" />
        <TextBlock
            DataContext="{Binding HallCrowdedness}"
            Text="{Binding NumberOfPeopleInHall}" />
    </StackPanel>
</Page>
```

**Wire-up in code-behind**

```csharp
// MainPage.xaml.cs
using Microsoft.UI.Xaml.Controls;

namespace TheFancyWeddingHall;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        this.DataContext =
            new HallCrowdednessViewModel(new HallCrowdednessService());
    }
}
```

**Why this works**

* `State.Async(...)` keeps the latest value.
* MVUX generates `HallCrowdednessViewModel` from the model, so XAML can bind without hand-made `INotifyPropertyChanged`.

---

## 2. Let user edit the hall people count

**Goal:** let the user change the number directly in a `TextBox`, using two-way binding to the MVUX state.

**Important idea:** the state value is **immutable** (`record`), so MVUX recreates the value when a property changes.

**XAML**

```xml
<StackPanel Spacing="12">
    <TextBlock Text="How many people are currently in the hall?" />

    <TextBox
        DataContext="{Binding HallCrowdedness}"
        Text="{Binding NumberOfPeopleInHall,
                       Mode=TwoWay,
                       UpdateSourceTrigger=PropertyChanged}" />
</StackPanel>
```

**What happens**

* User types “15”.
* MVUX adapter turns that into a new `HallCrowdedness(15)`.
* The state now holds the updated value.
* You didn’t write any “on text changed” code.

---

## 3. Save the hall people count to the service

**Goal:** add a button that runs an MVUX-generated command, which in turn calls your method in the model to persist the current state.

**Extend the service**

```csharp
public interface IHallCrowdednessService
{
    ValueTask<HallCrowdedness> GetHallCrowdednessAsync(CancellationToken ct);
    ValueTask SetHallCrowdednessAsync(HallCrowdedness crowdedness, CancellationToken ct);
}

public class HallCrowdednessService : IHallCrowdednessService
{
    private int _numberOfPeopleInHall = 5;

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

**Extend the model**

```csharp
using Uno.Extensions.Reactive;

namespace TheFancyWeddingHall;

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

**XAML with button**

```xml
<StackPanel Spacing="12">
    <TextBlock Text="How many people are currently in the hall?" />

    <TextBox
        DataContext="{Binding HallCrowdedness}"
        Text="{Binding NumberOfPeopleInHall,
                       Mode=TwoWay,
                       UpdateSourceTrigger=PropertyChanged}" />

    <!-- MVUX generates Command "Save" from method Save(...) -->
    <Button Content="Save"
            Command="{Binding Save}" />
</StackPanel>
```

**What MVUX generates**

* From `HallCrowdednessModel` it generates `HallCrowdednessViewModel`.
* From method `Save(...)` it generates a `Save` command.
* Button binds to that command → MVUX calls your method → your method calls the service.

---

## 4. Read the current state value in C# code

**Goal:** access the current number from code (e.g. to log, to validate, to compose another call).

**Model-side read**

```csharp
public async ValueTask LogCurrent(CancellationToken ct)
{
    var current = await HallCrowdedness; // awaitable!
    if (current is not null)
    {
        Console.WriteLine($"Current number in hall: {current.NumberOfPeopleInHall}");
    }
}
```

**Notes**

* `IState<T>` and `IFeed<T>` produced by MVUX are awaitable.
* Use this when you need the **actual value**, not just binding.

---

## 5. Show state status in the view (optional FeedView-style pattern)

**Goal:** show “loading”, then the result, without manual busy flags.

This page mentions using `FeedView`, so here is the small outcome version.

**XAML**

```xml
<Page
    x:Class="TheFancyWeddingHall.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:rxui="using:Uno.Extensions.Reactive.UI">

    <rxui:FeedView Source="{Binding HallCrowdedness}">
        <rxui:FeedView.LoadingTemplate>
            <TextBlock Text="Loading hall data..." />
        </rxui:FeedView.LoadingTemplate>

        <rxui:FeedView.ErrorTemplate>
            <TextBlock Text="Could not load hall data." />
        </rxui:FeedView.ErrorTemplate>

        <rxui:FeedView.ContentTemplate>
            <DataTemplate>
                <StackPanel Spacing="12">
                    <TextBlock Text="How many people are currently in the hall?" />
                    <TextBox
                        Text="{Binding NumberOfPeopleInHall,
                                       Mode=TwoWay,
                                       UpdateSourceTrigger=PropertyChanged}" />
                    <Button Content="Save" Command="{Binding DataContext.Save, RelativeSource={RelativeSource Mode=TemplatedParent}}" />
                </StackPanel>
            </DataTemplate>
        </rxui:FeedView.ContentTemplate>
    </rxui:FeedView>
</Page>
```

**What this gives you**

* MVUX drives loading/error/success.
* You stay declarative.
* Still binds to the state’s value.
