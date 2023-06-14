---
uid: Overview.Mvux.ListFeeds
---

# What are list-feeds?

A list-feed (`IListFeed`) is like a [feed](xref:Overview.Mvux.Feeds) which is stateless and keeps no track of changes, but is specialized for handling collections.

Unlike a feed, where each asynchronous operation, returns one single item (or a series of single items in the case of an `IAsyncEnumerable`), a list-feed returns a collection of items.  

A couple of points to note about list-feeds:

- [Operators](#operators) are applied to the items within the returned collection, rather than on the entire collection.

- When an empty collection is returned by the service, it's treated as an Empty message. The returned data axis Option will be `None`, even though the result was not `null`.  
This is because when a control of data items is displayed with an empty collection (for instance a `ListView`), there is no reason to display the `FeedView`'s `ValueTemplate` with an empty `ListView`. The "No data records" `NoneTemplate` makes much more sense in this case. For that reason, both a `null` result and an empty collection are regarded as `None`.

- The list-feed is uses the _key equality_ to track multiple versions of the same entity within different messages of the list-feed.
[Read more about _key equality_](xref:Overview.KeyEquality.Concept).

## How to create a list feed

To create an `IListFeed<T>`, use the static class `ListFeed` to call one of the same `Async`, `AsyncEnumerable`, and `Create` methods as in feed. The only difference is that it expects the Task or `IAsyncEnumerable` to return an `IImmutableList<T>` instead of `T`.

Here are some examples of creating a list-feed:

1. In this example the service returns a list of names on load/refresh - using a pull technique.

    Service code:

    ```csharp
    public ValueTask<IImutableList<string>> GetNames(CancellationToken ct = default);
    ```

    Model code:

    ```csharp
    public IListFeed<string> Names => ListFeed.Async(service.GetNames);
    ```

2. This one returns an immutable list of names when available - using push technique:

    Service code:  

    ```csharp
    public IAsyncEnumerable<IImutableList<string>> GetNames(
        [EnumeratorCancellation] CancellationToken ct = default);
    ```

    Model code:

    ```csharp
    public IListFeed<string> Names => ListFeed.AsyncEnumerable(service.GetNames);
    ```

    Pull and push are explained more in the [feeds page](xref:Overview.Mvux.Feeds#creation-of-feeds).

3. There are also two helper methods that enable conversion from a feed to a list-feed and vice versa.

     - On an `IFeed<TCollection>` where `TCollection` is an `IImmutableList<TItem>`, call `ToListFeed()` to convert it to an `IListFeed<TItem>`.
     - Otherwise, call `AsFeed()` on an `IListFeed<T>` to convert it to an `IFeed<IImmutableList<T>>`.

## Support for selection and pagination

MVUX also provides built-in support for selection and pagination.  
See more information on [Selection](xref:Overview.Mvux.Advanced.Selection) or [Pagination](xref:Overview.Mvux.Advanced.Pagination).

## Operators

As mentioned, unlike a `Feed<List<T>>` operators on a list-feed are directly interacting with the collection's items instead of the list itself.

### Where

This operator allows the filtering of the items.  

```csharp
public IListFeed<string> LongNames => Names.Where(name => name.Length >= 10);
```

If all items of the collection are filtered out, the resulting feed will go into `None` state.
