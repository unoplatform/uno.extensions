# The `FeedView` control

The `FeedView` control is the heart of MVUX in the View side.  
It seamlessly fulfills everything Feeds and States offer and displays all metadata on the screen providing rich customizability for many modes, as detailed below.

## How to use the FeedView control

To use the `FeedView` you have to add the MVUX UI controls namespace to your XAML file as follows:

```xaml
<Page x:Class="MyMvuxApp.MainPage"
	  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	  xmlns:mvux="using:Uno.Extensions.Reactive.UI">
```

## Common properties

Here are some of the notable properties of the `FeedView`:

### Source

The `Source` property is the entry point of the `FeedView` and it's to be set with a Feed or State object (or their List variant).

> [!TIP]  
> `IFeed<T>`, `IListFeed<T>`, `IState<T>`, and `IListState<T>` all share a common interface - `ISignal<T>`, this the core low-level interface that enables access to the asynchronous data.

Example:

```c#
public IFeed<Person> CurrentContact => ...
```

Then in the XAML:

```xaml
<Page
    ...
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <mvux:FeedView Source="{Binding CurrentContact}">
        <DataTemplate>
            <TextBlock Text="{Binding Data.Name}"/>
        </DataTemplate>
    </mvux:FeedView>
</Page>
```

The `Source` property of the `FeedView` is set to the generated Bindable Model Proxy's `CurrentContact` property, which will asynchronously requests the `Contact` from the service.
To learn more about bindable proxies, read [this]().

In the above example, `Data` is a property of the `FeedViewState` provided via the template, [read on](#data) for details.

### State

You might not have to use the `State` property but it's important to know exists.
This property provides a `FeedViewState` which exposes the current state of the `FeedView`'s underlying data Feed wrapped in its metadata in an accessible way for the View to represent easily using the built-in or the customized templates accordingly.

#### The `FeedViewState` object

The `FeedViewState` exposes the current state of the `FeedView` and its underlying data Feed.

This is also the object that is passed on to the `FeedView`'s templates for binding. So make sure you familiarize yourself with its properties detailed here.

#### `FeedViewState`'s Properties

##### Data

The `Data` property provides access to the value reported by the last message received from the source Feed.  
In other words, the current and most-recent value represented by the Feed.

```xaml
<Page
    ...
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <mvux:FeedView Source="{Binding CurrentContact}">
        <DataTemplate>
            <TextBlock Text="{Binding Data.Name}"/>
        </DataTemplate>
    </mvux:FeedView>
</Page>
```

In the `Text` property binding `Data.Name` of the above example, `Data` is a property of the `FeedViewState` accessible in the template, which gets the most-recent `Contact` from the server, and then we're binding to that `Contact`'s `Name` property.

##### Refresh

This provides a refresh command accessible from within the template which triggers the Feed to refresh itself by reloading the data from the service.

For example:

```xaml
<Page
    ...
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <mvux:FeedView Source="{Binding CurrentContact}">
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Data.Name}" />
                <Button Content="Refresh contact" Command="{Binding Refresh}" />
            </StackPanel>
        </DataTemplate
    </mvux:FeedView>
</Page>
```

The `Button`'s `Command` property binds to the `FeedViewState`'s `Refresh` property which exposes a special asynchronous command that when called, triggers a refresh of the parent Feed (in our example `CurrentContact`, and the data is re-obtained from the server.

> [!NOTE]
> The `FeedViewState` uses its `FeedView`'s `Refresh` property to provide this Command. See [here](#refresh-command-property).

##### Progress

This is a boolean property indicating whether the `FeedView` is currently under progress requesting or refreshing data from the service, and that the current data is 'transient', meaning it's to be replaced shortly with new data once available.

##### Error

This property returns an `Exception` if any occurred during the Feed's interaction with the service.

##### Parent

The `Parent` property gets the `DataContext` of the `FeedView` itself. It provides a bypass to the `FeedViewState`

### RefreshingState

By default, the `FeedView` will display a progress ring while awaiting data on load or refresh:

![](Assets/ProgressRing.gif)

However, in some scenarios you need to disable the default visual state and refreshing template.

This property accepts a value of the `FeedViewRefreshState` enumeration, which supports one the values below which you can set to change its behavior.

- `None` 
- `Default` / `Loading`

## Customizing the FeedView's templates

### ValueTemplate (default Template)

The `ValueTemplate` defines how the `FeedView` would be rendered when its state has concrete data to display, as opposed to no data while loading.  
As mentioned previously, the object the `FeedView` provides the `FeedViewState` as the data item for its `ValueTemplate`.

The `FeedView`'s default content directs to this property, so anything directly added to the `FeedView` element's XAML, is like setting its `ValueTemplate`.  
So

```xaml
<FeedView ...>
    <DataTemplate ... />
</FeedView>
```

is the same as:

```xaml
<FeedView ...>
    <FeedView.ValueTemplate>
        <DataTemplate ... />
    </FeedView.ValueTemplate>
</FeedView>
```

or even:

```xaml
<FeedView Source="..." ValueTemplate="{StaticResource MyFeedViewTemplate}" />
```

### ProgressTemplate

This template will display when the underlying Feed is currently awaiting the asynchronous request to finish.

It's default implementation will show a progress ring:

![](Assets/ProgressRing.gif)

But you can customize that by overriding the `ProgressTemplate`:

```xaml
<FeedView ...>
    <DataTemplate>
        ...
    </DataTemplate>

    <ProgressTemplate>
        <TextBlock Text="Please wait while requesting data..." />
    </ProgressTemplate>
</FeedView>
```


### NoneTemplate

If you set a template to this property, it will show if the data that was returned from the service contained no entries.  
For instance, if an `IFeed<T>` successfully completed its request which resulted in `null` result from the server, this is not considered an `Error`, it's considered a successful result with no data.  
Similarly, when using `IListFeed<T>`, the `NoneTemplate` will also display if the collection is empty, as well as if the result `null`.

Example:

```xaml
<FeedView ...>
    <DataTemplate>
        ...
    </DataTemplate>

    <NoneTemplate>
        <TextBlock Text="No results were found based on your search criteria" />
    </NoneTemplate>
</FeedView>
```

### ErrorTemplate

The `FeedView` will display this template if an Exception was thrown by the underlying service request.

### UndefinedTemplate

This template is displayed when the control loads, before the underlying asynchronous service-request has even been called.  
As soon as the asynchronous request is invoked and awaited, the `FeedView` will switch to its `ProgressTemplate`, until the request has resulted in data, which it will then switch to the `ValueTemplate`, or `NoneTemplate`, depending on the data result.  

> [!TIP]  
> Typically this template will only show for a a very short period - a split second or so, depending on how long it takes for the page and its Model to load.

## Other notable features

### Refresh command property

The `FeedView` provides an asynchronous Command you can bind to, which when executed, will refresh the underlying Feed by re-requesting its data source.

Here's how to utilize it:

```xaml
<FeedView
    x:Name="feedView"
    ...>
    <DataTemplate>
        ...
    </DataTemplate>
</FeedView>

<Button Content="Refresh" Command="{Binding Refresh, ElementName=feedView}" />
```
