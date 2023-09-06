---
uid: Overview.Maui.GrapeCity
---
# .NET MAUI Embedding - GrapeCity

The ComponentOne FlexGrid and Calendar controls from GrapeCity can be used in an Uno Platform application via .NET MAUI Embedding. 

## Installation

In order to use the ComponentOne controls, you first need to install the ComponentOne ControlPanel following [these instructions](https://www.grapecity.com/componentone/docs/maui/online-maui/get-started.html).

## Getting Started

1. Create a new application using the `unoapp` template, enabling .NET MAUI Embedding. In this case we're going to use the Blank template (`-preset blank`) and include .NET MAUI Embedding support (`-maui`).

    ```
    dotnet new unoapp -preset blank -maui -o GrapeCityApp
    ```

1. Remove the `net7.0` (or `net8.0`) target framework from both the GrapeCityApp and GrapeCityApp.MauiControls projects.  

1. Next, add a reference to the C1 .NET MAUI NuGet packages to the GrapeCityApp.MauiControls library. If you want to use the FlexGrid, add a reference to [`C1.Maui.Grid`](https://www.nuget.org/packages/C1.Maui.Grid); if you want to use the Calendar, add a reference to [`C1.Maui.Calendar`](https://www.nuget.org/packages/C1.Maui.Calendar).  

1. In the `AppBuilderExtensions` class, update the `UseMauiControls` extension method to call either, or both, the `RegisterFlexGridControls` or `RegisterCalendarControls` methods.  

```cs
using C1.Maui.Grid;
using C1.Maui.Calendar;

namespace GrapeCityApp;

public static class AppBuilderExtensions
{
	public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder) 
		=> builder
			.RegisterFlexGridControls()
			.RegisterCalendarControls()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("GrapeCityApp/Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
				fonts.AddFont("GrapeCityApp/Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
			});
}
```

## Adding FlexGrid

1. Follow the [FlexGrid Quick Start](https://www.grapecity.com/componentone/docs/maui/online-maui/flexgrid-quickstart.html) by applying XAML to the EmbeddedControl.xaml and c# to the EmbeddedControl.Xaml.cs (in constructor)  

1. Add Customer class (see https://www.grapecity.com/componentone/docs/maui/online-maui/customerclass.html)  

1. Wrap content in ScrollViewer on MainPage of the Uno Platform application to make sure the full FlexGrid and other controls can be scrolled into view.  


## Adding Calendar

1. Follow the [Calendar Quick Start](https://www.grapecity.com/componentone/docs/maui/online-maui/calendarquickstart.html) by applying XAML to the EmbeddedControl.xaml.  

1. In order for the Calendar control to render correctly on all platforms you should set both `HeightRequest` and `WidthRequest` attributes on the Calendar control.  


## Sample

A sample application that features GrapeCity controls is available [here](https://github.com/unoplatform/Uno.Samples/tree/dev/master/UI/MauiEmbedding/GrapeCityApp). The samples requires the ComponentOne ControlPanel to be installed.
