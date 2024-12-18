---
uid: Uno.Extensions.Reactive.ListFeed
---
# ListFeed

The `IListFeed<T>` is _feed_ specialized for handling collections.
It allows the declaration of an operator directly on items instead of dealing with the list itself. Unlike a feed, where each asynchronous operation returns one single item (or a series of single items in the case of an `IAsyncEnumerable`), a `ListFeed` returns a collection of items.
A _list feed_ goes in `None` if the list does not have any elements.

> [!NOTE]
> The `ListFeed` uses the _key equality_ to track multiple versions of the same entity within different messages of the `ListFeed`.
>[Read more about _key equality_](xref:Uno.Extensions.KeyEquality.Concept).

A couple of points to note about list-feeds:

- [Operators](#operators) are applied to the items within the returned collection, rather than on the entire collection.

- When an empty collection is returned by the service, it's treated as an Empty message. The returned data axis Option will be `None`, even though the result was not `null`. This is because when a control of data items is displayed with an empty collection (for instance a `ListView`), there is no reason to display the `FeedView`'s `ValueTemplate` with an empty `ListView`. The "No data records" `NoneTemplate` makes much more sense in this case. For that reason, both a `null` result and an empty collection are regarded as `None`.

## Sources: How to create a list feed

To create an `IListFeed<T>`, use the static class `ListFeed` to call one of the following methods:

 **Async**: Creates a `ListFeed` using a method that returns a `Task<IImmutableList<T>>`.

```csharp
public ValueTask<IImutableList<string>> GetNames(CancellationToken ct = default);
```

For example:

```csharp
public IListFeed<string> Names => ListFeed.Async(service.GetNames);
```

**AsyncEnumerable**: Creates a `ListFeed` using an `IAsyncEnumerable<IImmutableList<T>>`.

```csharp
public IAsyncEnumerable<IImutableList<string>> GetNames(
    [EnumeratorCancellation] CancellationToken ct = default);
```

For example:

```csharp
public IListFeed<string> Names => ListFeed.AsyncEnumerable(service.GetNames);
```

Pull and push are explained more in the [feeds page](xref:Uno.Extensions.Reactive.Feed#sources-how-to-create-a-feed).

**Create**: Provides custom initialization for a `ListFeed`.

```csharp
public static IListFeed<T> Create<T>(Func<CancellationToken, IAsyncEnumerable<Message<IImmutableList<T>>>> sourceProvider);
```

There are also 2 helpers that allow you to convert from/to a _feed_ to/from a _list feed_.

## Operators: How to interact with a list feed

Unlike a `Feed<List<T>>` operators on a _list feed_ are directly interacting with _items_ instead of the list itself.

### PaginatedAsync

This allows the creation of a feed of a paginated list.
The pagination can be made by cursor (cf. `ListFeed<T>.AsyncPaginatedByCursor`), or using a simple page index with `ListFeed.AsyncPaginated`.
Used among the generated view models and a `ListView`, when the user scroll and reach the end of the list, a `PageRequest` will be sent to the `ListFeed`,
which will trigger the load of the next page using the delegate that you provided.

```csharp
public static IListFeed<T> PaginatedAsync<T>(Func<PageRequest, CancellationToken, Task<IImmutableList<T>>> getPage);
```

For example:

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

### Where

This operator allows the filtering of _items_.

> [!WARNING]
> If all _items_ of the collection are filtered out, the resulting feed will go in `None` state.

```csharp
public static IListFeed<TSource> Where<TSource>(this IListFeed<TSource> source, Predicate<TSource> predicate);
```

For example:

```csharp
public IListFeed<string> LongNames => Names.Where(name => name.Length >= 10);
```

### AsFeed

This does the opposite of `AsListFeed` and converts a _list feed_ to a _feed of list_.

```csharp
public static IFeed<IImmutableCollection<T>> AsFeed<T>(this IListFeed<T> source);
```

For example:

```csharp
public void SetUp()
{
    IListFeed<string> stringsListFeed = ...;
    IFeed<IImmutableCollection<string>> stringsFeed = stringsListFeed.AsFeed();
}
```

### AsListFeed

A `ListFeed` can also be created from a `Feed` when the `Feed` exposes a collection (`IFeed<IImmutableCollection<T>>`):

```csharp
public static IListFeed<T> AsListFeed<T>(this IFeed<IImmutableCollection<T>> source);
```

For example:

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
