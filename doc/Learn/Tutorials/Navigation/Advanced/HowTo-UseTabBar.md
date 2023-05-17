---
uid: Learn.Tutorials.Navigation.Advanced.TabBar
---
# How-To: Use a TabBar to Switch Views

## Step-by-steps

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [instructions](xref:Overview.Extensions) for creating an application from the template.

The Navigation capabilities offered by Uno.Extensions include regions. Regions allow you to associate a specific sector of the view with an individual item on a navigation control from the same `Page`. Likewise, the Uno.Extensions library has built-in support for responding to navigation gestures from the [Toolkit](https://github.com/unoplatform/uno.toolkit.ui) `TabBar`. Follow the steps below to define a user interface centered around navigating with this control.

### 1. Add necessary XAML namespaces

* Update the `Page` element in `MainPage.xaml` to include XAML namespace mappings for Navigation and Uno Toolkit:

```xml
    <Page x:Class="UsingTabBar.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:local="using:UsingTabBar.Views"
          xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
          xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
          mc:Ignorable="d"
          xmlns:uen="using:Uno.Extensions.Navigation.UI"
          xmlns:utu="using:Uno.Toolkit.UI"
    ...
```

### 2. Define the view's layout

* Add `RowDefinition`s to the root `Grid` in `MainPage.xaml`:

```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
    </Grid>
```

* Define initial page and `TabBarItem` content. It's important to make each element that represents a sector of app content have it's `Visibility` explicitly set to `Collapsed`. Uno.Extensions will handle toggling it back to `Visible` when necessary

```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <Grid Grid.Row="1">
            <Grid Visibility="Collapsed">
                <TextBlock Text="One"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="Two"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="Three"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
        </Grid>
    </Grid>
```

* Add TabBar to the view:

```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <Grid Grid.Row="1">
            <Grid Visibility="Collapsed">
                <TextBlock Text="One"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="Two"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="Three"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
        </Grid>
        <utu:TabBar Grid.Row="2"
                    VerticalAlignment="Bottom">
            <utu:TabBar.Items>
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
```

### 3. Set up regions and specify navigator type

* Use the `Region.Attached="True"` attached property to enable regions on all of the following:
  * The `TabBar` control
  * The containing element of the collapsed content `Grid` definitions
  * The parent element of both controls

```xml
    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <Grid uen:Region.Attached="True"
              Grid.Row="1">
            <Grid Visibility="Collapsed">
                <TextBlock Text="One"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="Two"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="Three"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
        </Grid>
        <utu:TabBar Grid.Row="2"
                    uen:Region.Attached="True"
                    VerticalAlignment="Bottom">
            <utu:TabBar.Items>
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
```

* Name the regions you defined by using the `Region.Name` attached property on both the content itself and associated navigation control item:

```xml
    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <Grid uen:Region.Attached="True"
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
        <utu:TabBar Grid.Row="2"
                    uen:Region.Attached="True"
                    VerticalAlignment="Bottom">
            <utu:TabBar.Items>
                <utu:TabBarItem uen:Region.Name="One" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem uen:Region.Name="Two" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem uen:Region.Name="Three" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
```

* Specify the navigator type as `Visibility` using the `Region.Navigator` attached property on the containing element of your collapsed content `Grid` definitions:

```xml
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility"
          Grid.Row="1">
```

* When a `TabBarItem` is selected, the associated content region will now have its `Visibility` toggled to `Visible`
