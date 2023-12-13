---
uid: Overview.Markup.Appendix.AccessingControlInstance
---

# Accessing a Control Instance

There are a number of times where you may find yourself wanting to access the instance of a control that you initialized. This could be useful for bindings or even to later configure Event Handlers on the control. You can most easily do this with the `Assign` method as follows:

```cs
this.Content(
    new StackPanel()
        .Children(
            new Button()
                .Assign(out var button)
                .Content("Press Me")
        )
);

int i = 1;
button.Click += delegate {
    button.Content = $"Clicked {i++} times";
}
```
