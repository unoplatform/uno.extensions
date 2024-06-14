---
uid: Uno.Extensions.Markup.Styles
---
# Styles

Creating Styles with C# Markup is easy and intuitive using the `Style<T>` with extensions that let you easily set the value of a property the same as you might on a given control.

```cs
new Style<TextBlock>()
    .Setters(s => s.FontSize(14))
```

> [!Video https://www.youtube-nocookie.com/embed/McQi0-JCciw]

## Using Attached Properties

By default the C# Markup Generators will provide friendly extension methods for properties on the given type for `Style<T>`. This provides an API that is friendly and familiar to the XAML you might be used to as shown in the example above. Sometimes you may need to use Attached DependencyProperties for which there is no generated extension for `Style<T>`, in these cases you can simply call the Add method and pass in both the DependencyProperty and the value:

```cs
new Style<TextBlock>()
    .Setters(s => s.Add(HitchhikerGuide.AnswerProperty, 42));
```

## Basing a Style on another Style

Sometimes you may want to base a style on another style. This can be done one of two ways. The first is that you can provide the name/key of the style. It is important to remember that this has a limitation of only working for globally accessible styles through the Application and is best used for default styles.

```cs
new Style<TextBlock>()
    .BasedOn("GlobalStyleResourceName")
```

The second way is to provide a `Style` instance.

```cs
new Style<TextBlock>()
    .Assign(out var baseStyle);

new Style<TextBlock>()
    .BasedOn(baseStyle)
```

## Using Resources for Style Setters

Sometimes you might want to use a Static or Theme resource on your style property. In these cases, you can continue to use the Dependency Property Builder to set the value of the setter on a given property the same as you might when setting the value on a control instance.

```cs
// Static Resource
new Style<Button>()
    .Setters(s => s.Background(StaticResource.Get<Brush>("MyBrush")))

// Theme Resource
new Style<Button>()
    .Setters(s => s.Background(ThemeResource.Get<Brush>("MyBrush")))
```

Alternatively, if you are using a Theme Resource from [Uno.Material](xref:uno.themes.material.getstarted), you can use [Uno.Themes.WinUI.Markup](https://www.nuget.org/packages/Uno.Themes.WinUI.Markup) for a strongly typed API making it easier to both discover and make use of the various theme Styles, Colors, and Brushes. Instead of calling `StaticResource.Get` or `ThemeResource.Get` you will simply use the Theme call to access the Color, Brush, or Style.

```cs
new Style<Button>()
    .Setters(s => s.Background(Theme.Brushes.Primary.Default))
```

## Templates

You can similarly provide a template on a `Style` similar to how might

```cs
new Style<Button>()
    .Setters(setters => setters
        .Template(b => new Border()
            .Child(
                new ContentPresenter()
                    .Content(x => x.TemplateBind(() => b.Content))
            )
        )
    )
```

## Next Steps

Learn more about:

- [Templates](xref:Uno.Extensions.Markup.Templates)
- [VisualStateManagers](xref:Uno.Extensions.Markup.VisualStateManager)
- [Generating C# Extensions for your libraries](xref:Uno.Extensions.Markup.GeneratingExtensions)
