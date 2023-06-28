---
uid: Overview.Mvux.Feeds
---

# What are feeds?

Feeds are there to manage asynchronous operations (for example requesting data from a service) and expose the result to the View in an efficient manner.

They provide out of the box support for task-based methods as well as [Async-Enumerables](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable-1) ones.  

Feeds include additional metadata that indicates whether the operation is still in progress, ended in an error, or if it was successful, whether the data that was returned contains any entries or was empty.

## Feeds are stateless

Feeds are typically used to request data from services and expose it in a stateless manner so that the resulting data can be displayed by the View.  

Feeds are stateless and does not provide support for reacting to changes the user makes to the data on the View. The data can only be reloaded and refreshed upon request which is when the underlying task or Async-Enumerable will be invoked and the data refreshed. In other words, a feed is a read-only representation of the data received from the server.

In contrast to feeds, [states](xref:Overview.Mvux.States) (`IState` or `IListState`), as the name suggests, are stateful and keep track of the latest value, as updates are applied.

## How to use feeds?

### Creation of feeds

For the examples below, let's use a counter service that returns the current count number, starting from 1. It will be run 3 consecutive times delayed by a second each.
For the data type, we'll create a record type called `CounterValue`:

```csharp
public record CounterValue(int Value);
```

Feeds can be created directly from methods that return a `ValueTask` or `Task`, or from methods that return an `IAsyncEnumerable`. In both cases, the methods can optionally take a `CancellationToken` parameter.

#### Feed.Async factory

The `Feed.Async` factory method will create an IFeed by invoking a method that will return either a `ValueTask` or a `Task`. For example, the `CountOne` method will wait for a second (unless cancelled via the `CancellationToken`) before returning the next counter value.

```csharp
private int _currentCount = 0;

public async ValueTask<CounterValue> CountOne(CancellationToken ct)
{
    await Task.Delay(TimeSpan.FromSeconds(1), ct);

    // note that a service does not normally hold data
    // this example is for demonstration purposes
    return new CounterValue(++_currentCount);
}
```

The `Feed.Async` factory method can be used to create an `IFeed` by calling the `CountOne` method:

```csharp
public IFeed<CounterValue> Value => Feed.Async(_myService.CountOne);
```

This is known as a 'pull' method, as the `CountOne` method is awaited while retrieving the data. To get the next counter value, the `IFeed` needs to be signaled to call the `CountOne` method again.

For the most part `Task` and `ValueTask` are interchangeable. However, with MVUX if the method returns `Task<T>` the method needs to be awaited in the `Feed.Async` callback.

```csharp
// Service method
public async Task<CounterValue> CountOne() { ... }

// Feed creation - needs to await the CountOne call
public IFeed<CounterValue> CurrentCount => Feed.Async(async ct => await _myService.CountOne());
```

#### Feed.AsyncEnumerable factory

In contrast to Tasks which operate as 'pull' methods, the 'push' method is where data is returned as it becomes available via an `IAsyncEnumerable` instance. For example, the `StartCounting` method will return a new counter value every second until the `CancellationToken` is cancelled.

```csharp
public async IAsyncEnumerable<CounterValue> StartCounting([EnumeratorCancellation] CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                
        if (ct.IsCancellationRequested)
        {
            yield break;
        }

        yield return new CounterValue(++_currentCount);
    }
}
```

Referring to the `StartCounting` method from this example, a feed can be created as follows:

```csharp
public IFeed<CounterValue> CurrentCount => Feed.AsyncEnumerable(_myService.StartCounting);
```

`CancellationToken`s are essential to enable halting an ongoing async operation. However, if the API you're consuming does not have a `CancellationToken` parameter, you can disregard the incoming `CancellationToken` parameter as follows:

```csharp
public IFeed<CounterValue> CurrentCount => Feed.AsyncEnumerable(ct => StartCounting());
```

#### From a ListFeed

An `IListFeed<T>` can be converted down to an `IFeed<IImmutableCollection<T>>` using the `AsFeed` method:

```csharp
public void SetUp()
{
    IListFeed<string> stringsListFeed = ...;
    IFeed<IImmutableCollection<string>> stringsFeed = stringsListFeed.AsFeed();
}
```

### Consumption of feeds

#### Awaiting feeds

