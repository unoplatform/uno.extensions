---
uid: Uno.Extensions.Reactive.Upgrading
---

# Upgrading MVUX

## Upgrading to Extensions Reactive V5

Upgrading to Uno.Extension.Reactive V5 should not require any changes to your application code. However, there are new features and improvements that you may want to take advantage of.

### MVUX Generated Code

In V4, the MVUX analyzer generates a bindable proxy for each of the models in your app. For example, the bindable proxy for `MainModel` was named `BindableMainModel`.

This behavior has changed in V5, and the MVUX analyzer will now generate a ViewModel class for each of your app's models. For example, the generated code for `MainModel` will be named `MainViewModel`.

> [!IMPORTANT]
> You don't have to do anything if you update but want to keep the old behavior. The generator will continue to generate the bindable proxies.

To upgrade your application and use the latest generator, you must set an assembly level attribute, `BindableGenerationTool`, in the `GlobalUsing.cs`.

```csharp
// GlobalUsing.cs
[assembly: Uno.Extensions.Reactive.Config.BindableGenerationTool(3)]
```

You will also need to update the references to the generated classes. These are usually referenced in the code-behind file when using XAML, for example, `MainPage.xaml.cs`, or in the Page UI definition when using C# for Markup, for example, `MainPage.cs`.

```csharp
// V4
public MainPage()
{
    this.InitializeComponent();
    DataContext = new BindableMainModel();
}
```

```csharp
// V5
public MainPage()
{
    this.InitializeComponent();
    DataContext = new MainViewModel();
}
```
