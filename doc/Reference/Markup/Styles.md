---
uid: Reference.Markup.Styles
---
# Styles

Creating Styles with C# Markup is easy and intuitive using the `Style<T>` with extensions that let you easily set the value of a property the same as you might on a given control.

```cs
new Style<TextBlock>()
	.Setters(s => s.FontSize(14))
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

Sometimes you might want to use a Static or Theme resource on your style property. In these cases you can continue to use the Dependency Property Builder to set the value of the setter on a given property the same as you might when setting the value on a control instance.

```cs
new Style<Button>()
	.Setters(s => s.Background(StaticResource.Get<Brush>("MyBrush")))
```

Alternatively if you are using a Theme Resource from Uno.Material you can use Uno.Themes.WinUI.Markup to use a Theme Resource like:

```cs
new Style<Button>()
	.Setters(Theme.Brushes.Primary.Default)
```

## Templates

You can similarly provide a template on a Style similar to how might

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
