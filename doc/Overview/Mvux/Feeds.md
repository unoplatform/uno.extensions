---
uid: Overview.Mvux.Feeds
---

# What are Feeds?

Feeds are there to manage asynchronous data requests from a service and provide their result to the View in an efficient manner.

It provides out of the box support for data coming from task-based methods as well Async-Enumerables ones.  

They accompany the requests with additional metadata that tell whether the request is still in progress, ended in an error, and if it was successful, whether the data that was returned contains any entries or was empty.

## Feeds are stateless

Feeds are used as a gateway to request data from services and hold it in a stateless manner to be displayed by the View.  
Feeds are stateless, and do not provide support for reacting upon changes the user makes to the data on the View, the data can only be reloaded and refreshed upon request which is when the underlying task or Async-Enumerable will be invoked and the data refreshed.
In other words, a Feed is a read-only representation of the data received from the server.

> [!TIP]
> In contrast to Feeds, [States](xref:Overview.Mvux.States) are stateful and keep track of the up-to-date state as applied by changes from the View by the user.

## How to use Feeds?

### Creation of Feeds

For the examples below let's use a counter service that returns the current count number, starting from 1. It will be run 3 consecutive times delayed by a second each.
For the data type we'll create a record type called `CounterValue`:

```c#
public record CounterValue(int Value);
```

Feeds can be created directly from either `ValueTask` returning methods, or from `IAsyncEnumerable` methods,
both with a `CancellationToken` parameter.

#### Using Tasks

Asynchronous data can be obtained in several ways.

The most common is via a `ValueTask` that returns the data value(s) when ready:

```c#
private int _currentCount = 0;

public async ValueTask<CounterValue> CountOne(CancellationToken ct)
{
    await Task.Delay(TimeSpan.FromSeconds(1));

    // note that a service does not normally hold data
    // this example is for demonstration purposes
    return new CounterValue(++_currentCount);
}
```

> [!NOTE]
> `ValueTask` is interchangeable with `Task`, but `ValueTask` was chosen to be in unity with the `IAsyncEnumerable` interface.
> A `Task` is easily convertible to `ValueTask` nonetheless.
> Learn more about [`Task`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task), [`ValueTask`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask?view=net-6.0), or read [this article](https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/) discussing the differences between the two.

This is known as a 'pull' method, as we're repeatedly calling the Task when we're looking for new data,
and the Task returns the value when it's ready, unless it was cancelled using the token (this will be discussed in another tutorial).

Using the `CountOne` method, creating a Feed is as easy as:

```c#
public IFeed<CounterValue> Value => Feed.Async(_myService.CountOne);
```

`Feed` is a static class that provides Feed factory methods, as well as extension methods for Feeds.  
As mentioned above, the `Async` method takes a delegate of the a signature returning `ValueTask<T>` where `T` is the returned type, and has a `CancellationToken` parameter.

