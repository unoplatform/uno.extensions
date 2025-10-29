---
uid: Uno.Extensions.Mvux.HowToSimpleFeed
---

# MVUX Simple Feed

Load a single value from a service with `IFeed<T>` and render it with `FeedView`.

## Fetch weather data from a service

```csharp
namespace WeatherApp;

public partial record WeatherInfo(int Temperature);

public interface IWeatherService
{
    ValueTask<WeatherInfo> GetCurrentWeatherAsync(CancellationToken ct);
}

public class WeatherService : IWeatherService
{
    public async ValueTask<WeatherInfo> GetCurrentWeatherAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        var temperature = Random.Shared.Next(-40, 40);
        return new WeatherInfo(temperature);
    }
}
```

Records keep the payload immutable so each update produces a new snapshot.

## Expose the feed through the model

```csharp
public partial record WeatherModel(IWeatherService WeatherService)
{
    public IFeed<WeatherInfo> CurrentWeather =>
        Feed.Async(WeatherService.GetCurrentWeatherAsync);
}
```

`Feed.Async` executes the service call and wraps result metadata for the view.

## Show the weather in the UI

```xml
<Page ...
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    <mvux:FeedView Source="{Binding CurrentWeather}">
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Data.Temperature}" />
                <Button Content="Refresh" Command="{Binding Refresh}" />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView>
</Page>
```

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        DataContext = new WeatherViewModel(new WeatherService());
    }
}
```

`Data` exposes the latest `WeatherInfo`; `Refresh` replays the feed without blocking the UI.

## Customize the loading state

```xml
<mvux:FeedView.ProgressTemplate>
    <DataTemplate>
        <TextBlock Text="Fetching temperature..." />
    </DataTemplate>
</mvux:FeedView.ProgressTemplate>
```

Override progress, error, none, or undefined templates to match your UX.

## Resources

- List feed how-to: (xref:Uno.Extensions.Mvux.HowToListFeed)
- FeedView quick guide: (xref:Uno.Extensions.Mvux.FeedView)
- Weather sample app: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/WeatherApp
