---
uid: Uno.Extensions.Reactive.Concept
---
# Concept

When asynchronously loading data, the standard pattern is to use a `Task<T>`. A _Task_ represents data which will be available in the future:

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

But in both cases, if there is any exception the stream will be broken. This means that for instance in the example above if it is not possible to compute the shipping cost for a given country for any reason (network issue, invalid country, …) the stream of data will be terminated, and selecting another country won’t have any effect.

Also, when a dependency is being updated, we may need to do some asynchronous work, like update a projection. In our example, we asynchronously get the updated shipping cost when the country is changed. From the UI perspective, it would be great to have a visual indication that the shipping cost is being re-computed for the newly chosen country.

Neither `IObservable<T>` nor `IAsyncEnumerable<T>` have such metadata mechanism for produced values. That is the purpose of `IFeed<T>`.

With data, `IFeed<T>` currently supports 3 main metadata (named “axis”):

* Error: If there is any exception linked to the current data
* Progress: Indicates whether the current data is transient or final.
* Data: This represents the _data_ itself, but also adds information about it.
  * It wraps the value into an `Option<T>` that adds the ability to make a distinction between the different states of the value:
    * Some: Represents a valid data.
    * None: Indicates that a value has been loaded but should be considered empty, and we should not be rendered as-is in the UI. In our example, when you cannot ship to the selected country.
    * Undefined: This represents a missing value, i.e. there is no info about the data yet. Typically this is because we are asynchronously loading it.

Here is a diagram of common messages produced by a feed when asynchronously loading and refreshing data:

```diagram
     ┌─────────────────────┐
     │                     │
     │         ┌─────────┐ │
     │      ┌─►│ None    ├─┤
     │      │  └─────────┘ │
     ▼      │              │
┌─────────┐ │  ┌─────────┐ │
│ Loading ├─┼─►│ Some    ├─┤
└─────────┘ │  └─────────┘ │
            │              │
            │  ┌─────────┐ │
            └─►│ Error   ├─┘
               └─────────┘
```

[//]: # [Source](https://asciiflow.com/#/share/eJyrVspLzE1VssorzcnRUcpJrEwtUrJSqo5RqohRsrI0MdSJUaoEsozMLYGsktSKEiAnRkkBBB5N2UM5ionJgxmmgA3gUELQXKwaobLTdoFE%2FPLzUlGMwqacVJumIWvF9AdRhhFvJ4jlk5%2BYkpmXjqoH4sPg%2FFwMH1LZBWhexBl3yErINBvhL9eiovwibDGHoR5PulOqVaoFAO48kRs%3D)

> [!NOTE]
> Keep in mind that this is only an example of the common case, but each _axis_ is independent and can change from one state to another independently.
> There is no restriction between states and you can combine states like `Loading` and `Some` in your view if you want to.
