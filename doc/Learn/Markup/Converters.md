---
uid: Uno.Extensions.Markup.Converters
---
# Converters

Converters are components that allow for the conversion of values from one type to another during C# markup processing.
Converters are particularly useful when you want to display data from a domain model or data source in a customized way, applying formatting, calculations, or transformations before displaying it.

## Using a Converter

There may be times when you may want to simply use a converter instead of providing a delegate over and over. For these scenarios, you may want to provide some sort of `static readonly` context like:

```cs
public static class Converters
{
    public static readonly IValueConverter InverseBoolConverter = new InverseBoolConverter();
}

new Button()
    .Enabled(x => x.Binding(() => vm.IsBusy)
        .Converter(Converters.InverseBoolConverter));
```

## Convert

### Custom text

Converters allow the combination or some change on the value before it is displayed.
In this case, you can use the `Convert` method to provide a converter to the binding.

```csharp
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(
                new StackPanel()
                    .Children(
                        new TextBox()
                            .Text(() => vm.Query)
                        new TextBlock()
                            .Text(x => x.Binding(() => vm.Query)
                                .Convert(query => $"Search: {query}"))
                    )
            ));
    }
}
```

#### Shorthand Syntax

Instead of using the full `IDependencyPropertyBuilder` to provide our Binding and Converter, we can additionally use the shorthand syntax provided by an additional extension that is provided by the generator. To do this we simply need to provide our `Binding` and `Convert` delegates as follows:

```csharp
new TextBox()
    .Text(() => vm.Query, query => $"Search: {query}")
```

### Query and Conditionals

Sometimes, we may want to make conditionals or apply specific rules to values or attributes, for that we can simply use the convert to that treatment.

```csharp
new TextBox()
    .Text(() => vm.Query)
    .Foreground(x => x.Binding(() => vm.Query)
        .Convert(query => new SolidColorBrush(!string.IsNullOrEmpty(query) && query.Length > 5 ? Colors.Green : Colors.Red)));
```

### Static Class Converters

If we need to implement this same rule in different places or to maintain the structure, we can work with static classes for this purpose.

```csharp
public static class Converters
{
    public static readonly IValueConverter InverseBoolConverter = new InverseBoolConverter();
}
```

And use like this:

```csharp

new Button()
    .Enabled(x => x.Binding(() => vm.IsBusy)
        .Converter(Converters.InverseBoolConverter));
```

## ConvertBack

Similarly, you may need to convert the value back to the original type when the value is updated.
In this case, you can use the `ConvertBack` method to provide a converter to the binding.

```csharp
public partial class MainPage : Page
{
    public MainPage()
    {
        this.DataContext<MyViewModel>((page, vm) => page
            .Content(new TextBox()
                .Text(x => x.Binding(() => vm.Enabled)
                    .Convert(enabled => $"Enabled: {enabled}")
                    .ConvertBack(text => bool.TryParse(text.Replace("Search: ", ""), out var enabled) ? enabled : false))
            ));
    }
}
```

## Next Steps

- [Binding, Static & Theme Resources](xref:Uno.Extensions.Markup.DependencyPropertyBuilder)
- [Binding 101](xref:Uno.Extensions.Markup.Binding101)
- [Attached Properties](xref:Uno.Extensions.Markup.AttachedProperties)
- [Styles](xref:Uno.Extensions.Markup.Styles)
- [Templates](xref:Uno.Extensions.Markup.Templates)
- [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)
- [Generating C# Extensions for your libraries](xref:Uno.Extensions.Markup.GeneratingExtensions)
