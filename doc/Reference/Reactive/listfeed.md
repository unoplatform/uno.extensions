---
uid: Uno.Extensions.Reactive.ListFeed
---
# ListFeed

The `IListFeed<T>` is _feed_ specialized for handling collections.
It allows the declaration of an operator directly on items instead of dealing with the list itself.
A _list feed_ goes in `None` if the list does not have any elements.

> [!NOTE]
> `ListFeed<T>` is using the _key equality_ to track multiple version of a same entity whitin different messages of the `ListFeed<T>`.
> (Read more about _key equality_.)[../KeyEquality/concept.md]

## Sources: How to create a list feed
To create an `IListFeed<T>`, on the `ListFeed` class, the same `Async`, `AsyncEnumerable` and `Create` methods found on `Feed` can be used.

There are also 2 helpers that allow you to convert from/to a _feed_ to/from a _list feed_.

### PaginatedAsync
This allows the creation of a feed of a paginated list.
The pagination can be made by cursor (cf. `ListFeed<T>.AsyncPaginatedByCursor`), or using a simple page index with `ListFeed.AsyncPaginated`.
Used among the generated view models and a `ListView`, when the user scroll and reach the end of the list, a `PageRequest` will be sent to the `ListFeed`,
which will trigger the load of the next page using the delegate that you provided.

```csharp
public IListFeed<City> Cities => ListFeed.AsyncPaginated(async (page, ct) => _service.GetCities(pageIndex: page.Index, perPage: 20));
```

> [!CAUTION]
> On the `Page` struct you have a `DesiredSize` property.
> This is the number of items the view is requesting to properly fill its "viewport", 
> **BUT** there is no garantee that this value remains the same between multi pages, espcially if the user resize the app.
> As a consequency, it **must not** be used with `Index` for a "skip/take" pattern like `source.Skip(page.Index * page.DesiredSize).Take(page.DesiredSize)`.
> For such patterns, you can either just hard-code your _page size_ (e.g. `source.Skip(page.Index * 20).Take(20)`,
> either use the `page.TotalCount` property (e.g. `source.Skip(page.TotalCount).Take(page.DesiredSize)`).

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
