---
uid: Uno.Extensions.Mvux.FeedView
---

# The `FeedView` control

The `FeedView` control is one of the main ways to consume Feeds and States within an application. It uses different visual states to control what is displayed on the screen depending on the state of the underlying `IFeed`, or `IState`.

## How to use the `FeedView` control

To use the `FeedView`, you have to add the `Uno.Extensions.Reactive.UI` namespace to your XAML file as follows:

```xml
<Page
    x:Class="MyMvuxApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">
```

## Common properties

Here are some of the notable properties of the `FeedView`:

### Source

The `Source` property is the entry point of the `FeedView`, and it's to be set with an `IFeed` or `IState` object (or their list variant).

Example:

```csharp
public partial record MainModel {
    public IFeed<Person> CurrentContact => ...
}
```

Then, in the XAML:

```xml
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

The `Source` property of the `FeedView` is data bound to the `CurrentContact` property on the ViewModel (which will correlate to the `IFeed` property with the same name on the Model).

In the above example, [`Data`](#data) is a property of the `FeedViewState` instance that the `FeedView` creates from the `IFeed` and sets as the `DataContext` for the various templates.

### State

The `State` property returns a `FeedViewState`, which exposes the current state of the `FeedView`'s underlying data Feed. It's unlikely that you'll need to access the `State` property directly since the `FeedViewState` is automatically set as the `DataContext` of the various templates.

#### The `FeedViewState` object

The `FeedViewState` exposes the current state of the `FeedView` and its underlying data Feed.

This is also the object that is passed on to the `FeedView`'s templates for binding. So make sure you familiarize yourself with its properties detailed here.

#### `FeedViewState`'s Properties

##### Data

The `Data` property provides access to the value reported by the last message received from the source Feed, in other words, the current Feed value.

```xml
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

In the `Text` property binding `Data.Name` of the above example, `Data` is a property of the `FeedViewState` accessible in the template, which gets the most recent `Contact` from the server, and then we're binding to that `Contact`'s `Name` property.

##### Refresh

This provides a refresh command accessible from within the template, which triggers the Feed to refresh itself by reloading the data from the service.

For example:

```xml
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

The `Button`'s `Command` property binds to the `FeedViewState`'s `Refresh` property, which exposes a special asynchronous command that, when called, triggers a refresh of the parent feed, in our example, `CurrentContact`, and the data is re-obtained from the server.

It's also possible to bind the `Refresh` command to a Control outside the `FeedView` template, as shown below:

```xml
<Page
    ...
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">

    <mvux:FeedView x:Name="feedView" Source="{Binding CurrentContact}">
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Data.Name}" />
            </StackPanel>
        </DataTemplate
    </mvux:FeedView>

    <Button Content="Refresh" Command="{Binding Refresh, ElementName=feedView}" />
</Page>
```

##### Progress

This is a `boolean` property indicating whether the `FeedView` is currently requesting or refreshing data from the service, and that the current data is 'transient', meaning it's to be replaced shortly with new data once available.

##### Error

This property returns an `Exception` if any occurred during the Feed's interaction with the service.

##### Parent

The `Parent` property gets the `DataContext` of the `FeedView` itself. It provides a bypass to the `FeedViewState`

### RefreshingState

By default, the `FeedView` will display a progress ring while awaiting data on load or refresh:

![A running progress-ring control](Assets/ProgressRing.gif)

However, in some scenarios, you need to disable the default visual state and progress template.

This property accepts a value of the `FeedViewRefreshState` enumeration, which supports one of the values below, which you can set to change its behavior.

- `None`
- `Default` / `Loading`

## Customizing the FeedView's templates

### ValueTemplate (default Template)

The `ValueTemplate` defines how the `FeedView` would be rendered when its state has concrete data to display, as opposed to no data while loading.
As mentioned previously, the `FeedView` provides the `FeedViewState` as the data item for its `ValueTemplate`.

The `FeedView`'s default content directs to this property, so anything directly added to the `FeedView` element's XAML, is like setting its `ValueTemplate`.

So

```xml
<FeedView ...>
    <DataTemplate ... />
</FeedView>
```

is the same as:

```xml
<FeedView ...>
    <FeedView.ValueTemplate>
        <DataTemplate ... />
    </FeedView.ValueTemplate>
</FeedView>
```

or even:

```xml
<FeedView Source="..." ValueTemplate="{StaticResource MyFeedViewTemplate}" />
```

### ProgressTemplate

This template will display when the underlying Feed is currently awaiting the asynchronous request to finish.

Its default implementation will show a progress ring:

![A running progress-ring control](Assets/ProgressRing.gif)

But you can customize that by overriding the `ProgressTemplate`:

```xml
<FeedView ...>
    <DataTemplate>
        ...
    </DataTemplate>

    <ProgressTemplate>
        <DataTemplate>
            <TextBlock Text="Please wait while requesting data..." />
        </DataTemplate>
    </ProgressTemplate>
</FeedView>
```

### NoneTemplate

Setting a template to this property will show when the data returned from the service contains no entries. For instance, if an `IFeed<T>` completed its request successfully with the server returning a `null` result, it's important to note that this is not considered an error. Instead, it's still considered a successful result with no data. Similarly, when using `IListFeed<T>`, the `NoneTemplate` will also display if the collection is empty, or if the result is `null`.

Example:

```xml
<FeedView ...>
    <DataTemplate>
        ...
    </DataTemplate>

    <NoneTemplate>
        <DataTemplate>
            <TextBlock Text="No results were found based on your search criteria" />
        </DataTemplate>
    </NoneTemplate>
</FeedView>
```

### ErrorTemplate

The `FeedView` will display this template if the underlying asynchronous operation throws an Exception.

Example:

```xml
<FeedView ...>
    <DataTemplate>
        ...
    </DataTemplate>

    <ErrorTemplate>
        <DataTemplate>
            <TextBlock Text="An error has ocurred while loading the data..." />
        </DataTemplate>
    </ErrorTemplate>
</FeedView>
```

### UndefinedTemplate

This template is displayed when the `FeedView` loads before the underlying asynchronous operation has been called.
As soon as the asynchronous operation is invoked and awaited, the `FeedView` will switch to its `ProgressTemplate` until the operation results in data, at which point it will switch to the `ValueTemplate` or `NoneTemplate`, depending on the data result.

Typically, this template will only show for a very short period - a split second or so, depending on how long it takes for the page and its Model to load.

```xml
<FeedView ...>
    <DataTemplate>
        ...
    </DataTemplate>

    <UndefinedTemplate>
        <DataTemplate>
            <TextBlock Text="Uno Platform FeedView" />
        </DataTemplate>
    </UndefinedTemplate>
</FeedView>
```
