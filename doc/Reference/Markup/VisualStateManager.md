---
uid: Reference.Markup.VisualStateManager
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
		new TextBlock().Assign(out var textBlock)
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
		new TextBlock().Assign(out var textBlock)
	)
	.VisualStateManager(vsm => vsm
		.Group(group => group
			.State("SomeState", state => state
				.StateTriggers(
					new AdaptiveTrigger()
						.MinWindowWidth(StaticResource.Get<double>("WideMinWindowWidth"))
				))));
```

## Storyboards

In version 1.0 of the Markup Extensions Storyboards are not fully supported and will be addressed in a future release.

## Next Steps

Set up an Markup project

You can use this tutorial to learn how to set up an Uno Platform project to use Uno.Extensions.Markup..

- [HowToMarkupProject](xref:Reference.Markup.HowToMarkupProject)

Then start create your own projects.