Feeds are directly awaitable, so to get the data currently held in the feed, the feed needs to be awaited. This is useful when you want to use the current value in a method, for example:

```csharp
public IFeed<CurrentCount> CurrentCount => ...

private async ValueTask SomeAsyncMethod()
{
    int currentCount = await CurrentCount;
}
```

#### Use feeds in an MVUX Model

The MVUX analyzers generate a bindable proxy for each of the models in your app (those with `Model` suffix). For the code generation to work, mark the Models and entities with the `partial` modifier.

For every `public` feed property (returning `IFeed<T>` or `IListFeed<T>`) found in the model, a corresponding property is generated on the bindable proxy.  

MVUX recommends using plain [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object) (Plain Old CLR Object) `record` types for the models in your app as they're immutable, and will not require any property change notifications to be raised. The generated bindable proxy and its properties ensure that data-binding will work, even though property change notifications aren't being raised by the models themselves.

#### With regular data-binding

Feeds can be consumed directly by data-binding to the feed property declared on the Model. MVUX code-generation engine ensures the feeds and all entities they expose are going to be seamlessly data-bound with the XAML data-binding engine.

The feed can be consumed directly from the View, by data binding to a property exposed on the Model:

```xml
<Page ...>
    <TextBlock Text="{Binding CurrentCount.Value}" />
</Page>
```

#### With the `FeedView` control

The `FeedView` control has been designed to work with feeds and is tailored to the additional metadata mentioned [earlier](#what-are-feeds) that are disclosed by the feed and respond to it automatically and efficiently.  

The `FeedView` has templates that change the visible contents based on the current state of the data, such as when the data request is still in progress, an error has occurred, or when the data contained no records.  
Built-in templates are included with the `FeedView` for these states, but they can all be customized.

Here's how to utilize the `FeedView` to display the same data as before:

```xml
<Page
    ...
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <mvux:FeedView Source="{Binding CurrentCount}">
        <DataTemplate>
            <TextBlock DataContext="{Binding Data}" Text="{Binding Value}" />
        </DataTemplate>
    </mvux:FeedView>
</Page>
```

The `FeedView` wraps the data coming from the feed in a special `FeedViewState` class which includes the feed metadata.  One of its properties is `Data`, which provides access to the actual data of the feed's current state, in our example the most recent integer value from the `CountOne` or `StartCounting` method [above](#consumption-of-feeds).

## Feed Operators

An `IFeed` supports some LINQ operators that can be used to apply a transform and return a new `IFeed`.

### Where

The `Where` extension method enables filtering a Feed. It returns a new Feed where the values of the parent one match the specified criteria.  
For example:

```csharp
public IFeed<CounterValue> OmitEarlyCounts => CurrentCount.Where(currentCount => currentCount.Value > 10);
```

It's worth noting that unlike `IEnumerable<T>`, `IObservable<T>`, and `IAsyncEnumerable<T>`, if the predicate returns false, a result is still received but it contains a `Message<T>` with a data `Option<T>` of `None`.

### Select or SelectAsync

This one enables projecting one feed into another one by selecting one of its properties, or by passing it as a parameter to an external function.

```csharp
public IFeed<int> OmitEarlyCounts => CurrentCount.Select(currentCount => currentCount.Value);
```

The selection can also be asynchronous, and even use an external method:

```csharp
public IFeed<CountInfo> CountTrends => CurrentCount.SelectAsync(currentCount => myService.GetCountInfoAsync(currentCount));
```

### AsListFeed

When the current feed is of `IImmutableCollection<T>`, you might want to consider using a [list-feed](xref:Overview.Mvux.ListFeeds). This operator lets you easily convert an `IFeed<IImmutableCollection<T>>` to an `IListFeed<IImmutableCollection<T>>`.

For example:

```csharp
public void SetUp()
{
    IFeed<IImmutableCollection<string>> stringsFeed = ...;
    IListFeed<string> stringsListFeed = stringsFeed.AsListFeed();
}
```

### LINQ syntax

You can use the LINQ syntax if you prefer, which can improve the readability of your code, particularly if you combine multiple operators:

```csharp
public IFeed<int> OmitEarlyCounts =>
    from currentCount in CurrentCount
    where currentCount.Value > 10
    select currentCount.Value;
```
