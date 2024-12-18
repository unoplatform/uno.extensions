---
uid: Uno.Extensions.Navigation.Advanced.ContentControl
---
# How-To: Use a ContentControl to Display a View

If you want to display a view in a specific location in a page, `ContentControl` is the ideal UI element. For example, you might want to display a view in a `Grid` or `StackPanel` in a specific location. You can use a `ContentControl` to display a view in a specific location.

## Step-by-step

[!include[create-application](../../includes/create-application.md)]

### 1. Displaying Content with Content Control

- Add two new controls using the `UserControl` template, `LeftControl` and `RightControl` with the following XAML

    ```xml
    <UserControl x:Class="UsingContentControlRegion.Views.LeftControl"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:local="using:UsingContentControlRegion.Views"
                 xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                 mc:Ignorable="d">

        <TextBlock Text="Left"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
    </UserControl>

    <UserControl x:Class="UsingContentControlRegion.Views.RightControl"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:local="using:UsingContentControlRegion.Views"
                 xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
                 xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
                 mc:Ignorable="d">

        <TextBlock Text="Right"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
    </UserControl>
    ```

- Update `MainPage.xaml` with the following XAML

    ```xml
    <Page x:Class="UsingContentControlRegion.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:local="using:UsingContentControlRegion.Views"
          xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
          xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
          mc:Ignorable="d"
          xmlns:utu="using:Uno.Toolkit.UI"
          xmlns:uen="using:Uno.Extensions.Navigation.UI"
          Background="{ThemeResource MaterialBackgroundBrush}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition />
            </Grid.RowDefinitions>

            <utu:NavigationBar Content="Main Page"
                               Style="{StaticResource MaterialNavigationBarStyle}" />

            <StackPanel Grid.Row="1"
                        HorizontalAlignment="Center"
                        Orientation="Horizontal">

                <Button Content="Left"
                        uen:Navigation.Request="./Left" />
                <Button Content="Right"
                        uen:Navigation.Request="./Right" />
            </StackPanel>

            <ContentControl uen:Region.Attached="True"
                            Grid.Row="2"
                            HorizontalContentAlignment="Stretch"
                            VerticalContentAlignment="Stretch" />
        </Grid>
    </Page>
    ```

This code defines two buttons at the top of the page and a region that takes up the remainder of the page. The `ContentControl` is attached as a `NavigationRegion` using the `Region.Attached` attached property.

The `Navigation.Request` attached property is specified on both buttons with a route that starts with `./`. This prefix indicates that the navigation route should be applied to the nested `NavigationRegion`

### 2. Using a Named NavigationRegion

It is possible to use multiple `ContentControl` elements and load different content. In order to do this, the `ContentControl` elements need to be assigned a `Region.Name` and the navigation route needs to include the `Region.Name`.

- Update `MainPage.xaml` as follows:

    ```xml
    <Page x:Class="UsingContentControlRegion.Views.MainPage"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          xmlns:local="using:UsingContentControlRegion.Views"
          xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
          xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
          mc:Ignorable="d"
          xmlns:utu="using:Uno.Toolkit.UI"
          xmlns:uen="using:Uno.Extensions.Navigation.UI"
          Background="{ThemeResource MaterialBackgroundBrush}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition />
            </Grid.RowDefinitions>

            <utu:NavigationBar Content="Main Page"
                               Style="{StaticResource MaterialNavigationBarStyle}" />

            <StackPanel Grid.Row="1"
                        HorizontalAlignment="Center"
                        Orientation="Horizontal">
                <Button Content="Left"
                        uen:Navigation.Request="./Details/Left" />
                <Button Content="Right"
                        uen:Navigation.Request="./Details/Right" />
            </StackPanel>

            <ContentControl uen:Region.Attached="True"
                            uen:Region.Name="Details"
                            Grid.Row="2"
                            HorizontalContentAlignment="Stretch"
                            VerticalContentAlignment="Stretch" />
        </Grid>
    </Page>
    ```

In this XAML the `Region.Name` attached property has been set on the `ContentControl` and the `Navigation.Request` properties on each `Button` has been updated to include the `Region.Name`.
