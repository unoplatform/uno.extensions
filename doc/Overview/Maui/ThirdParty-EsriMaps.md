---
uid: Overview.Maui.ThirdParty.EsriMaps
---
# .NET MAUI Embedding - Esri ArcGIS Maps SDK for .NET

The MapView control, that's part of the ArcGIS Maps SDK for .NET, can be used in an Uno Platform application via .NET MAUI Embedding. 

## Sample App

An existing sample app that showcases the controls is available [here](https://github.com/unoplatform/Uno.Samples/tree/master/UI/MauiEmbedding/ArcGisApp).

> [!NOTE] 
> For this sample you don't need to have a license, because it's just a demo and for development purposes.

## Installation

In order to use the MapView control, you first need to create an account via the [ArcGIS Developers portal](https://developers.arcgis.com/sign-up/), and depending on the use of location services, you may also need an API key. This walkthrough does not require a license or an API key to run.

## Getting Started

1. Create a new application using the `unoapp` template, enabling .NET MAUI Embedding. In this case, we're going to use the Blank template (`-preset blank`) and include .NET MAUI Embedding support (`-maui`).

    ```
    dotnet new unoapp -preset blank -maui -o MauiEmbeddingApp
    ```

1. Next, add a reference to the [Esri.ArcGISRuntime.Maui NuGet package](https://www.nuget.org/packages/Esri.ArcGISRuntime.Maui) to the `MauiEmbeddingApp.MauiControls` project.

1. In the `AppBuilderExtensions` class, on `MauiEmbeddingApp.MauiControls` project, update the `UseMauiControls` extension method to call the `UseArcGISRuntime` method.

    ```cs
    using Esri.ArcGISRuntime;
    using Esri.ArcGISRuntime.Maui;
    using Esri.ArcGISRuntime.Security;

    namespace MauiEmbeddingApp;

    public static class AppBuilderExtensions
    {
        public static MauiAppBuilder UseMauiControls(this MauiAppBuilder builder) 
            => builder
                .UseArcGISRuntime(
                //config => config
                //    .UseLicense("[Your ArcGIS Maps SDK License key]")
                //    .UseApiKey("[Your ArcGIS location services API Key]")
                //    .ConfigureAuthentication(auth => auth
                //        .UseDefaultChallengeHandler() // Use the default authentication dialog
                //    )
                )
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("MauiEmbeddingApp/Assets/Fonts/OpenSansRegular.ttf", "OpenSansRegular");
                    fonts.AddFont("MauiEmbeddingApp/Assets/Fonts/OpenSansSemibold.ttf", "OpenSansSemibold");
                });
    }
    ```

    > [!NOTE]
    > If you have a license key and/or a location service API key, uncomment the `delegate` provided on `UseArcGISRuntime` method. This isn't required to run this sample.

## Adding MapView

1. Update the EmbeddedControl.xaml in the `MauiEmbedding.MauiControls` project with the following XAML that includes the `MapView` control.

    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <ContentView 
        xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
        xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
        xmlns:esriUI="clr-namespace:Esri.ArcGISRuntime.Maui;assembly=Esri.ArcGISRuntime.Maui"
        x:Class="MauiEmbeddingApp.MauiControls.EmbeddedControl">
        <Grid>
            <esriUI:MapView x:Name="mapView"
                            HeightRequest="300"
                            WidthRequest="300" />
        </Grid>
    </ContentView>
    ```

1. Update the EmbeddedControl.xaml.cs with the following code

    ```cs
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Mapping;
    using Map = Esri.ArcGISRuntime.Mapping.Map;

    namespace MauiEmbeddingApp.MauiControls;

    public partial class EmbeddedControl : ContentView
    {
        public EmbeddedControl()
        {
            InitializeComponent();

            var myMap = new Map()
            {
                InitialViewpoint = new Viewpoint(new Envelope(-180, -85, 180, 85, SpatialReferences.Wgs84)),
                Basemap = new Basemap(new Uri("https://arcgis.com/home/item.html?id=86265e5a4bbb4187a59719cf134e0018"))
            };

            // Assign the map to the MapView.
            mapView.Map = myMap;
        }
    }
    ```

1. Now the project is good to go! Press F5 and should see the MapView control running on your application.