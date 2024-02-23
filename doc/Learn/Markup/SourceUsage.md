---
uid: Uno.Extensions.Markup.SourceUsage
---
# Binding Usage

In this section, we explore practical examples of how to implement data binding in Uno Platform applications using C#Markup. These examples include direct source binding, relative source binding, and utilizing data contexts.

## Source Binding

Binding a property directly from one UI element to another is a common scenario. Here's how you can bind the Text property of a TextBox to the Content property of a button, and vice versa:

```cs
public partial class MainPage : Page
{
    public MainPage()
    {

        // Creating a TextBox and binding its Text property to a Button's Content

        var textBox = new TextBox()
                        .Text(x => x.Source(button)
                                    .Binding(() => button.Content));

        // Creating a Button and binding its Content property to the TextBox's Text property with TwoWay binding
        var button = new Button()
                        .Content(x => x.Source(textBox)
                                    .Binding(() => textBox.Text)
                                    .TwoWay());

        // Binding with a string identifier for the source button
        var textBox = new TextBox()
                        .Text(x => x.Source<Button>("myButton")
                                    .Binding(b => b.Content));

        // Binding to a property of a DataContext
        var textBox = new TextBox()
                        .Text(x => x.Source(button)
                                    .DataContext<MockViewModel>()
                                    .Binding(v => v.Message));
    }
}

```

## Relative Source Binding

RelativeSource binding allows you to bind to a property of the element itself or a relative element, such as a parent. This can be useful in various scenarios, including styling and templating:

Example of using RelativeSource to bind to another property of the same element:

```cs

public partial class MainPage : Page
{
    public MainPage()
    {

        var textBlock = new TextBlock()

                            // Binding an element's property to itself using RelativeSource
                            .Text(x => x.RelativeSource<TextBlock>(RelativeSourceMode.Self)
                                        .Binding(t => t.Tag))
                            .Tag("Uno Platform!");

    }
}

```



Example of using RelativeSource to bind a parent property to the element property:

```cs

public partial class MainPage : Page
{
    public MainPage()
    {

        var button = new Button()
                        .Style(
                            new Style<Button>()
                                .Setters(s => s
                                    .Template(b => new Border()
                                                    .Name("border")

                                                    // Using RelativeSource to bind to a TemplatedParent property in a style template
                                                    .Background(x => x.RelativeSource<Button>(RelativeSourceMode.TemplatedParent)
                                                                        .Binding(x => x.Background))
                                    )
                                )
                        )
                        .Background(Colors.Blue);
    }
}

```

These examples illustrate the flexibility of Uno Platform's binding capabilities, enabling you to create dynamic and responsive UIs efficiently.

## Binding Controls

Uno Platform provides the following methods to control the binding mode:

Binding() | Sets the Binding
DataContext<TDataContext>() | Sets the DataContext
Mode(BindingMode) | Sets Binding Mode
OneTime() | Sets Binding Mode to OneTime
OneWay() | Sets the Binding Mode to OneWay
TwoWay() | Sets the Binding Mode to TwoWay
Converter(IValueConverter)
Convert(Func<TSource, TTarget>)
ConvertBack(Func<TTarget, TSource>)
FallbackValue(T)
TargetNullValue(T)
UpdateSourceTrigger(UpdateSourceTrigger)

These methods allow you to specify how changes in the source property should be reflected in the target property, providing flexibility in creating dynamic and responsive UIs.
