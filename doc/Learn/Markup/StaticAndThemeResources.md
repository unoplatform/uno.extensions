---
uid: Uno.Extensions.Markup.StaticAndThemeResources
---

# Using Static and Theme Resources

Using Static or Theme Resources is built on top of the `IDependencyPropertyBuilder<T>`. This allows the greatest degree of flexibility and extensibility.

To get started let's assume that we have a string resource called `AppTitle` in our `ResourceDictionary`. This could be either on the element itself, somewhere within the Visual Tree, or even within the `Application.Resources`. In this case, we might provide the value like:

```csharp
new TextBlock()
    .Text(x => x.StaticResource("AppTitle"))
```

Similarly to this we might want to use a `ThemeResource` such as `ApplicationPageBackgroundThemeBrush` to set the Background of our `Page`. In this case we will see we can use nearly identical syntax like:

```csharp
public partial class MyPage : Page
{
    public MyPage()
    {
        this.Background(x => x.ThemeResource("ApplicationPageBackgroundThemeBrush"));
    }
}
```

## Using Strongly Typed Resources

One of the great benefits of using C# Markup of course is the strong typing. There may be many times when we might simply be able to provide a class like:

```csharp
public static class MyResources
{
    public const string MyString = "Hello from C# Markup";
}
```

This of course works great because we can bypass the overhead of looking up and applying a Resource that could be located anywhere in the Visual Tree from the element we are working on up to the `Window.Content` or within the `Application.Resources`. It simplifies our code to simply set the value like:

```csharp
new TextBlock()
    .Text(MyResources.MyString)
```

Unfortunately, app development is often not that simple. As a result it is helpful to find ways that we can make use of resources from our markup code. C# Markup provides us a few useful API's to achieve our goals.

### Creating a new Resource

Many times we may want to create resources, but we also want to be able to easily reference them later. Let's start by looking at a case where we need to define a path string that we will use later for an icon.

```cs
public static class MyResources
{
    public static readonly Resource<Geometry> MyIcon =
        StaticResource.Create<Geometry>(nameof(MyIcon), "{Path String}");
}
```

Looking at this sample we actually are achieving multiple things. One is that we are able to more easily make use of our path string as we can provide it the generic argument for a `Geometry` object which is what will be expected when we use it. We also are able to avoid trying to figure out how to convert it.

Lastly, in the case of WinUI we get the advantage that this will be created as a `Geometry` object each time we need it, this will keep it from being parented and prevent it from being reused later.

Now to use our `Resource<Geometry>`, we can simply reference it similar to our constant string, except that we need to also add it to a `ResourceDictionary` somewhere in the Visual Tree.

```csharp
public partial class MyPage : Page
{
    public MyPage()
    {
        this.Resources(r => r.Add(MyResources.MyIcon))
            .Content(
                new PathIcon()
                    .Data(MyResources.MyIcon)
            );
    }
}
```

Now let's look at the equivalent code of what this is in effect doing:

```csharp
public partial class MyPage : Page
{
    public MyPage()
    {
        this.Resources(r => r.Add<Geometry>("MyIcon", "{path string}"))
            .Content(
                new PathIcon()
                    .Data(x => x.StaticResource("MyIcon"))
            );
    }
}
```

### Getting an Existing Resource

Similarly, we may not want to use the lambda directly. In this case, we can again use the `StaticResource` type to get our resource. This can also be helpful for resources that you may not get as often and which may be added for you already to the Resources of a 3rd Party `ResourceDictionary` you have brought in.

```csharp
new PathIcon()
    .Data(StaticResource.Get<Geometry>("MyIcon"))
```

### Strongly Typed Keys

Along this same path we may want to use a resource frequently, but we don't need to handle creating the resource or adding it to the Resource Dictionary. In this case we might want to instead use the `StaticResourceKey<T>` similar to our `Resource<T>`. In this case we would simply update our `MyResources` class like follows:

```csharp
public static class MyResources
{
    public static readonly StaticResourceKey<Geometry> MyIcon = new StaticResourceKey<Geometry>(nameof(MyIcon));
}
```

Since we do not need to add it to the Resource Dictionary as it may have come from a Merged Dictionary higher up in the Visual Tree we now might have code that simply looks like:

```csharp
public partial class MyPage : Page
{
    public MyPage()
    {
        this.Content(
            new PathIcon()
                .Data(MyResources.MyIcon)
        );
    }
}
```

### Theme Resources

Similar to the Static Resource's, Theme Resources have all of the same basic helpers:

```csharp
public static class MyResources
{
    public static Action<IDependencyPropertyBuilder<Color>> MyThemeColor =>
        ThemeResource.Get<Color>("MyThemeColor");

    public static readonly ThemeResourceKey<Color> MyOtherColor =
        new ThemeResourceKey<Color>("MyOtherColor");
}
```

You may notice in the above example, we don't use `ThemeResource.Create`. This is because this is the one area where as you might imagine there is a small difference. To create a ThemeResource we simple would update our code to provide a 2nd value where the first value is used for the Light Theme and the 2nd value is used for the Dark Theme.

```csharp
public static class MyResources
{
    public static readonly Resource<Color> MyColor =
        ThemeResource.Create<Color>(nameof(MyColor), "#F6F6F6");
}
```

## Additional Reading

- [Uno.Themes.WinUI.Markup](xref:Uno.Extensions.Markup.UnoThemes)
