---
uid: Overview.Mvux.Overview
---

# MVUX Overview

## What is MVUX

MVUX stands for **M**odel-**V**iew-**U**pdate e**X**tended.

In MVU the **model** represents the state of the application and is displayed by the **view**.  
Input from the user triggers an **update** to the model. MVU is also referred to as the [Elm Architecture](https://en.wikipedia.org/wiki/Elm_(programming_language)#The_Elm_Architecture).

MVUX **extend**s MVU with a powerful toolset that makes it possible to define the state of the application using immutable models (instead of mutable ViewModels used in an MVVM style application)
whilst still leveraging the data binding capabilities of the Uno Platform.

## MVUX in Action

Before digging into the features of MVUX let's see MVUX in action with a simple example. This example shows how weather data can be loaded asynchronously and displayed using data binding.

One of the core concepts behind MVUX is that of an `IFeed`, which is similar in a number of ways to an `IObservable` (for those familiar with [System.Reactive(Rx.NET)](https://github.com/dotnet/reactive)), and represents a sequence of data.  
This example creates an `IFeed<WeatherInfo>` that will return the weather, which includes the temperature, loaded asynchronously from a weather service.

The `WeatherModel` is the **Model** in MVUX (This would be referred to as a ViewModel in MVVM).

```c#
public partial record WeatherModel(IWeatherService WeatherService)
{
    // MVUX code-generator reads this line and generates a cached feed behind the scenes    
    public IFeed<WeatherInfo> CurrentWeather => Feed.Async(WeatherService.GetCurrentWeather);
}
```

> [!Note]  
> For the code generation to work, mark the Models and entities with the `partial` modifier, and have the Feed properties' access modifier as `public`.  
You can learn more about partial classes and methods in [this article](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods).

The entity `WeatherInfo` wraps the temperature, and in a real application would be extended to hold other weather information such as humidity or rainfall:

```c#
public partial record WeatherInfo(double Temperature);
```

This is the weather service, which includes a small delay to simulate calling a service API.

```c#
public interface IWeatherService
{
    ValueTask<WeatherInfo> GetCurrentWeather(CancellationToken ct);
}

public class WeatherService : IWeatherService
{
    public async ValueTask<WeatherInfo> GetCurrentWeather(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), ct);

        return new WeatherInfo(new Random().Next(-40, 40));
    }
}
```

A special control, the `FeedView` is used to display data exposed as an `IFeed`, and provides different styling templates for the various states of the feed, for example loading data, error, no data etc.  
The `FeedView` is data bound to the `CurrentWeather` property which aligns with the `CurrentWeather` property exposed on the `WeatherModel`.  
Within the template, the `Data` property is used to access the current data exposed by the IFeed, which in this case is a proxy for the current `WeatherInfo` instance.  
The `Data` property is bound to the `DataContext` of the `TextBlock`, making it possible to bind the `Text` property of the `TextBlock` to the `Temperature` property.

```xaml
<Page x:Class="WeatherApp.MainPage"
	  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	  xmlns:mvux="using:Uno.Extensions.Reactive.UI">

	<mvux:FeedView Source="{Binding CurrentWeather}">
		<DataTemplate>
			<StackPanel>
				<TextBlock DataContext="{Binding Data}"
						   Text="{Binding Temperature}" />
				<Button Content="Refresh"
						Command="{Binding Refresh}" />
			</StackPanel>
		</DataTemplate>
	</mvux:FeedView>

</Page>

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        DataContext = new BindableWeatherModel(new WeatherService());
    }
}
```

The `Refresh` command is exposed by the `FeedView` and will cause the `IFeed` to requery its source. This is an example of how the model can be **updated** in MVUX.

It's important to note that MVUX uses code-generation to create the proxy types that makes it easier for the **View** to display `IFeed` data.  
This is how MVUX **extends** MVU to work with data binding.

> [!TIP]
> For the full example see [How to create a feed](xref:Overview.Mvux.HowToSimpleFeed)

## MVUX components

MVUX consists of four central components:

- [Model](#model)
- [View](#view)
- [Update](#update)
- [Extended](#extended)

### Model

In MVUX, the application state is represented by a model, which is updated by messages sent by the user in the view.  
The view is in charge of rendering the current state of the model, while any input from the user updates and recreates the model.
The Model in MVUX has some parallels with the 'View Model' in MVVM.

MVUX promotes immutability of data entities. Changes to the data are applied only via update messages sent across from the view to the model, the model responds by performing the updates and the view reflects those changes.

Immutable entities makes raising change notification redundant, enables easier object equality comparison as well as other advantages. The ideal type for creating immutable data-objects is [record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) types. They are immutable by nature and a perfect fit for working with feeds and MVU architecture.  
Record types also feature the special [`with` expression](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/with-expression), which enables recreating the same model with a modified set of properties, while retaining immutability.

### View

The view is the UI layer displaying the current state of the model.  
When a user interacts with the view and request changes to the data, an update message is sent back to the model that updates the data, which is then reflected back on the view.

### Update

Whenever a user makes any sort of input activity that affects the displayed model, instead of specifically notifying changes for those specific properties that were changed, a message is sent back to the model which responds by re-creating the model which the view then updates.  
It's important to note that the view is not being re-created, it rather just updates with the fresh model.

### Extended

MVUX extends the MVU model by harnessing powerful features that make it easier to track asynchronous requests, for example to a remote server, or other long running data-sources, and display the resulting data appropriately.  

The key features Uno Platform provides in the MVUX toolbox are:

- [Metadata](#metadata)
- [Code generation](#code-generation)
- [UI Controls](#ui-controls)
- [Binding engine](#binding-engine)

#### Metadata

MVUX wraps the asynchronous data request with metadata that tells us if the request is still in progress, has failed, or succeeded, and if succeeded, whether the request contains any data entries.

#### Code generation

MVUX comes with a powerful code-generation engine that supplements the user with generated boilerplate code that consists of model and entity proxy classes containing important properties and asynchronous commands. The proxy-classes assist the UI with displaying the data according to its current state provided with the metadata.

To learn more about the power of MVUX Commands, refer to [the Commands topic](xref:Overview.Mvux.Overview#commands) in the Advanced page.  
That page also explains how to [inspect the MVUX generated code](xref:Overview.Mvux.Overview#inspecting-the-generated-code).

#### UI Controls
MVUX also provides a set of UI tools that are specially tailored to automatically read and display that metadata, providing templates for the various states, such as no data, error, progress tracking, as well interactions with the server to enable easy refreshing and saving of the data.

#### Binding engine

It also comes with a powerful binding engine that reads the re-created model and updates the view accordingly.
