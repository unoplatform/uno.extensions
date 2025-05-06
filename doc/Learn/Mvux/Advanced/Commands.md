---
uid: Uno.Extensions.Mvux.Advanced.Commands
---

# Commands

This page covers the following topics:

- [Commands](#commands)
  - [What are commands](#what-are-commands)
  - [Command generation](#command-generation)
    - [Implicit command generation](#implicit-command-generation)
      - [Basic commands](#basic-commands)
      - [Using the CommandParameter](#using-the-commandparameter)
    - [Additional Feed parameters](#additional-feed-parameters)
      - [Command generation rules](#command-generation-rules)
    - [Configuring command generation using attributes](#configuring-command-generation-using-attributes)
      - [ImplicitCommands attribute](#implicitcommands-attribute)
      - [Command attribute](#command-attribute)
      - [ImplicitFeedCommandParameter attribute](#implicitfeedcommandparameter-attribute)
      - [FeedParameter attribute](#feedparameter-attribute)
    - [Using XAML behaviors to execute a command when an event is raised](#using-xaml-behaviors-to-execute-a-command-when-an-event-is-raised)
    - [Explicit command creation](#explicit-command-creation)
      - [Command.Async factory method](#commandasync-factory-method)
      - [Create \& Create\<T\>](#create--createt)
      - [Example](#example)

## What are commands

Commands provide a way to expose code within an application that performs an action (typically a method) so that it can be invoked by a user interaction with a UI element, such as clicking a `Button`.

For example the `MainModel` includes a public `Save` method:

```csharp
public partial record MainModel()
{
    public void Save() { ... }
}
```

The `Save` method will be exposed as an `IAsyncCommand` (an MVUX interface that extends the `ICommand` interface) on the generated ViewModel for the `MainModel`:

```csharp
public partial class MainViewModel
{
    public IAsyncCommand Save { get; }
}
```

The `Command` property on a `Button` can be bound to the `Save` command on the `MainViewModel`. When the `Button` is clicked, the `Save` command will be executed, which will invoke the `Save` method on `MainModel`.

```xml
<Button Command="{Binding Save}">Save</Button>
```

During the execution of the `Save` method, the `Button` will automatically be disabled, making it clear that the method is running.

## Command generation

By default, MVUX will generate a command in the ViewModel for each public method in a Model via [**Implicit command generation**](#implicit-command-generation). This behavior can be customized using attributes, or alternatively, can be disabled in favor of [**Explicit command creation**](#explicit-command-creation).

### Implicit command generation

#### Basic commands

By default, a command property will be generated in the ViewModel for each method on the Model that has no return value or is asynchronous (e.g. returns `ValueTask` or `Task`).

The asynchronous method on the Model may take a single `CancellationToken` parameter, which will be cancelled if the ViewModel is disposed of whilst commands are running. Although a `CancellationToken` parameter is not mandatory, it's a good practice to add one, as it enables the cancellation of the asynchronous operation.

For example, if the Model contains a method in any of the following signatures:

1. A method without a return value:

    ```csharp
    public void DoWork();
    ```

1. A method returning `ValueTask`, with a `CancellationToken` parameter:

    ```csharp
    public ValueTask DoWork(CancellationToken ct);
    ```

1. A method returning `ValueTask`, without a `CancellationToken` parameter:

    ```csharp
    public ValueTask DoWork();
    ```

a `DoWork` command will be generated in the ViewModel:

```csharp
public IAsyncCommand DoWork { get; }
```

In some scenarios, you may need to use the method only, without a command generated for it. You can use the [`ImplicitCommands` attribute](#implicitcommands-attribute) to switch off or back on command generation for certain methods, classes, or assemblies. For example, in this code, the `ImplicitCommand` attribute has been used to disable the creation of the command for the `DoWork` method.

```csharp
[ImplicitCommand(false)]
public ValueTask DoWork();
```

When command generation is switched off, the methods under the scope which has been switched off will be generated in the ViewModel as regular methods rather than as commands, meaning that they are still available to be data-bound or invoked via the ViewModel.

#### Using the CommandParameter

An additional parameter can be added to the method on the Model, which is then assigned with the value of the `CommandParameter` received from the View. For example, when a `Button` is clicked, the [`Button.CommandParameter`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.primitives.buttonbase.commandparameter) value will be passed to the command.
The `CommandParameter` value is first passed to the `CanExecute` method on the command to determine if the command can be executed. The command checks both that the `CommandParameter` value can be cast to the correct type, and that there's not already an invocation of the command for that `CommandParameter` value. Assuming `CanExecute` returns true, when the `Button` is clicked the `Execute` method on the command is invoked which routes the call, including the `CommandParameter` value (correctly cast to the appropriate type), to the method on the Model.

In this example the Model defines a method, `DoWork`, that accepts a parameter, `param`:

```csharp
public void DoWork(double param) { ... }
```

The corresponding command in the ViewModel looks the same as before. However, the implementation, which you can inspect in the generated code, includes logic to validate the type of the `CommandParameter`, and subsequently passes the cast value to the `DoWork` method on the Model:

```csharp
public IAsyncCommand DoWork { get; }
```

The command can be consumed in the View by setting the `CommandParameter` on the Button. In this case, the value is data bound to the `Value` property on the `Slider`:

```xml
<Slider x:Name="slider" Minimum="1" Maximum="100"/>
<Button Command="{Binding DoWork}" CommandParameter="{Binding Value, ElementName=slider}"/>
```

If the `CommandParameter` is null, or if its type doesn't match the parameter type of the method, the button will remain disabled.
On the other hand, in case the `CommandParameter` is specified in the View but the method in the Model doesn't have a parameter, the View's `CommandParameter` value will just be disregarded.

It's also still recommended to include a `CancellationToken`, which will allow the method to be cancelled. For example, the preferred definition for the `DoWork` method would be asynchronous and include the `CancellationToken` parameter.

```csharp
public ValueType DoWork(double param, CancellationToken ct) { ... }
```

### Additional Feed parameters

The current value of any Feed can be materialized in an asynchronous method by awaiting the Feed:

```csharp
public IFeed<int> MyFeed = ...;

public async ValueTask DoWork()
{
    int myFeedValue = await MyFeed;
}
```

However, MVUX commands also enable consuming the current value of Feed properties in the Model, using parameter names in the Model method, with a name and type matching the Feed property.
The name matching is NOT case-sensitive.

For example:

```csharp
public IFeed<int> CounterValue => ...

public void ResetCounter(int counterValue) { ... }
```

When the command is executed and the `ResetCounter` method is invoked, because the parameter `counterValue` matches a feed property in the Model by type and name, this parameter will be materialized with the actual most recent value from the Feed. Note that the type of the method parameter is the generic parameter type of the feed (in this case `int`), rather than the type of the feed property (so not `IFeed<int>`).

As before, it's recommended that the method be made asynchronous and include a `CancellationToken` parameter.

This behavior can be configured using the [`FeedParameter`](#feedparameter-attribute) and [`ImplicitFeedCommandParameter`](#implicitfeedcommandparameter-attribute) attributes. For example, the implicit resolution of feed parameters has been disabled and an explicit feed parameter has been specified for the newValue parameter for the `ResetCounter` method.

```csharp
public IFeed<int> CounterValue => ...

[ImplicitFeedCommandParameter(false)]
public void ResetCounter([FeedParameter(nameof(CounterValue))] int newValue) { ... }
```

#### Command generation rules

Here is a recap of the rules the Model method must comply with for an `IAsyncCommand` wrapper to be generated for it:

- The method may be synchronous (`void`) or asynchronous (`ValueTask`/`Task`)
- Any return values of the method (if any) will be discarded.
- The method may have one `CancellationToken` parameter or none.
- The method may have multiple parameters that can be resolved from feeds (see [Additional Feed parameters](#additional-feed-parameters) above).
- The method may have one additional parameter, other than parameters resolved from Feeds or `CancellationToken`, to be provided from the View's `CommandParameter` property.

### Configuring command generation using attributes

#### ImplicitCommands attribute

By default, implicit command generation is enabled when the MVUX package is referenced. That means that any method in the Model that matches the [command generation rules](#command-generation-rules) will have an accompanying command wrapper generated for it.

However, you may turn implicit command generation on or off for a specific class or assembly. Conversely, when implicit command generation has been disabled for an assembly, it can be enabled for specific classes.

Enabling, or disabling, implicit command generation can be achieved using the [`ImplicitCommands`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Config/ImplicitCommandsAttribute.cs) attribute.

Here is an example of disabling implicit command generation throughout an entire assembly:

```csharp
[assembly:ImplicitCommands(false)]
```

and then enabling implicit command generation for a single class

```csharp
[ImplicitCommands(true)]
public partial record MyModel(...)
```

#### Command attribute

In addition to the [`ImplicitCommands`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Config/ImplicitCommandsAttribute.cs) attribute which controls implicit command generation of a class or assembly, you can explicitly enable or disable the command generation for an individual method using the [`Command`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Presentation/Commands/CommandAttribute.cs) attribute. When command generation is disabled for a method, that method will still be generated in the ViewModel for the Model as a pass-through to the original method on the Model.

Assuming the `ImplicitCommands` attribute is used to disable implicit command generation for an assembly, or class, a command can be generated for the method by decorating it with the `Command` attribute (with its default value `true`):

```csharp
[assembly:ImplicitCommands(false)]

[Command]
public async ValueTask DoWork()
{
}
```

Or on the contrary, if implicit command generation is enabled for an assembly or class, using the `Command` attribute will prevent the generation of a command. Instead, a regular method, in this case called `DoWork` will be created on the ViewModel.

```csharp
[assembly:ImplicitCommands(true)]

[Command(false)]
public async ValueTask DoWork()
{
}
```

The `Command` attribute has precedence over the `ImplicitCommands` attribute. If a method is decorated with this attribute, whether or not a command is generated will depend solely on the `Command` attribute value.

One example of when you'd want to switch off command generation is if you are using [x:Bind Event Binding](https://learn.microsoft.com/windows/uwp/xaml-platform/x-bind-markup-extension#event-binding), you will want to opt-out from command generation for the bound method so that you can bind directly to the method on the ViewModel from the View.

In this example, a Save method with the same signature as the Save method on the Model will be created on the ViewModel.

```csharp
[Command(false)]
public async ValueTask Save () { ... }
```

The `Click` event on the `Button` can then be bound using x:Bind to the Save method on the ViewModel.

```xml
<Button Click="{x:Bind Save}">Save</Button>
```

#### ImplicitFeedCommandParameter attribute

You can opt-in or opt-out of implicit matching of Feeds and command parameters by decorating the current assembly or class with the [`ImplicitFeedCommandParameters`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Config/ImplicitFeedCommandParametersAttribute.cs) attribute:

```csharp
[assembly:ImplicitFeedCommandParameter(false)]

[ImplicitFeedCommandParameter(true)]
public partial record MyModel
```

#### FeedParameter attribute

You can also explicitly match a parameter with a Feed even if the names don't match. Decorate the parameter with the [`FeedParameter`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Presentation/Commands/FeedParameterAttribute.cs) attribute to explicitly match a parameter with a Feed:

```csharp
public IFeed<string> Message { get; }

public async ValueTask Share([FeedParameter(nameof(Message))] string msg) { ... }
```

`ImplicitFeedCommandParameter` and `FeedParameter` attributes can also be nested to enable or disable specific scopes in the app. The `FeedParameter` setting has priority over `ImplicitFeedCommandParameter`, so parameters decorated with `FeedParameter` will explicitly indicate that the parameter is to be fulfilled by a Feed.

### Using XAML behaviors to execute a command when an event is raised

You can also utilize MVUX's generated commands and invoke them when an event is raised.
This can be achieved with the [XamlBehaviors](https://github.com/unoplatform/Uno.XamlBehaviors) library (Nuget packages [Uno.Microsoft.Xaml.Behaviors.Interactivity.WinUI](https://www.nuget.org/packages/Uno.Microsoft.Xaml.Behaviors.Interactivity.WinUI) and [Uno.Microsoft.Xaml.Behaviors.WinUI.Managed](https://www.nuget.org/packages/Uno.Microsoft.Xaml.Behaviors.WinUI.Managed)).

For example, if you want to capture a `TextBlock` being double-tapped, you can add in the Model a method to be invoked on that event:

```csharp
public void TextBlockDoubleTapped(string text) { ... }
```

The `TextBlockDoubleTapped` method will be generated as a command, which you can then use XAML behaviors to invoke when the `TextBlock`'s `DoubleTapped` event occurs. You can also pass its command parameter to the method (although you can choose to omit it):

```xml
<Page
    ...
    xmlns:interactivity="using:Microsoft.Xaml.Interactivity"
    xmlns:interactions="using:Microsoft.Xaml.Interactions.Core">

    <TextBlock x:Name="textBlock" Text="Double-tap me">
        <interactivity:Interaction.Behaviors>
            <interactions:EventTriggerBehavior EventName="DoubleTapped">
                <interactions:InvokeCommandAction
                    Command="{Binding TextBlockDoubleTapped}"
                    CommandParameter="{Binding Text, ElementName=textBlock}"/>
            </interactions:EventTriggerBehavior>
        </interactivity:Interaction.Behaviors>
    </TextBlock>
</Page>
```

When the `TextBlock` is double-tapped (or double-clicked), the `TextBlockDoubleTapped` command which is generated in the ViewModel will be executed, and in turn, the `TextBlockDoubleTapped` method in the Model will be invoked. The text 'Double-tap me' will be passed in as the command parameter.

### Explicit command creation

Adding commands via code generation is sufficient enough to cover most scenarios. However, sometimes you may need to have more control over the command creation, which is where explicit command creation is useful.

Commands can be created manually using the static class [`Command`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Presentation/Commands/Command.cs), which provides factory methods for creating commands.

#### Command.Async factory method

The `Async` utility method takes an `AsyncAction` callback as its parameter. An `AsyncAction` refers to an asynchronous method that has a `CancellationToken` as its last parameter (preceded by any other parameters), and returns a `ValueTask`.

```csharp
public ICommand MyCommand => Command.Async(async(ct) => await PingServer(ct));
```

In the above example, `PingServer` is of the following signature:

```csharp
ValueTask PingServer(CancellationToken ct);
```

The `Command.Async` method will create a command that when executed will run the `PingServer` method asynchronously.

#### Create & Create\<T>

You can use the `Command.Create` factory methods to create a command. The `Command.Create` provides an `ICommandBuilder` parameter, which you can use to configure the command in a fluent API fashion.
This API is intended for Uno Platform's internal use but can be useful if you need to create custom commands.

`ICommandBuilder` provides the three methods below.

- ##### Given

  This method initializes a command from a Feed (or a State). The command will be triggered whenever a new value is available to the Feed. It takes a single `IFeed<T>` parameter.

  ```csharp
  public IFeed<int> PageCount => ...

  public IAsyncCommand MyCommand => Command.Create(builder => builder.Given(PageCount));
  ```

- ##### When

  Defines the 'can execute' of the command. It accepts a predicate of `T`, where `T` is the type the command has been created with. When this is configured, the command will be executed only if the condition is true.

  ```csharp
  public IAsyncCommand MyCommand => Command.Create<int>(builder => builder.When(i => i > 10));
  ```

  In the above example, the predicate passed into the `When` method will be executed when the UI wants to determine if the command can be executed, which will only happen when the command parameter is greater than 10.

- ##### Then

  This method sets the asynchronous callback to be invoked when the Command is executed. If there's a preceding parameter setting (via `Given` or `When`), it will be generic.

  ```csharp
  public IAsyncCommand MyCommand => Command.Create(builder => builder.Then(async ct => await ExecuteMyCommand(ct)));

  public ValueTask ExecuteMyCommand(CancellationToken ct)
  {
      ...
  }
  ```

  You can use the `Execute` instead of `Then`. These are just aliases of each other.

#### Example

Here's a complete example of how `MyCommand` is defined in the Model.

```csharp
public IAsyncCommand MyCommand =>
    Command.Create(builder =>
        builder
        .Given(CurrentPage)
        .When(currentPage => currentPage > 0)
        .Then(async (currentPage, ct) => await NavigateToPage(currentPage, ct)));

public IFeed<int> CurrentPage => ...

public ValueTask NavigateToPage(int currentPage, CancellationToken ct) { ... }
```

As with implicitly created commands, the `Command` property on UI controls, such as a `Button`, can be bound to the command, `MyCommand`. However, since commands created using the fluent API are not replicated on the ViewModel, the binding expression has to include the ViewModel's `Model` property to access the Model instance.

```xml
<Button Command="{Binding Model.MyCommand}" Content="Execute my command" />
```

In the above example (in the Model), when the button is clicked, the `Given` section will be materialized with the most recent value of the `CurrentPage` Feed, it will be then evaluated with the predicate provided in the `When` call, and if its value is greater than 0, it will be passed on to `Then`, and `NavigateToPage` will be called with the `CurrentPage` Feed value passed on.

This is a diagram detailing the factory methods in the Command class:

![A class diagram of MVUX command builder inheritance structure](../Assets/CommandsDiagram.jpg)

Below is a list of all methods and their signatures:

Methods of Command class:

Method name    | Signature
---------------|-----------------------------------------------------------------------------------------------------------------
**Async**      | public static IAsyncCommand Async(AsyncAction execute, [CallerMemberName] string? name = null)
**Create**     | public static IAsyncCommand Create(Action<ICommandBuilder> build)
**Create\<T>** | public static IAsyncCommand Create<T>(Action<ICommandBuilder<T>> build, [CallerMemberName] string? name = null)

Methods of `ICommandBuilder`:

Method name    | Signature
---------------|-------------------------------------------------------
**Given**      | public ICommandBuilder<T> Given<T>(IFeed<T> parameter)
**Then**       | public void Then(AsyncAction execute)

Methods of `ICommandBuilder<T>`:

Method name    | Signature
---------------|--------------------------------------------------------------------
**When**       | public IConditionalCommandBuilder<T> When(Predicate<T> canExecute)
**Then**       | public void Then(AsyncAction<T> execute)

Methods of `IConditionalCommandBuilder<T>`:

Method name    | Signature
---------------|------------------------------------------
**Then**       | public void Then(AsyncAction<T> execute);

`AsyncAction` refers to an action with a variable number of parameters (up to 16), with its last parameter being a `CancellationToken`, and returns a `ValueTask`:

```csharp
public delegate ValueTask AsyncAction<in T1, T2...>(T1 t1, T2 t2 ... , CancellationToken ct);
```
