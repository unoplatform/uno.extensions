---
uid: Reference.Markup.HowToCustomMarkupProjectTheme
---

# C# Markup project setup

In this tutorial you'll learn how to set up an Uno Platform project to use C# Markup to change the Theme. 

1. For this tutorial we will continue to use the Sample created on the [Custom your own C# Markup - Learn how to use Toolkit](xref:Reference.Markup.HowToCustomMarkupProjectToolkit) tutorial and changed on the [Custom your own C# Markup - Learn how to change Visual States and User Controls](xref:Reference.Markup.HowToCustomMarkupProjectVisualStates).

# Themes Overview

### Uno [Themes](https://platform.uno/docs/articles/external/uno.themes/doc/themes-overview.html) Styles

[Fluent](https://platform.uno/docs/articles/external/uno.themes/doc/fluent-getting-started.html) Controls Styles are the **Default Theme** for Uno Platform.
Uno Platform 3.0 and above supports control styles conforming to the Fluent design system.

[Uno.Themes](https://github.com/unoplatform/Uno.Themes) is the repository for add-ons NuGet packages that can be added to any new or existing Uno solution.

It contains two libraries

- [Material Overview](https://platform.uno/docs/articles/external/uno.themes/doc/material-getting-started.html)

- [Cupertino Overview](https://platform.uno/docs/articles/external/uno.themes/doc/cupertino-getting-started.html)

That help you style your application with a few lines of code include and change:

- Color system for both Light and Dark theme
- Styles for existing WinUI controls like Buttons, TextBox, etc.
- Change the Font of the Application

## Quick Start

In the two link above you have a Getting Started for Material and Cupertino. 

But the difference between they are the references that can be included on the Soliction.

You may notice that there are only minor differences in the XAML configuration in the link above and in this current tutorial.

Since we are using the Default Fluent Theme in the current project, we can choose to stay with it or add a different one.

Below we can see how to include and how to customize each of the two.

##  Material

Assuming that we are on the Fluent Theme and we want to change to the Material Theme, we just need to change some files (Or add some Nuget References) and it is done.

### Below is the list of files we need to modify

#### In the File Directory.Packages.props

In the Directory.Packages.props, we need to add the PackageReference bellow or using the Manage NuGet Packages and update the Versions for the Current version of the project or last version.

```xml
    <PackageVersion Include="Uno.Material.WinUI.Markup" Version="0.0.0" />	
    <PackageVersion Include="Uno.Material.WinUI" Version="0.0.0" />	
    <PackageVersion Include="Uno.Dsp.Tasks" Version="0.0.0" />
```
--------------------------------------------------------------
If you prefere you can use the Manage NuGet Packages:
In the Solution Explorer panel,
right-click on your app's App Code Library project (PROJECT_NAME.csproj)
and select Manage NuGet Packages...

**Install the following Packages**

- `Material.WinUI.Markup`, 
- `Uno.Material.WinUI` 
- `Uno.Dsp.Tasks`
--------------------------------------------------------------

#### In the File of Projects References

Add PackageReference to the PROJECT_NAME.csproj and the PROJECT_NAME.[Platform].csproj projects.

```xml
<PackageReference Include="Uno.Material.WinUI.Markup" />	
<PackageReference Include="Uno.Material.WinUI" />
<PackageReference Include="Uno.Dsp.Tasks" />
```

#### In the File GlobalUsings.cs
```csharp
global using Uno.Material;
```

**And it is that, we now have the Material Style on ours application.**

#### In the File AppResources.cs

But in case we want some customization, we need to Overrides the Colors and the Fonts.

First we need to create a Folder named Styles in the Shared Project.
Than Create a new class file to Override the ColorPallete, we named as ColorPaletteOverride.cs and add the content.

```csharp
namespace MySampleToolkitProject.Styles;

public sealed partial class ColorPaletteOverride : ResourceDictionary
{
	public ColorPaletteOverride()
	{
		this.Build(r => r
			.Add<Color>(Theme.Colors.Primary.Default.Key, "#5946D2", "#2F81D8")
			.Add<Color>(Theme.Colors.OnPrimary.Default.Key, "#FFFFFF", "#FFFFFF")
			.Add<Color>(Theme.Colors.Secondary.Default.Key, "#67E5AD", "#FEB839")
			.Add<Color>(Theme.Colors.OnSecondary.Default.Key, "#000000", "#000000")
			.Add<Color>(Theme.Colors.Background.Default.Key, "#F4F4F4", "#000000")
			.Add<Color>(Theme.Colors.OnBackground.Default.Key, "#000000", "#FFFFFF")
			.Add<Color>(Theme.Colors.Surface.Default.Key, "#FFFFFF", "#0F0F0F")
			.Add<Color>(Theme.Colors.OnSurface.Default.Key, "#000000", "#FFFFFF")
			.Add<Color>(Theme.Colors.Error.Default.Key, "#F85977", "#B2213C")
			.Add<Color>(Theme.Colors.OnError.Default.Key, "#FFFFFF", "#FFFFFF"));
	}
}
```

Than Create a new class file to Override the Fonts, we named as MaterialFontsOverride.cs.

```csharp
namespace MySampleToolkitProject.Styles;

public sealed class MaterialFontsOverride : ResourceDictionary
{
	public MaterialFontsOverride()
	{
		this.Build(r => r
			.Add<FontFamily>("MaterialLightFontFamily", "ms-appx:///Uno.Fonts.Roboto/Fonts/Material/Roboto-Light.ttf#Roboto")
			.Add<FontFamily>("MaterialMediumFontFamily", "ms-appx:///Uno.Fonts.Roboto/Fonts/Material/Roboto-Medium.ttf#Roboto")
			.Add<FontFamily>("MaterialRegularFontFamily", "ms-appx:///Uno.Fonts.Roboto/Fonts/Material/Roboto-Regular.ttf#Roboto"));
	}
}

```

And we can use this tutorial if we want to [Use the DSP Tooling in Uno.Material](https://platform.uno/docs/articles/external/uno.themes/doc/material-dsp.html).
In this tutorial, we can get exported file, and rename as the file ColorPaletteOverride.zip and add to the Style folder.

And than add the references to the AppResources.cs

```csharp
// Load Uno.UI.Toolkit and Material Resources
this.Build(r => r.Merged(
	new  MaterialTheme(
		new Styles.ColorPaletteOverride(),
		new Styles.MaterialFontsOverride())));
````

##### For a test, change the ColorPaletteOverride and run the project.

We change the Theme.Colors.Background.Default.Key and will be able to check the change if we customize the Background on the Page.

```csharp
//From
//.Add<Color>(Theme.Colors.Background.Default.Key, "#F4F4F4", "#000000")
//To
.Add<Color>(Theme.Colors.Background.Default.Key, "#A1B2C3", "#000000")
```

For use the color from the theme open the MainPage.cs and change the background of the page.

```csharp
public MainPage()
	{
		this
            //Remove the ApplicationPageBackgroundThemeBrush Background
            //.Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))

            //Use the Direct the Uno.Themes.Markup.Theme.Brushes
            //.Background(Theme.Brushes.Background.Default)

            //Or use the ResourceKeyDefinition to have the same result
            .Background(ThemeResource.Get<Brush>("BackgroundBrush"))
```

You always can back on the Material documentation and learning how the [Baseline color scheme](https://m3.material.io/styles/color/the-color-system/tokens) works


##  Cupertino

We do not suport Cupertino at the moment.

## Try it yourself

Now try to change your MainPage to have different layout and test other attributes and elements..


## Next Steps

- [Custom your own C# Markup - Learn how to change Visual States and User Controls](xref:Reference.Markup.HowToCustomMarkupProjectVisualStates)
- [Custom your own C# Markup - Learn how to use Toolkit](xref:Reference.Markup.HowToCustomMarkupProjectToolkit)
- [Custom your own C# Markup - Learn how to use MVUX](xref:Reference.Markup.HowToCustomMarkupProjectMVUX)
