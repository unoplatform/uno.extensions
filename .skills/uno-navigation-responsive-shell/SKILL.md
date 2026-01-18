---
name: uno-navigation-responsive-shell
description: Implement responsive navigation shells that adapt between TabBar and NavigationView based on screen size. Use when building adaptive apps that need different navigation patterns on mobile vs desktop.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Responsive Navigation Shell

This skill covers creating navigation shells that adapt to different screen sizes in Uno Platform.

## Overview

A responsive shell automatically switches between:
- **TabBar** (bottom navigation) on mobile/narrow screens
- **NavigationView** (sidebar) on tablet/desktop/wide screens

## Prerequisites

```xml
<UnoFeatures>Navigation;Toolkit</UnoFeatures>
```

## XAML Namespaces

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI"
      xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
      xmlns:um="using:Uno.Material">
```

## Responsive Markup Extension

Use the `Responsive` markup extension to switch values based on screen width:

```xml
<SomeProperty="{um:Responsive Narrow='valueA', Wide='valueB'}" />
```

Default breakpoints:
- **Narrow**: 0-640px
- **Wide**: 641px+

## Basic Responsive Shell

### Pattern: Visibility Switching

```xml
<Grid uen:Region.Attached="True">
    <!-- TabBar for narrow screens -->
    <utu:TabBar Visibility="{um:Responsive Narrow=Visible, Wide=Collapsed}"
                uen:Region.Attached="True"
                Style="{StaticResource BottomTabBarStyle}">
        <utu:TabBar.Items>
            <utu:TabBarItem Content="Home" uen:Region.Name="Home">
                <utu:TabBarItem.Icon>
                    <FontIcon Glyph="&#xE80F;" />
                </utu:TabBarItem.Icon>
            </utu:TabBarItem>
            <utu:TabBarItem Content="Search" uen:Region.Name="Search">
                <utu:TabBarItem.Icon>
                    <FontIcon Glyph="&#xE721;" />
                </utu:TabBarItem.Icon>
            </utu:TabBarItem>
            <utu:TabBarItem Content="Settings" uen:Region.Name="Settings">
                <utu:TabBarItem.Icon>
                    <FontIcon Glyph="&#xE713;" />
                </utu:TabBarItem.Icon>
            </utu:TabBarItem>
        </utu:TabBar.Items>
    </utu:TabBar>

    <!-- NavigationView for wide screens -->
    <muxc:NavigationView Visibility="{um:Responsive Narrow=Collapsed, Wide=Visible}"
                         uen:Region.Attached="True"
                         PaneDisplayMode="Left">
        <muxc:NavigationView.MenuItems>
            <muxc:NavigationViewItem Content="Home" uen:Region.Name="Home">
                <muxc:NavigationViewItem.Icon>
                    <FontIcon Glyph="&#xE80F;" />
                </muxc:NavigationViewItem.Icon>
            </muxc:NavigationViewItem>
            <muxc:NavigationViewItem Content="Search" uen:Region.Name="Search">
                <muxc:NavigationViewItem.Icon>
                    <FontIcon Glyph="&#xE721;" />
                </muxc:NavigationViewItem.Icon>
            </muxc:NavigationViewItem>
            <muxc:NavigationViewItem Content="Settings" uen:Region.Name="Settings">
                <muxc:NavigationViewItem.Icon>
                    <FontIcon Glyph="&#xE713;" />
                </muxc:NavigationViewItem.Icon>
            </muxc:NavigationViewItem>
        </muxc:NavigationView.MenuItems>

        <!-- Content Area -->
        <Grid uen:Region.Attached="True"
              uen:Region.Navigator="Visibility" />
    </muxc:NavigationView>

    <!-- Shared Content for TabBar (when visible) -->
    <Grid Grid.Row="0"
          Visibility="{um:Responsive Narrow=Visible, Wide=Collapsed}"
          uen:Region.Attached="True"
          uen:Region.Navigator="Visibility" />
