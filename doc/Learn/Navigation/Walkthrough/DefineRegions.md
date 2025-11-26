---
uid: Uno.Extensions.Navigation.Walkthrough.DefineRegions
title: Define Navigation Regions
tags: [uno, uno-platform, uno-extensions, navigation, regions, Region.Attached, Region.Navigator, Region.Name, NavigationView, TabBar, Visibility, nested-navigation, attached-properties, region-definition, region-based-navigation, content-switching, inline-content, route-based-content, region-frames, NavigationViewItem, TabBarItem, region-mapping, content-visibility, region-navigator, Uno.Toolkit]
---

# Navigate between regions inside the same page (Region-based navigation)

> [!IMPORTANT]
> **Do not use `Region.Attached="True"` inside Shell.xaml or ExtendedSplashScreen content.** The navigation host is not ready during Shell construction. Define regions on your actual pages (like `MainPage`) instead.

## Link navigation control with content using regions

```xml
<Grid uen:Region.Attached="True">
   <Grid uen:Region.Attached="True"
         uen:Region.Navigator="Visibility">
      <Grid uen:Region.Name="One" Visibility="Collapsed">
         <TextBlock Text="One" />
      </Grid>
      <Grid uen:Region.Name="Two" Visibility="Collapsed">
         <TextBlock Text="Two" />
      </Grid>
   </Grid>
   <utu:TabBar uen:Region.Attached="True">
      <utu:TabBarItem Content="One" uen:Region.Name="One" />
      <utu:TabBarItem Content="Two" uen:Region.Name="Two" />
   </utu:TabBar>
</Grid>
```

`Region.Name` links TabBarItem with content area.

## Use routes instead of inline content

```csharp
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new("One", View: views.FindByView<OnePage>(), IsDefault: true),
        new("Two", View: views.FindByView<TwoPage>())
    ]
)
```

```xml
<Grid uen:Region.Attached="True">
   <Grid uen:Region.Attached="True"
         uen:Region.Navigator="Visibility" />
   <utu:TabBar uen:Region.Attached="True">
      <utu:TabBarItem Content="One" uen:Region.Name="One" />
      <utu:TabBarItem Content="Two" uen:Region.Name="Two" />
   </utu:TabBar>
</Grid>
```

This allows **hierarchical** navigation where each region maintains independent control over page transitions.

Associate TabBar items with matching region names:

```xml
<utu:TabBar.Items>
   <utu:TabBarItem uen:Region.Name="One" Content="Tab One" />
   <utu:TabBarItem uen:Region.Name="Two" Content="Tab Two" />
   <utu:TabBarItem uen:Region.Name="Three" Content="Tab Three" />
</utu:TabBar.Items>
```

* When a TabBarItem is selected, the navigator sets the corresponding content's `Visibility` to `True`.

## Method 2: Route-Based Content

Use registered route names to associate views with navigation items.

Register routes:

```csharp
new("Main", View: views.FindByView<MainPage>(),
   Nested:
   [
      new("Products", View: views.FindByView<ProductsContentControl>(), IsDefault: true),
      new("Favorites", View: views.FindByView<FavoritesContentControl>()),
      new("Deals", View: views.FindByView<DealsContentControl>())
   ]
)
```

Associate TabBar items with route names:

```xml
<utu:TabBar.Items>
   <utu:TabBarItem uen:Region.Name="Products" Content="Tab One" />
   <utu:TabBarItem uen:Region.Name="Favorites" Content="Tab Two" />
   <utu:TabBarItem uen:Region.Name="Deals" Content="Tab Three" />
</utu:TabBar.Items>
```

* When a TabBarItem is selected, the navigator displays the page corresponding to the route name.

## Switch content using buttons

Use `Navigation.Request` with relative path `RegionName` to navigate within the current page's regions.

```xml
<Grid uen:Region.Attached="True">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition />
    </Grid.RowDefinitions>
    
    <!-- Button navigation to regions -->
    <StackPanel Orientation="Horizontal">
        <Button Content="Show Products" 
                uen:Navigation.Request="Products" />
        <Button Content="Show Favorites" 
                uen:Navigation.Request="Favorites" />
    </StackPanel>
    
    <!-- Region content -->
    <Grid Grid.Row="1"
          uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
        <Grid uen:Region.Name="Products" Visibility="Collapsed">
            <!-- Products content -->
        </Grid>
        <Grid uen:Region.Name="Favorites" Visibility="Collapsed">
            <!-- Favorites content -->
        </Grid>
    </Grid>
</Grid>
```

## Navigate from ItemsRepeater item click

Use `Navigation.Request` directly on ItemsRepeater to navigate to regions when an item is clicked.

```xml
<Grid uen:Region.Attached="True">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>
    
    <!-- ItemsRepeater with direct navigation -->
    <ItemsRepeater ItemsSource="{Binding Categories}"
                   uen:Navigation.Request="CategoryDetails">
        <ItemsRepeater.ItemTemplate>
            <DataTemplate x:DataType="models:Category">
                <StackPanel Margin="0,0,0,4">
                    <TextBlock Text="{Binding Name}" />
                </StackPanel>
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
    
    <!-- Region content -->
    <Grid Grid.Column="1"
          uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
        <Grid uen:Region.Name="CategoryDetails" Visibility="Collapsed">
            <!-- Category details content -->
        </Grid>
    </Grid>
</Grid>
```

Clicked item is automatically passed as navigation data to the region.

## Using Regions with NavigationView

Same principles apply:

1. Parent Grid with `Region.Attached="True"`
2. Content Grid with `Region.Attached="True"` and `Region.Navigator="Visibility"`
3. NavigationView with `Region.Attached="True"`
4. NavigationViewItems with `Region.Name` matching content or routes

```xml
<Grid uen:Region.Attached="True">
   <Grid uen:Region.Attached="True"
         uen:Region.Navigator="Visibility">
      <!-- Content areas with Region.Name -->
   </Grid>
   <NavigationView uen:Region.Attached="True">
      <NavigationView.MenuItems>
         <NavigationViewItem uen:Region.Name="Products" Content="Products" />
         <NavigationViewItem uen:Region.Name="Favorites" Content="Favorites" />
      </NavigationView.MenuItems>
   </NavigationView>
</Grid>
```
