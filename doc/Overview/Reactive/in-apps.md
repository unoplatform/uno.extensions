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

## Commands
The generated bindable counterpart of a class will automatically re-expose public methods that have 0 or 1 parameter and an optional `CancellationToken` as `ICommand`.
The parameter will be fulfilled using the `CommandParameter` property.

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
