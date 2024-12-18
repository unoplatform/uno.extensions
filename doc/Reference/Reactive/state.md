---
uid: Uno.Extensions.Reactive.State
---
# State

Like [feeds](xref:Uno.Extensions.Mvux.Feeds), states are used to manage asynchronous operations and wrap them in metadata that provides information about the current state of the operation, such as whether the operation is still in progress, when an error occurs, or if the result has no data.

However, unlike feeds, states are stateful, meaning they keep a record of the current data value. States also allow the current value to be modified, which is useful for two-way binding scenarios.

There are some noticeable differences with a _feed_:

* When subscribing to a state, the currently loaded value is going to be replayed.
* There is a [`Update`](#update) method that allows you to change the current value.
* _States_ are attached to an owner and share the same lifetime as that owner.
* The main usage of _state_ is for two-way bindings.

## States are attached to their owner

Besides holding the state information, a reference to the Model is shared with the states so that when the View is closed and disposed of, it tunnels down to the states and the Models and makes them available for garbage collection. States share the same lifetime as their owner.

## Sources: How to maintain a data

You can create a _state_ using one of the following:

### Empty

Creates a state without any initial value.

```csharp
public static IState<T> Empty<T>(object owner);
```

For example:

```csharp
public IState<string> City => State<string>.Empty(this);
```

### Value

Creates a state with a synchronous initial value.

```csharp
public static IState<T> Value<T>(object owner, Func<T> valueProvider);
```

For example:

```csharp
public IState<string> City => State.Value(this, () => "Montréal");
```

### Async

Creates a state with an asynchronous initial value.

```csharp
public static IState<T> Async<T>(object owner, Func<CancellationToken, Task<T>> asyncFunc, Signal? refreshSignal = null);
```

For example:

```csharp
public IState<string> City => State.Async(this, async ct => await _locationService.GetCurrentCity(ct));
```

### AsyncEnumerable

Like for `Feed.AsyncEnumerable`, this allows you to adapt an `IAsyncEnumerable<T>` into a _state_.

```csharp
public static IState<T> AsyncEnumerable<T>(object owner, Func<CancellationToken, IAsyncEnumerable<T>> asyncEnumerableFunc);
```

For example:

```csharp
public IState<string> City => State.AsyncEnumerable(this, () => GetCurrentCity());

public async IAsyncEnumerable<string> GetCurrentCity([EnumeratorCancellation] CancellationToken ct = default)
{
    while (!ct.IsCancellationRequested)
    {
        yield return await _locationService.GetCurrentCity(ct);
        await Task.Delay(TimeSpan.FromMinutes(15), ct);
    }
}
```

### Create

This gives you the ability to create your own _state_ by dealing directly with _messages_.

> This is designed for advanced usage and should probably not be used directly in apps.

```csharp
public static IState<T> Create<T>(object owner, Func<CancellationToken, IAsyncEnumerable<Message<T>>> messageFunc);
```

For example:

```csharp
public IState<string> City => State.Create(this, GetCurrentCity);

public async IAsyncEnumerable<Message<string>> GetCurrentCity([EnumeratorCancellation] CancellationToken ct = default)
{
    var message = Message<string>.Initial;
    var city = Option<string>.Undefined();
    var error = default(Exception);
    while (!ct.IsCancellationRequested)
    {
        try
        {
            city = await _locationService.GetCurrentCity(ct);
            error = default;
        }
        catch (Exception ex)
        {
            error = ex;
        }

        yield return message = message.With().Data(city).Error(error);
        await Task.Delay(TimeSpan.FromHours(1), ct);
    }
}
```

### From a feed

A state can easily be converted from a feed as follows:

```csharp
public static IState<T> FromFeed<T>(object owner, IFeed<T> feed);
```

For example:

```csharp
public IFeed<int> MyFeed => ...
public IState<int> MyState => State.FromFeed(this, MyFeed);
```

## Update: How to update a state

The _state_ is designed to allow to respect the [ACID properties](https://en.wikipedia.org/wiki/ACID).
This means that all update methods are requesting a delegate that accepts the current value to update.
This makes sure that you are working with the latest version of the data.

> [!IMPORTANT]
> The provided delegate might be invoked more than once in case of concurrent updates.
> It must be a [pure function](https://en.wikipedia.org/wiki/Pure_function) (i.e. it must not alter anything else than the provided data).

### UpdateAsync

This allows you to update the value only of the state.

```csharp
public static ValueTask UpdateAsync<T>(this IState<T> state, Func<T?, T?> updater, CancellationToken ct = default);
```

For example:

```csharp
public IState<string> City => State<string>.Empty(this);

public async ValueTask SetCurrent(CancellationToken ct)
{
    var city = await _locationService.GetCurrentCity(ct);
    await City.UpdateAsync(currentValue => city, ct);
}
```

### UpdateDataAsync

If you need to update both the value and the metadata of the state, you can use `UpdateDataAsync`. This method allows you to work with the entire `Option<T>`.

```csharp
public static ValueTask UpdateDataAsync<T>(this IState<T> state, Func<Option<T>, Option<T>> updater, CancellationToken ct = default);
```

For example:

```csharp
public IState<string> City => State<string>.Empty(this);

public async ValueTask UpdateCityMetadata(CancellationToken ct)
{
    await City.UpdateDataAsync(currentData =>
    {
        var newData = Option.Some("New York");
        return newData;
    }, ct);
}
```

### UpdateMessage

This gives you the ability to update a _state_, including the metadata.

> [!NOTE]
> This is the raw way to update a state and is designed for advanced usage and should probably not be used directly in apps.

### Usage of States

States are advanced Feeds. As such, they can also be awaited directly:

For example:

```csharp
City currentCity = await this.CurrentCity;
```

### Binding the View to a State

States are built to be cooperating with the data-binding engine. A State will automatically update its value when the user changes data in the View bound to this State.

1. In an MVUX app (read [How to set up an MVUX project](xref:Uno.Extensions.Mvux.HowToMvuxProject)), add a Model class with a State as follows:

```csharp
    public partial record SliderModel
    {
        public IState<double> SliderValue => State.Value(this, () => Random.Shared.NextDouble() * 100);
    }
```

1. Replace all child elements in the _MainPage.xaml_ with the following:

```xml
    <Page
        x:Class="SliderApp.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:SliderApp">
        <Page.DataContext>
            <local:SliderViewModel />
        </Page.DataContext>

        <StackPanel>
            <StackPanel Orientation="Horizontal" Spacing="5">
                <TextBlock Text="Current state value:" />
                <TextBlock Text="{Binding SliderValue}" />
            </StackPanel>

            <Border Height="1" Background="DarkGray" />

            <TextBlock Text="Set state value:"/>
            <Slider Value="{Binding SliderValue, Mode=TwoWay}" />
        </StackPanel>
    </Page>
```

In this scenario, the `DataContext` is set to an instance of the `SliderViewModel` class, which is the generated ViewModel for the `SliderModel` record.

1. When you run the app, moving the `Slider` instantly affects the upper `TextBox`. The `Silder.Value` property has a two-way binding with the `SliderValue` State, so any change to the Slider immediately updates the State value, which in turn affects the data-bound `TextBlock` on top:

    ![A video of the previous slider app in action](/Reference/Reactive/Assets/SliderApp-1.gif)

### Change data of a state

#### Update

To manually update the current value of a state, use its `UpdateAsync` method.

In this example we'll add the method `IncrementSlider` that gets the current value and increases it by one (if it doesn't exceed 100):

```csharp
    public async ValueTask IncrementSlider(CancellationToken ct = default)
    {
        static double incrementValue(double currentValue) =>
            currentValue <= 99
            ? currentValue + 1
            : 1;

        await SliderValue.UpdateAsync(updater: incrementValue, ct);
    }
```

The `updater` parameter of the `Update` method accepts a `Func<T, T>`. The input parameter provides the current value of the State when called. The return value is the new value that will be applied as the new value of the State, in our case we use the `incrementValue` [local function](https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/local-functions) to increment `currentValue` by one (or return `1` if the value exceeds `100`).

#### Set

There are additional methods that update the data of a State such as `Set` and `UpdateMessage`, explained [here](xref:Uno.Extensions.Reactive.State#update-how-to-update-a-state). The `Set` method is the same as the `Update`, except that in `Set` there is no callback that provides the current value, instead a new value is provided directly and the old value is discarded:

```csharp
public async ValueTask SetSliderMiddle(CancellationToken ct = default)
{
    await SliderValue.SetAsync(50, ct);
}
```

### Subscribing to changes

#### ForEach

 The `ForEach` enables executing a callback each time the value of the `IState<T>` is updated.

This extension method takes a single parameter which is an async callback that takes two parameters. The first parameter is of type `T?`, where `T` is type of the `IState`, and represents the new value of the state. The second parameter is a `CancellationToken` which can be used to cancel a long running action.

```csharp
public static IDisposable ForEach<T>(this IState<T> state, Func<T?, CancellationToken, Task> action);
 ```

For example:

```csharp
   public partial record Model
   {
       public IState<string> MyState => ...

       public async ValueTask EnableChangeTracking()
       {
           MyState.ForEach(PerformAction);
       }

       public async ValueTask PerformAction(string item, CancellationToken ct)
       {
           ...
       }
   }
```

Additionally, the `ForEach` method can be set using the Fluent API:

```csharp
   public partial record Model
   {
       public IState<string> MyState => State.Value(this, "Initial value")
                                             .ForEach(PerformAction);

       public async ValueTask PerformAction(string item, CancellationToken ct)
       {
           ...
       }
   }

```

### Commands

Part of the MVUX toolbox is the automatic generation of Commands.
In the `IncrementSlider` example [we've just used](#change-data-of-a-state), a special asynchronous Command will be generated that can be used in the View by a `Button` or other controls:

Let's modify the XAML [above](#how-to-bind-the-view-to-a-state) with the following:

```xml
        ...
        <TextBlock Text="Set state value:"/>
        <Slider Value="{Binding SliderValue, Mode=TwoWay}" />

        <Button Content="Increment slider" Command="{Binding IncrementSlider" />

    </StackPanel>
 </Page>
```

When pressing the _Increment slider_ button, the generated `IncrementSlider` command will be executed invoking the `IncrementSilder` method on the Model resulting in an incrementation of the value.

This is what the result will look like:

![A video that demonstrates the effect of the recent updates applied to the slider-app](/Reference/Reactive/Assets/SliderApp-2.gif)

The source code for the sample app can be found [here](https://github.com/unoplatform/Uno.Samples/tree/9d669111b0c3b3cc473cc73a68e49e261787a5be/UI/MvuxHowTos/SliderApp).

To learn more about Commands read the Commands section in [this article](xref:Uno.Extensions.Reactive.InApps#commands).
