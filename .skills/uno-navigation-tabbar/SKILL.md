---
name: uno-navigation-tabbar
description: Implement TabBar navigation in Uno Platform using Uno Toolkit TabBar control with region-based navigation. Use when building bottom navigation, tab-based interfaces, or main app navigation with tabs. Covers TabBar setup, region linking, styling, and data passing.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# TabBar Navigation

This skill covers implementing TabBar-based navigation in Uno Platform applications.

## Prerequisites

Add both `Navigation` and `Toolkit` to UnoFeatures:

```xml
<UnoFeatures>Navigation;Toolkit</UnoFeatures>
```

Enable Toolkit navigation in `App.xaml.cs`:

```csharp
var builder = this.CreateBuilder(args)
    .UseToolkitNavigation()
    .Configure(host => host
        .UseNavigation(RegisterRoutes)
    );
```

## XAML Namespace

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI">
```

## Basic TabBar Setup

### Structure with Inline Content

```xml
<Grid uen:Region.Attached="True">
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>

    <!-- Content Area -->
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
    <utu:TabBar Grid.Row="1"
                uen:Region.Attached="True"
                Style="{StaticResource BottomTabBarStyle}">
        <utu:TabBarItem Content="Home" uen:Region.Name="Home" />
        <utu:TabBarItem Content="Search" uen:Region.Name="Search" />
        <utu:TabBarItem Content="Profile" uen:Region.Name="Profile" />
    </utu:TabBar>
</Grid>
```

### Key Points

1. **Root Grid** must have `uen:Region.Attached="True"`
2. **Content Grid** needs `Region.Attached="True"` and `Region.Navigator="Visibility"`
3. **Child Grids** need `Region.Name` matching TabBarItem names
4. **Initial Visibility** should be `Collapsed` for all content areas
5. **TabBar** needs `Region.Attached="True"`
6. **TabBarItems** need `Region.Name` matching content areas

## TabBar with Registered Routes

### Route Registration

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>(),
        new ViewMap<HomePage, HomeViewModel>(),
        new ViewMap<SearchPage, SearchViewModel>(),
        new ViewMap<ProfilePage, ProfileViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
                    Nested:
                    [
                        new RouteMap("Home", View: views.FindByViewModel<HomeViewModel>(), IsDefault: true),
                        new RouteMap("Search", View: views.FindByViewModel<SearchViewModel>()),
                        new RouteMap("Profile", View: views.FindByViewModel<ProfileViewModel>())
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
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>

    <!-- Content Area - Views loaded automatically -->
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility" />

    <!-- TabBar -->
    <utu:TabBar Grid.Row="1"
                uen:Region.Attached="True"
                Style="{StaticResource BottomTabBarStyle}">
        <utu:TabBarItem Content="Home" uen:Region.Name="Home" />
        <utu:TabBarItem Content="Search" uen:Region.Name="Search" />
        <utu:TabBarItem Content="Profile" uen:Region.Name="Profile" />
    </utu:TabBar>
</Grid>
```

## TabBar Styles

### Bottom TabBar (Material)

```xml
<utu:TabBar Style="{StaticResource BottomTabBarStyle}">
```

### Vertical TabBar

```xml
<utu:TabBar Style="{StaticResource VerticalTabBarStyle}">
```

### TabBarItem with Icon

```xml
<utu:TabBarItem uen:Region.Name="Home"
                Content="Home">
    <utu:TabBarItem.Icon>
        <FontIcon Glyph="&#xE80F;" />
    </utu:TabBarItem.Icon>
</utu:TabBarItem>
```

Or using a path icon:

```xml
<utu:TabBarItem uen:Region.Name="Home"
                Content="Home">
    <utu:TabBarItem.Icon>
        <PathIcon Data="{StaticResource HomeIconPath}" />
    </utu:TabBarItem.Icon>
</utu:TabBarItem>
```

## Passing Data to Tabs

### With Navigation.Data

