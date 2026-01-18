---
name: uno-navigation-panel-visibility
description: Implement visibility-based navigation in Uno Platform using Panel or Grid controls. Use when you need lightweight content switching without Frame overhead, keeping all content pre-rendered with visibility toggling.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Panel/Grid Visibility Navigation

This skill covers using Panel or Grid controls with visibility-based navigation in Uno Platform.

## When to Use Visibility Navigation

- **Pre-rendered content** that needs instant switching
- **Lightweight transitions** without page lifecycle overhead
- **Simple tabbed interfaces** without TabBar control
- **Toggle between states** in a section of the UI
- **No back stack needed**

## Prerequisites

```xml
<UnoFeatures>Navigation</UnoFeatures>
```

## XAML Namespace

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI">
```

## Core Concept

The `Region.Navigator="Visibility"` attached property tells the navigation system to:
1. Show the target region by setting `Visibility="Visible"`
2. Hide sibling regions by setting `Visibility="Collapsed"`

## Basic Setup

### XAML Structure

```xml
<Grid uen:Region.Attached="True">
    <!-- Navigation Buttons -->
    <StackPanel Orientation="Horizontal" Spacing="8">
        <Button Content="Tab 1" uen:Navigation.Request="Tab1" />
        <Button Content="Tab 2" uen:Navigation.Request="Tab2" />
        <Button Content="Tab 3" uen:Navigation.Request="Tab3" />
    </StackPanel>

    <!-- Content Panel with Visibility Navigation -->
    <Grid Grid.Row="1"
          uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
        
        <Grid uen:Region.Name="Tab1" Visibility="Collapsed">
            <TextBlock Text="Tab 1 Content" />
        </Grid>
        
        <Grid uen:Region.Name="Tab2" Visibility="Collapsed">
            <TextBlock Text="Tab 2 Content" />
        </Grid>
        
        <Grid uen:Region.Name="Tab3" Visibility="Collapsed">
            <TextBlock Text="Tab 3 Content" />
        </Grid>
    </Grid>
</Grid>
```

### Key Elements

1. **Parent Grid** with `Region.Attached="True"` and `Region.Navigator="Visibility"`
2. **Child elements** with `Region.Name` and initial `Visibility="Collapsed"`
3. **Navigation buttons** with `Navigation.Request` matching Region.Name values

## Initial Visibility

Set the default visible content by:

### Option 1: Initial Visibility in XAML

```xml
<Grid uen:Region.Attached="True"
      uen:Region.Navigator="Visibility">
    
    <Grid uen:Region.Name="Tab1" Visibility="Visible">
        <!-- Default visible content -->
    </Grid>
    
    <Grid uen:Region.Name="Tab2" Visibility="Collapsed">
        <!-- Hidden until navigated -->
    </Grid>
</Grid>
```

### Option 2: IsDefault in Route Registration

```csharp
routes.Register(
    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
        Nested:
        [
            new RouteMap("Tab1", IsDefault: true),
            new RouteMap("Tab2"),
            new RouteMap("Tab3")
        ]
    )
);
```

## With Route-Loaded Views

Instead of inline content, load Views from routes:

### Route Registration

```csharp
views.Register(
    new ViewMap<MainPage, MainViewModel>(),
    new ViewMap<Tab1Page, Tab1ViewModel>(),
    new ViewMap<Tab2Page, Tab2ViewModel>(),
    new ViewMap<Tab3Page, Tab3ViewModel>()
);

routes.Register(
    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
        Nested:
        [
            new RouteMap("Tab1", View: views.FindByViewModel<Tab1ViewModel>(), IsDefault: true),
            new RouteMap("Tab2", View: views.FindByViewModel<Tab2ViewModel>()),
            new RouteMap("Tab3", View: views.FindByViewModel<Tab3ViewModel>())
        ]
    )
);
```

### XAML with Empty Regions

```xml
<Grid uen:Region.Attached="True"
      uen:Region.Navigator="Visibility">
    <!-- Views will be loaded into these regions from routes -->
</Grid>
```

The navigation system automatically creates the necessary container elements.

## Nested Visibility Regions

Create hierarchical visibility switching:

```xml
<Grid uen:Region.Attached="True"
      uen:Region.Navigator="Visibility">
    
    <Grid uen:Region.Name="Section1" Visibility="Collapsed">
        <Grid uen:Region.Attached="True"
              uen:Region.Navigator="Visibility">
            <Grid uen:Region.Name="SubA" Visibility="Collapsed">
                <TextBlock Text="Section 1 - Sub A" />
            </Grid>
            <Grid uen:Region.Name="SubB" Visibility="Collapsed">
                <TextBlock Text="Section 1 - Sub B" />
            </Grid>
        </Grid>
    </Grid>
    
    <Grid uen:Region.Name="Section2" Visibility="Collapsed">
        <TextBlock Text="Section 2 Content" />
    </Grid>
