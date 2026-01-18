---
name: uno-navigation-regions
description: Implement region-based navigation in Uno Platform using Region.Attached, Region.Name, and Region.Navigator properties. Use when building navigation hierarchies, linking navigation controls to content areas, visibility-based content switching, or nested navigation scenarios.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Region-Based Navigation

This skill covers implementing region-based navigation using attached properties in Uno Platform.

## What is a Navigation Region?

A region is a part of the user interface that manages navigation. Regions are organized in a hierarchy that mirrors the navigation controls structure, allowing navigation commands to flow between parent and child regions.

## Core Attached Properties

| Property | Description |
|----------|-------------|
| `Region.Attached="True"` | Enables navigation for an element |
| `Region.Name="Name"` | Identifies a region or links items to content |
| `Region.Navigator="Visibility"` | Sets visibility-based content switching |

## XAML Namespace

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI">
```

## Basic Region Setup

### Linking Navigation Control to Content

```xml
<Grid uen:Region.Attached="True">
    <!-- Navigation control -->
    <NavigationView uen:Region.Attached="True">
        <NavigationView.MenuItems>
            <NavigationViewItem Content="Products" uen:Region.Name="Products" />
            <NavigationViewItem Content="Settings" uen:Region.Name="Settings" />
        </NavigationView.MenuItems>
    </NavigationView>
    
    <!-- Content region -->
    <Grid uen:Region.Attached="True" uen:Region.Navigator="Visibility">
        <Grid uen:Region.Name="Products" Visibility="Collapsed">
            <!-- Products content -->
        </Grid>
        <Grid uen:Region.Name="Settings" Visibility="Collapsed">
            <!-- Settings content -->
        </Grid>
    </Grid>
</Grid>
```

### Key Concepts

1. **Parent Region Container**: The outer `Grid` with `Region.Attached="True"` establishes the navigation hierarchy

2. **Navigation Control**: `NavigationView`, `TabBar`, etc. with `Region.Attached="True"` and items marked with `Region.Name`

3. **Content Region**: Container with `Region.Attached="True"` and `Region.Navigator="Visibility"` that holds named content areas

4. **Region.Name Matching**: Navigation item `Region.Name` matches content area `Region.Name` for automatic linking

## Region Navigator Types

### Visibility Navigator

Toggles `Visibility` of child elements:

```xml
<Grid uen:Region.Attached="True" uen:Region.Navigator="Visibility">
    <Grid uen:Region.Name="One" Visibility="Collapsed">
        <TextBlock Text="Content One" />
    </Grid>
    <Grid uen:Region.Name="Two" Visibility="Collapsed">
        <TextBlock Text="Content Two" />
    </Grid>
</Grid>
```

**Behavior**: When navigating to "One", that grid becomes `Visible` and others become `Collapsed`.

### ContentControl Navigator

Loads content dynamically into a ContentControl:

```xml
<ContentControl uen:Region.Attached="True"
                uen:Region.Name="Details"
                HorizontalContentAlignment="Stretch"
                VerticalContentAlignment="Stretch" />
```

## Region-Based Navigation Patterns

### TabBar with Visibility Regions

```xml
<Grid uen:Region.Attached="True">
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    
    <!-- Content area -->
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
        <Grid uen:Region.Name="Home" Visibility="Collapsed">
            <TextBlock Text="Home Content" />
        </Grid>
        <Grid uen:Region.Name="Search" Visibility="Collapsed">
            <TextBlock Text="Search Content" />
        </Grid>
        <Grid uen:Region.Name="Profile" Visibility="Collapsed">
            <TextBlock Text="Profile Content" />
        </Grid>
    </Grid>
    
    <!-- TabBar -->
    <utu:TabBar Grid.Row="1" uen:Region.Attached="True">
        <utu:TabBarItem Content="Home" uen:Region.Name="Home" />
        <utu:TabBarItem Content="Search" uen:Region.Name="Search" />
        <utu:TabBarItem Content="Profile" uen:Region.Name="Profile" />
    </utu:TabBar>
</Grid>
```

### NavigationView with Regions

```xml
<Grid uen:Region.Attached="True">
    <NavigationView uen:Region.Attached="True">
        <NavigationView.MenuItems>
            <NavigationViewItem Content="Products" uen:Region.Name="Products" />
            <NavigationViewItem Content="Orders" uen:Region.Name="Orders" />
        </NavigationView.MenuItems>
        
        <!-- Content region inside NavigationView -->
        <Grid uen:Region.Attached="True"
              uen:Region.Navigator="Visibility">
            <Grid uen:Region.Name="Products" Visibility="Collapsed">
                <!-- Products page content -->
            </Grid>
            <Grid uen:Region.Name="Orders" Visibility="Collapsed">
                <!-- Orders page content -->
            </Grid>
        </Grid>
    </NavigationView>
</Grid>
```

### Routes with Region Content

Register routes and use `Region.Name` to link:

**App.xaml.cs:**
```csharp
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new RouteMap("Products", View: views.FindByView<ProductsPage>(), IsDefault: true),
        new RouteMap("Orders", View: views.FindByView<OrdersPage>())
    ]
)
```

**XAML:**
```xml
<Grid uen:Region.Attached="True">
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility" />
    
    <utu:TabBar uen:Region.Attached="True">
        <utu:TabBarItem Content="Products" uen:Region.Name="Products" />
        <utu:TabBarItem Content="Orders" uen:Region.Name="Orders" />
    </utu:TabBar>
</Grid>
```

The navigation framework automatically loads registered views into the content area.

## Important Rules

1. **Do NOT use `Region.Attached="True"` inside Shell.xaml** - The navigation host is not ready during Shell construction

2. **Always set initial `Visibility="Collapsed"`** on child regions when using `Region.Navigator="Visibility"`

3. **Root container must have `Region.Attached="True"`** for region navigation to work

4. **Region.Name must match** between navigation items and content areas

5. **Use `./` prefix for nested navigation** in `Navigation.Request`

## Programmatic Region Navigation

Navigate to specific regions from code:

```csharp
// Navigate to a region by name
await _navigator.NavigateRouteAsync(this, "Products");

// Navigate to a nested region
await _navigator.NavigateRouteAsync(this, "Main/Products");
```

## Setting Region Name Programmatically

For controls like NavigationView's SettingsItem:

```csharp
public MainPage()
{
    this.InitializeComponent();
    this.Loaded += MainPage_Loaded;
}

private void MainPage_Loaded(object sender, RoutedEventArgs e)
{
    var item = (NavigationViewItem)MyNavigationView.SettingsItem;
    Region.SetName(item, "Settings");
}
```
