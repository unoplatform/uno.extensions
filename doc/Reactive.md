# Reactive

This package is a building block to create a [reactive application](https://en.wikipedia.org/wiki/Reactive_programming).

## API
The main API provided by this package is [`IFeed<T>`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Core/IFeed.cs) which represents a stream of _data_.

Unlike `IObservable<T>` or `IAsyncEnumerable<T>`, the _feed_ streams are specialized to handle business data objects which are expected to be rendered by the UI. A _feed_ is a sequence of _messages_ that contains such immutable _data_ with some metadata like its loading progress and/or errors raised while loading it. Everything is about the _data_, this means that a _feed_ would never fail, it will instead report any error encountered with the data itself, remaining active for future updates.

Each [`Message<T>`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Core/Message.cs) contains the current and the previous state, as well as information about what changed. The means that a message is self-sufficient to get the current data, but also gives enough information to update from the previous state in an optimized way without rebuilding every thing.

## General guidelines

* A basic _feed_ is **state-less**. The state is contained only in _messages_ that are going through a given subscription.
* The _data_ is expected to be immutable. An update of the _data_ implies a new _message_ with an updated instance.
* Like in functional programming, _data_ are **optional**. A message may contain a _data_ (a.k.a. `Some`), the information about the fact that there is no _data_ (a.k.a. `None`) or nothing at all, if for instance the loading failed (a.k.a. `Undefined`).
* Public _feeds_ are expected to be exposed in property getters. A caching system embedded in reactive framework will then ensure to not re-create a new _feed_ on each get.

## Sources
You can build a _feed_ from different sources. Main entry point to do that is using the static class `Feed`:

| Method | Usage |
| ------ | ----- |
| `Async` | Creates a feed from an async method. The loaded data can be refreshed using a `Signal` trigger that will re-invoke the async method. |
| `AsyncEnumerable` | This adapts an `IAsyncEnumerable<T>` into a _feed_ |
| `Create` | This gives you the ability to create you own _feed_ by dealing directly with _messages_. |

## Operators
You can apply some operators directly on any _feed_:

| Method | Usage |
| ------ | ----- |
|`Where` | Applies a predicate on the _data_. Be aware that unlike `IEnumerable`, `IObservable` and `IAsyncEnumerable`, if the predicate returns false, a message with a `None` _data_ will be published. |
|`Select` | Synchronously project each data of the source feed. |
|`SelectAsync` | Asynchronously project each data of the source feed. |

Note : You can use the linq syntax with feeds:
```csharp
public IFeed<string> Value => from value in _values
	where value == 42
	select value.ToString();
```

## Usage in applications

Currently the recommended usage is only in view models.

The reactive framework allows you to design state-less view models, focusing on the presentation logic. You view models only have to request in their constructors the user _ inputs_ that are expected from the view. Inputs could be:
* An `IInput<T>` to get a _data_ from the view (i.e. for 2-way bindings);
* An `ICommandBuilder` for the "trigger" inputs.

Then in order to easily interact with the binding engine in a performant way, a `BindableXXX` class will be generated. It’s this class that will hold the state and which **has to been set as `DataContext` of your page**.

> Note: That bindable alter-ego of a class will be as soon as there is at least one constructor that do have a constructor which as a such _input_ parameter. You can customize that behavior using the `[ReactiveBindable]` attribute.

> Note: In order to be as smooth as possible, public properties of your view model, will also be accessible on the `BindableXXX` as long as there is no name conflict between such properties and any ctor’s _input_. You can also access to the view model itself through the ` Model` property.

### Display some data in the UI (VM to View)

To render a _feed_ in the UI, you only have expose it through a public property:
```csharp
public IFeed<Product[]> Products = Feed.Async(_productService.GetProducts);
```

Then in your page, you should add a `FeedView`:
```xaml
<ext:FeedView Source="{Binding Products}">
	<DataTemplate>
		<ListView ItemsSource={Binding Data} />
	</DataTemplate>
</ext:FeedView>
```

### `Input<T>`

The `Input<T>` represents a _data_ that the end-user can provide from the UI of the application. If the requested `T` is a "complex" type, **and `T` is a `record`**, then fields will de normalized in order to allow direct 2-way data binding on each field.
For instance, if you have a record `Filters` used in a `ProductsViewModel` like this:
```csharp
public record Filters(bool IsInStockOnly, bool IsShippedFromCanada);

public partial record ProductsViewModel(IInput< Filters > Filters);
```

Then in the view you can directly data-bind on each property:
```xaml
<Button Content="Filter">
	<Button.Flyout>
		<Flyout>
			<StackPanel>
				<ToggleSwitch Header="In stock only" IsOn="{Binding Filters.IsInStockOnly, Mode=TwoWay}" />
				<ToggleSwitch Header="In stock only" IsOn="{Binding Filters.IsShippedFromCanada, Mode=TwoWay}" />
			</StackPanel>
		</Flyout>
	</Button.Flyout>
</Button>
```

This is however not always desirable, for instance to data-bind the `SelectedItem` of a `Selector`. So, you can disable that behavior by adding a `[Value]` attribute on the _input_ parameter.
```csharp
public partial record ProductsViewModel([Value ]IInput< Filters > Filters);
```

### `ICommandBuilder`

This input represents a "trigger" from the end-user. It will be materialized as an [`ICommand`](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.input.icommand) that can then be data-bind to a `Button`.

You can also request a generic `ICommandBuilder<T>` where `T` is the expected type of the `CommandParameter`.

In the constructor of your view model, you then have to configure what action should be taken when the user triggers the command:
```csharp
public class MovieViewModel(
	IMovieService movieService,
	ICommandBuilder parameterLessAddToFavorite, 
	ICommandBuilder<string> parameterizedAddToFavorite)
{
	parameterLessAddToFavorite
		.Given(MyEntity) // Command will be CanExecute = false as long as the Movie feed didn’t get a value.
		.When(movie => !movie?.IsFavorite ?? false) // Configures the CanExecute
		.Then(movieService.AddToFavorite); // The action.

	// If the Movie has been data-bound to the CommandParameter
	parameterizedAddToFavorite
		.When(movie => !movie?.IsFavorite ?? false) // Configures the CanExecute
		.Then(movieService.AddToFavorite); // The action.
}

public IFeed<Movie> Movie = Feed.Async(_movieService.Load);

```

> Note: `Given` and `When` are booth optional, but the execute action defined in `Then` is required. If omitted, the command will remain to `CanExecute = false` (i.e. the `Button` will be disabled) 

## Testing

In order to test your reactive application, you should install the `Uno.Extensions.Reactive.Testing` package in you test project.

Make your test class inherit from `FeedTests`, then in your tests methods you can use the `.Record()` extensions method on the test you want to test. It will subscribe to your feed and persists all received messages. Then you can assert the expected messages using the fluent assertions:

```csharp
[TestMethod]
public async Task When_ProviderReturnsValueSync_Then_GetSome()
{
	var sut = Feed.Async(async ct =>
	{
		await Task.Delay(500, ct);
		return 42;
	});
	using var result = await sut.Record();

	result.Should().Be(r => r
		.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
		.Message(Changed.Data, 42, Error.No, Progress.Final)
	);
}
```

You define in the `Message` each axis (`Data` / `Error` / `Progress`) you want to validate. You can also define which axis are expected to have changed (`Changed`).

> Note: When developing a new _feed_, we recommend that you systematically validate all axes.

