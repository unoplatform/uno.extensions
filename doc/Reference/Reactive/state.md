---
uid: Uno.Extensions.Reactive.State
---
# State

Unlike a _feed_ an `IState<T>`, as its name suggests, is state-full.
While a _feed_ is just a query of a stream of _data_, a _state_ also implies a current value (a.k.a. the state of the application) that can be accessed and updated.

There are some noticeable differences with a _feed_:

* When subscribing to a state, the currently loaded value is going to be replayed.
* There is a [`Update`](#update) method that allows you to change the current value.
* _States_ are attached to an owner and share the same lifetime as that owner.
* The main usage of _state_ is for two-way bindings.

## Sources: How to maintain a data

You can create a _state_ using one of the following:

### Empty

Creates a state without any initial value.

```csharp
public IState<string> City => State<string>.Empty(this);
```

### Value

Creates a state with a synchronous initial value.

```csharp
public IState<string> City => State.Value(this, () => "Montréal");
```

### Async

Creates a state with an asynchronous initial value.

```csharp
public IState<string> City => State.Async(this, async ct => await _locationService.GetCurrentCity(ct));
```

### AsyncEnumerable

Like for `Feed.AsyncEnumerable`, this allows you to adapt an `IAsyncEnumerable<T>` into a _state_.

```csharp
public IState<string> City => State.AsyncEnumerable(this, () => GetCurrentCity());

public async IAsyncEnumerable<string> GetCurrentCity([EnumeratorCancellation] CancellationToken ct = default)
{
    while (!ct.IsCancellationRequested)
    {
        yield return await _locationService.GetCurrentCity(ct);
        await Task.Delay(TimeSpan.FromMinutes(15), ct);
    }
}
```

### Create

This gives you the ability to create your own _state_ by dealing directly with _messages_.

> This is designed for advanced usage and should probably not be used directly in apps.

```csharp
public IState<string> City => State.Create(this, GetCurrentCity);

public async IAsyncEnumerable<Message<string>> GetCurrentCity([EnumeratorCancellation] CancellationToken ct = default)
{
    var message = Message<string>.Initial;
    var city = Option<string>.Undefined();
    var error = default(Exception);
    while (!ct.IsCancellationRequested)
    {
        try
        {
            city = await _locationService.GetCurrentCity(ct);
            error = default;
        }
        catch (Exception ex)
        {
            error = ex;
        }

        yield return message = message.With().Data(city).Error(error);
        await Task.Delay(TimeSpan.FromHours(1), ct);
    }
}
```

## Update: How to update a state

The _state_ is designed to allow to respect the [ACID properties](https://en.wikipedia.org/wiki/ACID).
This means that all update methods are requesting a delegate that accepts the current value to update.
This makes sure that you are working with the latest version of the data.

> [!IMPORTANT]
> The provided delegate might be invoked more than once in case of concurrent updates.
> It must be a [pure function](https://en.wikipedia.org/wiki/Pure_function) (i.e. it must not alter anything else than the provided data).

### UpdateValue

This allows you to update the value only of the state.

```csharp
public IState<string> City => State<string>.Empty(this);

public async ValueTask SetCurrent(CancellationToken ct)
{
    var city = await _locationService.GetCurrentCity(ct);
    await City.UpdateValue(_ => city, ct);
}
```

### Set

For value types and strings, you also have a `Set` **which does not ensure the respect of the ACID properties**.

```csharp
public IState<string> Error => State<string>.Empty(this);

public async ValueTask Share(CancellationToken ct)
{
    try
    {
        ../..
        await Error.Set(string.Empty, ct);
    }
    catch (Exception error)
    {
        await Error.Set("Share failed.", ct);
    }
}
```

> [!CAUTION]
> This is designed for simple properties that are independent of their previous.
> You must not use a previously captured value of the state like this as it would break the ACID properties:
>
> ```csharp
> public IState<int> Counter => State<int>.Value(this, () => 0);
>
> public async ValueTask Up(CancellationToken ct)
> {
>   var current = await Counter;
>   Counter.Set(current + 1, ct);
> }
> ```
>
> You should instead use the `Update` method:
>
> ```csharp
> public async ValueTask Up(CancellationToken ct)
> {
>   Counter.Update(current => current + 1, ct);
> }
> ```

### UpdateMessage

This gives you the ability to update a _state_, including the metadata.

> [!NOTE]
> This is the raw way to update a state and is designed for advanced usage and should probably not be used directly in apps.
