---
uid: Uno.Extensions.Mvux.ListFeeds
---

# What are list-feeds?

A `ListFeed` (`IListFeed`) is like a [Feed](xref:Uno.Extensions.Mvux.Feeds) which is stateless and keeps no track of changes but is specialized for handling collections.

Unlike a feed, where each asynchronous operation returns one single item (or a series of single items in the case of an `IAsyncEnumerable`), a `ListFeed` returns a collection of items.

A couple of points to note about list-feeds:

- [Operators](#operators) are applied to the items within the returned collection, rather than on the entire collection.

- When an empty collection is returned by the service, it's treated as an Empty message. The returned data axis Option will be `None`, even though the result was not `null`. This is because when a control of data items is displayed with an empty collection (for instance a `ListView`), there is no reason to display the `FeedView`'s `ValueTemplate` with an empty `ListView`. The "No data records" `NoneTemplate` makes much more sense in this case. For that reason, both a `null` result and an empty collection are regarded as `None`.

- The `ListFeed` uses the _key equality_ to track multiple versions of the same entity within different messages of the `ListFeed`.
[Read more about _key equality_](xref:Uno.Extensions.KeyEquality.Concept).

## How to create a list feed

To create an `IListFeed<T>`, use the static class `ListFeed` to call one of the same `Async`, `AsyncEnumerable`, and `Create` methods as in `Feed`. The only difference is that it expects the Task or `IAsyncEnumerable` to return an `IImmutableList<T>` instead of `T`.

A `ListFeed` can be created in several ways.

### Async

Using the Async factory method, the service returns a list of names on load/refresh - using a pull technique. The `ListFeed` is then created with the async call.

- Service code:

```csharp
public ValueTask<IImutableList<string>> GetNames(CancellationToken ct = default);
```

- Model code:

```csharp
public IListFeed<string> Names => ListFeed.Async(service.GetNames);
```

### AsyncEnumerable

In this way, a `ListFeed` is created with an Async Enumerable method that returns an `IImmutableList` of names when available - using the push technique.

- Service code:  

```csharp
public IAsyncEnumerable<IImutableList<string>> GetNames(
    [EnumeratorCancellation] CancellationToken ct = default);
```

- Model code:

```csharp
public IListFeed<string> Names => ListFeed.AsyncEnumerable(service.GetNames);
```

Pull and push are explained more in the [feeds page](xref:Uno.Extensions.Mvux.Feeds#creation-of-feeds).

### Convert from `Feed` of an item-collection

A `ListFeed` can also be created from a `Feed` when the `Feed` exposes a collection (`IFeed<IImmutableCollection<T>>`):

```csharp
public void SetUp()
{
    IFeed<IImmutableCollection<string>> stringsFeed = ...;
    IListFeed<string> stringsListFeed = stringsFeed.AsListFeed();
}
```

## Support for selection and pagination

MVUX also provides built-in support for Selection and Pagination.
See more information on [Selection](xref:Uno.Extensions.Mvux.Advanced.Selection) or [Pagination](xref:Overview.Mvux.Advanced.Pagination).

## Operators

As mentioned, unlike a `Feed<List<T>>`, operators on a `ListFeed` are directly interacting with the collection's items instead of the list itself.

### Where

This operator allows the filtering of the items.

```csharp
public IListFeed<string> LongNames => Names.Where(name => name.Length >= 10);
```

If all items of the collection are filtered out, the resulting `Feed` will go into `None` state.

### AsFeed

This operator enables converting an `IListFeed<T>` to `IFeed<IImmutableList<T>>`:

```csharp
public void SetUp()
{
    IListFeed<string> stringsListFeed = ...;
    IFeed<IImmutableCollection<string>> stringsFeed = stringsListFeed.AsFeed();
}
```
