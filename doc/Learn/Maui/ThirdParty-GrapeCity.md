---
uid: Uno.Extensions.Maui.ThirdParty.GrapeCity
---
# .NET MAUI Embedding - GrapeCity ComponentOne .NET MAUI Controls

The FlexGrid and Calendar in the ComponentOne .NET MAUI Controls can be used in an Uno Platform application via .NET MAUI Embedding.

## Sample App

An existing sample app that showcases the controls is available [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MauiEmbedding/GrapeCityApp).

>[!NOTE]
> GrapeCity SDK for .NET is currently only compatible with Windows, Android, iOS, and Mac Catalyst when used with Uno Platform at the moment.

## Installation

In order to use the ComponentOne controls, you first need to install the ComponentOne ControlPanel following [these instructions](https://www.grapecity.com/componentone/docs/maui/online-maui/get-started.html).

## Getting Started

### [Visual Studio](#tab/vs)

> [!NOTE]
> If you don't have the **Uno Platform Extension for Visual Studio** installed, follow [these instructions](xref:Uno.GetStarted.vs2022).
- Launch **Visual Studio** and click on **Create new project** on the Start Window. Alternatively, if you're already in Visual Studio, click **New, Project** from the **File** menu.

- Type `Uno Platform` in the search box

- Click **Uno Platform App**, then **Next**

- Name the project `GrapeCityApp` and click **Create**

At this point you'll enter the **Uno Platform Template Wizard**, giving you options to customize the generated application.

- Select **Blank** in **Presets** selection

- Select the **Platforms** tab and unselect **Desktop** and **Web Assembly** platforms

- Select the **Features** tab and click on **.NET MAUI Embedding**

- Click **Create** to complete the wizard

The template will create a solution with a single cross-platform project, named `GrapeCityApp`, ready to run.

For more information on all the template options, see [Using the Uno Platform Template](xref:Uno.GettingStarted.UsingWizard).

### [Command Line](#tab/cli)

> [!NOTE]
> If you don't have the **Uno Platform dotnet new templates** installed, follow [dotnet new templates for Uno Platform](xref:Uno.GetStarted.dotnet-new).
Create a new application using the `unoapp` template, enabling .NET MAUI Embedding. In this case, we're going to use the Blank template (`-preset blank`) and include .NET MAUI Embedding support (`-maui`).

```bash
dotnet new unoapp -preset blank -maui -platforms "android" -platforms "ios" -platforms "maccatalyst" -platforms "windows" -o GrapeCityApp
```

This will create a new folder called **GrapeCityApp** containing the new application.

---

2. Next, add a reference to the C1 .NET MAUI NuGet packages to the MauiEmbeddingApp.MauiControls project. If you want to use the FlexGrid control, add a reference to [`C1.Maui.Grid`](https://www.nuget.org/packages/C1.Maui.Grid). If you want to use the Calendar control, add a reference to [`C1.Maui.Calendar`](https://www.nuget.org/packages/C1.Maui.Calendar).

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
                    fonts.AddFont("Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
                    fonts.AddFont("Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
                });
    }
    ```

## Adding FlexGrid

1. Follow the [FlexGrid Quick Start](https://www.grapecity.com/componentone/docs/maui/online-maui/flexgrid-quickstart.html) to apply XAML to the EmbeddedControl.xaml and C# code to the EmbeddedControl.xaml.cs (in the constructor).

1. Add Customer class (see https://www.grapecity.com/componentone/docs/maui/online-maui/customerclass.html).

1. Wrap content in `ScrollViewer` on the `MainPage` of the Uno Platform application to make sure the full `FlexGrid` and other controls can be scrolled into the view.

1. Now the project is good to go! Press F5 and should see the MapView control running on your application.

For more detailed instructions specific to each platform, refer to the [Debug the App](xref:Uno.GettingStarted.CreateAnApp.VS2022#debug-the-app) documentation.

**App Render Output**

- **Android:**
  - ![Android FlexGrid](Assets/Screenshots/Android/C1_FlexGrid.png)

- **Windows:**
  - ![Windows FlexGrid](Assets/Screenshots/Windows/C1_FlexGrid.png)


## Adding Calendar

1. Follow the [Calendar Quick Start](https://www.grapecity.com/componentone/docs/maui/online-maui/calendarquickstart.html) to apply XAML to the EmbeddedControl.xaml.

1. In order for the Calendar control to render correctly on all platforms you should set both `HeightRequest` and `WidthRequest` attributes on the Calendar control.

**App Render Output**

- **Android:**
  - ![Android Calendar](Assets/Screenshots/Android/C1_Calendar.png)

- **Windows:**
  - ![Windows Calendar](Assets/Screenshots/Windows/C1_Calendar.png)
