---
name: uno-navigation-navigationview
description: Implement NavigationView navigation in Uno Platform using region-based navigation. Use when building sidebar navigation, hamburger menu patterns, or desktop-style navigation with NavigationView control.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# NavigationView Navigation

This skill covers implementing NavigationView-based navigation in Uno Platform applications.

## Prerequisites

```xml
<UnoFeatures>Navigation;Toolkit</UnoFeatures>
```

## XAML Namespace

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:muxc="using:Microsoft.UI.Xaml.Controls">
```

## Basic NavigationView Setup

### With Inline Content

```xml
<Grid uen:Region.Attached="True">
    <muxc:NavigationView uen:Region.Attached="True">
        <muxc:NavigationView.MenuItems>
            <muxc:NavigationViewItem Content="Products" uen:Region.Name="Products" />
            <muxc:NavigationViewItem Content="Orders" uen:Region.Name="Orders" />
            <muxc:NavigationViewItem Content="Settings" uen:Region.Name="Settings" />
        </muxc:NavigationView.MenuItems>

        <!-- Content Area -->
        <Grid uen:Region.Attached="True"
              uen:Region.Navigator="Visibility">
            <Grid uen:Region.Name="Products" Visibility="Collapsed">
                <TextBlock Text="Products Content" />
            </Grid>
            <Grid uen:Region.Name="Orders" Visibility="Collapsed">
                <TextBlock Text="Orders Content" />
            </Grid>
            <Grid uen:Region.Name="Settings" Visibility="Collapsed">
                <TextBlock Text="Settings Content" />
            </Grid>
        </Grid>
    </muxc:NavigationView>
</Grid>
```

### Key Structure

1. **Root Grid** with `Region.Attached="True"`
2. **NavigationView** with `Region.Attached="True"`
3. **NavigationViewItems** with `Region.Name` for each menu item
4. **Content Grid** inside NavigationView with `Region.Attached="True"` and `Region.Navigator="Visibility"`
5. **Content areas** with matching `Region.Name` and `Visibility="Collapsed"`

## NavigationView with Registered Routes

### Route Registration

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>(),
        new ViewMap<ProductsPage, ProductsViewModel>(),
        new ViewMap<OrdersPage, OrdersViewModel>(),
        new ViewMap<SettingsPage, SettingsViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
                    Nested:
                    [
                        new RouteMap("Products", View: views.FindByViewModel<ProductsViewModel>(), IsDefault: true),
                        new RouteMap("Orders", View: views.FindByViewModel<OrdersViewModel>()),
                        new RouteMap("Settings", View: views.FindByViewModel<SettingsViewModel>())
                    ]
                )
            ]
        )
    );
}
```

### XAML with Routes

```xml
<Grid uen:Region.Attached="True">
    <muxc:NavigationView uen:Region.Attached="True">
        <muxc:NavigationView.MenuItems>
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
            <muxc:NavigationViewItem Content="Settings" uen:Region.Name="Settings">
                <muxc:NavigationViewItem.Icon>
                    <FontIcon Glyph="&#xE713;" />
                </muxc:NavigationViewItem.Icon>
            </muxc:NavigationViewItem>
        </muxc:NavigationView.MenuItems>

        <!-- Content Area - Views loaded from routes -->
        <Grid uen:Region.Attached="True"
              uen:Region.Navigator="Visibility" />
    </muxc:NavigationView>
</Grid>
```

## Settings Item

NavigationView's SettingsItem is auto-generated and requires code-behind to set Region.Name:

### Code-Behind Setup

```csharp
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
        this.Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (MyNavigationView.SettingsItem is NavigationViewItem settingsItem)
        {
            Region.SetName(settingsItem, "Settings");
        }
    }
}
```

### XAML with Named NavigationView

```xml
<muxc:NavigationView x:Name="MyNavigationView"
                     uen:Region.Attached="True">
    <!-- Menu items -->
</muxc:NavigationView>
```

## Hierarchical NavigationView

For nested menu items:

