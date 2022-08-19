---
uid: Overview.Reactive.InApps
---
# Usage in applications of feeds

The recommended use for feeds is only in view models.

The reactive framework allows you to design state-less view models, focusing on the presentation logic. 
Your view models only have to request in their constructors the user _ inputs_ that are expected from the view. Inputs could be:
* An `IInput<T>` to get _data_ from the view (e.g. for 2-way bindings);
* An `ICommandBuilder` for the "trigger" inputs.

In order to easily interact with the binding engine in a performant way, a `BindableXXX` class is automatically generated. It is this class that will hold the state and which **has to be set as `DataContext` of your page**.

> [!NOTE]
> The bindable counterpart of a class will be created when the class name ends with "ViewModel".
> You can customize that behavior using the `[ReactiveBindable]` attribute.

> [!NOTE]
> For easier View Model creation, public properties will also be accessible on the `BindableXXX` class. 
> You can also access the view model itself through the `Model` property.

## Display some data in the UI (VM to View)

To render a _feed_ in the UI, you only have to expose it through a public property:
```csharp
public IFeed<Product[]> Products = Feed.Async(_productService.GetProducts);
```

Then in your page, you can add a `FeedView`:
```xml
<Page 
	x:Class="MyProject.MyPage"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:uer="using:Uno.Extensions.Reactive.UI">

<uer:FeedView Source="{Binding Products}">
	<DataTemplate>
		<ListView ItemsSource="{Binding Data}" />
	</DataTemplate>
</uer:FeedView>
```

## Refreshing a data

The `FeedView` exposes a `Refresh` command directly in the data context of its content.
You can use this command to trigger a refresh from the view, like a "pull to refresh".

```xml
<Page 
	x:Class="MyProject.MyPage"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:uer="using:Uno.Extensions.Reactive.UI">

<uer:FeedView Source="{Binding Products}">
	<DataTemplate>
		<Grid>
			<Grid.RowDefinitions>
				<RowDefinition Height="Auto" />
				<RowDefinition />
			<Grid.RowDefinitions>

			<Button Grid.Row="0" Content="Refresh" Command="{Binding Refresh}" />
			<ListView Grid.Row="1" ItemsSource="{Binding Data}" />
		</Grid>
	</DataTemplate>
</uer:FeedView>
```

## Pagination

When your source supports pagination (like `ListFeed.Paginated`), you can data-bind the collection exposed by the generated view model to a `ListView` to automatically enable pagination.

```xml
<uer:FeedView Source="{Binding Items}">
	<DataTemplate>
		<ListView ItemsSource="{Binding Data}" />
	</DataTemplate>
</uer:FeedView>
```

The collection exposed for binding by the generated view models also exposes some [extended properties](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive.UI/Presentation/Bindings/Collections/Facets/BindableCollectionExtendedProperties.cs), 
so you can enhance the UX of the pagination, like display a loading indicator while loading the next page.

```xml
<uer:FeedView Source="{Binding Items}">
	<DataTemplate>
		<ListView
			Grid.Row="1"
			ItemsSource="{Binding Data}">
			<ListView.Footer>
				<ProgressBar
					Visibility="{Binding Data.ExtendedProperties.IsLoadingMoreItems}"
					IsIndeterminate="{Binding Data.ExtendedProperties.IsLoadingMoreItems}"
					HorizontalAlignment="Stretch" />
			</ListView.Footer>
		</ListView>
	</DataTemplate>
</uer:FeedView>
```


## Commands
The generated bindable counterpart of a class will automatically re-expose as `ICommand` public methods that are compatible (cf. "general rules" below).

For instance, in your ViewModel:

```csharp
public async ValueTask Share() 
{

}
```

This will be exposed into an `ICommand` that can be data-bound to the `Command` property of a `Button`

```xml
<Button Command="{Binding Share}" Content="Share" />
```

By default, if the method has a parameter `T myValue` and there is a property `Feed<T> MyValue` on the same class (matching type and name), that parameter will automatically be filled from that feed.

For instance, in your ViewModel:

```csharp
public IFeed<string> Message { get; }

public async ValueTask Share(string message) 
{

}
```

Can be used with or without any `CommandParameter` from the view:

```xml
<!-- Without CommandParameter -->
<!-- 'message' arg in the 'Share' method will be the current value of the Message _feed_ -->
<Button Command="{Binding Share}" Content="Share" />

<!-- With CommandParameter -->
<!-- 'message' arg in the 'Share' method will be "hello world" -->
<Button Command="{Binding Share}" CommandParameter="hello world" Content="Share" />
```

You can also use both "feed parameters" and "view parameter" (i.e. the value of the `CommandParameter`):

```csharp
public IFeed<MyEntity> Entity { get; }

public async ValueTask Share(MyEntity entity, string origin) 
{
}
```

```xml
<Button Command="{Binding Share}" CommandParameter="command_bar" Content="Share" />
```

General rules for methods to be re-exposed as commands are:
* At most one paramater that cannot be resolved from a _Feed_  property in your VM (a.k.a the `CommandParameter`);
* At most one `CancellationToken`;
* Method can be sync, or async

> [!NOTE]
> The automatic parameters resolution be configured using attributes:
> - `[ImplicitFeedCommandParameters(is_enabled)]` on assembly or class to enable or disable the implicit parameters resolution.
> - `[FeedParameter("MyEntity")]` to explicit the property to use for a given parameter, e.g.
>      ```csharp
>      public IFeed<string> Message { get; }
>      
>      public async ValueTask Share([FeedParameter(nameof(Message))] string msg)
>      {
>      }
>      ```
