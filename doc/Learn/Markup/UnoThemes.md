---
uid: Uno.Extensions.Markup.UnoThemes
---

# Using Uno.Themes.WinUI.Markup

The [Uno.Themes.WinUI.Markup](https://www.nuget.org/packages/Uno.Themes.WinUI.Markup) package is designed to make it easier to use the [Uno.Material Theme](xref:Uno.Themes.Material.GetStarted). This provides a strongly typed Theme API that is descriptive and self-documenting for all of the available theme resources including Colors, Brushes, and Styles. If you have already read the docs for [Static &amp; Theme Resources](xref:Uno.Extensions.Markup.StaticAndThemeResources), this should feel familiar.

```csharp
new Button()
    .Style(Theme.Button.Resources.Styles.Elevated)
```

Using the API in the Uno.Themes has the added benefit of making built in styles more easily discoverable, easier to use, and apparent when they are being used correctly or incorrectly.
