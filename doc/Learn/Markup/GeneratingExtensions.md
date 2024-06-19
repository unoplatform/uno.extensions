---
uid: Uno.Extensions.Markup.GeneratingExtensions
---
# Generating Extensions

[**Uno.Extensions.Markup.Generator**](https://www.nuget.org/packages/Uno.Extensions.Markup.Generators) is a source generator that will scan one or more assemblies for you to create the C# Markup Extensions for you to use. Once you have added the Source Generator to a given project it will scan that project and automatically generate extensions for the types that are found.

## Pre-Generated Markup Extensions

The Uno Platform team is shipping a number of pre-generated extension libraries for building your apps with C# Markup. You can find the package with the naming convention `{package name}.Markup`. Some common ones you may want to use are:

- [Uno.WinUI.Markup](https://www.nuget.org/packages/Uno.WinUI.Markup)
- [Uno.Toolkit.WinUI.Markup](https://www.nuget.org/packages/Uno.Toolkit.WinUI.Markup)
- [Uno.Material.WinUI.Markup](https://www.nuget.org/packages/Uno.Material.WinUI.Markup)
- [Uno.Extensions.Navigation.WinUI.Markup](https://www.nuget.org/packages/Uno.Extensions.Navigation.WinUI.Markup)
- [Uno.Extensions.Reactive.WinUI.Markup](https://www.nuget.org/packages/Uno.Extensions.Reactive.WinUI.Markup)
- [Uno.Themes.WinUI.Markup](https://www.nuget.org/packages/Uno.Themes.WinUI.Markup)

## Using the Generator for 3rd Party Libraries

To generate extensions for another assembly (i.e. from a NuGet dependency) you can add the `GenerateMarkupForAssembly` attribute to the assembly with a specified reference type from the assembly to scan.

```cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Generator;

[assembly: GenerateMarkupForAssembly(typeof(FrameworkElement))]
```

> [!TIP]
> If you do not add the reference to the Generator NuGet this attribute will be ignored and no source will be generated.
