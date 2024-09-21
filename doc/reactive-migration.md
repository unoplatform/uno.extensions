---
uid: Extensions.Migration.Reactive
---

# Upgrading Extensions Reactive Version

## Upgrading to Extesions Reactive V5

Uno.Extension.Reactive V5 is a major release that includes a number of breaking changes. The following guide will help you upgrade your application from V4 to V5.

### MVUX Generated Code

As of V4, the MVUX analyzer generated a bindable proxy for each of the models in your app. For example, the bindable proxy for `MainModel` was named `BindableMainModel`.

This behavior changed in V5, and the MVUX analyzer will now generate a ViewModel class for each of your app's models. For example, the generated code for `MainModel` is named `MainViewModel`.

To upgrade your application, you will need to update the references to the generated classes. These are usually referenced in the code-behind file when using XAML, for example, `MainPage.xaml.cs`, or in the Page UI definition when using C# for Markup, for example, `MainPage.cs`.

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
