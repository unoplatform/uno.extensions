---
uid: Overview.Mvux.Overview
---

# MVUX Overview

**M**odel, **V**iew, **U**pdate, e**X**tended (**MVUX**) is a variation of the MVU design pattern that encourages unidirectional flow of immutable data, whilst leveraging the data binding capabilities that makes MVVM pattern so powerful.

To better understand MVUX, let us consider a weather application that will display the current temperature, obtained from an external weather service. At face value, this seems simple enough: call service to retrieve latest temperature and display the returned value.  
  
Although this seems like an easy problem, as is often the case, there are more details to consider than may be immediately apparent.

- What if the external service isn't immediately available when starting the app?
- How does the app show that data is being loaded? Or being updated?
- What if no data is returned from the external service?
- What if an error occurs while obtaining or processing the data?
- How to keep the app responsive while loading or updating the UI?
- How do we refresh the current data?
- How do we avoid threading or concurrency issues when handling new data in the background?
- How do we make sure the code is testable?

Individually, these questions are simple enough to handle, but hopefully, they highlight that there is more to consider in even a very trivial application. Now imagine an application that has more complex data and user interface, the potential for complexity and the amount of required code can grow enormously.

MVUX is a response to such situations and makes it easier to handle the above scenarios.  

## What is MVUX?

MVUX is an extension to the MVU design pattern, and leverages code generation in order to take advantage of the uniuqe data-binding engine of WinUI and the Uno Platform.

### Model

The **Model** in MVUX is similar in many ways to the ViewModel in MVVM in that it defines the properties that will be available for data binding and methods that include any business logic. In MVUX this is referred to as the Model, highlighting that it is immutable by design.

For our weather app, `WeatherModel` is the **Model**, and defines a property named `CurrentWeather`.

```csharp
public partial record WeatherModel(IWeatherService WeatherService)
{
    public IFeed<WeatherInfo> CurrentWeather => Feed.Async(this.WeatherService.GetCurrentWeather);
}
```

