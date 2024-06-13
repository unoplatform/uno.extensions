---
uid: Uno.Extensions.Markup.Upgrading
---

# Upgrading C# Markup

## Changes in v5.2

### ItemsPanel Property

The signature for the `ItemsPanel` property extension method has changed to a strongly typed generic method that accepts a delegate to configure the root element of the `ItemsPanelTemplate`.

The `ItemsPanel` property extension method that previously looked like this:

```cs
new ListView()
    .ItemsPanel(() => new ItemsStackPanel()
        .Orientation(Orientation.Vertical))
```

Should now be updated to:

```cs
new ListView()
    .ItemsPanel<ItemsStackPanel>(panel => panel.Orientation(Orientation.Vertical))
```

To learn more about different types of templates in C# Markup, see the [Templates section](xref:Uno.Extensions.Markup.Templates).
