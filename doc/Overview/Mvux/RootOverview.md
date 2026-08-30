---
uid: Uno.Extensions.Reactive.Overview.Short
---

# MVUX Framework Overview

In the realm of application development, a fundamental aspect revolves around managing the application state, encompassing data fetched from APIs and user input states. Various conventional approaches exist for effective state management.

Model-View-Update-eXtended (MVUX) combines the strengths of both Model-View-Update (MVU) and Model-View-ViewModel (MVVM) architectures. This document presents an overview of the key advantages MVUX offers, along with an example demonstrating its efficiency for building applications.

## Key Advantages of MVUX
### 1. Reactive Application Development

MVUX employs reactive programming, automating UI updates in response to changes in application state, ensuring a dynamic and responsive user experience with simplified code.

### 2. Immutable Models for Stability

MVUX simplifies the creation of immutable models that maintain coherent states in multi-threaded environments. This approach enhances stability and predictability in complex application scenarios.

### 3. Asynchronous Everywhere

MVUX takes care of executing all non-UI-related operations on a background thread without requiring developers to write boilerplate code, such as implementing `INotifyPropertyChanged`. With native support for common data states, users are informed of background data fetching or error occurrences, contributing to a seamless user experience.

### 4. Code Testability

The MVUX framework promotes test-driven development by decoupling presentation logic from the view. This separation facilitates the testing of code components, ensuring robust and reliable applications.

### 5. Declarative Code Friendly

Express your code's intentions more clearly with MVUX's declarative approach. The framework encourages writing code in a declarative fashion, enhancing readability and maintainability.

## Quick Start: Building a Counter App

To illustrate the simplicity and power of MVUX, let's explore building a basic counter application. This entails considering two main elements:

### The view
This component, constructed in XAML or C# markup, is a single-instance mutable object dedicated solely to manage the user interface.


```xml
<StackPanel VerticalAlignment="Center">
	<TextBox
		Margin="12"
		HorizontalAlignment="Center"
		PlaceholderText="Step Size"
		Text="{Binding Step, Mode=TwoWay}"
		TextAlignment="Center" />

	<TextBlock
		Margin="12"
		HorizontalAlignment="Center"
		TextAlignment="Center">
		<Run Text="Counter: " /><Run Text="{Binding Count}" />
	</TextBlock>

	<Button
		Margin="12"
		HorizontalAlignment="Center"
		Command="{Binding IncrementCommand}"
		Content="Increment Counter by Step Size" />
</StackPanel>
```

### The model
The model, a single-instance immutable object, it holds the business state utilizing `IState<T>` properties, where `T` is also immutable but multi-instances. The model seamlessly interacts with user inputs through 2-way bindings or commands, ensuring a clear separation between business logic and UI concerns.

```csharp
internal partial record MainModel
{
    public IState<int> Count => State.Value(this, () => 0);

    public IState<int> Step => State.Value(this, () => 1);

    public ValueTask IncrementCommand(int Step, CancellationToken ct)
            => Count.Update(c => c + Step, ct);
}
```

Key Features in this example:

* **Reactive Properties**: `Counter` and `Step` are exposed as plain binding-friendly integer properties to the view, supporting two-way data binding. Updates are reported on the UI thread for the view and on the background thread for the model.

* **Command Handling**: The `IncrementCommand` method is automatically exposed as an `ICommand`  that can be data bound to any element in the view that has a `Command` property, such as `Button`. Additionally, `ICommand.CanExecute` is updated during the execution of the method, so that the state of the data bound element is correct.

* **Parameter Flexibility**: The method accepts a step parameter, which can be provided by the view using the command parameter. If not provided, it defaults to the `Step` state, offering flexibility and consistency.

* **Boilerplate code free**: MVUX eliminates the necessity for manual implementation of `INotifyPropertyChanged` or dispatcher concerns. All such tasks are managed by MVUX, enhancing code cleanliness.

MVUX empowers developers to implement the presentation layer quickly and efficiently, allowing a primary focus on business logic. For a more in-depth exploration, consult the comprehensive [MVUX documentation](xref:Overview.Mvux.Overview).
