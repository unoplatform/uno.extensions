---
uid: Overview.Maui.GrapeCity
---
# .NET MAUI Embedding - GrapeCity

The ComponentOne FlexGrid and Calendar controls from GrapeCity can be used in an Uno Platform application via .NET MAUI Embedding. 

## Installation

In order to use the ComponentOne controls, you first need to install the ComponentOne ControlPanel following [these instructions](https://www.grapecity.com/componentone/docs/maui/online-maui/get-started.html).

## Getting Started

Create a new application using the `unoapp` template, enabling .NET MAUI Embedding. In this case we're going to use the Blank preset to keep things simple.

`dotnet new unoapp -preset blank -maui -o GrapeCityApp`

Next, add a reference to the C1 .NET MAUI nuget packages to the GrapeCity.MauiControls library. If you want to use the FlexGrid, add a reference to C1.Maui.Grid; if you want to use the Calendar, add a reference to C1.Maui.Calendar.  

In the `AppBuilderExtensions` class, update the `UseMauiControls` extension method to call either, or both, the `RegisterFlexGridControls` or `RegisterCalendarControls` methods.

```cs
using C1.Maui.Grid;
using C1.Maui.Calendar;
using CommunityToolkit.Maui;

namespace GrapeCityApp;

public static class AppBuilderExtensions
{
	public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder) =>
		builder.UseMauiCommunityToolkit()
			.RegisterFlexGridControls()
      .RegisterCalendarControls();
}
```

The last thing required in order for the ComponentOne controls to work is to make sure there is a MainPage value set for the current application. This is to prevent an exception being raised when ComponentOne attempts a license check.

```cs
public EmbeddedControl()
{
  // Required to prevent application crashing during license check
	Application.Current.MainPage = new Page();

	InitializeComponent();
}
```

## Adding FlexGrid

Follow the [FlexGrid Quick Start](https://www.grapecity.com/componentone/docs/maui/online-maui/flexgrid-quickstart.html) by applying XAML to the EmbeddedControl.xaml and c# to the EmbeddedControl.Xaml.cs (in constructor)

Add Customer class (see https://www.grapecity.com/componentone/docs/maui/online-maui/customerclass.html)

Wrap content in ScrollViewer on MainPage of the Uno Platform application to make sure the full FlexGrid and other controls can be scrolled into view.


## Adding Calendar

Follow the [Calendar Quick Start](https://www.grapecity.com/componentone/docs/maui/online-maui/calendarquickstart.html) by applying XAML to the EmbeddedControl.xaml

In order for the Calendar control to render correctly on all platforms you should set both `HeightRequest` and `WidthRequest` attributes on the Calendar control.

