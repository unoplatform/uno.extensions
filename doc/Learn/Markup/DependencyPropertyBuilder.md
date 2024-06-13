---
uid: Uno.Extensions.Markup.DependencyPropertyBuilder
---
# Dependency Property Builder

While many properties may make sense to use a fluent API to simply set a hard-coded value, it is often the case that you need to provide a `Binding` or use a Static Resource or a Theme Resource. For these more complex scenarios, you will find the `DependencyPropertyBuilder` to be useful to set these values.

## Binding

The `DependencyPropertyBuilder` provides a fluent API to help you create a binding. The `DependencyPropertyBuilder` is a generic class that takes the type of the property you are binding to as a generic parameter. This allows you to create a binding to a property of a specific type. The `DependencyPropertyBuilder` provides a fluent API to help you create a binding. The `DependencyPropertyBuilder` is a generic class that takes the type of the property you are binding to as a generic parameter. This allows you to create a binding to a property of a specific type.

```cs
new TextBlock()
    .Text(x => x.Binding(() => vm.Message))
```

When creating a simplified binding such as the one above you may alternatively simply provide the binding expression like:

```cs
new TextBlock()
    .Text(() => vm.Message);
```

It is also possible to provide more complex binding paths using either of the above Binding expressions like this:

```cs
new TextBlock()
    .Text(() => vm.Client.Contact.FirstName)
```

### Understanding the Binding Expression

The **Binding Expression** is a lambda expression that provides the path to the property you are binding to.

The lambda expression is parsed to determine the path to the property. It can be a simple property access such as `() => vm.Message` or it can be a more complex expression such as `() => vm.Client.Contact.FirstName`. It is important to remember that this is equivalent to the following XAML:

```xml
<TextBlock Text="{Binding Message}" />
<TextBlock Text="{Binding Client.Contact.FirstName}" />
```

While the Markup Extensions expose a delegate method for you to provide a path and strong typing for the property, it is easiest to think of the delegate as a string. If you think of the string value of `() => vm.Client.Contact.FirstName`, this will get evaluated by taking the substring following the first period leaving you with the path `Client.Contact.FirstName`. This is the path that will be used to create the binding.

If you wish to perform tasks such as manipulate the value of the binding or format the text displayed see the [Converters](xref:Uno.Extensions.Markup.Converters) documentation.

### Reference Sources

Sometimes you aren't binding to the `DataContext` of the element and instead, you need to reference another source:

```cs
new Slider().Name(out var slider),
new TextBlock()
    .Text(x => x
        .Source(slider)
        .Binding(() => slider.Value))
```

 > [!NOTE]
 > Using the `.Name(out var fe)` syntax not only gives you a variable representing your `FrameworkElement` control but also sets the control's `Name` property to match the variable name. For example, in the scenario mentioned earlier, the `Slider` would have its `Name` property set to "slider" because the variable name used is `slider`.

## Additional Reading

- [Binding 101](xref:Uno.Extensions.Markup.Binding101)
- [Using Converters](xref:Uno.Extensions.Markup.Converters)
- [Using Static &amp; Theme Resources](xref:Uno.Extensions.Markup.StaticAndThemeResources)
