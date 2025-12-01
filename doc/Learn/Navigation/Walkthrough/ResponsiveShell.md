---
uid: Uno.Extensions.Navigation.Walkthrough.Advanced.ResponsiveShell
title: Build Responsive Navigation Layouts using NavigationView and TabBar with VisualStateManager
tags: [uno, uno-platform, uno-extensions, uno-toolkit, navigation, responsive-design, adaptive-layout, NavigationView, TabBar, VisualStateManager, visual-states, regions, breakpoints, screen-size, responsive-navigation, adaptive-ui, mobile-navigation, desktop-navigation, AdaptiveTrigger, MinWindowWidth, NavigationViewItem, TabBarItem, Region.Attached, Region.Name, Region.Navigator, Visibility, multi-device, cross-platform-ui]
---

# Build Responsive Navigation Layouts using NavigationView and TabBar with VisualStateManager

> **UnoFeature:** Navigation (and Toolkit for TabBar)

* Import namespaces in XAML:

    ```xml
    <Page xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
          xmlns:uen="using:Uno.Extensions.Navigation.UI"
          xmlns:utu="using:Uno.Toolkit.UI">
    ```

## Define Base Layout

Create structure with NavigationView.

* Add row definitions:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
    </Grid>
    ```

* Add NavigationView with menu items:

    ```xml
    <muxc:NavigationView Grid.Row="1" x:Name="NavView">
        <muxc:NavigationView.MenuItems>
            <muxc:NavigationViewItem Content="One" />
            <muxc:NavigationViewItem Content="Two" />
            <muxc:NavigationViewItem Content="Three" />
        </muxc:NavigationView.MenuItems>
    </muxc:NavigationView>
    ```

* Add content areas with collapsed visibility:

    ```xml
    <muxc:NavigationView.Content>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
            <Grid>
                <Grid Visibility="Collapsed">
                    <TextBlock Text="One" FontSize="24" />
                </Grid>
                <Grid Visibility="Collapsed">
                    <TextBlock Text="Two" FontSize="24" />
                </Grid>
                <Grid Visibility="Collapsed">
                    <TextBlock Text="Three" FontSize="24" />
                </Grid>
            </Grid>
        </Grid>
    </muxc:NavigationView.Content>
    ```

* Set `Visibility="Collapsed"` — Navigation toggles to `Visible` when needed.

## Add alternative navigation for small screens

Add TabBar as alternative navigation for smaller screens.

* Add TabBar to content area:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>
        <Grid>
            <!-- Collapsed content areas -->
        </Grid>
        <utu:TabBar Grid.Row="1"
                    x:Name="Tabs"
                    VerticalAlignment="Bottom">
            <utu:TabBar.Items>
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                    <utu:TabBarItem.Icon>
                        <PathIcon Data="{StaticResource Icon_storefront}" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                    <utu:TabBarItem.Icon>
                        <PathIcon Data="{StaticResource Icon_bolt}" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                    <utu:TabBarItem.Icon>
                        <PathIcon Data="{StaticResource Icon_person}" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
    ```

## Set Up Regions

Enable region navigation for both controls.

* Add `Region.Attached="True"`:
  * Root Grid of Page
  * NavigationView
  * Content Grid
  * TabBar

    ```xml
    <Grid uen:Region.Attached="True">
        <muxc:NavigationView uen:Region.Attached="True">
            <muxc:NavigationView.Content>
                <Grid>
                    <Grid uen:Region.Attached="True">
                        <!-- Content areas -->
                    </Grid>
                    <utu:TabBar uen:Region.Attached="True">
                        <!-- Tab items -->
                    </utu:TabBar>
                </Grid>
            </muxc:NavigationView.Content>
        </muxc:NavigationView>
    </Grid>
    ```

* Add `Region.Name` to all navigation items and content:

    ```xml
    <muxc:NavigationView.MenuItems>
        <muxc:NavigationViewItem Content="One" uen:Region.Name="One" />
        <muxc:NavigationViewItem Content="Two" uen:Region.Name="Two" />
        <muxc:NavigationViewItem Content="Three" uen:Region.Name="Three" />
    </muxc:NavigationView.MenuItems>

    <Grid uen:Region.Attached="True">
        <Grid uen:Region.Name="One" Visibility="Collapsed">
            <TextBlock Text="One" />
        </Grid>
        <Grid uen:Region.Name="Two" Visibility="Collapsed">
            <TextBlock Text="Two" />
        </Grid>
        <Grid uen:Region.Name="Three" Visibility="Collapsed">
            <TextBlock Text="Three" />
        </Grid>
    </Grid>

    <utu:TabBar.Items>
        <utu:TabBarItem uen:Region.Name="One" />
        <utu:TabBarItem uen:Region.Name="Two" />
        <utu:TabBarItem uen:Region.Name="Three" />
    </utu:TabBar.Items>
    ```

* Set `Region.Navigator="Visibility"`:

    ```xml
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
    ```

## Adapt navigation for different screen sizes

Use VisualStateManager to toggle between NavigationView and TabBar.

* Define visual states with breakpoints:

    ```xml
    <Grid uen:Region.Attached="True">
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup>
                <VisualState x:Name="Narrow">
                    <VisualState.Setters>
                        <Setter Target="Tabs.Visibility" Value="Visible" />
                        <Setter Target="NavView.IsPaneToggleButtonVisible" Value="false" />
                        <Setter Target="NavView.PaneDisplayMode" Value="LeftMinimal" />
                        <Setter Target="NavView.IsPaneOpen" Value="False" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="Normal">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="700" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="Tabs.Visibility" Value="Collapsed" />
                        <Setter Target="NavView.IsPaneToggleButtonVisible" Value="True" />
                        <Setter Target="NavView.IsPaneVisible" Value="true" />
                        <Setter Target="NavView.PaneDisplayMode" Value="Auto" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="Wide">
                    <VisualState.StateTriggers>
                        <AdaptiveTrigger MinWindowWidth="1000" />
                    </VisualState.StateTriggers>
                    <VisualState.Setters>
                        <Setter Target="Tabs.Visibility" Value="Collapsed" />
                        <Setter Target="NavView.IsPaneToggleButtonVisible" Value="True" />
                        <Setter Target="NavView.IsPaneVisible" Value="true" />
                        <Setter Target="NavView.PaneDisplayMode" Value="Auto" />
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>
        <!-- Content -->
    </Grid>
    ```

## Visual States Explained

### Narrow (< 700px)

* **TabBar** — Visible (primary navigation)
* **NavigationView Pane** — Hidden
* **Toggle Button** — Hidden
* **Use Case** — Mobile phones

### Normal (700px - 999px)

* **TabBar** — Hidden
* **NavigationView Pane** — Visible with toggle
* **Display Mode** — Auto
* **Use Case** — Tablets

### Wide (≥ 1000px)

* **TabBar** — Hidden
* **NavigationView Pane** — Visible with toggle
* **Display Mode** — Auto
* **Use Case** — Desktops, large tablets
