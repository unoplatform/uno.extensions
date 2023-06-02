---
uid: Overview.Mvux.Overview
---

# MVUX Overview

MVUX is a variation of the MVU design pattern that will also feel familiar to developers who have previously worked with MVVM.

Its main advantages are cutting down on boilerplate code, easy maintenance of asynchronous data requests, and the ability to use immutable objects 

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

Looking at each of the MVUX elements, it's easiest to start with the View.

### View

The **View** is the UI. You can write it with XAML, C#, or a combination of the two, much as you would when using another design pattern.

You use data-binding to bind to the underlying presentation layer (we'll get to it soon) to keep the UI code (the View) separate from the presentation code.

### Model

The **Model** in MVUX is fairly similar to the ViewModel in MVVM in that it embodies the presentation layer code, though, in MVUX, the ViewModel is referred to as just Model.

In MVUX, change detection is similar to using the `INotifyPropertyChanged` interface, except MVUX automatically handles the change detection for you. This is achieved by a Proxy Model MVUX generates for each Model (that has the suffix 'Model'), and wraps around the source Model providing direct access to all its properties and methods, as well as extending it with additional features.

MVUX thus enables working with immutable data structures ([C# records](https://learn.microsoft.com/dotnet/csharp/whats-new/tutorials/records)). Working with C# records can be a challenging transition for developers who have previously only worked with `INotifyPropertyChanged` implementing classes.
The fundamental difference is that since a record is immutable, making a change to a record means creating a new one with different data instead of modifying data within an existing one. Here's where the Proxy Model comes in handy and uses as a bridge between data-binding and change-notification, and immutable classes.

### Update

When the user provides input to the View, it triggers an **Update** that makes changes to the Model.

In the standard MVU pattern, an update will cause a new View to be created upon each change. In MVUX the existing View will be updated via data-binding by the Bindable Proxy instead.

### eXtended

The **eXtended** part of the pattern includes the toolset to adapt between the data-binding engine and the Model which includes code generation and UI controls.

## Using MVUX to create an app

This example creates an app that displays the current weather (in degrees Fahrenheit), loaded asynchronously from a weather service.
The weather service (`IWeatherService`), has an async `ValueTask` method that returns a `WeatherInfo` entity with a `Temperature` property that returns a different temperature each time it's called.

### Model

The `WeatherModel` is the **Model** in MVUX (this would be referred to as a ViewModel in MVVM).
It includes a Feed property named `CurrentWeather`:

```csharp
public partial record WeatherModel(IWeatherService WeatherService)
{
    public IFeed<WeatherInfo> CurrentWeather => Feed.Async(this.WeatherService.GetCurrentWeather);
}
```

The `CurrentWeather` property is an `IFeed<WeatherInfo>`. It uses as an asynchronous connection point to the service and also holds information about the current status of the data.

The MVUX code generator (part of the X in MVUX) generates a data-binding-ready clone of the Model (prefixed `Bindable`), along with its properties. A `CurrentWeather` property is generated in the Proxy Model and acts as a bridge to bind the View to the Model.

### View

A special control, the `FeedView` is used to display data exposed as an `IFeed<T>`, and provides different display templates for the various statuses of the Feed (e.g. loading, refreshing, error, etc.).

The `FeedView` is data-bound to the `CurrentWeather` property of the Proxy Model. The property in the Proxy Model acts as a bridge to the `CurrentWeather` property of the 'Model' (not the proxy), the bridge is there to enable data-binding for classes that do not implement `INotifyPropertyChanged` and uses as an adapter between immutable classes and data-binding.

When new data is available in the Feed, the `Data` property is updated with the new data, and the data-binding picks up those changes and the `TextBlock` is updated accordingly:

```xml
<Page x:Class="WeatherApp.MainPage"
	  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
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

In the View's code-behind, we're assigning the page's `DataContext` to the generated Proxy Model, passing in a `WeatherService` instance to its constructor (the Proxy Model has the same constructors as the Model):

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

In the XAML above, within the template scope of the `FeedView`, the `Data` property is used to access the data that was obtained from the service, currently available by the IFeed. In this case, it's the most recent `WeatherInfo` result from the service.

The `Refresh` command is exposed by the `FeedView` as part of its Template object. As we are inside the `FeedView`'s Template, the `Data` and `Refresh` properties are exposed to the Template, and will cause the Feed to requery its source (the service).

For the full tutorial see [How to create a feed](xref:Overview.Mvux.HowToSimpleFeed).

## Recap

One of the big differences between MVUX and patterns like MVVM is that change-notification properties are not required.  
On the other hand MVUX introduces 'Feeds'. Feeds are used when a value is to be loaded asynchronously and then maybe refreshed asynchronously as well. While a traditional property represents a single value, a Feed is similar to a stream of values. When an updated value is received, the Proxy Model replaces the current object with a new one that has been created fresh with the new data. The View in turn reacts to the change by displaying the new data.  
In this way, it is similar to reactive programming.

MVUX also adds to the value another layer of data combined with the Feed value. These are statuses that represent the current state of the Feed.  
Common statuses for a feed include 'loading', 'has value', 'refreshing', 'empty', and 'error'.  
MVUX includes controls that make it easy to display these different statuses in the View without the need to create additional properties to manipulate and control what to show.

A Feed can be a stream of a single value or a stream of a collection. It can also keep track of its current and previous value(s). Keeping track of values is necessary, for example if a subsequent request to refresh the data fails - we still want to display the old value.

Models are always created asynchronously and any necessary transitions to the UI thread are handled automatically.

In many ways, MVUX is closer to functional programming than the imperative approach often taken by developers using the MVVM pattern.

The MVUX pattern brings four key benefits over other design patterns used to build native applications.

- It's entirely `async` to ensure a responsive user interface.
- It's reactive rather than event-driven and needs less boilerplate code.
- It encourages the immutability of Model classes, which in turn can lead to improved efficiency and performance, simpler code, and easier object comparison and testing.
- It automatically tracks and reports the statuses (or 'states') of asynchronously requested data, which also simplifies the code you need to write. 

Now that you know the fundamentals of the pattern, let's see how to use it to create the simple weather app mentioned above. 