</Grid>
```

Navigate to nested region:

```xml
<Button Content="Go to Section 1 Sub B"
        uen:Navigation.Request="Section1/SubB" />
```

## Programmatic Navigation

```csharp
// Navigate to specific visibility region
await _navigator.NavigateRouteAsync(this, "Tab2");

// Navigate to nested region
await _navigator.NavigateRouteAsync(this, "Section1/SubB");
```

## Using Panel Instead of Grid

Any Panel-derived control works:

### StackPanel Example

```xml
<StackPanel uen:Region.Attached="True"
            uen:Region.Navigator="Visibility">
    <Border uen:Region.Name="View1" Visibility="Collapsed">
        <TextBlock Text="View 1" />
    </Border>
    <Border uen:Region.Name="View2" Visibility="Collapsed">
        <TextBlock Text="View 2" />
    </Border>
</StackPanel>
```

### Canvas Example

```xml
<Canvas uen:Region.Attached="True"
        uen:Region.Navigator="Visibility">
    <Grid uen:Region.Name="Layer1" Visibility="Collapsed" />
    <Grid uen:Region.Name="Layer2" Visibility="Collapsed" />
</Canvas>
```

## Visibility vs Frame Navigation

| Aspect | Visibility | Frame |
|--------|------------|-------|
| Content Lifetime | Always in memory | Created/destroyed on navigation |
| Performance | Faster switching | Slower transitions |
| Memory | Higher (all views loaded) | Lower (only current view) |
| Back Stack | No | Yes |
| Page Events | Not triggered | OnNavigatedTo/From called |
| State Preservation | Automatic | Manual |

## Toggle Pattern

For two-state toggles:

```xml
<Grid uen:Region.Attached="True"
      uen:Region.Navigator="Visibility">
    
    <Grid uen:Region.Name="ListView" Visibility="Visible">
        <ListView ItemsSource="{Binding Items}" />
        <Button Content="Switch to Grid"
                uen:Navigation.Request="GridView" />
    </Grid>
    
    <Grid uen:Region.Name="GridView" Visibility="Collapsed">
        <GridView ItemsSource="{Binding Items}" />
        <Button Content="Switch to List"
                uen:Navigation.Request="ListView" />
    </Grid>
</Grid>
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

        <!-- Tab-like Navigation -->
        <StackPanel Orientation="Horizontal"
                    Spacing="4">
            <Button Content="Overview"
                    Style="{StaticResource TextButtonStyle}"
                    uen:Navigation.Request="Overview" />
            <Button Content="Details"
                    Style="{StaticResource TextButtonStyle}"
                    uen:Navigation.Request="Details" />
            <Button Content="History"
                    Style="{StaticResource TextButtonStyle}"
                    uen:Navigation.Request="History" />
        </StackPanel>

        <!-- Visibility-Switched Content -->
        <Grid Grid.Row="1"
              uen:Region.Attached="True"
              uen:Region.Navigator="Visibility"
              Margin="0,16,0,0">

            <Grid uen:Region.Name="Overview"
                  Visibility="Visible">
                <StackPanel>
                    <TextBlock Text="Overview"
                               Style="{StaticResource TitleTextBlockStyle}" />
                    <TextBlock Text="This is the overview section."
                               Style="{StaticResource BodyTextBlockStyle}" />
                </StackPanel>
            </Grid>

            <Grid uen:Region.Name="Details"
                  Visibility="Collapsed">
                <StackPanel>
                    <TextBlock Text="Details"
                               Style="{StaticResource TitleTextBlockStyle}" />
                    <TextBlock Text="Detailed information goes here."
                               Style="{StaticResource BodyTextBlockStyle}" />
                </StackPanel>
            </Grid>

            <Grid uen:Region.Name="History"
                  Visibility="Collapsed">
                <StackPanel>
                    <TextBlock Text="History"
                               Style="{StaticResource TitleTextBlockStyle}" />
                    <TextBlock Text="Historical data and logs."
                               Style="{StaticResource BodyTextBlockStyle}" />
                </StackPanel>
            </Grid>
        </Grid>
    </Grid>
</Page>
```

## Best Practices

1. **Set initial Visibility** - One child should start with `Visibility="Visible"` or use `IsDefault: true`

2. **Keep content lightweight** - All children are in memory simultaneously

3. **Use for related content** - Best when switching between views of the same data

4. **Match Region.Name exactly** - Case-sensitive matching with Navigation.Request

5. **Prefer Grid children** - They layer properly when switching visibility

## Common Issues

| Issue | Solution |
|-------|----------|
| Nothing visible initially | Set one child to `Visibility="Visible"` or use `IsDefault: true` |
| Content stacking/overlapping | Ensure children occupy same grid cell (row 0, column 0) |
| Navigation not switching | Verify `Region.Navigator="Visibility"` on parent |
| Region.Name not found | Check for typos and case sensitivity |
| Memory issues | Consider Frame navigation for heavy content |
