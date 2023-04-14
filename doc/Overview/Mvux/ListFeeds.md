---
uid: Overview.Mvux.ListFeeds
---

# What are List-Feeds?

The `IListFeed<T>` is like a [Feed](xref:Overview.Mvux.Feeds) which is stateless and keeps no track of changes, but is specialized for handling collections.

In Feeds, each data request from the service, returns one single item, whereas in List-Feed each request returns a collection of items.  
Its distinctive feature is the ability to use its [Operators](xref:Overview.Mvux.ListFeeds#operators) directly on its items, rather than on the entire collection.

Another important thing to remember is that when the service returns an empty collection, it's treated as an Empty message, and the data axis Option will be `None`, although the result was not `null`.
Read this if you would like to review the [Feed Messages section](xref:Overview.Mvux.Feeds#messages).


> [!NOTE]  
> The List-Feed is using the _key equality_ to track multiple version of a same entity within different messages of the List-Feed.
(Read more about _key equality_.)[xref:Overview.KeyEquality.Concept]

## How to create a list feed

To create an `IListFeed<T>`, use the the static class `ListFeed` to call one of the same `Async`, `AsyncEnumerable` and `Create` methods as in Feed.  
The only difference is that it expects the Task or Async Enumerable to return an `IImmutableList<T>` instead of `T`.

Here are a couple of examples of creating a List-Feed:

1. In this example the service returns a list of names on load/refresh - using a pull technique.

    Service code:
    ```c#
    public ValueTask<IImutableList<string>> GetNames(CancellationToken ct = default);
    ```

    Model code:
    ```c#
    public IListFeed<string> Names => ListFeed.Async(service.GetNames);
    ```
   
2. This one returns an immutable list of names when available - using push technique:

    Service code:  
    ```c#   
    public IAsyncEnumerable<IImutableList<string>> GetNames(
        [EnumeratorCancellation] CancellationToken ct = default);
    ```

    Model code:
    ```   
    public IListFeed<string> Names => ListFeed.AsyncEnumerable(service.GetNames);
    ```

    Pull and push are explained more in the [Feeds page](xref:Overview.Mvux.Feeds#creation-of-feeds).

3. There are also two helper methods that allow you to convert from a Feed to a List-Feed and vice versa.

     - On an `IFeed<TCollection>` where `TCollection` is an `IImmutableList<TItem>`, you can call `ToListFeed()` to convert it to an `IListFeed<TItem>`.
     - In contrast, you can call `AsFeed()` on an `IListFeed<T>` to convert it to an `IFeed<IImmutableList<T>>`.

## Support for pagination

There is built in support for pagination, which you can read about [here](xref:Overview.Reactive.ListFeed#paginatedasync) (Model) and [here](Overview.Reactive.InApps#pagination) (View).

## Operators

As mentioned, unlike a `Feed<List<T>>` operators on a List-Feed are directly interacting with the collection's items instead of the list itself.

### Where

This operator allows the filtering of the items.

> [!IMPORTANT]
> If all items of the collection are filtered out, the resulting Feed will go in `None` state.

```csharp
public IListFeed<string> LongNames => Names.Where(name => name.Length >= 10);
```

