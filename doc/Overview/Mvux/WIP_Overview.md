---
uid: Overview.Mvux.Overview
---

# MVUX Overview

MVUX is a variation of the MVU design pattern that will also feel familiar to developers who have previously worked with MVVM.

Its main advantages are cutting down on boilerplate code, and easy maintenance of asynchronous data requests, and the ability to use immutable objects 

To better understand the capabilities of MVUX and the benefits it can provide, let us consider a simple application scenario. The application will display the current temperature, obtained from an external service. That should seem simple enough. All it needs to do is put a number on the screen.  
Although this seems like an easy problem, as is often the case, there are more details to consider than may be immediately apparent.

- What if the external data isn't immediately available when starting the app?
- How to show that data is being initially loaded? Or updated?
- What if no data is available?
- What if an error occurs while obtaining or processing the data?
- How to keep the app responsive while requesting or updating the UI?
- Does the app need to periodically request new data or listen to the external source provide it?
- How do we avoid threading or concurrency issues when handling new data in the background?
- How do we make sure the code is testable?

Individually, these questions and scenarios are simple to handle, but hopefully, they highlight that there is more to consider in even a very trivial application. Now imagine an application that you need to build, and with more complex data and UIs, the potential for complexity and the amount of required code can grow enormously.

MVUX is a response to such situations and makes it easier to handle the above scenarios.  
We'll look at the pattern and how to use it as we walk through creating this app. 

## What is MVUX?

It stands for **M**odel, **V**iew, **U**pdate, e**X**tended. MVUX is a design pattern that extends MVU, making it an ideal design pattern in Uno Platform apps, utilizing code generation and the WinUI data-binding engine.

Looking at each individual element, it's easiest to start with the View.

### View

The **View** is the UI. You can write it with XAML, C#, or a combination of the two, much as you would when using another design pattern.