</Grid>
```

## Using ResponsiveView Control

The Toolkit's `ResponsiveView` provides cleaner template switching:

```xml
<utu:ResponsiveView uen:Region.Attached="True">
    <utu:ResponsiveView.NarrowTemplate>
        <DataTemplate>
            <Grid uen:Region.Attached="True">
                <!-- Content Area -->
                <Grid uen:Region.Attached="True"
                      uen:Region.Navigator="Visibility"
                      Margin="0,0,0,64" />
                
                <!-- Bottom TabBar -->
                <utu:TabBar VerticalAlignment="Bottom"
                            uen:Region.Attached="True"
                            Style="{StaticResource BottomTabBarStyle}">
                    <utu:TabBar.Items>
                        <utu:TabBarItem Content="Home" uen:Region.Name="Home">
                            <utu:TabBarItem.Icon>
                                <FontIcon Glyph="&#xE80F;" />
                            </utu:TabBarItem.Icon>
                        </utu:TabBarItem>
                        <utu:TabBarItem Content="Search" uen:Region.Name="Search">
                            <utu:TabBarItem.Icon>
                                <FontIcon Glyph="&#xE721;" />
                            </utu:TabBarItem.Icon>
                        </utu:TabBarItem>
                    </utu:TabBar.Items>
                </utu:TabBar>
            </Grid>
        </DataTemplate>
    </utu:ResponsiveView.NarrowTemplate>
    
    <utu:ResponsiveView.WideTemplate>
        <DataTemplate>
            <muxc:NavigationView uen:Region.Attached="True"
                                 PaneDisplayMode="Left">
                <muxc:NavigationView.MenuItems>
                    <muxc:NavigationViewItem Content="Home" uen:Region.Name="Home">
                        <muxc:NavigationViewItem.Icon>
                            <FontIcon Glyph="&#xE80F;" />
                        </muxc:NavigationViewItem.Icon>
                    </muxc:NavigationViewItem>
                    <muxc:NavigationViewItem Content="Search" uen:Region.Name="Search">
                        <muxc:NavigationViewItem.Icon>
                            <FontIcon Glyph="&#xE721;" />
                        </muxc:NavigationViewItem.Icon>
                    </muxc:NavigationViewItem>
                </muxc:NavigationView.MenuItems>

                <Grid uen:Region.Attached="True"
                      uen:Region.Navigator="Visibility" />
            </muxc:NavigationView>
        </DataTemplate>
    </utu:ResponsiveView.WideTemplate>
</utu:ResponsiveView>
```

## Custom Breakpoints

Define custom breakpoints in App.xaml:

```xml
<Application.Resources>
    <ResourceDictionary>
        <x:Double x:Key="ResponsiveNarrowMaxWidth">600</x:Double>
        <x:Double x:Key="ResponsiveNormalMinWidth">601</x:Double>
        <x:Double x:Key="ResponsiveNormalMaxWidth">900</x:Double>
        <x:Double x:Key="ResponsiveWideMinWidth">901</x:Double>
    </ResourceDictionary>
</Application.Resources>
```

Use in XAML:

```xml
<Visibility="{um:Responsive Narrow=Collapsed, Normal=Visible, Wide=Visible}" />
```

## Route Registration

Routes work the same regardless of which shell is visible:

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<HomePage, HomeViewModel>(),
        new ViewMap<SearchPage, SearchViewModel>(),
        new ViewMap<SettingsPage, SettingsViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new RouteMap("Home", View: views.FindByViewModel<HomeViewModel>(), IsDefault: true),
                new RouteMap("Search", View: views.FindByViewModel<SearchViewModel>()),
                new RouteMap("Settings", View: views.FindByViewModel<SettingsViewModel>())
            ]
        )
    );
}
```

## Shared Content Area Pattern

To avoid duplicating content regions:

```xml
<Grid uen:Region.Attached="True">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="{um:Responsive Narrow=0, Wide=250}" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- NavigationView Pane (Wide only) -->
    <muxc:NavigationView Grid.Row="0"
                         Grid.RowSpan="3"
                         Grid.Column="0"
                         Visibility="{um:Responsive Narrow=Collapsed, Wide=Visible}"
                         uen:Region.Attached="True"
                         PaneDisplayMode="Left"
                         IsPaneOpen="True">
        <muxc:NavigationView.MenuItems>
            <muxc:NavigationViewItem Content="Home" uen:Region.Name="Home" />
            <muxc:NavigationViewItem Content="Search" uen:Region.Name="Search" />
        </muxc:NavigationView.MenuItems>
    </muxc:NavigationView>

    <!-- Shared Content Area -->
    <Grid Grid.Row="1"
          Grid.Column="1"
          uen:Region.Attached="True"
          uen:Region.Navigator="Visibility" />

    <!-- TabBar (Narrow only) -->
    <utu:TabBar Grid.Row="2"
                Grid.Column="0"
                Grid.ColumnSpan="2"
                Visibility="{um:Responsive Narrow=Visible, Wide=Collapsed}"
                uen:Region.Attached="True"
                Style="{StaticResource BottomTabBarStyle}">
        <utu:TabBar.Items>
            <utu:TabBarItem Content="Home" uen:Region.Name="Home" />
            <utu:TabBarItem Content="Search" uen:Region.Name="Search" />
        </utu:TabBar.Items>
    </utu:TabBar>
</Grid>
```

