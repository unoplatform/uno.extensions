---
uid: Uno.Extensions.Markup.Resources
---
# Resource Dictionaries

All `FrameworkElement`-inheriting types and `Application` have a couple of special extensions to help you manage your resources.

> [!Video https://www.youtube-nocookie.com/embed/W3P9Vd8QuGY]

## Merging Resource Dictionaries

To merge resource dictionaries, you simply need to add a `ResourceDictionary` to the `Application` or `FrameworkElement` like:

```cs
new Page()
    // Where MyColorResources & MyFontResources are both a ResourceDictionary
    .Resources(r => r.Merged(
        new MyColorResources(),
        new MyFontResources()))
```

## Adding Resources

To add a resource to the `ResourceDictionary` you can use the fluent builder like:

```cs
new Page()
    .Resources(resources => resources
        .Add("MyColor", Colors.Red)
        .Add("MyBrush", new SolidColorBrush(Colors.Red))
)
```

### Resource Conversions

There may be times when it may be difficult to provide a resource using the correct type. For example, you may want to provide a HEX string for a given color, but not want to convert the HEX values to bytes to create the color. The Markup library will use the built in converters to attempt to convert the supplied value to the correct type. For these sorts of resources it is recommended that you use the generic overload of the `Add` method which will convert the resource at the time it is created in the `ResourceDictionary` rather than each time it is used.

```cs
new Page()
    .Resources(resources => resources
        .Add<Color>("MyColor", "#FF0000") // Converted on Add ONLY
        .Add("MyOtherColor", "#676767") // Converted EVERY time it is used
    )
    .Content(new TextBlock()
        .Foreground(new SolidColorBrush()
            .Color(StaticResource.Get<Color>("MyColor"))))
```

> [!TIP]
> Existing XAML based projects may be using string resources for the path of an `Icon`. When using Markup be sure to convert this to a Geometry resource rather than leaving it a string. For example: `resources.Add<Geometry>("MyIcon", "M 0 0 L 10 10");`
> While it will work either way, this will help your app's performance by ensuring the icons are only converted once and not each time they are used.

### Implicit Styles

To add an implicit style be sure to use the [`Style<T>`](xref:Uno.Extensions.Markup.Styles). You can simply pass in the `Style<T>` and do not need to specify a key.

```cs
new Page()
    .Resources(resources => resources
        .Add(new Style<TextBlock>()
            .Setters(s => s.FontSize(14))
        )
    )
```

### Theme Resources

Theme Resources build off of what you have already seen, except they give you the ability to provide both a Light and Dark theme resource.

```cs
new Page()
    .Resources(resources => resources
        .Add("MyColor", Colors.Red, Colors.White))
```

Similarly with Implicit Styles you can simply pass in a Light and Dark theme version of your style and it will be added to the Resource Dictionary ThemeDictionaries.

```cs
new Page()
    .Resources(resources => resources
        .Add(
            // Light Theme Style
            new Style<TextBlock>()
                .Setters(s => s.Foreground(Colors.Black)),
            // Dark Theme Style
            new Style<TextBlock>()
                .Setters(s => s.Foreground(Colors.White))
        )
    )
```

### Adding Theme Dictionaries

To add a Theme Dictionary you can use the fluent builder like:

```cs
new Page()
    .Resources(resources => resources
        .Theme("MyKey", new MyThemeDictionary()))
```

## Creating Resources

One of the benefits of C# Markup is a strongly typed context. For this reason we have added a few helpers to make it easier to create and reference Resources eliminating, the need to use magic strings and while maintaining type safety.

```cs
public static class MyResources
{
    public static readonly Resource<Geometry> MyIcon =
        StaticResource.Create<Geometry>(nameof(MyIcon), "M 0 0 L 10 10");
}
```

The API is meant to be self documenting, as you can see that we are working with a `Resource<Geometry>` and we are creating a `StaticResource`. Similarly we can create a ThemeResource like:

```cs
public static class MyResources
{
    public static readonly Resource<Color> MyColor =
        ThemeResource.Create<Color>(nameof(MyColor), "#FF0000", "#FFFFFF");
}
```

Whether we have a `StaticResource` or `ThemeResource`, once we have it defined we can use it in our markup like:

```cs
public class MyPage : Page
{
    public MyPage()
    {
        this.Resources(r => r.Add(MyResources.MyIcon))
            .Content(new PathIcon()
                .Data(MyResources.MyIcon));
    }
}
```

It's important to note that this is not simply returning the value that was defined when we created the resource.

Instead when being added to the `ResourceDictionaryBuilder` it will extract the key and value(s) to be added. Subsequently, when referenced in the `Data` property in the above example it is implicitly converted to an `Action<IDepenendencyPropertyBuilder<Geometry>>` giving you the equivalent of:

```cs
new PathIcon()
    .Data(x => x.StaticResource("MyIcon"))
```
