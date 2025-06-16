---
uid: Uno.Extensions.Maui.Overview
---
# .NET MAUI Embedding

With .NET MAUI Embedding 3rd party control libraries for .NET MAUI can be used within an Uno Platform application. Note that these controls work only for target platforms .NET MAUI reaches – iOS, Android, MacOS, and Windows.

## Overview

The integration of .NET MAUI controls into an Uno application consists of two parts, the .NET MAUI class library and the MauiHost control.

The `MauiHost` control (that's part of the Uno.Extensions.Maui.WinUI package) is added to any Uno page or control to embed a .NET MAUI control. The `Source` property on the `MauiHost` is the type of .NET MAUI control that will be added as a child of the `MauiHost` control. As the .NET MAUI control will be added as a child, it will inherit the data context of the `MauiHost` control. The DataContext on the `MauiHost` is mapped to the `BindingContext` of the .NET MAUI control.

It's recommended that instead of hosting individual .NET MAUI controls in a `MauiHost` element, a `ContentView` should be created inside a .NET MAUI class library and set as the `Source` on a `MauiHost` element. This makes it easier to add multiple .NET MAUI controls, as well as setting properties, including data binding, in XAML.

## MAUI Embedding with Skia rendered applications

.NET MAUI Embedding is also supported in applications that use Skia rendering via [Native Control Embedding](xref:Uno.Skia.Embedding.Native).

> [!IMPORTANT]
> Native elements require non-infinite layout bounds in order to render correctly. Refer to the [Features section](xref:Uno.Skia.Embedding.Native#features) of the Native Control Embedding documentation for more information on how to use Skia rendering with native controls.

## Get Started - New Uno Application

> [!NOTE]
> Make sure to setup your environment first by [following our instructions](xref:Uno.GetStarted.vs2022).

For new applications created using the Uno Platform Template Wizard for Visual Studio, .NET MAUI Embedding can be selected as a feature when creating the application.

1. Start by selecting either the **Blank** or **Recommended** preset template.

    ![Select startup type](Assets/GettingStarted01-StartupType.png)

1. Select the Features section and include the MAUI Embedding feature, before clicking the Create button to complete the creation of the application.

    ![Select feature - .NET MAUI Embedding](Assets/GettingStarted03-MAUIEmbedding.png)

1. In addition to the typical Uno Platform application structure, there is a .NET MAUI class library (e.g. SimpleMauiApp.MauiControls). The MauiControls class library is where you add references to third-party .NET MAUI control libraries, add any services or call registration/initialization methods on the third-party libraries, and of course, define the .NET MAUI controls that you want to display within the application.

    ![Project structure](Assets/GettingStarted04-ProjectStructure.png)

1. The application can be run by selecting the desired platform in the dropdown, then pressing F5 or clicking the Run button. The default layout shows a text, "Hello Uno Platform", which is an Uno TextBlock, followed by an Image, two TextBlock, and, a Button, which are all .NET MAUI controls. For more detailed instructions specific to each platform, refer to the [Debug the App](xref:Uno.GettingStarted.CreateAnApp.VS2022#debug-the-app) documentation.

    ![Selecting target](Assets/GettingStarted04.1-ProjectStructure.png)

    ![Running on Windows](Assets/GettingStarted05-RunningOnWindows.png)

These steps can also be achieved via the command lines by invoking the following commands

```dotnetcli
dotnet new install uno.templates
dotnet new unoapp -preset blank -maui -o SimpleMauiApp
```

## Get Started - Existing Uno Application

.NET MAUI Embedding can be added to any existing Uno application via the following steps. The .NET MAUI Embedding feature is only supported in Uno applications that target iOS, Android, MacCatalyst, and Windows, as shown in the Platforms folder in the following solution structure.

### MauiEmbedding Dependencies

#### [**Single Project Template**](#tab/single-project)

![Starting solution structure - SingleProject](Assets/GettingStartedExisting01-StartSingleProjectStructure.png)

##### UnoFeatures

In the **.csproj** file, find the `<UnoFeatures>` property and add `MauiEmbedding`. This step adds all the dependencies you need for you to have Maui Embedding working with your project.

```diff
<UnoFeatures>
   ...
   Serialization;
   Localization;
   Navigation;
+  MauiEmbedding;
</UnoFeatures>
```

#### [**Multi-Head Project Template (Legacy)**](#tab/multi-project)

![Starting solution structure](Assets/GettingStartedExisting01-StartProjectStructure.png)

1. **Uno.Extensions.Maui.WinUI**
Add a reference in the existing class library to [Uno.Extensions.Maui.WinUI](https://www.nuget.org/packages/Uno.Extensions.Maui.WinUI). It's not necessary to add this package to other projects.

    ![Adding reference to Uno.Extensions.Maui.WinUI](Assets/GettingStartedExisting02-AddingReference.png)

1. **Package References**

    If the application is using Central Package Management, remove PackageVersion elements for the following packages

    ```xml
    <Project ToolsVersion="15.0">
      <ItemGroup>
        ...
        <!--<PackageVersion Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.22621.3233" /> -->
        <!-- <PackageVersion Include="Microsoft.WindowsAppSDK" Version="1.5.240311000" />-->
        ...
        <!--<PackageVersion Include="Xamarin.Google.Android.Material" Version="1.9.0.2" />-->
        ...
      </ItemGroup>
    </Project>
    ```

    In the project file for the existing Class Library, add the following ItemGroup, and remove the specified PackageReferences

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      ...
      <!-- Add this ItemGroup to add references to Android packages -->
      <ItemGroup Condition="$(IsAndroid)">
        <PackageReference Include="Xamarin.Google.Android.Material" VersionOverride="1.9.0.2" />
        <PackageReference Include="Xamarin.AndroidX.Navigation.UI" VersionOverride="2.6.0.1" />
        <PackageReference Include="Xamarin.AndroidX.Navigation.Fragment" VersionOverride="2.6.0.1" />
        <PackageReference Include="Xamarin.AndroidX.Navigation.Runtime" VersionOverride="2.6.0.1" />
        <PackageReference Include="Xamarin.AndroidX.Navigation.Common" VersionOverride="2.6.0.1" />
      </ItemGroup>
      ...
      <Choose>
        <When Condition="$(IsWinAppSdk)">
          ...
          <!-- Remove these package references and default to those specified by .NET Maui -->
          <!--<ItemGroup>
            <PackageReference Include="Microsoft.WindowsAppSDK" />
            <PackageReference Include="Microsoft.Windows.SDK.BuildTools" />
          </ItemGroup>-->
        </When>
    </Project>
    ```

    Update the project file (csproj) for the Mobile target with the following PackageReferences

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      ...
      <Choose>
        <When Condition="$(IsAndroid)">
          <ItemGroup>
            <!-- Add, or amend, the reference to Xamarin.Google.Android.Material with the VersionOverride -->
            <PackageReference Include="Xamarin.Google.Android.Material" VersionOverride="1.9.0.2" />

            <!-- Add reference to additional Android wrapper libraries -->
            <PackageReference Include="Xamarin.AndroidX.Navigation.UI" VersionOverride="2.6.0.1" />
            <PackageReference Include="Xamarin.AndroidX.Navigation.Fragment" VersionOverride="2.6.0.1" />
            <PackageReference Include="Xamarin.AndroidX.Navigation.Runtime" VersionOverride="2.6.0.1" />
            <PackageReference Include="Xamarin.AndroidX.Navigation.Common" VersionOverride="2.6.0.1" />
            ...
          </ItemGroup>
          ...
    </Project>
    ```

    Update the project file (csproj) for the Windows target to remove the following PackageReferences

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      ...
      <ItemGroup>
        <PackageReference Include="Uno.WinUI" />

        <!-- Remove the following PackageReferences and use the packages included by .NET MAUI -->
        <!-- <PackageReference Include="Microsoft.WindowsAppSDK" /> -->
        <!-- <PackageReference Include="Microsoft.Windows.SDK.BuildTools" />-->
      </ItemGroup>
    </Project>
    ```

***

### Add MAUI Application

1. **MAUI Class Library**

    Add a new .NET MAUI Class Library to the application, `ExistingUnoApp.MauiControls`.

    ![.NET MAUI Class Library](Assets/GettingStartedExisting03-MauiClassLibrary.png)

1. **MauiControls - EmbeddedControl**

    Remove the Class1.cs file that was created by the .NET MAUI Class Library template and add a .NET MAUI ContentView, `EmbeddedControl`

    ![.NET MAUI ContentView](Assets/GettingStartedExisting04-ContentView.png)

1. **MauiControls - App**

    Add a new .NET MAUI Resource Dictionary called App.

    ![.NET MAUI ResourceDictionary](Assets/GettingStartedExisting05-ResourceDictionary.png)

    This will generate App.xaml and App.xaml.cs, which we'll update to inherit from the `Application` base class instead of being a ResourceDictionary

    ```xml
    <?xml version = "1.0" encoding = "UTF-8" ?>
    <Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
          xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
          x:Class="ExistingUnoApp.MauiControls.App">
      <Application.Resources>
        <ResourceDictionary>
          <ResourceDictionary.MergedDictionaries>
          </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
      </Application.Resources>
    </Application>
    ```

    ```csharp
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }
    }
    ```

1. **ProjectReference - MauiControls**

    In the project file (csproj) for the Uno class library, add a reference to the .NET MAUI class library

    ```xml
    <Project Sdk="Microsoft.NET.Sdk">
      ...
      <ItemGroup>
        <!-- Add the reference to the .NET MAUI class library as a ProjectReference -->
        <ProjectReference Include="..\..\ExistingUnoApp.MauiControls\ExistingUnoApp.MauiControls.csproj" />
      </ItemGroup>
      ...
    </Project>

    ```

1. **EmbeddingApplication**

    Back in the Uno class library, change the base class in `App.xaml.cs` (XAML) or `App.cs` (C# Markup) from `Application` to `EmbeddingApplication`

    ```csharp
    public class App : EmbeddingApplication
    {
        protected Window? MainWindow { get; private set; }
        protected IHost? Host { get; private set; }
    }
    ```

    Also, in the `App.xaml` file, change the type of the root element to `EmbeddingApplication`

    ```xml
    <unoxt:EmbeddingApplication xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                                xmlns:unoxt="using:Uno.Extensions.Maui.Platform"
                                x:Class="ExistingUnoApp.MauiControls.App">
        <Application.Resources>
            <ResourceDictionary>
                <ResourceDictionary.MergedDictionaries>
                </ResourceDictionary.MergedDictionaries>
            </ResourceDictionary>
        </Application.Resources>
    </unoxt:EmbeddingApplication>
    ```

1. **UseMauiEmbedding**

    If using Uno.Extensions, add UseMauiEmbedding to the IHost builder, before the Customize method. Alternatively, add a call to the UseMauiEmbedding extension method for the application instance.

    **With** Uno.Extensions

    ```csharp
    public class App : EmbeddingApplication
    {
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var builder = this.CreateBuilder(args)
                ...
                .UseMauiEmbedding<MauiControls.App>()
                ...
                .Configure(host => host
                ...
    ```

    **Without** Uno.Extensions

    ```csharp
    public class App : EmbeddingApplication
    {
        protected async override void OnLaunched(LaunchActivatedEventArgs args)
        {
          ...
          this.UseMauiEmbedding<MauiControls.App>(MainWindow);
          ...
    ```

1. **MauiHost**

    Add a `MauiHost` element to the MainPage and set the Source property to match the EmbeddedControl

    ```xml
    <Page x:Class="ExistingUnoApp.Presentation.MainPage"
      ... >

      <Grid>
        ...
        <embed:MauiHost x:Name="MauiHostElement"
                MaxHeight="500"
                xmlns:embed="using:Uno.Extensions.Maui"
                xmlns:controls="using:ExistingUnoApp.MauiControls"
                Source="controls:EmbeddedControl" />
        ...
      </Grid>
    </Page>
    ```

> [!NOTE]
> A MaxHeight is set here to ensure proper layout and rendering of the embedded control on Skia rendered applications. Refer to the [MAUI Embedding with Skia rendered applications](#maui-embedding-with-skia-rendered-applications) section for more information.

Build and run on each target platform to see the .NET MAUI control embedded within the Uno application.
