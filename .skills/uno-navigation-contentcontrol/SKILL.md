---
name: uno-navigation-contentcontrol
description: Implement ContentControl region navigation in Uno Platform for dynamic content areas without back stack. Use when you need to swap content in a specific area of your layout without navigation history.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# ContentControl Region Navigation

This skill covers using ContentControl for navigation in Uno Platform, ideal for dynamic content areas.

## When to Use ContentControl

- **Simple content switching** without back stack needed
- **Embedded content areas** within a larger page
- **Wizard-style flows** where you control progression
- **Sidebar or detail panels** that change based on selection

## Prerequisites

```xml
<UnoFeatures>Navigation</UnoFeatures>
```

## XAML Namespace

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI">
```

## Basic Setup

### XAML Structure

```xml
<Grid uen:Region.Attached="True">
    <!-- Navigation Controls -->
    <StackPanel Grid.Column="0">
        <Button Content="View A" uen:Navigation.Request="ViewA" />
        <Button Content="View B" uen:Navigation.Request="ViewB" />
        <Button Content="View C" uen:Navigation.Request="ViewC" />
    </StackPanel>

    <!-- ContentControl Region -->
    <ContentControl Grid.Column="1"
                    uen:Region.Attached="True"
                    HorizontalContentAlignment="Stretch"
                    VerticalContentAlignment="Stretch" />
</Grid>
```

### Route Registration

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap<MainPage, MainViewModel>(),
        new ViewMap<ViewAPage, ViewAViewModel>(),
        new ViewMap<ViewBPage, ViewBViewModel>(),
        new ViewMap<ViewCPage, ViewCViewModel>()
    );

    routes.Register(
        new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
            Nested:
            [
                new RouteMap("ViewA", View: views.FindByViewModel<ViewAViewModel>(), IsDefault: true),
                new RouteMap("ViewB", View: views.FindByViewModel<ViewBViewModel>()),
                new RouteMap("ViewC", View: views.FindByViewModel<ViewCViewModel>())
            ]
        )
    );
}
```

## ContentControl Alignment

Always set alignment for proper content display:

```xml
<ContentControl uen:Region.Attached="True"
                HorizontalContentAlignment="Stretch"
                VerticalContentAlignment="Stretch" />
```

Without these properties, content may not fill the available space.

## Named Regions

Use Region.Name to target specific ContentControl instances:

```xml
<Grid uen:Region.Attached="True">
    <!-- Left Panel -->
    <ContentControl Grid.Column="0"
                    uen:Region.Attached="True"
                    uen:Region.Name="LeftPanel"
                    HorizontalContentAlignment="Stretch"
                    VerticalContentAlignment="Stretch" />

    <!-- Right Panel -->
    <ContentControl Grid.Column="1"
                    uen:Region.Attached="True"
                    uen:Region.Name="RightPanel"
                    HorizontalContentAlignment="Stretch"
                    VerticalContentAlignment="Stretch" />
</Grid>
```

Navigate to specific panel:

```xml
<Button Content="Show Details in Right"
        uen:Navigation.Request="./RightPanel/Details" />
```

## ContentControl vs Frame

| Feature | ContentControl | Frame |
|---------|---------------|-------|
| Back Stack | No | Yes |
| Page History | No | Yes |
| Lightweight | Yes | No |
| Animation | Manual | Built-in |
| Use Case | Simple switching | Full navigation |

## Programmatic Navigation

Navigate to content in a ContentControl:

```csharp
// By route name
await _navigator.NavigateRouteAsync(this, "ViewB");

// By ViewModel
await _navigator.NavigateViewModelAsync<ViewBViewModel>(this);

// With data
await _navigator.NavigateDataAsync(this, data: myData);
```

## Passing Data

### DataViewMap Registration

```csharp
views.Register(
    new DataViewMap<DetailsPage, DetailsViewModel, Product>()
);

routes.Register(
    new RouteMap("Details", View: views.FindByViewModel<DetailsViewModel>())
);
```