Should the signature of your method be different, for example if the method returns `Task<T>` (instead of `ValueTask<T>`, or when it a `CancellationToken` parameter is not present, `Feed.Async` can be called as follows:

```c#
// Service method
public async Task<CounterValue> CountOne() { ... }

// Feed creation
public IFeed<CounterValue> CurrentCount => Feed.Async(async ct => await _myService.CountOne());
```

#### From Async Enumerables

In contrast to Tasks which operate as 'pull' methods, the 'push' method is where we call a method and establish some sort of connection with it,
while it sends new data packets as they become available:

```c#
public async IAsyncEnumerable<CounterValue> StartCounting([EnumeratorCancellation] CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
                
        if (ct.IsCancellationRequested)
        {
            yield break;
        }

        yield return new CounterValue(++_currentCount);
    }
}
```

Referring to the Async Enumerable from the example a Feed can be created in the following way:

```c#
public async IAsyncEnumerable<CounterValue> StartCounting(CancellationToken ct) { ... }

public IFeed<CounterValue> CurrentCount => Feed.AsyncEnumerable(_myService.StartCounting);
```

`CancellationToken`s are essential to enable halting an ongoing async operation.  
However, if the API you're consuming does not have a `CancellationToken` parameter, you can disregard that incoming `CancellationToken` parameter as following:

```c#
public IFeed<CounterValue> CurrentCount => Feed.AsyncEnumerable(ct => StartCounting());
```

> [!NOTE]
> `Feed` is a static class that provide factory methods that create `IFeed<T>`s, as well as extension methods for `IFeed<T>`.

> [!NOTE]  
> There are additional ways to load data (e.g. Observables), but most of them are easily convertible to one of the above two.

> [!TIP]
> Feeds can also be constructed manually using the `Feed.Create` method.

### Consumption of Feeds

#### Directly await Feeds

Feeds are directly awaitable, so to get the data currently held in the feed, this is useful when you want to use the current value in a command etc.  
You can await it in the following manner:

```c#
public IFeed<CurrentCount> CurrentCount => ...

private async ValueTask SomeAsyncMethod()
{
    int currentCount = await CurrentCount;
}
```

> [!TIP]
> This is possible thanks to the `GetAwaiter` extension method of `IFeed<T>`. Read [this](https://devblogs.microsoft.com/pfxteam/await-anything) for more.

#### Use Feeds in an MVUX Model

The MVUX analyzers generate a proxy entity for each of the models in your app (those with `Model` suffix). For every Feed property (returning `IFeed<T>` or `IListFeed<T>`) found in the model, a corresponding Feed (or List-Feed) property is being generated on the proxy entity.  
MVUX recommends using plain [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object) (Plain Old CLR Object) `record` types for the models in your app as they're immutable, and will not require any property change notifications to be raised.  
The generated proxy and its properties ensure that data-binding will work, even though property change notifications aren't being raised by the models themselves.

> [!Note]  
> For the code generation to work, mark the Models and entities with the `partial` modifier, and have the Feed properties' access modifier as `public`.  
You can learn more about partial classes and methods in [this article](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods).

#### With regular data-binding

Feeds can be consumed directly by data-binding to the Feed property declared on the Model.  
MVUX code-generation engine ensures the Feeds and all entities they expose are going to be seamlessly data-bound with the XAML data-binding engine.

The Feed can be consumed directly from the View, it's as simple as binding a regular value property exposed on the Model:

```xaml
<Page ...>
    <TextBlock Text="{Binding CurrentCount.Value}" />
</Page>
```

> [!TIP]
> The data-binding engine will await the feed under the hood and will display the awaited data once available.

#### With the `FeedView` control

The `FeedView` control has been designed to work with Feeds and is tailored to the additional metadata mentioned [earlier](#what-are-feeds) that are disclosed by the Feed and respond to it automatically and efficiently.  
A `FeedView` has built-in templates that adapt the View according to the current state of the data, such as when the data request is still in progress, an error has occurred, or when the data contained no records.  
Built-in templates are included with the `FeedView` for these states, but they can all be customized.

Here's how to utilize the `FeedView` to display the same data as before:

```xaml
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

> [!TIP]  
> The `FeedView` wraps the data coming from the Feed in a special `FeedViewState` class which includes the Feed metadata.  
One of its properties is `Data`, which provides access to the actual data of the Feed's current state, in our example the most recent integer value from the `CountOne` or `StartCounting` method [above](#consumption-of-feeds).

## Messages

Messages are one of the core components of MVUX. They refer to the metadata that wrap around the entities streaming along as discussed earlier.

The Feed encapsulates a stream of Messages for each packet of data received from the underlying request. For a Task it would be each execution of the Task and obtaining the refreshed/up-to-date value, and similarly with Async-Enumerable it would be each iteration and yielding of a refreshed value, until it's cancelled using the `CancellationToken`.

Messages are extensible, but currently a Message provides several metadata types (called axis/axes).  
The most common three are:

- Data  
This discloses information about the data that was allegedly returned by the request.  
The data is encapsulated in an `Option<T>` (where `T` refers to the data entity type):
    - None - indicates absence of data, i.e. the data-request resulted in no records.
    - Some - indicates presence of a value / values.
    - Undefined - we can't currently determine if there's data, e.g. we're still awaiting it or if an error occurred.
- Progress  
The underlying task/request is in progress, if there's data, it's regarded as transient till the request completes and a new result is available.
- Error  
An error has occurred. The `Exception` is attached.

The following illustration show how the classes are built. Note that this diagram shows a stripped-down version of the actual types, for brevity:

![](Assets/FeedMessagesDiagram.jpg)

> [!TIP]  
> MVUX provides you with peripheral tools that read the metadata Messages for you so that you don't normally even have to know about the Message structure!

## Feed Operators

The Feed supports some LINQ operators that enable readjusting it into a new one.

### Where

The `Where` extension method enables filtering a Feed. It returns a new Feed where the values of the parent one match the specified criteria.  
For example:

```c#
public IFeed<CounterValue> OmitEarlyCounts => CurrentCount.Where(currentCount => currentCount.Value > 10);
```

> [!Note]  
> Be aware that unlike `IEnumerable<T>`, `IObservable<T>`, and `IAsyncEnumerable<T>`, if the predicate returns false, a result is still received but it contains a `Message<T>` with a data `Option<T>` of `None`.

### Select or SelectAsync

This one enables projecting one feed into another one by selecting one of its properties, or by passing it as a parameter to an external function.

```c#
public IFeed<int> OmitEarlyCounts => CurrentCount.Select(currentCount => currentCount.Value);
```

The selection can also be asynchronous, and even use an external method:

```c#
public IFeed<CountInfo> CountTrends => CurrentCount.SelectAsync(currentCount => myService.GetCountInfoAsync(currentCount));
```

> [!TIP]  
> You can use the LINQ syntax if you prefer, or combine the operators:

```c#
public IFeed<int> OmitEearlyCounts =>
    from currentCount in CurrentCount
    where currentCount.Value > 10
    select currentCount.Value;
```
