---
uid: Uno.Extensions.Markup.BindToDataTemplate
---

# Binding to DataTemplates

When working with `DataTemplate`s, accessing data or commands from the parent context can be challenging. `DataTemplate`s operate within their own scope, making it difficult to bind properties or trigger actions from the parent `DataContext`. This separation can lead to confusion, especially when dealing with nested templates and multiple layers of data context.

In the following example, the parent `DataContext` is the `ViewModel` that contains a `RemoveItemCommand`. The code demonstrates how to access that `ICommand` within the `ListView`'s `ItemTemplate`.

```csharp
this.DataContext(new ViewModel(), (page, vm) => page.Content(
    new ListView()
        .ItemsSource(() => vm.Items)
        .ItemTemplate<Item>(item =>
            new StackPanel()
                .Children(
                    new TextBlock().Text(() => item.Text),
                    new Button()
                        .Content("Delete")
                        .CommandParameter(() => item)
                        // Since we have access to the `page` and `vm` alias from the DataContext method
                        // We can take advantage of them and use them on our binding expression
                        .Command(x => x
                            .Source(page)
                            .DataContext()
                            .Binding(() => vm.RemoveItemCommand)
                        )
                )
        )
))
```

Alternatively, we could extract the `Button` into a helper method and take advantage of the `RelativeSource` method to provide the `CommandParameter`.

```csharp
...

.Children(
    new TextBlock()
        .Text(() => item.Text),
    CreateRemoveButton()
)
...

private Button CreateRemoveButton() => new Button()
    .Content("Delete")
    .CommandParameter(x => x
        .RelativeSource<Button>(RelativeSourceMode.TemplatedParent)
        .Binding(btn => btn.DataContext)
    )
   .Command(x => x
        // Since we don't have access to the `page` alias here (as we have in the previous example)
        // We need to set `this` as the source
        .Source(this)
        // Here we specify the DataContext type so that we can access the ViewModel alias in the Binding method
        .DataContext<MainViewModel>()
        .Binding(vm => vm.RemoveItemCommand)
    );
```

Given that `CommandParameter` is the `DataContext` of the `ListView.ItemTemplate`, we can simplify it by using the XAML equivalent of `{Binding .}`. In C# Markup this is `(x => x.Binding())`:

```csharp
private Button CreateRemoveButton() => new Button()
    .Content("Delete")
    .Command(x => x.Source(this)
        .DataContext<MainViewModel>()
        .Binding(vm => vm.RemoveItemCommand)
    )
    .CommandParameter(x => x.Binding());
```

For more information about `Source` and `RelativeSource` usages in C# Markup, refer to the [Source and Relative Source](xref:Uno.Extensions.Markup.SourceUsage) documentation.

## Next Steps

- [Binding, Static & Theme Resources](xref:Uno.Extensions.Markup.DependencyPropertyBuilder)
- [Binding 101](xref:Uno.Extensions.Markup.Binding101)
- [Attached Properties](xref:Uno.Extensions.Markup.AttachedProperties)
- [Styles](xref:Uno.Extensions.Markup.Styles)
- [Templates](xref:Uno.Extensions.Markup.Templates)
- [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)
- [Generating C# Extensions for your libraries](xref:Uno.Extensions.Markup.GeneratingExtensions)
