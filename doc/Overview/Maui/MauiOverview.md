---
uid: Overview.Maui
---
# .NET Maui Embedding

.NET Maui Embedding provides limited support for Uno Applications to make use of 3rd party control libraries when the required app platforms match one of those available from .NET MAUI.

## Initialization

After installing the `Uno.Extensions.Maui.WinUI` NuGet package you will need to update your App.cs in the core/shared project of your Uno Platform Project.

```cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    this.UseMauiEmbedding();
}
```

Similarly if you are using Uno.Extensions.Hosting for Dependency Injection you can call the `UseMauiEmbedding()` extension off of the `IApplicationBuilder` like:

```cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .UseMauiEmbedding();
}
```

In the event that your 3rd party control library requires initialization on the `MauiAppBuilder` you can initialize those libraries by simply providing a delegate for the `MauiAppBuilder` in the `UseMauiEmbedding()` extension method:

```cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .UseMauiEmbedding(maui => maui
            .UseTelerikControls())
}
```

## Getting Started

With Maui Initialized we are now able to make use of the MauiContent control provided by Uno.Extensions.Maui.WinUI.

```xml
<Page x:Class="DemoTelerikApp.Presentation.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:embed="using:Uno.Extensions.Maui"
      xmlns:maui="using:Microsoft.Maui.Controls"
      Background="{ThemeResource BackgroundBrush}">

  <embed:MauiContent>
    <maui:Label Text="Hello From .NET MAUI &amp; Uno Platform" />
  </embed:MauiContent>
</Page>
```

### Resources & Styles

When the MauiContent is created it will walk the Visual Tree and look for any parents including `Application.Current` which have a ResourceDictionary which has Resources and/or Styles. It will then do it's best to bring these over to the available MAUI Resources. This includes being able to reuse Colors, Brushes, Thickness, and Converters as well as some limited support for Styles.

### XAML Extensions

There are several XAML Extensions that you may decide to make use of while building an app with .NET MAUI Embedding.

- MauiBinding - As the name suggests this will allow you to create a binding to your DataContext. **NOTE** You can not use `x:Bind` or `Binding` on a MAUI Control.
- MauiColor: If you want to be able to directly apply a Hex string or `Color` name to a `Color` property on a MAUI Control you can use the `MauiColor` Extension to provide that value.
- MauiThickness - If you want to be able to directly apply a Thickness, you can use the MauiThickness extension to provide a value like `10`, `10,20`, or `10,20,10,5`
- MauiResource: As explained in the previous section, the `MauiContent` control will bring over the WinUI Resources automatically to make it easier to provide consistent styling even within the `MauiContent` of your View. This will allow you to provide a resource Key to apply to a given property.

```xml
<maui:VerticalStackLayout BackgroundColor="{embed:MauiColor Value='#ABABAB'}">
  <maui:Label Text="{embed:MauiBinding Path='Message'}" />
</maui:VerticalStackLayout>
```

### Limitations and Known issues

- Some controls like the ScrollView from .NET MAUI will not have the content property automatically recognized by the XAML compiler. As a result you will need to be more verbose with these controls like:
  ```xml
  <ScrollView>
    <ScrollView.Content>
      <!-- My Content -->
    </ScrollView.Content>
  </ScrollView>
  ```
- Common type conversions such as hex string or color name to Maui Graphics Color will not work with the XAML Compiler. Similarly types like Thickness `10` or `10,20` will not be picked up and converted by the XAML Compiler. For these primitive types especially it is recommended to simply provide a WinUI Thickness or Color in your Resource Dictionary. These native types will be automatically converted to MAUI types and available to use with the `MauiResource` XAML Extension.
- .NET MAUI Hot Reload will throw an exception and cause the app the crash. You will need to disable this in Visual Studio for now.
