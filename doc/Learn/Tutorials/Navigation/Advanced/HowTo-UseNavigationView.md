---
uid: Learn.Tutorials.Navigation.Advanced.NavigationView
---
# How-To: Use a NavigationView to Switch Views

> [!WARNING]
> **Work in progress -** 
>This page is currently under construction. It will be available soon. 🚧
>
> **Have questions or feedback?**
>You can help shape the documentation for this topic by providing feedback on the Uno.Extensions [repo](https://github.com/unoplatform/uno.extensions/discussions/categories/general)

> [!TIP]
> This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](xref:Overview.Extensions)

## Step-by-steps

### Code example

```xml
<Page x:Class="UsingNavigationView.Views.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:UsingTabBar.Views"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      mc:Ignorable="d"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI"
      xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
      Background="{ThemeResource MaterialBackgroundBrush}">

    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <muxc:NavigationView uen:Region.Attached="true"
                             Grid.Row="1">
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem Content="One"
                                         uen:Region.Name="One" />
                <muxc:NavigationViewItem Content="Two"
                                         uen:Region.Name="Two" />
                <muxc:NavigationViewItem Content="Three"
                                         uen:Region.Name="Three" />
            </muxc:NavigationView.MenuItems>
            <Grid uen:Region.Attached="True"
                  uen:Region.Navigator="Visibility"
                  Grid.Row="1">
                <Grid uen:Region.Name="One"
                      Visibility="Collapsed">
                    <TextBlock Text="One"
                               FontSize="24"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center" />
                </Grid>
                <Grid uen:Region.Name="Two"
                      Visibility="Collapsed">
                    <TextBlock Text="Two"
                               FontSize="24"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center" />
                </Grid>
                <Grid uen:Region.Name="Three"
                      Visibility="Collapsed">
                    <TextBlock Text="Three"
                               FontSize="24"
                               HorizontalAlignment="Center"
                               VerticalAlignment="Center" />
                </Grid>
            </Grid>
        </muxc:NavigationView>
    </Grid>
</Page>
```

<!-- - Show how to use navigationview to switch between views -->
