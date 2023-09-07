---
uid: Overview.Maui.ThirdParty.MauiCommunityToolkit
---
# .NET MAUI Embedding - MauiCommunityToolkit

The controls from MauiCommunityToolkit can be used in an Uno Platform application via .NET MAUI Embedding. 

## Sample App

An existing sample app that showcases the controls is available [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MauiEmbedding/MauiCommunityToolkitApp)

## Getting Started

1. Create a new application using the `unoapp` template, enabling .NET MAUI Embedding. In this case we're going to use the Blank template (`-preset blank`) and include .NET MAUI Embedding support (`-maui`).

    ```
    dotnet new unoapp -preset blank -maui -o MauiEmbeddingApp
    ```

1. Remove the `net7.0` target framework from both the MauiEmbeddingApp and MauiEmbeddingApp.MauiControls projects.

1. Next, add a reference to the [CommunityToolkit.Maui NuGet package](https://www.nuget.org/packages/CommunityToolkit.Maui) to the MauiEmbeddingApp.MauiControls project.

1. In the `AppBuilderExtensions` class, on `MauiEmbeddingApp.MauiControls` project, update the `UseMauiControls` extension method to call the `UseMauiCommunityToolkit` method.

    ```cs
    using CommunityToolkit.Maui;

    namespace MauiEmbeddingApp;

    public static class AppBuilderExtensions
    {
        public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder) 
            => builder
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("MauiEmbeddingApp/Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
                    fonts.AddFont("MauiEmbeddingApp/Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
                });
    }
    ```

## Adding DrawingView

1. On `MauiEmbeddingApp.MauiControls` project, add a new Xaml file that has `ContentView` as the root. Then add the `DrawingView` control, as shown in the snippet below.

    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <ContentView
        x:Class="MauiEmbeddingApp.MauiControls.DrawingViewControl"
        xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
        xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
        xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit">
        <toolkit:DrawingView
            HeightRequest="600"
            IsMultiLineModeEnabled="{Binding IsMultiLineModeEnabled}"
            LineColor="Fuchsia"
            LineWidth="5"
            ShouldClearOnFinish="{Binding ShouldCleanOnFinish}"
            WidthRequest="600" />
    </ContentView>
    ```

    > [!NOTE]
    > You may noticed that the `Binding` markup extension is used on some properties. The `MauiEmbedding` can handle bindings between Maui Controls and UnoPlatform, just make > > sure to use the name on your ViewModel.

1. On `MauiEmbeddingApp` project, find the `MainPage.xaml` file there you may find the `embed:MauiHost` control. This control will hold any `MauiControl` that you've on your `MauiEmbeddingApp.MauiControls` project. Update the `Source` property to reflect the `DrawingViewControl` that you've created on the previous step. You can see how the `MauiHost` control will look like.

    ```xml
    <embed:MauiHost
        xmlns:controls="using:MauiEmbeddingApp.MauiControls"
        xmlns:embed="using:Uno.Extensions.Maui"
        MinWidth="600"
        MinHeight="300"
        Source="controls:DrawingViewControl" />
    ```

    > [!NOTE]
    > The usage of `MinWidth` and `MinHeight` is optional

1. Now let's create the controls that will control the `DrawingView.IsMultiLineModeEnabled` and `DrawingView.ShouldClearOnFinish` properties. The final layout will be soemthing like this:

    ```xml
    <StackPanel>
        <TextBlock Text="Clean on Finish:" />
        <ToggleSwitch IsOn="{Binding ShouldCleanOnFinish, Mode=TwoWay}" />
        <TextBlock Text="MultiLineMode:" />
        <ToggleSwitch x:Name="MultiLineMode" IsOn="{Binding IsMultiLineModeEnabled, Mode=TwoWay}" />
        <embed:MauiHost
            xmlns:controls="using:MauiEmbeddingApp.MauiControls"
            xmlns:embed="using:Uno.Extensions.Maui"
            MinWidth="600"
            MinHeight="300"
            Source="controls:DrawingViewControl" />
    </StackPanel>
    ```

1. It's time to create the ViewModel that will hold the properties that will be binded to the `DrawingView` control. On `MauiEmbeddingApp` project, create a new folder called `ViewModels` and add a new class called `MainPageViewModel`. This class will have the following code:

    ```cs
    namespace MauiEmbeddingApp.ViewModels;
    partial class DrawingViewViewModel : ObservableObject
    {
        [ObservableProperty]
        bool isMultiLineModeEnabled;

        [ObservableProperty]
        bool shouldCleanOnFinish;
    }
    ```

    > [!IMPORTANT]
    > This sample is using the CommunityToolkit.MVVM, we recommend it to avoid a lot of boiler plate code, you can fid the package [here](https://www.nuget.org/packages/CommunityToolkit.Mvvm).

1. The final step is to add the `DrawingViewViewModel` as the `DataContext` of the `MainPage.xaml`. Here's all your `MainPage.xaml` should look
 
    ```xml
    <Page x:Class="MauiEmbeddingApp.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MauiEmbeddingApp.ViewModels"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <StackPanel>
            <StackPanel.DataContext>
                <local:DrawingViewViewModel />
            </StackPanel.DataContext>

            <TextBlock Text="Clean on Finish:" />
            <ToggleSwitch IsOn="{Binding ShouldCleanOnFinish, Mode=TwoWay}" />
            <TextBlock Text="MultiLineMode:" />
            <ToggleSwitch x:Name="MultiLineMode" IsOn="{Binding IsMultiLineModeEnabled, Mode=TwoWay}" />
            <embed:MauiHost
                xmlns:controls="using:MauiCommunityToolkitApp.MauiControls"
                xmlns:embed="using:Uno.Extensions.Maui"
                MinWidth="600"
                MinHeight="300"
                Source="controls:DrawingViewControl" />
        </StackPanel>
    </Page>
    ```

1. Now the project is good to go! Press F5 and you should see the `DrawingView` control working as expected. And tweaking the `ToggleSwitch` controls should change the `DrawingView` behavior.