---
uid: Learn.Tutorials.Maui.HowToUse3rdPartLibs
---

# How-To: Install and use third part libraries on Uno apps with Maui Embedding

When work in an application project, you may need to install a third party library. This guide will show you how to install and use a third party library in your Uno app with Maui Embedding.

For this sample we will use the `CommunityToolkit.Maui` library.

## Step-by-steps

### 1. Installing the NuGet package

On Visual Studio you can use the `Nuget Package Manager` to install the `CommunityToolkit.Maui` package. Install it on your class library project and mobile and Windows project (because this package support those platforms).

To verify if the package is installed you can look on your csproj file and check if the package is there. It should be something like this:

```xml
<ItemGroup>
    <PackageReference Include="CommunityToolkit.Maui" Version="2.0.0" />
</ItemGroup>
```

### 2. Using the library

In order to use the library we need to initialize it, we do that during the app initialization. So go to your `App.cs` file and add the following code:

```csharp
using using CommunityToolkit.Maui;

protected async override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .UseMauiEmbedding(maui =>
        {
            maui.UseMauiCommunityToolkit();
        })
        // the rest of your configuration
}
```

> [!INFO]
> If you are using other libraries that targets .NET MAUI and requires initialization, you can do it > inside the lambda function on `UseMauiEmbedding` call.

Now let's use it on our Page. For that you have to open your XAML page and add the necessary namespaces and the controls you want to use. In this sample we will use the `DrawingView` control.

```xml
<Page
	x:Class="MauiEmbedding.Presentation.MCTControlsPage"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	xmlns:embed="using:Uno.Extensions.Maui"
	xmlns:local="using:MauiEmbedding.Presentation"
	xmlns:maui="using:Microsoft.Maui.Controls"
	xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	xmlns:toolkit="http://schemas.microsoft.com/dotnet/2022/maui/toolkit"
	xmlns:uen="using:Uno.Extensions.Navigation.UI"
	xmlns:utu="using:Uno.Toolkit.UI"
	Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"
	NavigationCacheMode="Required"
	mc:Ignorable="d">

    <StackPanel
			HorizontalAlignment="Center"
			VerticalAlignment="Center">
			<embed:MauiContent>
				<maui:ScrollView>
					<maui:ScrollView.Content>
						<maui:VerticalStackLayout Spacing="16">
							<toolkit:DrawingView
								HeightRequest="300"
								IsMultiLineModeEnabled="true"
								LineColor="{embed:MauiColor Value='#FF0000'}"
								LineWidth="5"
								WidthRequest="300" />
						</maui:VerticalStackLayout>
					</maui:ScrollView.Content>
				</maui:ScrollView>
			</embed:MauiContent>
		</StackPanel>
</Page>
```

Let's take a moment and review this piece of code. First we added the `toolkit` namespace, this is the namespace that contains the controls from the `CommunityToolkit.Maui` library. In order to consume the `DrawingView` control. We need to use the `MauiContent` control, that lives on `embed` namespace, this control holds all the .NET MAUI types (layouts, controls, converters, etc), you can't use .NET MAUI types outside this control.

Also, Uno doesn't convert the Color type from .NET MAUI to the native platform, so we need to use the `MauiColor` markup extension to convert the color to the native platform, as you can see in the `LineColor` property.

### 3. Conclusion

With that you should be good to go and run your app, when it runs you should be able to see the `DrawingView` control and interact with it.