---
uid: Uno.Extensions.Markup.Storyboards
---
# Getting Started with Storyboards

To start with Storyboards, it's important to consider for a moment how we might use them in XAML before looking at the C# Markup.

```xml
<Storyboard x:Name="myStoryboard1">
  <DoubleAnimation
    Storyboard.TargetName="MyAnimatedRectangle"
    Storyboard.TargetProperty="Opacity"
    From="1.0" To="0.0" Duration="0:0:1"/>
</Storyboard>
```

```xml
<Rectangle x:Name="MyAnimatedRectangle" />
```

With the Storyboard there are really 2 elements at play that we need to replicate in our C# Markup. The first is the `x:Name` on our Target element. The second is the attached properties on our animation. In XAML `x:Name` ef

```cs
new Rectangle().Name("MyAnimatedRectangle");
```

## Creating the Storyboard

Now that we understand how to reference our UserControls within the UI, it's time to create a Storyboard. To create the storyboard we would work with it like any other class in C# Markup using [Attached Properties](xref:Uno.Extensions.Markup.AttachedProperties):

```cs
new Storyboard().Children(
    new DoubleAnimation()
        .From(1.0)
        .To(0.0)
        .Duration(TimeSpan.FromSeconds(1))
        .Storyboard(targetName: "MyAnimatedRectangle", targetProperty: "Opacity")
)
```
