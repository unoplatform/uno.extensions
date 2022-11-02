---
uid: Reference.Markup.DependencyPropertyBuilder
---
# Dependency Property Builder

While many properties may make sense to use a fluent API to simply set a hard coded value, it is often the case that you need to provide a Binding or use a Static Resource or a Theme Resource. For these more complex scenarios you will find the `DependencyPropertyBuilder` to be useful to set these values.

## Binding

The `DependencyPropertyBuilder` provides a fluent API to help you create a binding. The `DependencyPropertyBuilder` is a generic class that takes the type of the property you are binding to as a generic parameter. This allows you to create a binding to a property of a specific type. The `DependencyPropertyBuilder` provides a fluent API to help you create a binding. The `DependencyPropertyBuilder` is a generic class that takes the type of the property you are binding to as a generic parameter. This allows you to create a binding to a property of a specific type.

```cs
new TextBlock()
	.Text(x => x.Bind(() => vm.Message))
```

When creating a simplified binding such as the one above you may alternatively simply provide the binding expression like:

```cs
new TextBlock()
	.Text(() => vm.Message);
```

It is also possible to provide more complex binding paths using either of the above Binding expressions like this:

```cs
new TextBlock()
	.Text(x => x.Client.Contact.FirstName)
```

### Converters

The `DependencyPropertyBuilder`'s Bind method will return a strongly typed `BindingFactory`. This can help you as you tackle more advanced scenarios for Bindings such as by providing strongly typed converters that are picked up by the combination of the expected property type and the property type of the bound property. For instance you might have something like:

```cs
new TextBox()
	.Text(() => vm.Query)
	.Foreground(x => x.Bind(() => vm.Query)
		.Convert(query => new SolidColorBrush(!string.IsNullOrEmpty(query) && query.Length > 5 ? Colors.Green : Colors.Red)));
```

### Reference Sources

Sometimes you aren't binding to the DataContext of element and instead you need to reference another source. With WinUI we have 2 ways of doing this. The first is that we could specify a source directly such as:

```cs
new Slider().Assign(out var slider),
new TextBlock()
	.Text(x => x.Bind(() => slider.Value)
		.Source(slider))
```

The second is that we can leverage the element name for our binding such as the following:

```cs
new TextBlock()
	.Text(x => x.Bind(() => slider.Value)
		.ElementName("slider")),
new Slider().Name("slider")
```
