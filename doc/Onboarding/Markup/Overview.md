---
uid: Onboarding.Markup.CSharpMarkup
---

# Uno C# Markup Overview

## Overview

C# Markup is a declarative, fluent-style syntax for defining the layout of an application in C#. With C# Markup you can define both the layout and the logic of your application using the same language. C# Markup leverages the same underlying object model as XAML, meaning that it has all the same capabilities such as data binding, converters, and access to resources. You can use all the built-in controls, any custom controls you create, and any 3rd party controls all from C# Markup.

You will quickly discover why C# Markup is a developer favorite with:

- A Declarative syntax
- Strong Typing giving you compile time checks on your Bindings and Resources
- Better Intellisense
- Support for Custom Controls and 3rd party libraries

To have sense about how it would like, here's a simple Hello World sample:

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

The first thing you will notice with C# Markup is that there is nothing special to learn. You can simply create a new instance of the controls you want to work with and access the generated extensions matching the names of the properties that you want to work with. Because we are in C# we can additionally benefit even when we are not Data Binding or using Resources with properties like our Vertical and Horizontal Alignment. Where we might have the magic string `Center` in XAML we get the full intellisense for the values on the associated enum with compile time validation that we do not have typos.

## C# Markup APIs

Let's take a look into some APIs and how it's used. Strong typing is important to developers and C# Markup delivers on this by providing extensions on your UserControls to provide a delegate action with a value for your DataContext Type.

```cs
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this
            .DataContext(new MainViewModel(), (page, vm) => page
            .Content(
                // Your Controls here...
            ));
    }
}
```

An important consideration to remember is that within the delegate of the `DataContext` the `page` parameter will be the actual instance of the `MainPage` in this sample, or whatever the type is that you called the `DataContext` extension on. However, the `vm` parameter will be null, even if you have C# nullability enabled, and it will not give you warnings about it being null. This is by design. This parameter is meant specifically to provide you the strong typing you need for Binding Expressions without being obtrusive. It is not meant for you to set or update the values of the properties, only to use in Binding Expressions.

> [!NOTE]
> While we have shown here the ability to provide an instance of your ViewModel to set the DataContext, you may optionally provide the generic overload that allows you provide the type with the understanding that the DataContext may be set automatically like it might be with Uno.Extensions.Navigation or Prism.

### Bindings

Now that we have the DataContext extension with a reference to our ViewModel type we can set up our binding with the strong typing we would expect. In it's most basic form we can simply provide a delegate using our `vm` property from the DataContext extension to access the property that we want to bind to.

```cs
new TextBlock().Text(() => vm.Title)
```

Not all bindings are this simple though. Sometimes we actually have the wrong type or we need to do some sort of conversion. C# Markup makes this easy by providing the ability to provide a simple convert delegate with your binding like this:

```cs
new TextBlock().Text(() => vm.CountValue, count => $"Counter: {count}")
```

Sometimes though, we need more control over our bindings. We might need to change the binding mode, we may want to provide an instance of a ValueConverter we already have, we might need to also provide a delegate to ConvertBack, or maybe we want to set the source of the Binding. In these scenarios we can leverage the full `IDependencyPropertyBuilder` API to take more control over the specific properties that are available to us with our Binding.

```cs
new TextBox()
    .Text(x => x.Bind(() => vm.SearchQuery)
                .Convert(s => s.ToLower())
                .ConvertBack(s => s.ToLower())
                .TwoWay())
```

### Automatic Type Converters

In addition to the strong typing that you would expect from C# Markup, we additionally provide overloads for certain common types that provide the automatic type conversion you might expect to make your development process easier.

- Brush / SolidColorBrush
  ```cs
  new TextBlock()
      .Foreground("#696969")
      .Background(Colors.Red)
  ```
- Color
  ```cs
  new SolidColorBrush().Color("#696969")
  ```
- CornerRadius
  ```cs
  new Button().CornerRadius(15)
  ```
- FontFamily
  ```cs
  new TextBlock().FontFamily("Arial")
  ```
- Geometry
  ```cs
  new PathIcon().Data("{path string}")
  ```
- ImageSource
  ```cs
  new Image().Source("ms-appx:///Counter/Assets/logo.png")
  ```
- Thickness
  ```cs
  new TextBlock().Margin(5, 20)
  ```

In addition to these automatic overloads for these primitive types in C# Markup, there is additionally a helper to provide the same succinct syntax that you would expect in XAML rather than needing to provide the much more verbose Column or Row Definition types with appropriate GridLength instance for each Row or Column.

```cs
new Grid()
    .RowDefinitions("*, Auto, 30, 2*")
```

### Resources

C# Markup provides a number of helpers to provide a strongly typed API to Get, Create and Add both Static and Theme Resources. To start let's look at how we might get the `ApplicationPageBackgroundThemeBrush` from the default WinUI Fluent Theme:

```cs
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"));
    }
}
```

This declarative syntax for both `StaticResource` and `ThemeResource` provides you with self documenting code that is strongly typed ensuring that we know that we expect a Brush from our Resource and that we are getting it from the available Resources in the Visual Tree.

We can however create custom Resources like:

```cs
public sealed partial class MainPage : Page
{
    private static readonly Resource<string> HelloWorld =
        StaticResource.Create<string>(nameof(HelloWorld), "Hello Uno Platform");

    public MainPage()
    {
        this.Resources(r => r.Add(HelloWorld))
            .Content(
                new TextBlock().Text(HelloWorld)
            );
    }
}
```

### Putting it Together

We've learned about using the `DataContext` for helping us provide strongly typed Bindings, using Resources, and even some of the helpers that we can use for common primitive types.

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this
            .DataContext(new MainViewModel(),(page, vm) => page
            .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
            .Content(new StackPanel()
                .VerticalAlignment(VerticalAlignment.Center)
                .HorizontalAlignment(HorizontalAlignment.Center)
                .Children(
                    new Image()
                        .Width(150)
                        .Height(150)
                        .Source("ms-appx:///Counter/Assets/logo.png"),
                    new TextBox()
                        .Margin(12)
                        .PlaceholderText("Step Size")
                        .Text(x => x.Bind(() => vm.StepSize).TwoWay()),
                    new TextBlock()
                        .Margin(12)
                        .HorizontalTextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
                        .Text(() => vm.CounterValue, txt => $"Counter: {txt}"),
                    new Button()
                        .Margin(12)
                        .HorizontalAlignment(HorizontalAlignment.Center)
                        .Command(() => vm.IncrementCommand)
                        .Content("Click me to increment Counter by Step Size")

                )));
    }
}
```

## Conclusion

With this overview you're ready to go and try out the C# markup for creating UIs.

// Suggest the next steps.