### XAML Navigation with Data

```xml
<Button Content="View Details"
        uen:Navigation.Request="Details"
        uen:Navigation.Data="{Binding SelectedProduct}" />
```

### ViewModel Receives Data

```csharp
public partial class DetailsViewModel : ObservableObject
{
    public DetailsViewModel(Product product)
    {
        Product = product;
    }

    public Product Product { get; }
}
```

## Multiple ContentControl Regions

### Split-View Pattern

```xml
<Grid uen:Region.Attached="True">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="300" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- Master List -->
    <ContentControl Grid.Column="0"
                    uen:Region.Attached="True"
                    uen:Region.Name="Master" />

    <!-- Detail View -->
    <ContentControl Grid.Column="1"
                    uen:Region.Attached="True"
                    uen:Region.Name="Detail" />
</Grid>
```

### Navigate to Specific Region

```xml
<!-- Navigate Master region -->
<Button uen:Navigation.Request="./Master/ProductList" />

<!-- Navigate Detail region -->
<Button uen:Navigation.Request="./Detail/ProductDetails" />
```

## Conditional Content Display

Use ContentControl for conditional content based on selection:

```xml
<Grid uen:Region.Attached="True">
    <!-- Category selector -->
    <ListView ItemsSource="{Binding Categories}"
              SelectedItem="{Binding SelectedCategory, Mode=TwoWay}">
        <ListView.ItemTemplate>
            <DataTemplate>
                <TextBlock Text="{Binding Name}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>

    <!-- Dynamic content based on selection -->
    <ContentControl Grid.Column="1"
                    uen:Region.Attached="True" />
</Grid>
```

Navigate programmatically on selection:

```csharp
partial void OnSelectedCategoryChanged(Category value)
{
    _navigator.NavigateRouteAsync(this, value.RouteKey);
}
```

## Complete Example

### MainPage.xaml

```xml
<Page x:Class="MyApp.Views.MainPage"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid uen:Region.Attached="True"
          Padding="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Navigation Buttons -->
        <StackPanel Orientation="Horizontal"
                    Spacing="8">
            <Button Content="Overview"
                    uen:Navigation.Request="Overview" />
            <Button Content="Statistics"
                    uen:Navigation.Request="Statistics" />
            <Button Content="Reports"
                    uen:Navigation.Request="Reports" />
        </StackPanel>

        <!-- Content Area -->
        <ContentControl Grid.Row="1"
                        uen:Region.Attached="True"
                        HorizontalContentAlignment="Stretch"
                        VerticalContentAlignment="Stretch"
                        Margin="0,16,0,0" />
    </Grid>
</Page>
```

### Route Registration

```csharp
routes.Register(
    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
        Nested:
        [
            new RouteMap("Overview", View: views.FindByView<OverviewPage>(), IsDefault: true),
            new RouteMap("Statistics", View: views.FindByView<StatisticsPage>()),
            new RouteMap("Reports", View: views.FindByView<ReportsPage>())
        ]
    )
);
```

## Best Practices

1. **Always set ContentAlignment** - Without it, content may not fill space

2. **Use IsDefault** - Set one route as default for initial content

3. **Use Region.Name** when you have multiple ContentControls

4. **Nest routes properly** - Child routes should be nested under parent

5. **Keep content lightweight** - ContentControl doesn't manage back stack

## Common Issues

| Issue | Solution |
|-------|----------|
| Content doesn't fill space | Set `HorizontalContentAlignment="Stretch"` and `VerticalContentAlignment="Stretch"` |
| Navigation not working | Ensure `Region.Attached="True"` on ContentControl |
| Wrong content displayed | Check route nesting matches XAML hierarchy |
| Multiple regions conflict | Use `Region.Name` to distinguish regions |
| Initial content missing | Set `IsDefault: true` on one nested route |
