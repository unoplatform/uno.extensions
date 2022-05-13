# Reactive
## Concept

When asynchronously loading a data, the standard pattern is to use a `Task<T>`. A _task_ represents data which  will be available in the future:
```csharp
public async Task<decimal> GetShippingCost(CancellationToken ct)
{
	var country = SelectedCountry;
	var cost = await _shippingService.GetShippingCost(country);

	return cost;
}
```
An issue here is that `Task<T>` represents only one value, data must manually be fetched again each time one of its dependencies is updated. For instance, here, each time the user updates the selected country, `GetShippingCost` has to be manually re-invoked and the UI updated.

A solution to this would be to use `IObservable<T>` or `IAsyncEnumerable<T>`. Both are representing a stream of value. The example above can be written like this using `IObservable<T>`:
```csharp
public IObservable<Country> SelectedCountry { get; }

public IObservable<decimal> ShippingCost => _selectedCountry.SelectAsync(country => _shippingService.GetShippingCost(country));
```
Or with `IAsyncEnumerable`:
```csharp
public async IAsyncEnumerable<decimal> GetShippingCost([EnumerationCancellation] CancellationToken ct = default)
{
	await foreach (var country in SelectedCountry)
	{
		yield return await _shippingService.GetShippingCost(country);
	}
}
```

But in both cases, if there is any exception the stream will be broken. This means that for instance in example above, if it is not possible to compute the shipping cost for a given country for any reason (network issue, invalid country, …) the stream of data will be terminated, and selecting another country won’t have any effect.

Also, when a dependency is being updated and we may need to do some asynchronously work, like update a projection. In our example, we asynchronously get the updated shipping cost when country is changed. From a UI perspective, it would be great to have a visual indication that the shipping cost is being re-computed for the newly chosen country.

Neither `IObservable<T>` nor `IAsyncEnumerable<T>` have such metadata mechanism for produced values, that is the purpose of `IFeed<T>`.

With data, `IFeed<T>` currently does supports 3 main metadata (named “axis”):
* Error: If there is any exception linked to the current data
* Progress: We indicates that the current data is transient or final.
* Data: This represents the _data_ itself, but also adds an information about it. 
	It wraps the value into an `Option<T>` that adds the ability to make distinction between the different state of the value:
		- Some: Represents a valid data.
		- None: Indicates that a value has been loaded, but should be consider as empty, and we should not be rendered as is in the UI. In our example, when you cannot ship to the selected country.
		- Undefined: This represents a missing value, i.e. there is no info about the data yet. Typically this is because we are asynchronously loading it.

Here is a diagram of common messages produced by a feed when asynchronously loading data:


> Keep in mind that this is only an example of the common case, but each _axis_ is independent and can change from one state to another. There is no restriction between states.


## API
The main API provided by this package is [`IFeed<T>`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Core/IFeed.cs) which represents a stream of _data_.

Unlike `IObservable<T>` or `IAsyncEnumerable<T>`, _feed_ streams are specialized to handle business data objects which are expected to be rendered by the UI.
A _feed_ is a sequence of _messages_ which contains immutable _data_ with metadata such as its loading progress and/or errors raised while loading it.
Everything is about _data_, this means that a _feed_ will never fail. It will instead report any error encountered with the data itself, remaining active for future updates.

Each [`Message<T>`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Core/Message.cs) contains the current and the previous state, as well as information about what changed.
This means that a message is self-sufficient to get the current data, but also gives enough information to update from the previous state in an optimized way without rebuilding everything.

## General guidelines

* A basic _feed_ is **state-less**. The state is contained only in _messages_ that are going through a given subscription.
* The _data_ is expected to be immutable. An update of the _data_ implies a new _message_ with an updated instance.
* As in functional programming, _data_ is **optional**. A message may contain _data_ (a.k.a. `Some`), the information about the fact that there is no _data_ (a.k.a. `None`) or nothing at all, if for instance the loading failed (a.k.a. `Undefined`).
* Public _feeds_ are expected to be exposed in property getters. A caching system embedded in reactive framework will then ensure to not re-create a new _feed_ on each get.

## Sources: How to create a feed
You can build a _feed_ from different sources. The main entry point is the static class `Feed`:

Given an `IWeatherService` and `ILocationService`:
```csharp
public interface IWeatherService
{
	/// <summary>
	/// Asynchronously gets the current weather
	/// </summary>
	Task<WeatherInfo> GetCurrentWeather(CancellationToken ct);

	/// <summary>
	/// Asynchronously gets the details of a weather alert.
	/// </summary>
	Task<string> GetAlertDetails(WeatherAlert alert, CancellationToken ct);

	/// <summary>
	/// Asynchronously gets the weather forecast for the given day
	/// </summary>
	Task<WeatherInfo> GetWeatherForecast(DateTime date, CancellationToken ct);
}

public record WeatherInfo(double Temperature, WeatherAlert? Alert);

public record WeatherAlert(Guid Id, string? Title);

public interface ILocationService
{
	/// <summary>
	/// Asynchronously gets the name of the current city.
	/// </summary>
	Task<string> GetCurrentCity(CancellationToken ct);

	/// <summary>
	/// Gets teh list of all supported cities.
	/// </summary>
	Task<ImmutableList<string>> GetCities(CancellationToken ct);
}
```

