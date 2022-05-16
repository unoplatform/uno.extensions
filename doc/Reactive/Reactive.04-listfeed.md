#ListFeed

The `IListFeed<T>` is _feed_ specialized for handling collections.
It allows the declaration of an operator directly on items instead of dealing with the list itself.
A _list feed_ goes in `None` if the list does not have any elements.

## Sources: How to create a feed
To create an `IListFeed<T>`, on the state `ListFeed` class, the same `Async`, `AsyncEnumerable` and `Create` methods found on `Feed` can be used.

There are also 2 helpers that allows you do convert from/to a _feed_ to/from a _list feed_.

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

## Operators: How to interact with a list feed
Unlike a `Feed<List<T>>` operators on a _list feed_ are directly interacting with _items_ instead of the list itself.

### Where
This operator allows the filtering of _items_.

> [!WARNING]
> If all _items_ of the collection are filtered out, the resulting feed will go in _none_ state.

```csharp
public IListFeed<WeatherInfo> HighTempDays => ForecastFeed.Where(weather => weather.Temperature >= 28);
```