```xml
<muxc:NavigationView uen:Region.Attached="True">
    <muxc:NavigationView.MenuItems>
        <muxc:NavigationViewItem Content="Products" uen:Region.Name="Products">
            <muxc:NavigationViewItem.MenuItems>
                <muxc:NavigationViewItem Content="All Products" uen:Region.Name="AllProducts" />
                <muxc:NavigationViewItem Content="Categories" uen:Region.Name="Categories" />
            </muxc:NavigationViewItem.MenuItems>
        </muxc:NavigationViewItem>
        <muxc:NavigationViewItem Content="Orders" uen:Region.Name="Orders" />
    </muxc:NavigationView.MenuItems>
</muxc:NavigationView>
```

### Nested Route Registration

```csharp
new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
    Nested:
    [
        new RouteMap("Products", 
            Nested:
            [
                new RouteMap("AllProducts", View: views.FindByView<AllProductsPage>(), IsDefault: true),
                new RouteMap("Categories", View: views.FindByView<CategoriesPage>())
            ]
        ),
        new RouteMap("Orders", View: views.FindByViewModel<OrdersViewModel>())
    ]
)
```

## Passing Data to Menu Items

```xml
<muxc:NavigationViewItem Content="Product Details"
                         uen:Region.Name="ProductDetails"
                         uen:Navigation.Data="{Binding SelectedProduct}" />
```

With corresponding DataViewMap:

```csharp
new DataViewMap<ProductDetailsPage, ProductDetailsViewModel, Product>()
```

## Programmatic Navigation

Navigate to specific NavigationView items from code:

```csharp
// Navigate to a specific item
await _navigator.NavigateRouteAsync(this, "Orders");

// Navigate with data
await _navigator.NavigateViewModelAsync<ProductDetailsViewModel>(this, data: product);
```

## NavigationView with Frame

For Frame-based navigation instead of visibility:

```xml
<muxc:NavigationView x:Name="NavView"
                     uen:Region.Attached="True">
    <muxc:NavigationView.MenuItems>
        <muxc:NavigationViewItem Content="Home" uen:Region.Name="Home" />
        <muxc:NavigationViewItem Content="Settings" uen:Region.Name="Settings" />
    </muxc:NavigationView.MenuItems>

    <Frame uen:Region.Attached="True" />
</muxc:NavigationView>
```

This approach uses Frame navigation (with back stack) instead of visibility switching.

## Display Modes

NavigationView supports different display modes:

```xml
<muxc:NavigationView PaneDisplayMode="Left"
                     uen:Region.Attached="True">
    <!-- Content -->
</muxc:NavigationView>
```

Options:
- `Left` - Always visible sidebar
- `LeftCompact` - Compact sidebar with icons
- `LeftMinimal` - Hidden until hamburger clicked
- `Top` - Top navigation bar
- `Auto` - Adapts based on window size

## Complete Example

```xml
<Page x:Class="MyApp.Views.MainPage"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid uen:Region.Attached="True">
        <muxc:NavigationView x:Name="NavView"
                             uen:Region.Attached="True"
                             PaneDisplayMode="Left"
                             IsBackButtonVisible="Collapsed"
                             IsSettingsVisible="True">
            
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

            <!-- Content Area -->
            <Grid uen:Region.Attached="True"
                  uen:Region.Navigator="Visibility" />
        </muxc:NavigationView>
    </Grid>
</Page>
```

## Best Practices

1. **Use `IsDefault: true`** for initial menu item

2. **Nest routes under Main page** to update only content area

3. **Set Region.Name on SettingsItem** via code-behind

4. **Use icons** for all NavigationViewItems

5. **Consider PaneDisplayMode** for responsive behavior

6. **Match Region.Name exactly** between menu items and routes/content

## Common Issues

| Issue | Solution |
|-------|----------|
| Menu item doesn't update content | Check Region.Name matches route name |
| Settings item not navigating | Set Region.Name in code-behind Loaded event |
| Content appears outside NavigationView | Ensure content Grid is inside NavigationView |
| Back button issues | Consider setting `IsBackButtonVisible="Collapsed"` for region navigation |
