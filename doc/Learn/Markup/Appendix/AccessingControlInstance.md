---
uid: Uno.Extensions.Markup.Appendix.AccessingControlInstance
---

# Accessing a Control Instance

There are a number of times when you may find yourself wanting to access the instance of a control that you initialized. This could be useful for bindings or even to later configure Event Handlers on the control. You can most easily do this with the `Name` method as follows:

```csharp
this.Content(
    new StackPanel()
        .Children(
            new Button()
                .Name(out var button)
                .Content("Press Me")
        )
);

int i = 1;
button.Click += delegate {
    button.Content = $"Clicked {i++} times";
}
```

You can also define a `delegate` using the `Name` method:

```csharp
int i = 1;

this.Content(
    new StackPanel()
        .Children(
            new Button()
                .Name(button => 
                {
                    button.Click += (s, e) =>
                    {
                        button.Content = $"Clicked {i++} times";
                    };
                })
                .Content("Press Me")
        )
);
```

Or if you want to expose a variable and also define a `delegate`:

```csharp
int i = 1;

this.Content(
    new StackPanel()
        .Children(
            new Button()
                .Name(out var button, (b) => 
                {
                    b.Click += (s, e) =>
                    {
                        b.Content = $"Clicked {i++} times";
                    };
                })
                .Content("Press Me")
        )
);

var buttonContent = button.Content;
```

 > [!NOTE]
 > Using the `.Name(out var button)` or `.Name(button => { ... })` syntax not only gives you a variable representing your `FrameworkElement` control but also sets the control's `Name` property to match the variable name. For example, in the scenario mentioned earlier, the `Button` would have its `Name` property set to "button" because the variable name used is `button`.