### Async
Creates a feed from an async method.

```csharp
private IWeatherService _weatherService;

public IFeed<WeatherInfo> Weather => Feed.Async(async ct => await _weatherService.GetCurrentWeather(ct));
```

The loaded data can be refreshed using a `Signal` trigger that will re-invoke the async method.

> [!NOTE]
> A `Signal` represents a trigger that can be raised at anytime, for instance within an `ICommand` data-bound to a "pull-to-refresh".

```csharp
private IWeatherService _weatherService;
private Signal _refreshWeather = new();

public IFeed<WeatherInfo> Weather => Feed.Async(async ct => await _weatherService.GetCurrentWeather(ct), _refreshWeather);

public void RefreshWeather()
	=> _refreshWeather.Raise();
```


### AsyncEnumerable
This adapts an `IAsyncEnumerable<T>` into a _feed_.

```csharp
private IWeatherService _weatherService;

public IFeed<WeatherInfo> Weather => Feed.AsyncEnumerable(() => GetWeather());

private async IAsyncEnumerable<WeatherInfo> GetWeather([EnumeratorCancellation] CancellationToken ct = default)
{
	while (!ct.IsCancellationRequested)
	{
		yield return await _weatherService.GetCurrentWeather(ct);
		await Task.Delay(TimeSpan.FromHours(1), ct);
	}
}
```
> [!NOTE]
> Make sure to use a `CancellationToken` marked with the [`[EnumeratorCancellation]` attribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.enumeratorcancellationattribute).
> This token will be flagged as cancelled when the last subscription of the feed is being removed.
> Typically this will be when the `ViewModel` is being disposed.

### Create
This gives you the ability to create a specialized _feed_ by dealing directly with _messages_.

> [!NOTE]
> This is designed for advanced usage and should probably not be used directly in apps.

```csharp
public IFeed<WeatherInfo> Weather => Feed.Create(GetWeather);

private async IAsyncEnumerable<Message<WeatherInfo>> GetWeather([EnumeratorCancellation] CancellationToken ct = default)
{
	var message = Message<WeatherInfo>.Initial;
	var weather = Option<WeatherInfo>.Undefined();
	var error = default(Exception);
	while (!ct.IsCancellationRequested)
	{
		try
		{
			weather = await _weatherService.GetCurrentWeather(ct);
			error = default;
		}
		catch (Exception ex)
		{
			error = ex;
		}

		yield return message = message.With().Data(weather).Error(error);
		await Task.Delay(TimeSpan.FromHours(1), ct);
	}
}
```

## Operators: How to interact with a feed
You can apply some operators directly on any _feed_.

> [!NOTE]
> You can use the linq syntax with feeds:
> ```csharp
> private IFeed<int> _values;
> 
> public IFeed<string> Value => from value in _values
> 	where value == 42
> 	select value.ToString();
> ```

### Where
Applies a predicate on the _data_.

Be aware that unlike `IEnumerable`, `IObservable` and `IAsyncEnumerable`, if the predicate returns false, a message with a `None` _data_ will be published.

```csharp
public IFeed<WeatherAlert> Alert => Weather
	.Where(weather => weather.Alert is not null)
	.Select(weather => weather.Alert!);
```

### Select
Synchronously projects each data from the source _feed_.

```csharp
public IFeed<WeatherAlert> Alert => Weather
	.Where(weather => weather.Alert is not null)
	.Select(weather => weather.Alert!);
```

### SelectAsync
Asynchronously projects each data from the source _feed_.

```csharp
public IFeed<string> AlertDetails => Alert
	.SelectAsync(async (alert, ct) => await _weatherService.GetAlertDetails(alert, ct));
```

### GetAwaiter
This allows the use of `await` on a _feed_, for instance when you want to capture the current value to use it in a command.
```csharp
public async ValueTask ShareAlert(CancellationToken ct)
{
	var alert = await Alert; // Gets the current WeatherAlert
	await _shareService.Share(alert, ct);
}
```

## ListFeed: _Feed_ of collections of _items_
The `IListFeed<T>` is _feed_ specialized for handling collections.
It allows the declaration of an operator directly on items instead of dealing with the list itself.
A _list feed_ goes in `None` if the list does not have any elements.

To create an `IListFeed<T>`, on the state `ListFeed` class, the same `Async`, `AsyncEnumerable` and `Create` methods found on `Feed` can be used.

