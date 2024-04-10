---
uid: Uno.Extensions.Markup.Templates
---
# Templates

WinUI has a number of different templates. All templates are generated based on a given delegate that returns some sort of `UIElement`.

## DataTemplates

`DataTemplate` is a specialized template that typically has its own `DataContext`. As a result `DataTemplate` properties are built using a strongly typed generic extension that helps the creation of bindings for the given data type of the model the `DataTemplate` will be used for.

```cs
new ListView()
    .ItemTemplate<TodoItem>(item => new TextBlock().Text(() => item.Title))
```

> [!WARNING]
> The delegate property `item` will be null and cannot be used to access the state of the model. Attempting to access property values directly will result in a `NullReferenceException`. You must use Bindings to set values from your Model. If you need to dynamically update the view based on the state of the model, you should consider using a `DataTemplateSelector` instead.

### DataTemplateSelector

Sometimes it is needed to provide a different UI based on the state of an individual item. It is important to consider that the actual instance of the model type will always be null for your delegate as a `DataTemplate` cannot have any state. In these cases, you must instead make use of the `DataTemplateSelector`. The `DataTemplateSelector` property will give you access to the `DataTemplateSelectorBuilder` which again will give you the opportunity to create your DataTemplates plus specify a delegate that can evaluate the state of the model.

```cs
new ListView()
    .ItemTemplateSelector<IVehicle>((vehicle, selector) => selector
        .Default(() => new TextBlock().Text("Some Vehicle"))
        .Case(v => v.Year < 1960, () => new TextBlock().Text(() => vehicle.Model))
        .Case<Car>(car => car.Doors > 2, car => new StackPanel()
            .Children(
                new Image().Source(StaticResource.Get<ImageSource>("sedan.png")),
                new TextBlock().Text(() => car.Model)))
        .Case<Truck>(truck => new StackPanel().Children(
            new Image().Source(StaticResource.Get<ImageSource>("truck.png")),
            new TextBlock().Text(() => truck.TowingCapacity)
        )))
```

## ContentTemplate

Similar to `DataTemplate`, `ContentTemplate` is a special case that must take into consideration the target type of the parent control to which it is being applied. This has been handled automatically for you by the generated extensions. Similar to the `DataTemplate`, the method will be strongly typed, only the type will be that of the parent control.

```cs
new Button()
    .Content("Click Me")
    .Template(button => new Border()
        .RenderTransform(new RotateTransform().Angle(45))
        .Child(new ContentPresenter()
            .Content(x => x.TemplateBind(() => button.Content))))
```

## ItemsPanelTemplate

An `ItemsPanelTemplate` should contain exactly one `FrameworkElement`-derived class that serves as the root element for items. As a result, `ItemsPanelTemplate` properties are built using a strongly typed generic extension that will give you access to a `configureItemsPanel` delegate that will allow you to configure the element used as the root of the `ItemsPanelTemplate` with the desired properties.

```cs
new ListView()
    .ItemsPanel<ItemsStackPanel>(panel => panel.Orientation(Orientation.Vertical))
```

## All other Framework Templates

In all other cases, `Template` properties are treated the same with a simple delegate expected to return the desired UI. In these cases, it is assumed that the same context as the parent is available and there is no need for additional typing on the delegate.

## Next Steps

Learn more about:

- [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)
- [Generating C# Extensions for your libraries](xref:Uno.Extensions.Markup.GeneratingExtensions)
