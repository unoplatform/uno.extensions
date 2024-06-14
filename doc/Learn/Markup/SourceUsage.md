---
uid: Uno.Extensions.Markup.SourceUsage
---
# Source and Relative Source

Sometimes, when working with binding expressions, specifying the source or a relative source becomes necessary. In this section, we'll explore how to do that using strongly typed sources.

## Source Binding

Binding a property directly from one UI element to another is a common scenario. Here's how you can bind the Text property of a TextBox to the Content property of a button, and vice versa:

```csharp
new StackPanel()
    .Children(
        new Button()
            .Name(out var button)
            .Content("I am a button"),

        // Creating a TextBox and binding its Text property to a Button's Content
        new TextBox()
            .Text(x => x.Source(button)
                        .Binding(() => button.Content));
    )
```

```csharp
new StackPanel()
    .Children(
        new TextBox()
            .Name(out var textBox)
            .Text("I am a TextBox"),

        // Creating a Button and binding its Content property to the TextBox's Text property with TwoWay binding
        new Button()
            .Content(x => x.Source(textBox)
                            .Binding(() => textBox.Text)
                            .TwoWay());
    )
```

```csharp
// Binding with a string identifier for the source button
new TextBox()
    .Text(x => x.Source<Button>("myButton")
                .Binding(b => b.Content));
```

```csharp
new StackPanel()
    .Children(
        new Button()
            .Name(out var button)
            .Content("I am a button"),

        // Binding to a property of a DataContext
        new TextBox()
            .Text(x => x.Source(button)
                        .DataContext<MockViewModel>()
                        .Binding(v => v.Message));
    )
```

## Relative Source Binding

RelativeSource binding allows you to bind to a property of the element itself or a relative element, such as a parent. This can be useful in various scenarios, including styling and templating:

Example of using RelativeSource to bind to another property of the same element:

```csharp

var textBlock = new TextBlock()
                    .Tag("Uno Platform!")
                    // Binding an element's property to itself using RelativeSource
                    .Text(x => x.RelativeSource<TextBlock>(RelativeSourceMode.Self)
                                .Binding(t => t.Tag));

```

Example of using RelativeSource to bind a parent property to the element property:

```csharp

var button = new Button()
                 .Style(
                    new Style<Button>()
                        .Setters(s => s
                            .Template(b => new Border()
                                            // Using RelativeSource to bind to a TemplatedParent property in a style template
                                               .Background(x => x.RelativeSource<Button>(RelativeSourceMode.TemplatedParent)
                                                                 .Binding(x => x.Background)
                                               )
                            )
                        )
                 )
                 .Background(Colors.Blue);

```

## Binding Expression Methods

Below is a list of methods available for use within binding expressions:

| Property                                  | Description                                                                                                     |
| ----------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `Binding()`                               | Sets the Binding Path                                                                                                 |
| `DataContext<TDataContext>()`             | Sets the DataContext                                                                                            |
| `Mode(BindingMode)`                       | Sets Binding Mode                                                                                               |
| `OneTime()`                               | Sets Binding Mode to OneTime                                                                                    |
| `OneWay()`                                | Sets the Binding Mode to OneWay                                                                                 |
| `TwoWay()`                                | Sets the Binding Mode to TwoWay                                                                                 |
| `Converter(IValueConverter)`              | Sets a custom IValueConverter to convert data between the source and target during binding                      |
| `Convert(Func<TSource, TTarget>)`         | Sets a conversion function to transform the source data to the target type during binding                       |
| `ConvertBack(Func<TTarget, TSource>)`     | Sets a conversion function to transform the target data back to the source type during binding (TwoWay binding) |
| `FallbackValue(T)`                        | Sets a fallback value to be used when the source data is null or cannot be converted                            |
| `TargetNullValue(T)`                      | Sets a value to be used as the target when the source data is null                                              |
| `UpdateSourceTrigger(UpdateSourceTrigger)`| Sets the trigger that determines when the source property is updated during TwoWay binding.                     |
