---
uid: Uno.Extensions.Mvux.HowToSimpleFeed
---

# MVUX Simple Feed

Quick recipe for surfacing service data through an `IFeed<T>` and displaying it with `FeedView`.

## TL;DR
- Build an MVUX project (see xref:Uno.Extensions.Mvux.HowToMvuxProject) and add a service that returns immutable data.
- Expose the service call via `Feed.Async` inside your model; the analyzer generates a bindable `WeatherViewModel`.
- Bind `FeedView.Source` to the feed and use its templates (`ValueTemplate`, `ProgressTemplate`, `Refresh`) for UI feedback.

## 1. Define the Service Contract
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

## 2. Surface a Feed from the Model
```csharp
public partial record WeatherModel(IWeatherService WeatherService)
{
    public IFeed<WeatherInfo> CurrentWeather =>
        Feed.Async(WeatherService.GetCurrentWeatherAsync);
}
```
- MVUX generates `WeatherViewModel` exposing the same `CurrentWeather` feed for binding.
- You can await the feed inside the model if needed: `var value = await CurrentWeather;`.

## 3. Bind the View
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
- `Data` gives the latest `WeatherInfo` snapshot.
- `Refresh` re-executes the feed and toggles loading state automatically.

## 4. Customize FeedView States
```xml
<mvux:FeedView Source="{Binding CurrentWeather}">
    <mvux:FeedView.ValueTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Data.Temperature}" />
        </DataTemplate>
    </mvux:FeedView.ValueTemplate>
    <mvux:FeedView.ProgressTemplate>
        <DataTemplate>
            <TextBlock Text="Requesting temperature..." />
        </DataTemplate>
    </mvux:FeedView.ProgressTemplate>
</mvux:FeedView>
```
- Override `ProgressTemplate`, `NoneTemplate`, `ErrorTemplate`, or `UndefinedTemplate` to handle other feed states.

## Related Material
- Weather app reference implementation: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/WeatherApp
- Feed basics (xref:Uno.Extensions.Mvux.Overview)
- FeedView customization (xref:Uno.Extensions.Mvux.FeedView)
