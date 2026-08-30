---
uid: Uno.Extensions.Maui.ThirdParty.Telerik
---
# .NET MAUI Embedding - Telerik UI for .NET MAUI

The controls in the Telerik UI for .NET MAUI can be used in an Uno Platform application via .NET MAUI Embedding.

## Sample App

An existing sample app that showcases the controls is available [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MauiEmbedding/TelerikApp).

> [!NOTE]
> Telerik SDK for .NET is currently only compatible with Windows, Android, iOS, and Mac Catalyst when used with Uno Platform at the moment.

## Installation

In order to use the Telerik controls, you first need to create an account and order a license to use the controls. You can find more instructions [here](https://www.telerik.com/). Then follow the instructions to have access to the private NuGet feed.

## Getting Started

### [Visual Studio](#tab/vs)

> [!NOTE]
> If you don't have the **Uno Platform Extension for Visual Studio** installed, follow [these instructions](xref:Uno.GetStarted.vs2022).
- Launch **Visual Studio** and click on **Create new project** on the Start Window. Alternatively, if you're already in Visual Studio, click **New, Project** from the **File** menu.

- Type `Uno Platform` in the search box

- Click **Uno Platform App**, then **Next**

- Name the project `TelerikApp` and click **Create**

At this point you'll enter the **Uno Platform Template Wizard**, giving you options to customize the generated application.

- Select **Blank** in **Presets** selection

- Select the **Platforms** tab and unselect **Desktop** and **Web Assembly** platforms

- Select the **Features** tab and click on **.NET MAUI Embedding**

- Click **Create** to complete the wizard

The template will create a solution with a single cross-platform project, named `TelerikApp`, ready to run.

For more information on all the template options, see [Using the Uno Platform Template](xref:Uno.GettingStarted.UsingWizard).

### [Command Line](#tab/cli)

> [!NOTE]
> If you don't have the **Uno Platform dotnet new templates** installed, follow [dotnet new templates for Uno Platform](xref:Uno.GetStarted.dotnet-new).
Create a new application using the `unoapp` template, enabling .NET MAUI Embedding. In this case, we're going to use the Blank template (`-preset blank`) and include .NET MAUI Embedding support (`-maui`).

```bash
dotnet new unoapp -preset blank -maui -platforms "android" -platforms "ios" -platforms "maccatalyst" -platforms "windows" -o TelerikApp
```

This will create a new folder called **TelerikApp** containing the new application.

---

1. Next, add a reference to the `Telerik.UI.for.Maui` NuGet package to the `TelerikApp.MauiControls` project.

1. In the `AppBuilderExtensions` class, on `TelerikApp.MauiControls` project, update the `UseMauiControls` extension method to call the `UseTelerik` method.

    ```cs
    using Telerik.Maui.Controls;
    using Telerik.Maui.Controls.Compatibility;

    namespace TelerikApp;

    public static class AppBuilderExtensions
    {
        public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder)
            => builder
                .UseTelerik()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
                    fonts.AddFont("Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
                });
    }
    ```

## Adding RadSignaturePad Control

1. Update the EmbeddedControl.xaml in the  `TelerikApp.MauiControls` project with the following XAML that includes the `RadSignaturePad` control.

    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                xmlns:telerik="http://schemas.telerik.com/2022/xaml/maui"
                x:Class="TelerikApp.MauiControls.EmbeddedControl"
                HorizontalOptions="Fill"
                VerticalOptions="Fill">
            <telerik:RadSignaturePad x:Name="SignaturePad"
                                    HeightRequest="300"
                                    WidthRequest="300"
                                    BorderThickness="1"
                                    BorderColor="LightGray" />
    </ContentView>
    ```

1. Update the EmbeddedControl.xaml.cs with the following code.

    ```cs
    namespace TelerikApp.MauiControls;

    public partial class EmbeddedControl : ContentView
    {
        public EmbeddedControl()
        {
            InitializeComponent();
        }
    }
    ```

1. Now the project is good to go! Press F5 and you should see the `RadSignaturePad` control working as expected.

For more detailed instructions specific to each platform, refer to the [Debug the App](xref:Uno.GettingStarted.CreateAnApp.VS2022#debug-the-app) documentation.

**App Render Output**

- **Android:**
  - ![Android Telerik](Assets/Screenshots/Android/Telerik.png)

- **Windows:**
  - ![Windows Telerik](Assets/Screenshots/Windows/Telerik.png)
