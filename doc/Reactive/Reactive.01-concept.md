# Concept

When asynchronously loading a data, the standard pattern is to use a `Task<T>`. A _task_ represents data which  will be available in the future:
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

But in both cases, if there is any exception the stream will be broken. This means that for instance in example above, if it is not possible to compute the shipping cost for a given country for any reason (network issue, invalid country, …) the stream of data will be terminated, and selecting another country won’t have any effect.

Also, when a dependency is being updated and we may need to do some asynchronously work, like update a projection. In our example, we asynchronously get the updated shipping cost when country is changed. From a UI perspective, it would be great to have a visual indication that the shipping cost is being re-computed for the newly chosen country.

Neither `IObservable<T>` nor `IAsyncEnumerable<T>` have such metadata mechanism for produced values, that is the purpose of `IFeed<T>`.

With data, `IFeed<T>` currently does supports 3 main metadata (named “axis”):
* Error: If there is any exception linked to the current data
* Progress: We indicates that the current data is transient or final.
* Data: This represents the _data_ itself, but also adds an information about it. 
	It wraps the value into an `Option<T>` that adds the ability to make distinction between the different state of the value:
	* Some: Represents a valid data.
	* None: Indicates that a value has been loaded, but should be consider as empty, and we should not be rendered as is in the UI. In our example, when you cannot ship to the selected country.
	* Undefined: This represents a missing value, i.e. there is no info about the data yet. Typically this is because we are asynchronously loading it.

Here is a diagram of common messages produced by a feed when asynchronously loading data:


> Keep in mind that this is only an example of the common case, but each _axis_ is independent and can change from one state to another. There is no restriction between states.