```xml
<utu:TabBarItem uen:Region.Name="Profile"
                uen:Navigation.Data="{Binding CurrentUser}"
                Content="Profile" />
```

### DataViewMap Registration

```csharp
new DataViewMap<ProfilePage, ProfileViewModel, User>()
```

### ViewModel Receives Data

```csharp
public class ProfileViewModel
{
    public ProfileViewModel(User user)
    {
        // Use user data
    }
}
```

## Nested Navigation from Tab

Navigate to sub-pages from within a tab:

```xml
<!-- In HomePage -->
<Button Content="View Details"
        uen:Navigation.Request="ProductDetails"
        uen:Navigation.Data="{Binding SelectedProduct}" />
```

```csharp
// Route registration - ProductDetails nested under Home
new RouteMap("Home", View: views.FindByViewModel<HomeViewModel>(),
    Nested:
    [
        new RouteMap("ProductDetails", View: views.FindByViewModel<ProductDetailsViewModel>())
    ]
)
```

## Responsive TabBar

Show different TabBar layouts based on screen size:

```xml
<Grid uen:Region.Attached="True">
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>

    <!-- Content Area -->
    <Grid Grid.Column="1"
          uen:Region.Attached="True"
          uen:Region.Navigator="Visibility" />

    <!-- Horizontal TabBar (Normal screens) -->
    <utu:TabBar Grid.Row="1"
                Grid.Column="1"
                Visibility="{utu:Responsive Normal=Visible, Wide=Collapsed}"
                uen:Region.Attached="True"
                Style="{StaticResource BottomTabBarStyle}">
        <!-- TabBarItems -->
    </utu:TabBar>

    <!-- Vertical TabBar (Wide screens) -->
    <utu:TabBar Grid.RowSpan="2"
                Visibility="{utu:Responsive Normal=Collapsed, Wide=Visible}"
                uen:Region.Attached="True"
                Style="{StaticResource VerticalTabBarStyle}">
        <!-- Same TabBarItems -->
    </utu:TabBar>
</Grid>
```

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Tabs don't switch content | Verify `Region.Attached="True"` on root Grid |
| Content doesn't appear | Check `Region.Name` matches between TabBarItem and content |
| Navigation throws errors | Ensure `UseToolkitNavigation()` is called |
| Wrong tab selected initially | Set `IsDefault: true` in route registration |
| Content not collapsed initially | Add `Visibility="Collapsed"` to content areas |

## Best Practices

1. **Nest tab routes under the main page** to update only content, not entire page

2. **Use `IsDefault: true`** for the initial tab

3. **Keep TabBar items simple** - use icons and short labels

4. **Use `BottomTabBarStyle`** for mobile-first design

5. **Implement responsive layout** for tablet/desktop support

6. **Match Region.Name exactly** between TabBarItem and content/routes

## Complete Example

```xml
<Page x:Class="MyApp.Views.MainPage"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition />
            <RowDefinition Height="Auto" />
        </Grid.RowDefinitions>

        <Grid uen:Region.Attached="True"
              uen:Region.Navigator="Visibility" />

        <utu:TabBar Grid.Row="1"
                    uen:Region.Attached="True"
                    Style="{StaticResource BottomTabBarStyle}">
            <utu:TabBarItem uen:Region.Name="Home" Content="Home">
                <utu:TabBarItem.Icon>
                    <FontIcon Glyph="&#xE80F;" />
                </utu:TabBarItem.Icon>
            </utu:TabBarItem>
            <utu:TabBarItem uen:Region.Name="Search" Content="Search">
                <utu:TabBarItem.Icon>
                    <FontIcon Glyph="&#xE721;" />
                </utu:TabBarItem.Icon>
            </utu:TabBarItem>
            <utu:TabBarItem uen:Region.Name="Profile" Content="Profile">
                <utu:TabBarItem.Icon>
                    <FontIcon Glyph="&#xE77B;" />
                </utu:TabBarItem.Icon>
            </utu:TabBarItem>
        </utu:TabBar>
    </Grid>
</Page>
```
