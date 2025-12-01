---
uid: Uno.Extensions.Navigation.Walkthrough.Advanced.UseTabBar
title: Navigate Between Tab Views using TabBar with Region-Based Content Switching
tags: [uno, uno-platform, uno-extensions, uno-toolkit, navigation, TabBar, TabBarItem, regions, Region.Attached, Region.Name, Region.Navigator, Visibility, Navigation.Data, tab-navigation, tab-switching, region-based-navigation, content-switching, nested-navigation, UseToolkitNavigation, RouteMap, ViewMap, IsDefault, tab-content, tab-items, visibility-based-navigation]
---

# Navigate Between Tab Views using TabBar with Region-Based Content Switching

> **UnoFeature:** Navigation (and Toolkit for TabBar)

* Enable Toolkit navigation in `App.xaml.cs`:

    ```csharp
    var builder = this.CreateBuilder(args)
        .UseToolkitNavigation()
        .Configure(host => host....);
    ```

* Import namespaces in XAML:

    ```xml
    <Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
          xmlns:utu="using:Uno.Toolkit.UI">
    ```

## Set Up Tabbar for page navigation

Create the basic structure with TabBar and content areas.

* Add row definitions:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
    </Grid>
    ```

* Add content areas with collapsed visibility:

    ```xml
    <Grid Grid.Row="1">
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
    ```

* Set initial visibility to `Collapsed` — Navigation will toggle to `Visible` when needed.

* Add TabBar control:

    ```xml
    <utu:TabBar Grid.Row="2" VerticalAlignment="Bottom">
        <utu:TabBar.Items>
            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
        </utu:TabBar.Items>
    </utu:TabBar>
    ```

## Set Up Regions and link Tabbar items with page content

Enable region navigation for TabBar.

* Add `Region.Attached="True"` to:
  * Parent Grid
  * Content Grid container
  * TabBar control

    ```xml
    <Grid uen:Region.Attached="True">
        <Grid uen:Region.Attached="True" Grid.Row="1">
            <!-- Content areas -->
        </Grid>
        <utu:TabBar uen:Region.Attached="True" Grid.Row="2">
            <!-- Tab items -->
        </utu:TabBar>
    </Grid>
    ```

* Add `Region.Navigator="Visibility"` to content container:

    ```xml
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility"
          Grid.Row="1">
    ```

* Assign `Region.Name` to both content and TabBarItems:

    ```xml
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility"
          Grid.Row="1">
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
    
    <utu:TabBar uen:Region.Attached="True" Grid.Row="2">
        <utu:TabBar.Items>
            <utu:TabBarItem uen:Region.Name="One"
                            Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            <utu:TabBarItem uen:Region.Name="Two"
                            Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            <utu:TabBarItem uen:Region.Name="Three"
                            Style="{StaticResource MaterialBottomTabBarItemStyle}" />
        </utu:TabBar.Items>
    </utu:TabBar>
    ```

## Navigate to specific sections within tabs

Add TabBarItem that navigates to a separate Page.

* Create a Page `SignUpPage.xaml`:

    ```xml
    <Page x:Class="UsingTabBar.Views.SignUpPage"
          Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Grid>
            <StackPanel Orientation="Vertical">
                <TextBlock Text="Benefits of subscribing:" FontSize="24" />
                <Button Content="Sign Up" />
            </StackPanel>
        </Grid>
    </Page>
    ```

* Create ViewModel `SignUpViewModel.cs`:

    ```csharp
    public class SignUpViewModel
    {
        public SignUpViewModel() { }
    }
    ```

* Register view and route in `App.xaml.cs`:

    ```csharp
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<SignUpPage, SignUpViewModel>(),
            new ViewMap<MainPage, MainViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(),
                        Nested:
                        [
                            new RouteMap("SignUp", View: views.FindByViewModel<SignUpViewModel>())
                        ]
                    )
                ]
            )
        );
    }
    ```

* **Important**: Nest tab routes under `Main` to ensure only content updates, not the entire page.

* Add TabBarItem with route name:

    ```xml
    <utu:TabBarItem uen:Region.Name="SignUp"
                    Style="{StaticResource MaterialBottomTabBarItemStyle}" />
    ```

## Pass data to tab content

Send data to the ViewModel when navigating.

* Add `Navigation.Data` to TabBarItem:

    ```xml
    <utu:TabBarItem uen:Region.Name="SignUp"
                    uen:Navigation.Data="{Binding Entity}"
                    Style="{StaticResource MaterialBottomTabBarItemStyle}" />
    ```

* Set up `DataViewMap` for data injection. See [Define Routes - ViewMap](xref:Uno.Extensions.Navigation.HowToDefineRoutes#viewmap).
