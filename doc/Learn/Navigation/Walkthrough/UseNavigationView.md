---
uid: Uno.Extensions.Navigation.Walkthrough.Advanced.UseNavigationView
title: Navigate Between Menu Items using NavigationView with Adaptive Region-Based Navigation
tags: [uno, uno-platform, uno-extensions, navigation, NavigationView, NavigationViewItem, regions, Region.Attached, Region.Name, Region.Navigator, Visibility, Navigation.Data, adaptive-navigation, menu-navigation, region-based-navigation, responsive-ui, content-switching, nested-navigation, side-navigation, hierarchical-navigation, MenuItems, RouteMap, ViewMap, IsDefault]
---

# Navigate Between Menu Items using NavigationView with Adaptive Region-Based Navigation

> **UnoFeature:** Navigation

## Create menu-based navigation interface

Create the basic structure with NavigationView and content area.

* Add Grid container:

    ```xml
    <Grid>
        <NavigationView>
            <!-- Content will go here -->
        </NavigationView>
    </Grid>
    ```

* Add navigation menu items:

    ```xml
    <NavigationView.MenuItems>
        <NavigationViewItem Content="One" />
        <NavigationViewItem Content="Two" />
        <NavigationViewItem Content="Three" />
    </NavigationView.MenuItems>
    ```

* Add content area with collapsed content:

    ```xml
    <NavigationView>
        <NavigationView.MenuItems>
            <!-- Menu items -->
        </NavigationView.MenuItems>
        <Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="One"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="Two"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid Visibility="Collapsed">
                <TextBlock Text="Three"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
        </Grid>
    </NavigationView>
    ```

## Set Up Navigation

Enable region navigation for NavigationView.

* Add `Region.Attached="True"` to:
  * Parent Grid
  * NavigationView
  * Content Grid

    ```xml
    <Grid uen:Region.Attached="True">
        <NavigationView uen:Region.Attached="True">
            <Grid uen:Region.Attached="True">
                <!-- Content areas -->
            </Grid>
        </NavigationView>
    </Grid>
    ```

* Add `Region.Navigator="Visibility"` to content Grid:

    ```xml
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
    ```

* **Important**: Even when navigating to Pages defined with routes, include the Grid with `Region.Navigator="Visibility"` and `Region.Attached="True"` for navigator functionality.

* Add `Region.Name` to NavigationViewItems and content areas:

    ```xml
    <NavigationView.MenuItems>
        <NavigationViewItem Content="One" uen:Region.Name="One" />
        <NavigationViewItem Content="Two" uen:Region.Name="Two" />
        <NavigationViewItem Content="Three" uen:Region.Name="Three" />
    </NavigationView.MenuItems>

    <Grid uen:Region.Attached="True" uen:Region.Navigator="Visibility">
        <Grid uen:Region.Name="One" Visibility="Collapsed">
            <TextBlock Text="One" FontSize="24" />
        </Grid>
        <Grid uen:Region.Name="Two" Visibility="Collapsed">
            <TextBlock Text="Two" FontSize="24" />
        </Grid>
        <Grid uen:Region.Name="Three" Visibility="Collapsed">
            <TextBlock Text="Three" FontSize="24" />
        </Grid>
    </Grid>
    ```

## Navigate to specific pages from menu

Add NavigationViewItem that navigates to a separate Page.

* Create a Page `ProductsPage.xaml`:

    ```xml
    <Page x:Class="UsingNavigationView.Views.ProductsPage"
          Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Grid>
            <TextBlock Text="Products"
                       FontSize="24"
                       HorizontalAlignment="Center"
                       VerticalAlignment="Center" />
        </Grid>
    </Page>
    ```

* Create ViewModel `ProductsViewModel.cs`:

    ```csharp
    public class ProductsViewModel
    {
        public ProductsViewModel() { }
    }
    ```

* Register view and route in `App.xaml.cs`:

    ```csharp
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<ProductsPage, ProductsViewModel>(),
            new ViewMap<MainPage, MainViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
                        Nested:
                        [
                            new RouteMap("Products", View: views.FindByViewModel<ProductsViewModel>())
                        ]
                    )
                ]
            )
        );
    }
    ```

* **Important**: Nest NavigationViewItem routes under `Main` to update only the content region, not the entire page.

* Add NavigationViewItem with route name:

    ```xml
    <NavigationViewItem Content="Products"
                        uen:Region.Name="Products" />
    ```

## Pass data to menu pages

Send data to the ViewModel when navigating.

* Add `Navigation.Data` to NavigationViewItem:

    ```xml
    <NavigationViewItem Content="Products"
                        uen:Navigation.Data="{Binding Entity}"
                        uen:Region.Name="Products" />
    ```

* Set up `DataViewMap` for data injection. See [Define Routes - ViewMap](xref:Uno.Extensions.Navigation.HowToDefineRoutes#viewmap).