There are also a few dedicated operators:

### AsListFeed
This allows the creation of a _list feed_ from a _feed of list_.

```csharp
public IListFeed<WeatherInfo> Forecast => Feed
	.Async(async ct => new []
	{
		await _weatherService.GetWeatherForecast(DateTime.Today.AddDays(1), ct),
		await _weatherService.GetWeatherForecast(DateTime.Today.AddDays(2), ct),
	})
	.Select(list => list.ToImmutableList())
	.AsListFeed();
```

### AsFeed
This does the opposite of `AsListFeed` and converts a _list feed_ to a _feed of list_.

```csharp
public IFeed<IImmutableList<WeatherInfo>> ForecastFeed => Forecast.AsFeed();
```

### Where
This operator allows the filtering of _items_.
If all _items_ of the collection are filtered out, the resulting feed will go in _none_ state.

```csharp
public IListFeed<WeatherInfo> HighTempDays => ForecastFeed.Where(weather => weather.Temperature >= 28);
```

## State: How to maintain and update data

Unlike a _feed_ a `IState<T>`, as its name suggest, is state full. 
While a _feed_ is just a query of a stream of _data_, a _state_ also implies a current value (a.k.a. the state of the application) which can be accessed and updated.

There are some noticeable differences with a _feed_:
* When subscribing to a state, the currently loaded value is going to be replayed.
* There is a [`Update`](#update) method which allows you to change the current value.
* _States_ are attached to a owner and share the same lifetime of that owner.
* The main usage of _state_ is for two-way bindings.

You can create a _state_ using one of the following:

### Empty
Creates a state without any initial value.

```csharp
public IState<string> City => State<string>.Empty(this);
```

### Value
Creates a state with a synchronous initial value.

```csharp
public IState<string> City => State.Value(this, () => "Montréal");
```

### Async
Creates a state with an asynchronous initial value.

```csharp
public IState<string> City => State.Async(this, async ct => await _locationService.GetCurrentCity(ct));
```

### AsyncEnumerable
Like for `Feed.AsyncEnumerable`, this allows you to adapt an `IAsyncEnumerable<T>` into a _state_.

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

## Usage in applications

The recommended use for feeds is only in view models.

The reactive framework allows you to design state-less view models, focusing on the presentation logic. 
Your view models only have to request in their constructors the user _ inputs_ that are expected from the view. Inputs could be:
* An `IInput<T>` to get _data_ from the view (e.g. for 2-way bindings);
* An `ICommandBuilder` for the "trigger" inputs.

Then in order to easily interact with the binding engine in a performant way, a `BindableXXX` class is automatically generated. It is this class that will hold the state and which **has to been set as `DataContext` of your page**.

> Note: That bindable counterpart of a class will be created as soon as the class name ends with "ViewModel".
> You can customize that behavior using the `[ReactiveBindable]` attribute.

> Note: In order to be as smooth as possible, public properties of your view model, will also be accessible on the `BindableXXX` class as long as there is no name conflict between these properties and any constructor's _input_. You can also access to the view model itself through the `Model` property.

### Display some data in the UI (VM to View)

To render a _feed_ in the UI, you only have to expose it through a public property:
```csharp
public IFeed<Product[]> Products = Feed.Async(_productService.GetProducts);
```

Then in your page, you can add a `FeedView`:
```xaml
<Page 
	x:Class="MyProject.MyPage"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:uer="using:Uno.Extensions.Reactive.UI">

<reactive:FeedView Source="{Binding Products}">
	<DataTemplate>
		<ListView ItemsSource="{Binding Data}" />
	</DataTemplate>
</reactive:FeedView>
```

### Commands
The generated bindable counterpart of a class will automaticlaly re-expose public methods that has 0 or 1 parameter and an optional `CancellationToken` as `ICommand`.
The parameter will be fulfilled using the `CommandParameter` property.

## Testing

In order to test your reactive application, you should install the `Uno.Extensions.Reactive.Testing` package in you test project.

Make your test class inherit from `FeedTests`, then in your tests methods you can use the `.Record()` extensions method on the test you want to test. It will subscribe to your feed and persist all received messages. Then you can assert the expected messages using the fluent assertions:

```csharp
[TestMethod]
public async Task When_ProviderReturnsValueSync_Then_GetSome()
{
	var sut = Feed.Async(async ct =>
	{
		await Task.Delay(500, ct);
		return 42;
	});
	using var result = await sut.Record();

	result.Should().Be(r => r
		.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
		.Message(Changed.Data, 42, Error.No, Progress.Final)
	);
}
```

You define each axis (`Data` / `Error` / `Progress`) in the `Message` you want to validate. You can also define which axes are expected to have changed (`Changed`).

> Note: When developing a new _feed_, we recommend that you systematically validate all axes.

