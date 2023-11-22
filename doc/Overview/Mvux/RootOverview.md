# MVUX Framework Overview

MVUX, a powerful state management system, seamlessly combines the strengths of both MVU (Model-View-Update) and MVVM (Model-View-ViewModel) architectures. This document presents a brief yet comprehensive overview of the key advantages MVUX offers, along with a simplified example demonstrating its efficiency in building applications.

## Key Advantages of MVUX
### 1. Reactive Application Development

MVUX empowers developers to create highly responsive applications that automatically react to state changes, effortlessly updating the view. This reactivity ensures a dynamic and engaging user experience.

### 2. Immutable Models for Stability

Building on the concept of immutability, MVUX simplifies the creation of models that maintain coherent states in multi-threaded environments. This approach enhances stability and predictability in complex application scenarios.

### 3. Asynchronous Everywhere

MVUX takes care of executing all non-UI-related operations on a background thread without requiring developers to write boilerplate code, such as INotifyPropertyChanged. With native support for common data states, users are informed of background data fetching or error occurrences, contributing to a seamless user experience.

### 4. Code Testability

The MVUX framework promotes test-driven development by decoupling presentation logic from the view. This separation facilitates the testing of code components, ensuring robust and reliable applications.

### 5. Declarative Code Friendly

Express your code's intentions more clearly with MVUX's declarative approach. The framework encourages writing code in a declarative fashion, enhancing readability and maintainability.

## Quick Start: Building a Counter App

To illustrate the simplicity and power of MVUX, let's explore building a basic counter application. Below is a minimal model for a counter:

```csharp
public partial record CounterModel
{
    public IState<int> Counter => State.Value(this, () => 0);

    public IState<int> Step => State.Value(this, () => 42);

    public async ValueTask Increment(int step) 
        => await Counter.Update(i => i + step);
}
```

Key Features in the Code:

* **Reactive Properties**: Counter and Step are exposed as plain binding-friendly integer properties to the view, supporting two-way data binding. Updates are reported on the UI thread for the view and on the background thread for the model.

* **Command Handling**: The Increment method is automatically exposed as an ICommand to the view, enabling seamless integration with ButtonBase elements. Additionally, ICommand.CanExecute allows intelligent button state management during execution.

* **Parameter Flexibility**: The method accepts a step parameter, which can be provided by the view using the command parameter. If not provided, it defaults to the Step state, offering flexibility and consistency.

MVUX empowers developers to implement the presentation layer quickly and efficiently, allowing a primary focus on business logic. For a more in-depth exploration, consult the comprehensive [MVUX documentation](https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/Mvux/Overview.html).