The `CurrentWeather` property represents a feed (`IFeed`) of `WeatherInfo` entities (for those familiar with [Reactive](https://reactivex.io/) this is similar in many ways to an `Observable`). When the `CurrentWeather` property is accessed an `IFeed` is created via the `Feed.Async` factory method, which will asynchronously call the `GetcurrentWeather` service.  

### View

The **View** is the UI, which can be written in XAML, C#, or a combination of the two, much as you would when using another design pattern. For example the following can be used to data bind to the `CurrentWeather.Temperature` property.

```xml
<Page x:Class="WeatherApp.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel>
        <TextBlock Text="{Binding CurrentWeather.Temperature}" />
    </StackPanel>
</Page>
```  

If you're familiar with MVVM, the above XAML would look familiar, as it's the same XAML you would write if you had a ViewModel that exposed a CurrentWeather property that returns a WeatherInfo entity with a Temperature property. What's unique to MVUX is the additional information that `IFeed` exposes, such as when data is being loaded. For this, we can leverage the `FeedView` control that is part of MVUX.

```xml
<Page x:Class="WeatherApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    
    <mvux:FeedView Source="{Binding CurrentWeather}">
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Data.Temperature}" />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView>
    
</Page>
```

The `FeedView` control is designed to work with an `IFeed`, and has different visual states that align with the different states that an `IFeed` can be in (e.g. loading, refreshing, error, etc.). The above XAML defines the ValueTemplate, which is required in order to display the Data from the `IFeed`. Other templates include ProgressTemplate, ErrorTemplate and NoneTemplate, which can be defined in order to control what's displayed depending on the state of the `IFeed`.

### Update

An **Update** is any action that will result in a change to the **Model**. Whilst an **Update** is often triggered via an interaction by the user with the **View**, such as clicking a Button, an **Update** can also be triggered from background processes (for example a data sync operation, or perhaps a notification triggered by a hardware sensor, such as a GPS).

In the weather example, if we wanted to refresh the current weather data, a `Refresh` method can be added to the `WeatherModel`.

```csharp
public partial record WeatherModel(IWeatherService WeatherService)
{
    public IFeed<WeatherInfo> CurrentWeather => Feed.Async(this.WeatherService.GetCurrentWeather);
    public async ValueTask Refresh() { ... }
}
```  

In the `View` this can be data bound to a `Command` property on a `Button`.

```xml
<Page x:Class="WeatherApp.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel>
        <TextBlock Text="{Binding CurrentWeather.Temperature}" />
        <Button Command="{Binding Refresh}" Content="Refresh" />
    </StackPanel>
</Page>
```  

As refreshing a feed is such a common scenario, the `FeedView` control exposes a `Refresh` command that removes the requirement to have a `Refresh` method on the `WeatherModel` and can be data bound, again to the Command property, of a Button, as follows:  

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

Clicking the button will execute the `Refresh` command on the `FeedView` which will signal the `IFeed` to reload, in the case of the weather app it would invoke the `GetCurrentWeather` service again.

### eXtended

At this point you might be wondering how we're able to data bind to `CurrentWeather.Temperature`, as if it were a property that returns a single value, and then also bind the `CurrentWeather` property to the `Source` property of the `FeedView` to access a much richer set of information about the `IFeed`.  This is possible because of the bindable proxies that are being generated by the MVUX source code generators.

The **eXtended** part of MVUX includes the generation of these bindable proxies, that bridge the gap between the **Model** that exposes asynchronous feeds of immutable data and the synchronous data binding capability of WinUI and the Uno Platform. Instead of an instance of `WeatherModel`, the `DataContext` on the **View** is set to be an instance of the generated `BindableWeatherModel` which exposes a property, `CurrentWeather`, the same as the original `WeatherModel`.

For the purpose of this example, the `DataContext` property can be set in the page's code-behind file:

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

### Result

When the app is lanuched, a waiting progress ring appears while the service loads the current temperature:

![Video showing a progress-ring running in the app while waiting for data](Assets/SimpleFeed-3.gif)

It is thereafter replaced with the temperature as soon as it's received from the service:

![A screenshot of the app showing a refresh button](Assets/SimpleFeed-5.jpg)

When the 'Refresh' button is pressed, the progress ring shows again for a short time, until the new temperature is received from the server. The Refresh button is automatically disabled while a refresh request is in progress:

![A screenshot showing the refresh button disabled and temperature updated to 24](Assets/SimpleFeed-6.jpg)

For the full weather app tutorial see [How to create a feed](xref:Overview.Mvux.HowToSimpleFeed).

## Recap

In order to summarize what we've just seen, let's return to the list of challenges posed by our simple application.

- What if the external service isn't immediately available when starting the app?  
**The `FeedView` has an error template that can be used to control what's displayed when data can't be retrieved via the `ErrorTemplate`.**

- How does the app show that data is being loaded? Or being updated?  
**The `FeedView` has a progress template that defaults to a `ProgressRing` but can be overwritten via the `ProgressTemplate` property.**  

- What if no data is returned from the external service?  
**The `FeedView` has a no data template that can be defined via the `NoneTemplate` property.**  

- What if an error occurs while obtaining or processing the data?  
**The `FeedView` has both a error and undefined templates that can be used to control what's displayed when data can't be retrieved.**  

- How to keep the app responsive while loading or updating the UI?  
**All operations are inherently asynchronous and operations are dispatched to background threads to avoid congestion on the UI thread.**

- How do we refresh the current data?  
**Feeds support re-querying the source data and the `FeedView` exposes a Refresh property that can be bound to Command properties on UI elements such as Button.**

- How do we avoid threading or concurrency issues when handling new data in the background?  
**The `IFeed` handles dispatching actions to background threads and then marshalling response back to the UI thread as required.**

- How do we make sure the code is testable?  
**The **Model** doesn't depend on any UI elements, so can be unit tested, along with the generated bindable proxies.**


## Key points to note  

- Feeds are reactive in nature, similar in many ways to Observables
- Models and associated entities are immutable
- Operations are asynchronous by default
- Feeds include various dimensions such as loading, data and error
- Feeds borrow from Option concept in functional programming where no data is a valid state for the feed
- MVUX combines the unidirectional flow of data, and immutability of MVU, with the data binding capabilities of MVVM 






<!-- 




Its main advantages are cutting down on boilerplate code, easy maintenance of asynchronous data requests, and the ability to use immutable objects 


We'll look at the pattern and how to use it as we walk through creating this app. 

The MVUX code generator (part of the X in MVUX) generates a data-binding-ready clone of the Model (prefixed `Bindable`), along with its properties. A `CurrentWeather` property is generated in the Proxy Model and acts as a bridge to bind the View to the Model.



In MVUX, change detection is similar to using the `INotifyPropertyChanged` interface, except MVUX automatically handles the change detection for you. This is achieved by a Proxy Model MVUX generates for each Model (that has the suffix 'Model'), and wraps around the source Model providing direct access to all its properties and methods, as well as extending it with additional features.

MVUX thus enables working with immutable data structures ([C# records](https://learn.microsoft.com/dotnet/csharp/whats-new/tutorials/records)). Working with C# records can be a challenging transition for developers who have previously only worked with `INotifyPropertyChanged` implementing classes.
The fundamental difference is that since a record is immutable, making a change to a record means creating a new one with different data instead of modifying data within an existing one. Here's where the Proxy Model comes in handy and uses as a bridge between data-binding and change-notification, and immutable classes.


It uses as an asynchronous connection point to the service and also holds information about the current status of the data.




As the **Model** is immutable each **Update** will result in a new **Model** being created



In the standard MVU pattern, an update will cause a new View to be created upon each change. In MVUX the existing View will be updated via data-binding by the Bindable Proxy instead.


You might be wondering 

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

In the XAML above, within the template scope of the `FeedView`, the `Data` property is used to access the data that was obtained from the service, currently available by the IFeed. In this case, it's the most recent `WeatherInfo` result from the service.

You use data-binding to bind to the underlying presentation layer (we'll get to it soon) to keep the UI code (the View) separate from the presentation code.





toolset to adapt between the data-binding engine and the Model, which consists of generated code and and UI controls.



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



The `Refresh` command is exposed by the `FeedView` as part of its Template object. As we are inside the `FeedView`'s Template, the `Data` and `Refresh` properties are exposed to the Template, and will cause the Feed to requery its source (the service).




### Update

The MV**U**X Update is the part in the generated code where the data-binding actions are captured by the Proxy Model and are updating the data in the Model.  
This includes ensuring that even if the objects are immutable (record types), they will be recreated anew and be set to their matching property in the Model.






One of the big differences between MVUX and patterns like MVVM is that change-notification properties are not required.  
On the other hand MVUX introduces 'Feeds'. Feeds are used when a value is to be loaded asynchronously and then maybe refreshed asynchronously as well. While a traditional property represents a single value, a Feed is similar to a stream of values. When an updated value is received, the Proxy Model replaces the current object with a new one that has been created fresh with the new data. The View in turn reacts to the change by displaying the new data.  
In this way, it is similar to reactive programming.

MVUX also adds to the value another layer of data combined with the Feed value. These are statuses that represent the current state of the Feed.  
Common statuses for a feed include 'loading', 'has value', 'refreshing', 'empty', and 'error'.  
MVUX includes controls that make it easy to display these different statuses in the View without the need to create additional properties to manipulate and control what to show.

A Feed can be a stream of a single value or a stream of a collection. A Feed can also keep track of its current and previous value(s). Keeping track of values is necessary, for example if a subsequent request to refresh the data fails, as we'd still want to display the old value.

Models are always created asynchronously and any necessary transitions to the UI thread are handled automatically.

In many ways, MVUX is closer to functional programming than the imperative approach often taken by developers using the MVVM pattern.

The MVUX pattern brings four key benefits over other design patterns used to build native applications.

- It's entirely `async` to ensure a responsive user interface.
- It's reactive rather than event-driven and needs less boilerplate code.
- It encourages the immutability of Model classes, which in turn can lead to improved efficiency and performance, simpler code, and easier object comparison and testing.
- It automatically tracks and reports the statuses (or 'states') of asynchronously requested data, which also simplifies the code you need to write. 

Now that you know the fundamentals of the pattern, let's see how to use it to create the simple weather app mentioned above.  

-->