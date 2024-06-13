---
uid: Uno.Extensions.Markup.Overview
---

# C# Markup

## Overview

C# Markup is a declarative, fluent-style syntax for defining the layout of an application in C#. With C# Markup you can define both the layout and the logic of your application using the same language. C# Markup leverages the same underlying object model as XAML, meaning that it has all the same capabilities such as data binding, converters, and access to resources. You can use all the built-in controls, any custom controls you create, and any 3rd party controls all from C# Markup.

You will quickly discover why C# Markup is a developer favorite with:

- A Declarative syntax
- Strongly typed data binding
- Intellisense and compile-time validation
- Refactoring support
- Custom Controls and 3rd party libraries

> [!Video https://www.youtube-nocookie.com/embed/BC3c1qO_kbU]

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

Unlike XAML which is littered with string constants, C# Markup provides a strongly typed API for setting properties. For example, in XAML you would set `HorizontalAlignment` with `HorizontalAlignment="Center"`, but in C# Markup you would use the `HorizontalAlignment` enum with `HorizontalAlignment.Center`, ensuring that you get compile time validation and intellisense for all the available values.

## Getting Started

Let's take a look at how to get started with C# Markup. We'll start with a simple sample that displays a counter and a button that increments the counter by a step size. The sample will have a ViewModel that has a `Count` and a `Step` property, as well as an `IncrementCommand` that increments the `Count` by the `Step` when executed.

### Constructor and Properties

We'll start with creating a control and setting some properties, as shown in the following example that creates a `TextBlock`. The fluent API allows you to chain together multiple properties, as shown in the following example that sets the `Margin`, `TextAlignment`, and `Text` properties.

```cs
new TextBlock()
    .Margin(12)
    .TextAlignment(TextAlignment.Center)
    .Text("Counter: 0")
```

Similar to the `HorizontalAlignment` property mentioned earlier, the `TextAlignment` property is set using an enum rather than a string constant. This ensures that you get compile time validation and intellisense for all the available values.

In XAML you would set the `Margin` property using a single number (for example `<TextBlock Margin="12" />`), but the actual `Margin` property on the `TextBlock` class requires a `Thickness` type, resulting in the following C# Markup:

```cs
new TextBlock().Margin(new Thickness(12))
```

However, C# Markup provides automatic type conversion for common types such as `Thickness`, as well as a number of other types such as `Brush`, `Color`, `CornerRadius`, `FontFamily`, `Geometry` and `ImageSource`. This means that you can set the `Margin` property using a single number, as shown in the following example.

```cs
new TextBlock().Margin(12)
```

### Data Binding

Since our counter example requires the value of the `TextBlock` to be updated each time the counter changes, we'll need to use data binding. C# Markup provides a strongly typed API for data binding, as shown in the following example that binds the `Text` property to the `Count` property of the ViewModel.

```cs
new TextBlock().Text(() => vm.Count)
```

At this point, you might be wondering what the `vm` is. The `vm` is a placeholder reference for the ViewModel type that you provide to the `DataContext` extension.

```cs
.DataContext(new MainViewModel(), (page, vm) => page
    .Content(
        new TextBlock().Text(() => vm.Count)
    )
);
```

We refer to `vm` as a placeholder because at this point it is not actually a reference to the ViewModel. It is simply a placeholder that allows us to provide a strongly typed API for data binding. Rather than invoking the code `vm.Count` when the `TextBlock` is created, the expression tree, `() => vm.Count` is used to create a binding expression that will be evaluated when the `DataContext` for the `TextBlock` is set.

In the above example, an instance of `MainViewModel` is provided to the `DataContext` extension method. If an instance of the ViewModel isn't available at the time the `DataContext` is set, you can provide the type of the ViewModel instead, as shown in the following example.

```cs
.DataContext<MainViewModel>((page, vm) => page
    .Content(
        new TextBlock().Text(() => vm.Count)
    )
);
```

Currently, the data binding is directly on the `Count` property meaning that the text shown in the `TextBlock` will just be the value of the `Count` property. However, we want to display the text "Counter: {value}" where {value} is the value of the `Count` property. To do this we can provide a delegate that will be invoked each time the value of the `Count` property changes, as shown in the following example.

```cs
new TextBlock().Text(() => vm.Count, count => $"Counter: {count}")
```

In some cases, you need more control over the binding, such as when you need to change the binding mode or provide a converter. In these cases, you can use the `Bind` extension method, as shown in the following example that two-way data binds the `Step` property to the `Text` property of a `TextBox`.

```cs
new TextBox().Text(x => x.Bind(() => vm.Step).TwoWay()),
```

### Resources

C# Markup provides a strongly typed API to get, create, and add both static and theme resources. For example, to set the `Background` to the theme resource, get the `ApplicationPageBackgroundThemeBrush` from the default WinUI Fluent theme:

```cs
this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"));
```

## Counter Sample

Putting all the pieces we've just covered together, we have the layout of a counter application that will increment the `Count` by the `Step`.

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext(new MainViewModel(),(page, vm) => page
            .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content(new StackPanel()
                .VerticalAlignment(VerticalAlignment.Center)
                .Children(
                    new Image()
                        .Width(150)
                        .Height(150)
                        .Margin(12)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .Source("ms-appx:///Assets/logo.png"),
                    new TextBox()
                        .Margin(12)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .PlaceholderText("Step Size")
                        .Text(x => x.Bind(() => vm.Step).TwoWay()),
                    new TextBlock()
                        .Margin(12)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .TextAlignment(TextAlignment.Center)
                        .Text(() => vm.Count, txt => $"Counter: {txt}"),
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

This overview provides a brief introduction to C# Markup, including how to create controls, set properties, and use data binding and resources. For a comprehensive guide on using C# Markup to build a complete application, check out the full [Counter tutorial](xref:Uno.Workshop.Counter). This tutorial offers two variants: [C# Markup + MVUX](xref:Uno.Workshop.Counter.CSharp.MVUX) and [C# Markup + MVVM](xref:Uno.Workshop.Counter.CSharp.MVVM).

## Next Steps

Learn more about:

- [Binding, Static & Theme Resources](xref:Uno.Extensions.Markup.DependencyPropertyBuilder)
  - [Binding 101](xref:Uno.Extensions.Markup.Binding101)
  - [Converters](xref:Uno.Extensions.Markup.Converters)
  - [Using Static & Theme Resources](xref:Uno.Extensions.Markup.StaticAndThemeResources)
  - [Using Uno.Themes.WinUI.Markup](xref:Uno.Extensions.Markup.UnoThemes)
- [Attached Properties](xref:Uno.Extensions.Markup.AttachedProperties)
- [Styles](xref:Uno.Extensions.Markup.Styles)
- [Templates](xref:Uno.Extensions.Markup.Templates)
- [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)
  - [Storyboards](xref:Uno.Extensions.Markup.Storyboards)
- [Generating C# Extensions for your libraries](xref:Uno.Extensions.Markup.GeneratingExtensions)
- [Upgrading C# Markup](xref:Uno.Extensions.Markup.Upgrading)
