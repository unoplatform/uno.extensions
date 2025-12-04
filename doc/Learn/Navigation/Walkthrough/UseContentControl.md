---
uid: Uno.Extensions.Navigation.Walkthrough.Advanced.UseContentControl
title: Display Views in Specific Locations using ContentControl with Region-Based Navigation
tags: [uno, uno-platform, uno-extensions, navigation, ContentControl, UserControl, regions, Region.Attached, Region.Name, Navigation.Request, nested-navigation, content-placement, region-based-navigation, view-display, view-injection, content-area, RouteMap, ViewMap, Button-navigation, dynamic-content, region-content]
---

# Display Views in Specific Locations using ContentControl with Region-Based Navigation

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

* Import namespaces in XAML:

    ```xml
    <Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
          xmlns:utu="using:Uno.Toolkit.UI">
    ```

## Display different views in same location

Use ContentControl as a region to display different views.

* Create UserControls `LeftControl.xaml` and `RightControl.xaml`:

    ```xml
    <UserControl x:Class="UsingContentControlRegion.Views.LeftControl">
        <TextBlock Text="Left"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
    </UserControl>

    <UserControl x:Class="UsingContentControlRegion.Views.RightControl">
        <TextBlock Text="Right"
                   FontSize="24"
                   HorizontalAlignment="Center"
                   VerticalAlignment="Center" />
    </UserControl>
    ```

* Define layout in `MainPage.xaml`:

    ```xml
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
    ```

* `ContentControl` is attached as a NavigationRegion using `Region.Attached`.
* `Navigation.Request` uses `./` prefix to indicate nested region navigation.

## Control multiple content areas

Use multiple ContentControls with different content by naming regions.

* Update `MainPage.xaml` with named region:

    ```xml
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
    ```

* `Region.Name="Details"` identifies the ContentControl region.
* `Navigation.Request="./Details/Left"` includes the region name in the route.
