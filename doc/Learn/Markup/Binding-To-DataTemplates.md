---
uid: Uno.Extensions.Markup.BindToDataTemplate
---

# Binding to DataTemplates

When working with DataTemplates, accessing data or commands from the parent context can be challenging. DataTemplates operate within their own scope, making it difficult to bind properties or trigger actions in the parent view model. This separation can lead to confusion, especially when dealing with nested templates and multiple levels of data context. 

See the following example how we could access the command `RemoveItemCommand` defined inside the `ViewModel` class inside the ListView `ItemTemplate`.

```csharp
this.DataContext(new ViewModel(), (page, vm)
	=> page
	.Content(
        new ListView()
            .ItemsSource(() => vm.Items)
            .ItemTemplate<Item>(item =>
                new StackPanel()
                    .Children(
                        new TextBlock()
                            .Text(() => item.Text),
                        new Button()
                            .Content("Delete")
                            .CommandParameter(() => item)

                            // Since we have access to the `page` and `vm` alias from the DataContext method
                            // We can take advantage of them and use them on our binding expression
                            .Command(x => x.Source(page)
                                           .DataContext()
                                           .Binding(() => vm.RemoveItemCommand)
                            )
                    )
            )
    )
)
```

Alternatively we could extract the button instance to a helper method and take advantage of the `RelativeSource` method to provide the `CommandParameter`.

```csharp
...

.Children(
    new TextBlock()
        .Text(() => item.Text),
    CreateButton()
)
...

private Button CreateButton()
    => new Button()
           .Content("Delete")
           .CommandParameter(x => x.RelativeSource<Button>(RelativeSourceMode.TemplatedParent)
                                   .Binding(btn => btn.DataContext)
            )
           .Command(x => x
                        // Since we we don't have access to the `page` alias here (as we have on the previous example)
                        // We need to set `this` as the source
                        .Source(this)
                        // Here we specify the DataContext type so that we can access the ViewModel alias in the Binding method
                        .DataContext<MainViewModel>()
                        .Binding(vm => vm.RemoveItemCommand)
            );
```

Since the `CommandParameter` we're providing it's the `Item` from the List, we can simplify it by using the Xaml equivalent as `{Binding .}` that in C# Markup is `(x => x.Binding())`. So the code would look like:

```csharp
private Button CreateButton()
    => new Button()
           .Content("Delete")
           .CommandParameter(x => x.Binding())
           .Command(x => x.Source(this)
                          .DataContext<MainViewModel>()
                          .Binding(vm => vm.RemoveItemCommand)
            );
```

To know more about the `Source` and `RelativeSource` usage in C# Markup access our [Source and Relative Source](xref:Uno.Extensions.Markup.SourceUsage) docs.

## Next Steps

- [Binding, Static & Theme Resources](xref:Uno.Extensions.Markup.DependencyPropertyBuilder)
- [Binding 101](xref:Uno.Extensions.Markup.Binding101)
- [Attached Properties](xref:Uno.Extensions.Markup.AttachedProperties)
- [Styles](xref:Uno.Extensions.Markup.Styles)
- [Templates](xref:Uno.Extensions.Markup.Templates)
- [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)
- [Generating C# Extensions for your libraries](xref:Uno.Extensions.Markup.GeneratingExtensions)
