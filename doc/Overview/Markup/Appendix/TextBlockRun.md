---
uid: Uno.Extensions.Markup.Appendix.TextBlockRun
---
# TextBlock Run

While we make every effort to keep the API as close as possible to what you might expect from the equivalent XAML one area we have had to introduce an entirely new API is with the Run often used by TextBlocks.

```xml
<TextBlock>
  <Run Text="This text demonstrates " />
  <Span FontWeight="SemiBold">
    <Run FontStyle="Italic">the use of inlines </Run>
    <Run>with formatting.</Run>
  </Span>
</TextBlock>
```

In general for the sample XAML above the C# Markup would be exactly what you expect:

```cs
new TextBlock()
    .Inlines(
        new Run().Text("This text demonstrates "),
        new Span()
            .FontWeight(FontWeights.SemiBold)
            .Inlines(
                new Run()
                    .FontStyle(FontStyle.Italic)
                    .Text("the use of inlines "),
                new Run()
                    .Text("with formatting.")
            )
    )
```

Where we run into issues is that while Binding's are supported in XAML, there is no public `DependencyProperty` on the managed Run class in C# for the Text property. As a result the C# Markup generators are not able provide an extension with the `Action<IDependencyPropertyBuilder<string>>` which will as a result, prevent you from binding values to the Run's Text property. In order to solve this issue you must instead use the `MarkupRun`. This exposes a proper `DependencyProperty` that allows us to create the binding and update the Run's displayed text.

```cs
// Long Form
new MarkupRun().Text(x => x.Bind(() => vm.Text))

// Short Form
new MarkupRun().Text(() => vm.Text)
```

> [!NOTE]
> This issue is currently being tracked at [microsoft/microsoft-ui-xaml#8253](https://github.com/microsoft/microsoft-ui-xaml/issues/8253).
