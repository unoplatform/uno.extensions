---
uid: Uno.Extensions.Markup.AttachedProperties
---
# Attached Properties

Attached Properties are often something that you simply need a quick way to access and set the value for. For example, you may want to set the `Grid.Row` or `Column` of an element. Attached properties like these can be found with an extension name matching the type name of the class they are defined in, along with an optional parameter for each attached property.

```cs
new TextBlock()
    .Grid(row: 3, column: 2)
```

> [!TIP]
> Extensions like shown above for Grid do not have generated extensions that only provide the most common parameters like `row` and `column`. It is a better practice when using this version of the extension to specify the parameter name as shown above. This will lead to code that is easier to read and maintain as you know for sure what each value is being set to.

## Using the Builder Pattern

When simply providing a value for an optional parameter isn't enough, you may also use the builder pattern which the Markup Extensions commonly provide. The Markup Extensions Generator will also create a Property Builder for Attached Properties.

```cs
new TextBlock()
    .Grid(grid => grid
        .Row(3)
        .Column(2))
```

### Binding to Attached Properties

Using the builder still gives you the ability to provide an explicit value while also taking advantage of other extensions that you see on normal Dependency Properties such as using the [DependencyPropertyBuilder](xref:Uno.Extensions.Markup.DependencyPropertyBuilder) to create a binding, or provide a Static or Theme resource.

```cs
new TextBlock()
    .AutomationProperties(x => x
        .Name(() => vm.Property))
```

## Next Steps

Learn more about:

- [Styles](xref:Uno.Extensions.Markup.Styles)
- [Templates](xref:Uno.Extensions.Markup.Templates)
- [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)
- [Generating C# Extensions for your libraries](xref:Uno.Extensions.Markup.GeneratingExtensions)
