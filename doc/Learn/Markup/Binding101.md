---
uid: Uno.Extensions.Markup.Binding101
---
# Binding 101

Whether you've been around frameworks that use bindings for a while or you're brand new to the concept, this guide will help you understand the basics of Bindings.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new TextBlock()
                    .Text(vm.Message) // This will throw a Null Reference Exception
            )
        );
    }
}
```

At first glance the code sample above may look correct and it will compile. However it is important to consider that within the `DataContext` method the vm property will **always** be null. This method is provided to help you provide strongly typed bindings for your ViewModel. To fix this we need to provide a binding expression to the `Text` method.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new TextBlock()
                    .Text(() => vm.Message)
            )
        );
    }
}
```

Alternatively, we may want to reduce the overhead on the binding by only having it apply once to set the `Text` property and not be watched for changes. For example, if we're setting a title or something that doesn't change,  we can use the `Mode` method to set the binding mode to `OneTime`.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new TextBlock()
                    .Text(x => x
                        .Binding(() => vm.Title)
                        .Mode(BindingMode.OneTime)
                    )
            )
        );
    }
}
```

The BindingMode additionally provides you with some shorthand methods to set the BindingMode to `OneWay`, `TwoWay` or `OneTime`.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new TextBlock()
                    .Text(x => x
                        .Binding(() => vm.Title)
                        .OneTime()
                    )
            )
        );
    }
}
```

Sometimes, you may want to update the format of the value before it is displayed. In this case, you can use the `Convert` method to provide a converter to the binding.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new TextBlock()
                    .Text(x => x
                        .Binding(() => vm.Query)
                        .Convert(query => $"Search: {query}")
                    )
            )
        );
    }
}
```

You can also use the shorthand version of this to simply provide the binding and a converter.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new TextBlock()
                    .Text(() => vm.Query, query => $"Search: {query}")
            )
        );
    }
}
```

Similarly, you may need to convert the value back to the original type when the value is updated. In this case you can use the `ConvertBack` method to provide a converter to the binding.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new TextBox()
                    .Text(x => x
                        .Binding(() => vm.Enabled)
                        .Convert(enabled => $"Enabled: {enabled}")
                        .ConvertBack(text => bool.TryParse(text.Replace("Search: ", ""), out var enabled) ? enabled : false)
                    )
            )
        );
    }
}
```

## Binding to other sources

Sometimes you may want or need to bind to your ViewModel. The first case we'll take a look at is one in which we want to bind to a property of another element. In this case we will use the `Source` method to update our binding so that it will bind to a named element in the Visual Tree.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new TextBox()
                    .Name(out var searchBox)
                    .Text(() => vm.Query),
                new TextBox()
                    .Text(x => x
                        .Source(nameof(searchBox))
                        .Binding(() => searchBox.Text))
            )
        );
    }
}
```

Sometimes you may want to have a custom control that uses it's own ViewModel to help keep your ViewModel's clean and simple.

In the event that you need to grab some context to create the ViewModel for the Child control's `DataContext` you may put these various concepts together to create a `DataContext` for the custom control using a binding on the parent's `DataContext` while updating the Binding to use the current `Page` as the source.

```cs
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new MyCustomControl()
                    .DataContext(x => x
                        .Source(this)
                        .Binding(() => this.DataContext)
                        .Convert(dataContext => {
                            if (dataContext is MyViewModel myViewModel)
                            {
                                return new MyCustomControlViewModel(myViewModel.SomeContext);
                            }
                            return null;
                        })
                        .OneTime()
                    )
            )
        );
    }
}
```

## Next Steps

Learn more about:

- [Attached Properties](xref:Uno.Extensions.Markup.AttachedProperties)
- [Styles](xref:Uno.Extensions.Markup.Styles)
- [Templates](xref:Uno.Extensions.Markup.Templates)
- [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)
- [Generating C# Extensions for your libraries](xref:Uno.Extensions.Markup.GeneratingExtensions)
