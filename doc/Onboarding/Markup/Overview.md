---
uid: Onboarding.Markup.CSharpMarkup
---

# C# Markup

## Overview

C# Markup is a declarative, fluent-style syntax for defining the layout of an application in C#. With C# Markup you can define both the layout and the logic of your application using the same language. C# Markup leverages the same underlying object model as XAML, meaning that it has all the same capabilities such as data binding, converters, and access to resources. You can use all the built-in controls, any custom controls you create, and any 3rd party controls all from C# Markup.

You will quickly discover why C# Markup is a developer favorite with:

- A Declarative syntax
- Strongly typed data binding
- Intellisense and compile time validation
- Refactoring support
- Custom Controls and 3rd party libraries

Let's jump in and take a look at a simple sample that displays 'Hello Uno Platform!' in the center of the screen:

```cs
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.Content
        (
            new StackPanel()
                .VerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Children
                (
                    new TextBlock()
                        .Text("Hello Uno Platform!")
                )
        );
    }
}
```

The first thing you will notice with C# Markup is that there is nothing special to learn. You can simply create a new instance of the controls you want to work with (eg `new TextBlock()`) and set properties using the generated extension method with the same name (eg `.Text("Hello Uno Platform!')`). 

Unlike XAML which is littered with string constants, C# Markup provides a strongly typed API for setting properties. For example, in XAML you would set HorizontalAlignment with `HorizontalAlignment="Center"`, but in C# Markup you would use the `HorizontalAlignment` enum with `HorizontalAlignment.Center`, ensuring that you get compile time validation and intellisense for all the available values.

## Getting Started

Let's take a look at how to get started with C# Markup. We'll start with a simple sample that displays a counter and a button that increments the counter by a step size. We'll start with a simple ViewModel that has a `CounterValue` and a `StepSize` property, as well as a `IncrementCommand` that increments the `CounterValue` by the `StepSize` when executed. 

### Constructor and Properties

We'll start with creating a control and setting properties, as shown in the following example that creates a `TextBlock`. The fluent API allows you to chain together multiple properties, as shown in the following example that sets the `Margin`, `HorzontalTextAlignment` and `Text` properties.

```cs
new TextBlock()
    .Margin(12)
    .HorizontalTextAlignment(TextAlignment.Center)
    .Text("Counter: 0")
```

Similar to the `HorizontalAlignment` property mentioned earlier, the `HorizontalTextAlignment` property is set using an enum rather than a string constant. This ensures that you get compile time validation and intellisense for all the available values.

The `Margin` property has the same name as the property in XAML, but the value is set using a different type. In XAML you would set the `Margin` property with `Margin="12"`, but `Margin` property in C# requires a `Thickness` type. Setting the `Margin` with `Margin(12)` is supported because C# Markup provides automatic type conversion for common types such as `Thickness`, as well as a number of other types such as `Brush`, `Color`, `CornerRadius`, `FontFamily`, `Geometry`, `ImageSource`, and `GridLength`. 

### Data Binding

Since our counter example requires the value of the `TextBlock` to be updated each time the counter changes, we'll need to use data binding. C# Markup provides a strongly typed API for data binding, as shown in the following example that binds the `Text` property to the `CounterValue` property of the ViewModel.

```cs
new TextBlock().Text(() => vm.CounterValue)
```

At this point you might be wondering what the `vm` is. C# Markup provides a strongly typed API for setting the `DataContext` of a control, as shown in the following example that sets the `DataContext` to the ViewModel. The `vm` is a placeholder reference for the ViewModel type that you provide to the `DataContext` extension.

```cs
.DataContext(
    new MainViewModel(), 
    (page, vm) => page
        .Content(
            new TextBlock().Text(() => vm.CounterValue)
        )
);
```

We refer to `vm` as a placeholder because at this point it is not actually a reference to the ViewModel. It is simply a placeholder that allows us to provide a strongly typed API for data binding. Rather than invoking the code `vm.CounterValue` when the `TextBlock` is created, the expression tree, `() => vm.CounterValue` is used to create a binding expression that will be evaluated when the `DataContext` for the `TextBlock` is set.

In the above example, an instance of `MainViewModel` is provided to the `DataContext` extension method. If an instance of the ViewModel isn't available at the time the `DataContext` is set, you can provide the type of the ViewModel instead, as shown in the following example.

```cs
.DataContext<MainViewModel>(
    (page, vm) => page
        .Content(
            new TextBlock().Text(() => vm.CounterValue)
        )
);
```

Currently the data binding is directly on the `CounterValue` property meaning that the text shown in the `TextBlock` will just be the value of the `CounterValue` property. However, we want to display the text "Counter: {value}" where {value} is the value of the `CounterValue` property. To do this we can provide a delegate that will be invoked each time the value of the `CounterValue` property changes, as shown in the following example.

```cs
new TextBlock().Text(() => vm.CounterValue, count => $"Counter: {count}")
```

In some case you need more control over the binding, such as when you need to change the binding mode or provide a converter. In these cases you can use the `Bind` extension method, as shown in the following example that two-way data binds the `StepSize` property to the `Text` property of a `TextBox`. 

```cs
new TextBox().Text(x => x.Bind(() => vm.StepSize).TwoWay()),
```

### Resources

C# Markup provides a strongly typed API to get, create and add both static and theme resources. For example, to set the `Background` to theme resource get the `ApplicationPageBackgroundThemeBrush` from the default WinUI Fluent theme:

```cs
this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"));
```

There are also predefined resources for theme brushes, as shown in the following example:

```cs
this.Background(Theme.Brushes.Background.Default);
```

## Counter Sample

Putting all the pieces we've just covered together, we have the layout of a counter application that will increment the `CounterValue` by the `StepSize`.

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this
            .DataContext(
                new MainViewModel(),
                (page, vm) => page
                    .Background(Theme.Brushes.Background.Default)
                    .Content(new StackPanel()
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Children(
                            new Image()
                                .Width(150)
                                .Height(150)
                                .Margin(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Source("ms-appx:///Counter/Assets/logo.png"),
                            new TextBox()
                                .Margin(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .PlaceholderText("Step Size")
                                .Text(x => x.Bind(() => vm.StepSize).TwoWay()),
                            new TextBlock()
                                .Margin(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .HorizontalTextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
                                .Text(() => vm.CounterValue, txt => $"Counter: {txt}"),
                            new Button()
                                .Margin(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Command(() => vm.IncrementCommand)
                                .Content("Click me to increment Counter by Step Size")
                        )
                    )
            );
    }
}
```

## Conclusion

This overview has provided a brief introduction to C# Markup with the introduction on how to create controls and set properties, as well as how to use data binding and resources. Check out the full [Counter sample](TBD) to see how to use C# Markup to create a complete application. Check out the [C# Markup reference](TBD) for more information on available extension methods.

