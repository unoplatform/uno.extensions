---
uid: Reference.Markup.GettingStarted
---
# Getting Started

Uno.Extensions.Markup is a collection of packages to make it easier for developers to build UI for their Uno and WinUI applications using a fluent and entirely C# based approach. You may want to add one or more packages based on your needs for your application.

## Pre-Generated Markup Extensions

In addition to the core package and generator the Uno team is shipping a number of pre-generated extension libraries for building your apps with C# Markup. You can find the package with the naming convention `{package name}.Markup`. Some common ones you may want to use are:

- [Uno.WinUI.Markup](https://www.nuget.org/packages/Uno.WinUI.Markup)
- [Uno.Toolkit.WinUI.Markup](https://www.nuget.org/packages/Uno.Toolkit.WinUI.Markup)
- [Uno.Material.WinUI.Markup](https://www.nuget.org/packages/Uno.Material.WinUI.Markup)
- [Uno.Extensions.Navigation.WinUI.Markup](https://www.nuget.org/packages/Uno.Extensions.Navigation.WinUI.Markup)
- [Uno.Extensions.Reactive.WinUI.Markup](https://www.nuget.org/packages/Uno.Extensions.Reactive.WinUI.Markup)
- [Uno.Themes.WinUI.Markup](https://www.nuget.org/packages/Uno.Themes.WinUI.Markup)

> [!TIP]
> For more information on generating extensions on your own libraries see [Generating Extensions](xref:Reference.Markup.GeneratingExtensions).

## Set up an Markup project

You can use this tutorial to learn how to set up an Uno Platform project to use Uno.Extensions.Markup..

- [HowToMarkupProject](xref:Learn.Tutorials.HowToMarkupProject)

Then start practicing using the guidelines below.

## Using the Markup Extensions

A fundamental premise of the Uno.Extensions.Markup library is that it should be easy to use with extensions that are generally discoverable. For this reason, the markup extensions exist within the namespace of the type they are generated for with the explicit exception of types in the `Microsoft.UI.Xaml.Controls.Primitives` namespace which instead are generated in the `Microsoft.UI.Xaml.Controls` namespace. Extensions are generated generically for all types that are not sealed in an effort to reduce the number of extensions that are required for each class. Getting started couldn't be easier!

```cs
new TextBlock()
	.Text("Hello World")
	.FontSize(20)
```

### Type Helpers

When setting values for certain types on either the element or through a setter you, helpers have been added to make this easier and more closely aligned with how you might set these automatically in XAML. Currently helper extensions are available for:

- Thickness
- CornerRadius
- Brush
- Color

```cs
new Button()
	.Foreground(Colors.Red)
	.Background(new SolidColorBrush().Color("#676767"))
	.Margin(10, 20)
	.Padding(10, 20, 30, 40)
	.CornerRadius(15)
```

## Strongly Typed DataContext

When building the content of a given control you can make use of the DataContext extension to provide a strongly typed context for bindings. It is important to note that these extensions will not create or resolve your DataContext. These extensions are meant to help you create [strongly typed bindings](xref:Reference.Markup.DependencyPropertyBuilder).

**Strongly Typed DataContext**
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

**Providing the DataContext**
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

**Binding a DataContext**
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

## Next Steps

Learn more about:

- [Binding, Static & Theme Resources](xref:Reference.Markup.DependencyPropertyBuilder)
- [Binding 101](xref:Reference.Markup.Binding101)
- [Converters](xref:Reference.Markup.Converters)
- [Attached Properties](xref:Reference.Markup.AttachedProperties)
- [Styles](xref:Reference.Markup.Styles)
- [Templates](xref:Reference.Markup.Templates)
- [VisualStateManagers](xref:Reference.Markup.VisualStateManager)
- [Generating C# Extensions for your libraries](xref:Reference.Markup.GeneratingExtensions)
- [Create your own C# Markup](xref:Learn.Tutorials.HowToCreateMarkupProject)

