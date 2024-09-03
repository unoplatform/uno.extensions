---
uid: Uno.Extensions.Reactive.Feed
---
# What are feeds?

Feeds are there to manage asynchronous operations (for example requesting data from a service) and expose the result to the View in an efficient manner.

They provide out of the box support for task-based methods as well as [Async-Enumerables](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable-1) ones.

Feeds include additional metadata that indicates whether the operation is still in progress, ended in an error, or if it was successful, whether the data that was returned contains any entries or was empty.

## Feeds are stateless

Feeds are typically used to request data from services and expose it in a stateless manner so that the resulting data can be displayed by the View.

Feeds are stateless and do not provide support for reacting to changes the user makes to the data on the View. The data can only be reloaded and refreshed upon request which is when the underlying task or Async-Enumerable will be invoked and the data refreshed. In other words, a feed is a read-only representation of the data received from the server.

In contrast to feeds, [states](xref:Uno.Extensions.Mvux.States) (`IState` or `IListState`), as the name suggests, are stateful and keep track of the latest value, as updates are applied.

## Sources: How to create a feed

### Async

Feeds can be created from an async method using the Feed.Async factory method. Here’s an example:

```csharp
public static IFeed<T> Async<T>(Func<CancellationToken, Task<T>> asyncFunc, Signal? refreshSignal = null);
```

The loaded data can be refreshed using a `Signal` trigger that will re-invoke the async method.

> [!NOTE]
> A `Signal` represents a trigger that can be raised at anytime, for instance within an `ICommand` data-bound to a "pull-to-refresh".

### AsyncEnumerable

This adapts an `IAsyncEnumerable<T>` into a _feed_ using Feed.AsyncEnumerable. Here's an example:

```csharp
public static IFeed<T> AsyncEnumerable<T>(Func<CancellationToken, IAsyncEnumerable<T>> asyncEnumerableFunc);
```

> [!NOTE]
> Make sure to use a `CancellationToken` marked with the [`[EnumeratorCancellation]` attribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.enumeratorcancellationattribute).
> This token will be flagged as cancelled when the last subscription of the feed is being removed.
> Typically this will be when the `ViewModel` is being disposed.

### Create

This gives you the ability to create a specialized _feed_ by dealing directly with _messages_ using Feed.Create. Here's an example:

> [!NOTE]
> This is designed for advanced usage and should probably not be used directly in apps.

```csharp
public static IFeed<T> Create<T>(Func<CancellationToken, IAsyncEnumerable<Message<T>>> messageFunc);
```

### GetAwaiter

This allows the use of `await` on a _feed_, for instance when you want to capture the current value to use it in a command.

```csharp
public static TaskAwaiter<T> GetAwaiter<T>(this IFeed<T> feed);
```

## Operators: How to interact with a feed

You can apply some operators directly on any _feed_.

> [!TIP]
> You can use the linq syntax with feeds:
>
> ```csharp
> private IFeed<int> _values;
>
> public IFeed<string> Value => from value in _values
>   where value == 42
>   select value.ToString();
> ```

### Where

Applies a predicate on the _data_.

Be aware that unlike `IEnumerable`, `IObservable`, and `IAsyncEnumerable`, if the predicate returns false, a message with a `None` _data_ will be published.

```csharp
public static IFeed<T> Where<T>(this IFeed<T> feed, Func<T, bool> predicate);
```

### Select

Synchronously projects each data from the source _feed_.

```csharp
public static IFeed<TResult> Select<TSource, TResult>(this IFeed<TSource> feed, Func<TSource, TResult> selector);
```

### SelectAsync

Asynchronously projects each data from the source _feed_.

```csharp
public static IFeed<TResult> SelectAsync<TSource, TResult>(this IFeed<TSource> feed, Func<TSource, CancellationToken, Task<TResult>> selector);
```
