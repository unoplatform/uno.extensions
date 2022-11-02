---
uid: Reference.Markup.Resources
---
# Resource Dictionaries

All Framework Elements and the Application have a couple of special extensions to help you manage your resources.

## Merging Resource Dictionaries

To merge resource dictionaries you simply need to add a Resource Dictionary to the Application or Framework Element like:

```cs
new Page()
	// Where MyColorResources is a ResourceDictionary
	.Resources(new MyColorResources())
```

## Adding Resources

To add a resource to the Resource Dictionary you can use the fluent builder like:

```cs
new Page()
	.Resources(resources => resources
		.Add("MyColor", Colors.Red)
		.Add("MyBrush", new SolidColorBrush(Colors.Red))
)
```

### Resource Conversions

There may be times when it may be difficult to provide a resource as the correct type. For example, you may want to provide a HEX string for a given color, but not want to convert the HEX values to bytes to create the color. The Markup library will use the built in converters to attempt to convert the supplied value to the correct type. For these sorts of resources it is recommended that you use the generic overload of the Add method which will convert the resource at the time it is created in the Resource Dictionary rather than each time it is used.

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
> Existing XAML based projects may be using string resources for the path of an Icon. When using Markup be sure to convert this to a Geometry resource rather than leaving it a string. For example: `resources.Add<Geometry>("MyIcon", "M 0 0 L 10 10");`
> While it will work either way, this will help your app's performance by ensuring the icons are only converted once and not each time they are used.

### Implicit Styles

To add an implicit style be sure to use the [`Style<T>`](Styles.md). You can simply pass in the `Style<T>` and do not need to specify a key.

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
			new Style<TextBlock>()
				.Setters(s => s.Foreground(Colors.Black)),
			new Style<TextBlock>()
				.Setters(s => s.Foreground(Colors.White))
		)
	)
```

### Adding Merged Dictionaries

To add a merged Dictionary you can use the fluent builder like:

```cs
new Page()
	.Resources(resources => resources
		.Merged(new MyOtherResources()))
```

### Adding Theme Dictionaries

To add a Theme Dictionary you can use the fluent builder like:

```cs
new Page()
	.Resources(resources => resources
		.Theme("MyKey", new MyThemeDictionary()))
```