## Maintaining Selection State

When switching between shells, both navigation controls should reflect the current route. The navigation system handles this automatically when:

1. Both controls have matching `Region.Name` values
2. Both are attached to the same region hierarchy

## Adaptive Content Layout

Combine with responsive content layouts:

```xml
<Grid uen:Region.Attached="True"
      uen:Region.Navigator="Visibility">
    
    <Grid uen:Region.Name="Home">
        <!-- Responsive grid columns for content -->
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="{um:Responsive Narrow='*', Wide='2*'}" />
            <ColumnDefinition Width="{um:Responsive Narrow='0', Wide='*'}" />
        </Grid.ColumnDefinitions>
        
        <StackPanel Grid.Column="0">
            <TextBlock Text="Main Content" />
        </StackPanel>
        
        <StackPanel Grid.Column="1"
                    Visibility="{um:Responsive Narrow=Collapsed, Wide=Visible}">
            <TextBlock Text="Sidebar" />
        </StackPanel>
    </Grid>
</Grid>
```

## Complete Example

```xml
<Page x:Class="MyApp.Views.ShellPage"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI"
      xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
      xmlns:um="using:Uno.Material"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="{um:Responsive Narrow=Auto, Wide=0}" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="{um:Responsive Narrow=0, Wide=Auto}" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- Desktop NavigationView -->
        <muxc:NavigationView Grid.Row="0"
                             Grid.Column="0"
                             Visibility="{um:Responsive Narrow=Collapsed, Wide=Visible}"
                             uen:Region.Attached="True"
                             PaneDisplayMode="LeftCompact"
                             IsBackButtonVisible="Collapsed">
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem Content="Dashboard" uen:Region.Name="Dashboard">
                    <muxc:NavigationViewItem.Icon>
                        <FontIcon Glyph="&#xE80F;" />
                    </muxc:NavigationViewItem.Icon>
                </muxc:NavigationViewItem>
                <muxc:NavigationViewItem Content="Products" uen:Region.Name="Products">
                    <muxc:NavigationViewItem.Icon>
                        <FontIcon Glyph="&#xE7BF;" />
                    </muxc:NavigationViewItem.Icon>
                </muxc:NavigationViewItem>
                <muxc:NavigationViewItem Content="Orders" uen:Region.Name="Orders">
                    <muxc:NavigationViewItem.Icon>
                        <FontIcon Glyph="&#xE7C1;" />
                    </muxc:NavigationViewItem.Icon>
                </muxc:NavigationViewItem>
            </muxc:NavigationView.MenuItems>
        </muxc:NavigationView>

        <!-- Shared Content Area -->
        <Grid Grid.Row="0"
              Grid.Column="1"
              uen:Region.Attached="True"
              uen:Region.Navigator="Visibility"
              Padding="16" />

        <!-- Mobile TabBar -->
        <utu:TabBar Grid.Row="1"
                    Grid.Column="0"
                    Grid.ColumnSpan="2"
                    Visibility="{um:Responsive Narrow=Visible, Wide=Collapsed}"
                    uen:Region.Attached="True"
                    Style="{StaticResource BottomTabBarStyle}">
            <utu:TabBar.Items>
                <utu:TabBarItem uen:Region.Name="Dashboard">
                    <utu:TabBarItem.Icon>
                        <FontIcon Glyph="&#xE80F;" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
                <utu:TabBarItem uen:Region.Name="Products">
                    <utu:TabBarItem.Icon>
                        <FontIcon Glyph="&#xE7BF;" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
                <utu:TabBarItem uen:Region.Name="Orders">
                    <utu:TabBarItem.Icon>
                        <FontIcon Glyph="&#xE7C1;" />
                    </utu:TabBarItem.Icon>
                </utu:TabBarItem>
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
</Page>
```

## Best Practices

1. **Match Region.Name values** - Both navigation controls must use identical region names

2. **Use shared content area** - Avoid duplicating content regions when possible

3. **Test at breakpoints** - Verify smooth transitions at responsive breakpoints

4. **Consider SafeArea** - Use `utu:SafeArea` for notched devices on mobile

5. **Keep icons consistent** - Use same icons in both TabBar and NavigationView

6. **Set IsDefault** - Ensure one route is marked default for initial content

## Common Issues

| Issue | Solution |
|-------|----------|
| Content duplicated on resize | Ensure single shared content region |
| Selection not synced | Verify Region.Name matches exactly |
| Layout shifts on resize | Use responsive column/row definitions |
| TabBar overlaps content | Add bottom margin/padding matching TabBar height |
| NavigationView too narrow | Set minimum column width for NavigationView |
