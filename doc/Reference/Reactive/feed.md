---
uid: Uno.Extensions.Reactive.Feed
---
# Feed

## Sources: How to create a feed

You can build a _feed_ from different sources. The main entry point is the static class `Feed`:

Given an `IWeatherService` and `ILocationService`:

```csharp
public interface IWeatherService
{
    /// <summary>
    /// Asynchronously gets the current weather
    /// </summary>
    Task<WeatherInfo> GetCurrentWeather(CancellationToken ct);

    /// <summary>
    /// Asynchronously gets the details of a weather alert.
    /// </summary>
    Task<string> GetAlertDetails(WeatherAlert alert, CancellationToken ct);

    /// <summary>
    /// Asynchronously gets the weather forecast for the given day
    /// </summary>
    Task<WeatherInfo> GetWeatherForecast(DateTime date, CancellationToken ct);
}

public record WeatherInfo(double Temperature, WeatherAlert? Alert);

public record WeatherAlert(Guid Id, string? Title);

public interface ILocationService
{
    /// <summary>
    /// Asynchronously gets the name of the current city.
    /// </summary>
    Task<string> GetCurrentCity(CancellationToken ct);

    /// <summary>
    /// Gets teh list of all supported cities.
    /// </summary>
    Task<ImmutableList<string>> GetCities(CancellationToken ct);
}
```

### Async

Creates a feed from an async method.

```csharp
private IWeatherService _weatherService;

public IFeed<WeatherInfo> Weather => Feed.Async(async ct => await _weatherService.GetCurrentWeather(ct));
```

The loaded data can be refreshed using a `Signal` trigger that will re-invoke the async method.

> [!NOTE]
> A `Signal` represents a trigger that can be raised at anytime, for instance within an `ICommand` data-bound to a "pull-to-refresh".

```csharp
private IWeatherService _weatherService;
private Signal _refreshWeather = new();

public IFeed<WeatherInfo> Weather => Feed.Async(async ct => await _weatherService.GetCurrentWeather(ct), _refreshWeather);

public void RefreshWeather()
    => _refreshWeather.Raise();
```

### AsyncEnumerable

This adapts an `IAsyncEnumerable<T>` into a _feed_.

```csharp
private IWeatherService _weatherService;

public IFeed<WeatherInfo> Weather => Feed.AsyncEnumerable(() => GetWeather());

private async IAsyncEnumerable<WeatherInfo> GetWeather([EnumeratorCancellation] CancellationToken ct = default)
{
    while (!ct.IsCancellationRequested)
    {
        yield return await _weatherService.GetCurrentWeather(ct);
        await Task.Delay(TimeSpan.FromHours(1), ct);
    }
}
```

> [!NOTE]
> Make sure to use a `CancellationToken` marked with the [`[EnumeratorCancellation]` attribute](https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.enumeratorcancellationattribute).
> This token will be flagged as cancelled when the last subscription of the feed is being removed.
> Typically this will be when the `ViewModel` is being disposed.

### Create

This gives you the ability to create a specialized _feed_ by dealing directly with _messages_.

> [!NOTE]
> This is designed for advanced usage and should probably not be used directly in apps.

```csharp
public IFeed<WeatherInfo> Weather => Feed.Create(GetWeather);

private async IAsyncEnumerable<Message<WeatherInfo>> GetWeather([EnumeratorCancellation] CancellationToken ct = default)
{
    var message = Message<WeatherInfo>.Initial;
    var weather = Option<WeatherInfo>.Undefined();
    var error = default(Exception);
    while (!ct.IsCancellationRequested)
    {
        try
        {
            weather = await _weatherService.GetCurrentWeather(ct);
            error = default;
        }
        catch (Exception ex)
        {
            error = ex;
        }

        yield return message = message.With().Data(weather).Error(error);
        await Task.Delay(TimeSpan.FromHours(1), ct);
    }
}
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
public IFeed<WeatherAlert> Alert => Weather
    .Where(weather => weather.Alert is not null)
    .Select(weather => weather.Alert!);
```

### Select

Synchronously projects each data from the source _feed_.

```csharp
public IFeed<WeatherAlert> Alert => Weather
    .Where(weather => weather.Alert is not null)
    .Select(weather => weather.Alert!);
```

### SelectAsync

Asynchronously projects each data from the source _feed_.

```csharp
public IFeed<string> AlertDetails => Alert
    .SelectAsync(async (alert, ct) => await _weatherService.GetAlertDetails(alert, ct));
```

### GetAwaiter

This allows the use of `await` on a _feed_, for instance when you want to capture the current value to use it in a command.

```csharp
public async ValueTask ShareAlert(CancellationToken ct)
{
    var alert = await Alert; // Gets the current WeatherAlert
    await _shareService.Share(alert, ct);
}
```
