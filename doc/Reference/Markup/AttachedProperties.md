---
uid: Reference.Markup.AttachedProperties
---
# Attached Properties

Attached Properties are often something that you simply need a quick way to access and set the value for. For example you may want to set a the Grid Row or Column of an element. Attached properties like these can be found with an extension name matching the type name of the class they are defined in, along with an optional parameter for each attached property.

```cs
new TextBlock()
	.Grid(row: 3, column: 2)
```

When simply providing a value for an optional parameter isn't enough, you may also use the builder pattern which the Markup Extensions commonly provide. The Markup Extensions Generator will also create a Property Builder for Attached Properties.

```cs
new TextBlock()
	.Grid(grid => grid
		.Row(3)
		.Column(2))
```

Using the builder still gives you the ability to provide an explicit value while also taking advantage of other extensions that you see on normal Dependency Properties such as using the [DependencyPropertyBuilder](xref:Reference.Markup.DependencyPropertyBuilder) to create a binding, or provide a Static or Theme resource.

```cs
new TextBlock()
	.SomeClass(x => x
		.Property(() => vm.Property))
```
