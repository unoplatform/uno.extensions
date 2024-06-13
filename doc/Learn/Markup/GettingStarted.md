---
uid: Uno.Extensions.Markup.GettingStarted
---
# Getting Started

Uno.Extensions.Markup is a collection of packages to make it easier for developers to build UI for their Uno Platform and WinUI applications using a fluent and entirely C# based approach. You may want to add one or more packages based on your needs for your application.

## Pre-Generated Markup Extensions

In addition to the core package and generator, the Uno Platform team is shipping a number of pre-generated extension libraries for building your apps with C# Markup. You can find the package with the naming convention `{package name}.Markup`. Some common ones you may want to use are:

- [Uno.WinUI.Markup](https://www.nuget.org/packages/Uno.WinUI.Markup)
- [Uno.Toolkit.WinUI.Markup](https://www.nuget.org/packages/Uno.Toolkit.WinUI.Markup)
- [Uno.Material.WinUI.Markup](https://www.nuget.org/packages/Uno.Material.WinUI.Markup)
- [Uno.Extensions.Navigation.WinUI.Markup](https://www.nuget.org/packages/Uno.Extensions.Navigation.WinUI.Markup)
- [Uno.Extensions.Reactive.WinUI.Markup](https://www.nuget.org/packages/Uno.Extensions.Reactive.WinUI.Markup)
- [Uno.Themes.WinUI.Markup](https://www.nuget.org/packages/Uno.Themes.WinUI.Markup)

> [!TIP]
> For more information on generating extensions on your own libraries see [Generating Extensions](xref:Uno.Extensions.Markup.GeneratingExtensions).

## Using the Markup Extensions

A fundamental premise of the [Uno.Extensions.Markup.WinUI](https://www.nuget.org/packages/Uno.Extensions.Markup.WinUI) library is that it should be easy to use with extensions that are generally discoverable. For this reason, the markup extensions exist within the namespace of the type they are generated for. C# Markup Extensions are generated generically for all types that are not sealed in an effort to reduce the number of extensions that are required for each class. Getting started couldn't be easier!

```cs
new TextBlock()
    .Text("Hello World")
    .FontSize(20)
```

### Type Helpers

When setting values for certain types on either the element or through a setter, helpers have been added to make this operation easier and more closely aligned with how you might set these automatically in XAML. Currently helper extensions are available for:

- Brush
- Color
- CornerRadius
- FontFamily
- Geometry
- ImageSource
- Thickness

```cs
new Button()
    .Foreground(Colors.Red)
    .Background(new SolidColorBrush().Color("#676767"))
    .Margin(10, 20)
    .Padding(10, 20, 30, 40)
    .CornerRadius(15)
```

## Strongly Typed DataContext

When building the content of a given control, you can make use of the `DataContext` extension to provide a strongly typed context for bindings. It is important to note that these extensions will not create or resolve your DataContext. These extensions are meant to help you create [strongly typed bindings](xref:Uno.Extensions.Markup.DependencyPropertyBuilder).

### Strongly Typed DataContext

```cs
public partial class MyPage : Page
{
    public MyPage()
    {
        this.DataContext<MyPageViewModel>((page, vm) => page
            .Content(new Grid()));
    }
}
```

### Providing the DataContext

```cs
public partial class MyPage : Page
{
    public MyPage()
    {
        this.DataContext(new MyPageViewModel(), (page, vm) => page
            .Content(new Grid()));
    }
}
```

### Binding a DataContext

```cs
public partial class MyPage : Page
{
    public MyPage()
    {
        this.DataContext<MyPageViewModel>((page, vm) => page
            .Content(new Grid().Children(
                new StackPanel()
                    // Binding to a property of the parent's DataContext
                    .DataContext(() => vm.MyModel, (panel, myModel) => panel
                    .Children(
                            new TextBlock().Text(() => myModel.Name),
                            new TextBlock().Text(() => myModel.Description)
                        )
                    )
            )));
    }
}
```

> [!TIP]
> Whether you are using a Binding or providing an instance of the ViewModel, the value passed into your configuration delegate for the ViewModel will **ALWAYS** be null even with nullability enabled. This is done intentionally as you should never evaluate the state of your ViewModel or it's properties as part of the configuration delegate.

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