You use data-binding to bind to the underlying presentation layer (we'll get to it soon) to keep the UI code (the View) separate from the presentation code.

### Update

When the user interacts with the View and provides input or action, it triggers an **Update** that makes changes to the presentation layer.

### Model

The **Model** in MVUX is fairly similar to the ViewModel in MVVM in that it embodies the presentation layer code, though in MVUX, the ViewModel is referred to as just Model.

In MVUX, change detection is similar to using the `INotifyPropertyChanged` interface, except MVUX automatically handles the change detection for you, this is achieved by a Proxy Model MVUX generates for each Model (that has the suffix 'Model'), and wraps around its original Model providing direct access to all its properties and methods, as well as extending it with additional features.

MVUX thus enables working with immutable data structures (C# records). Working with (C# records) can be a challenging transition for developers who have previously only worked with `INotifyPropertyChanged` implementing classes.
The fundamental difference is that since a record is immutable, making a change to it means creating a new object with different data instead of modifying data within an existing one. Here's where the Proxy Model comes handy and uses as a bridge between data-binding and change-notification, and immutable classes.

In the standard MVU pattern, an update will cause a new View to be created upon each change.
In MVUX the existing View will be updated via data-binding by the Proxy Model instead.

### eXtended

The **eXtended** part of the pattern provides the toolset to adapt between the data-binding engine and immutable data objects, as well as the rest of the functionality that will be covered later on.

One of the big differences between MVUX and patterns like MVVM is the use of 'Feeds' in place of some of the change-notification properties. While a property has a single value, a feed is similar to a stream of values.  
When an updated value is received, it replaces the old value, and the View reacts to the change. In this way, it is similar to reactive programming. However, MVUX additionally combines the Feed's value with 'statuses'.  
Common statuses for a feed include 'loading', 'has value', 'refreshing', 'empty', and 'error'. MVUX includes controls that make displaying these different statuses in the View easy without the need to create additional properties to manipulate and control what to show.

A feed can be a stream of a single value or a collection of values. It can also keep track of its current value(s). Keeping track of values is necessary if they may need to be 'updated' while providing performance optimizations if you only need to display the value in the View without changing it.
Models are always created asynchronously and automatically handle any necessary transitions to the UI thread.  

In many ways, MVUX is closer to functional programming than the imperative approach often taken by developers using the MVVM pattern. Writing code this way brings benefits in code reuse and can simplify testing.

The MVUX pattern brings four key benefits over other design patterns used to build native applications.

- It's entirely `async` to ensure a responsive user interface.
- It's reactive rather than event-driven and needs less boilerplate code.
- It encourages the immutability of Model classes, which in turn can lead to improved efficiency and performance, simpler code, and easier object comparison and testing.
- It automatically tracks and reports the status (or 'state') of asynchronously requested data, which also simplifies the code you need to write. 

Now that you know the fundamentals of the pattern, let's see how to use it to create the simple weather app mentioned above. 

## Using MVUX to create an app

This example creates an `IFeed<WeatherInfo>` that will return the weather, which includes the temperature, loaded asynchronously from a weather service.

### Service

The `WeatherInfo` entity received from the service holds the temperature.

```c#
public partial record WeatherInfo(double Temperature);
```

The service has a single method that asynchronously returns the current temperature, as defined in this interface.

```c#
public interface IWeatherService
{
    ValueTask<WeatherInfo> GetCurrentWeather(CancellationToken ct);
}
```

This is the implementation of the `IWeatherServiec` interface.
It returns a random value as the current weather (in Fahrenheit degrees), after a delay of one second, to simulate the asynchronous interaction with a remote server:

```c#
public class WeatherService : IWeatherService
{
    public async ValueTask<WeatherInfo> GetCurrentWeather(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), ct);

        return new WeatherInfo(new Random().Next(-40, 40));
    }
}
```

### Model

The `WeatherModel` is the **Model** in MVUX (this would be referred to as a ViewModel in MVVM):

```c#
public partial record WeatherModel(IWeatherService WeatherService)
{
    public IFeed<WeatherInfo> CurrentWeather => Feed.Async(WeatherService.GetCurrentWeather);
}
```

> [!IMPORTANT]  
> For the code generation to work, mark the Models and entities with the `partial` modifier, and have the Feed properties' access modifier as `public`.  
You can learn more about partial classes and methods in [this article](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods).

The `CurrentWeather` property is an `IFeed<WeatherInfo>`. It uses as an asynchronous gateway to the service, and also holds information about the current status of the data.

The MVUX code-generator (part of the X in MVUX) generates a data-binding-ready clone of the Model (prefixed `Bindable`), it re-generates its properties, and also introduces commands for all of its public methods that meet a certain criteria, which will be discussed later.

### View

A special control, the `FeedView` is used to display data exposed as an `IFeed<T>`, and provides different display templates for the various statuses of the Feed (e.g. loading, refreshing, error etc.).

The `FeedView` is data-bound to the `CurrentWeather` property which aligns with the `CurrentWeather` property exposed on the `WeatherModel`:

```xml
<Page x:Class="WeatherApp.MainPage"
	  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	  xmlns:mvux="using:Uno.Extensions.Reactive.UI">

	<mvux:FeedView Source="{Binding CurrentWeather}">
		<DataTemplate>
			<StackPanel>
				<TextBlock Text="{Binding Data.Temperature}" />
				<Button Content="Refresh"
						Command="{Binding Refresh}" />
			</StackPanel>
		</DataTemplate>
	</mvux:FeedView>

</Page>
```

In the View's code-behind, we're assigning the page's `DataContext` to the generated clone of the Model:

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        DataContext = new BindableWeatherModel(new WeatherService());
    }
}
```

Within the template, the `Data` property is used to access the data that was obtained from the service, currently available by the IFeed. In this case it's the most recent `WeatherInfo` result from the service.
The `Data` property is bound to the `DataContext` of the `TextBlock`, making it possible to bind the `Text` property of the `TextBlock` to the `Temperature` property.

The `Refresh` command is exposed by the `FeedView` and will cause the Feed to requery its source (the service).

> [!NOTE]
> For the full example see [How to create a feed](xref:Overview.Mvux.HowToSimpleFeed)
