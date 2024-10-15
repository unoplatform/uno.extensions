---
uid: Uno.Extensions.Markup.VisualStateManager
---
# Visual State Manager

Similar to other API's the `VisualStateManager` provides a fluent Builder API to help adding one or more groups, each with one or more states. In its simplest form, adding a group with a single state like this:

```cs
new Grid()
    .VisualStateManager(vsm => vsm
        .Group(group => group
            .State("SomeState", state => state
                .Setters(grid => grid.Background(StaticResource.Get<Brush>("SomeResource"))))))
```

## Updating Children

When updating the children of a given control, it is important to ensure that these children exist in scope. You can provide a reference to a given control instance for the `Setters` to update those specific to the current instance.

```cs
new Grid()
    .Children(
        new TextBlock().Name(out var textBlock)
    )
    .VisualStateManager(vsm => vsm
        .Group(group => group
            .State("SomeState", state => state
                .Setters(grid => grid.Background(StaticResource.Get<Brush>("SomeResource")))
                .Setters(textBlock, tb => tb.Text("You are in Some State")))));
```

## Using Triggers

In addition to adding various style setters to a given state, it is also possible to add one or more `Triggers` to a Visual State.

```cs
new Grid()
    .Children(
        new TextBlock().Name(out var textBlock)
    )
    .VisualStateManager(vsm => vsm
        .Group(group => group
            .State("SomeState", state => state
                .StateTriggers(
                    new AdaptiveTrigger()
                        .MinWindowWidth(StaticResource.Get<double>("WideMinWindowWidth"))
                ))));
```

> [!IMPORTANT]  
> When you use `StateTriggers`, ensure that the `VisualStateGroup` is declared under the first child of the root of a templated control in order for the triggers to take effect automatically. See [WinUI documentation](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.visualstate#remarks) for more information.

## Next Steps

- [Storyboards](xref:Uno.Extensions.Markup.Storyboards)
