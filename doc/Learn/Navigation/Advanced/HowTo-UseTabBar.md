---
uid: Uno.Extensions.Navigation.Advanced.TabBar
---
# How-To: Use a TabBar to Switch Views

The navigation capabilities offered by Uno.Extensions include regions. Regions allow you to associate a specific sector of the view with an individual item on a navigation control from the same `Page`. Likewise, the Uno.Extensions library has built-in support for responding to navigation gestures from the [Toolkit](https://github.com/unoplatform/uno.toolkit.ui) `TabBar`. Follow the steps below to define a user interface centered around navigating with this control.

Since `TabBar` comes from the `Uno.Toolkit` you need to make sure your project has `Toolkit` added to the `<UnoFeatures>` property in the Class Library (.csproj) file and that you call the `UseToolkitNavigation` extension method on the `IApplicationBuilder` (not `IHostBuilder`). For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

```csharp
var builder = this.CreateBuilder(args)
    // Add navigation support for toolkit controls such as TabBar and NavigationView
    .UseToolkitNavigation()
    .Configure(host => host....);
```

[!include[getting-help](../../includes/mvvm-approach.md)]

## Step-by-step

[!include[create-application](../../includes/create-application.md)]

### 1. Add necessary XAML namespaces

* Update the `Page` element in `MainPage.xaml` to include XAML namespace mappings for Navigation and Uno Toolkit:

    ```xml
    <Page x:Class="UsingTabBar.Views.MainPage"
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:local="using:UsingTabBar.Views"
            xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
            mc:Ignorable="d"
            xmlns:uen="using:Uno.Extensions.Navigation.UI"
            xmlns:utu="using:Uno.Toolkit.UI"
    ...
    ```

### 2. Define the view's layout

* Add `RowDefinition`s to the root `Grid` in `MainPage.xaml`:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
    </Grid>
    ```

* Define initial page and `TabBarItem` content. It's important to make each element that represents a sector of app content have it's `Visibility` explicitly set to `Collapsed`. Uno.Extensions will handle toggling it back to `Visible` when necessary

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                            Style="{StaticResource MaterialNavigationBarStyle}" />
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
    </Grid>
    ```

* Add `TabBar` to the view:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                            Style="{StaticResource MaterialNavigationBarStyle}" />
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
        <utu:TabBar Grid.Row="2"
                    VerticalAlignment="Bottom">
            <utu:TabBar.Items>
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
    ```

### 3. Set up regions and specify navigator type

* Use the `Region.Attached="True"` attached property to enable regions on all of the following:
  * The `TabBar` control
  * The containing element of the collapsed content `Grid` definitions
  * The parent element of both controls

    ```xml
    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                            Style="{StaticResource MaterialNavigationBarStyle}" />
        <Grid uen:Region.Attached="True"
                Grid.Row="1">
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
        <utu:TabBar Grid.Row="2"
                    uen:Region.Attached="True"
                    VerticalAlignment="Bottom">
            <utu:TabBar.Items>
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
    ```

* Name the regions you defined by using the `Region.Name` attached property on both the content itself and associated navigation control item:

    ```xml
    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                            Style="{StaticResource MaterialNavigationBarStyle}" />
        <Grid uen:Region.Attached="True"
                Grid.Row="1">
            <Grid uen:Region.Name="One" 
                    Visibility="Collapsed">
                <TextBlock Text="One"
                            FontSize="24"
                            HorizontalAlignment="Center"
                            VerticalAlignment="Center" />
            </Grid>
            <Grid uen:Region.Name="Two" 
                    Visibility="Collapsed">
                <TextBlock Text="Two"
                            FontSize="24"
                            HorizontalAlignment="Center"
                            VerticalAlignment="Center" />
            </Grid>
            <Grid uen:Region.Name="Three" 
                    Visibility="Collapsed">
                <TextBlock Text="Three"
                            FontSize="24"
                            HorizontalAlignment="Center"
                            VerticalAlignment="Center" />
            </Grid>
        </Grid>
        <utu:TabBar Grid.Row="2"
                    uen:Region.Attached="True"
                    VerticalAlignment="Bottom">
            <utu:TabBar.Items>
                <utu:TabBarItem uen:Region.Name="One" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem uen:Region.Name="Two" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem uen:Region.Name="Three" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
    ```

* Specify the navigator type as `Visibility` using the `Region.Navigator` attached property on the containing element of your collapsed content `Grid` definitions:

    ```xml
    <Grid uen:Region.Attached="True"
        uen:Region.Navigator="Visibility"
        Grid.Row="1">
    ```

#### Navigating to Page elements

* You may want to navigate to a `Page` view element represented by a route name. It is possible to do this without defining a view element alongside the other content regions. For instance, you may need to display a subscription sign up page `SignUpPage` which will be defined in a separate XAML file.

* Add a new **Page** item to your app called `SignUpPage` with the following code:

  ```xml
  <Page
      x:Class="UsingTabBar.Views.SignUpPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:Uno.Extensions.Navigation.UI.Samples"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      mc:Ignorable="d"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
  
      <Grid>
            <StackPanel Orientation="Vertical">
                <TextBlock Text="Benefits of subscribing:"
                            FontSize="24"
                            HorizontalAlignment="Center"
                            VerticalAlignment="Center" />
                <Button Content="Sign Up"
                        HorizontalAlignment="Center"
                        VerticalAlignment="Center" />
            </StackPanel>
      </Grid>
  </Page>
  ```

* For the purposes for this tutorial, `SignUpPage` will be associated with its own view model `SignUpViewModel`. Add a new **Class** item to your app called `SignUpViewModel` with the following code:

  ```csharp
  namespace UsingTabBar.ViewModels;
  
  public class SignUpViewModel
  {
      public SignUpViewModel()
      {

      }
  }
  ```

* Register `ViewMap` and `RouteMap` instances inside the `RegisterRoutes` method in `App.xaml.cs`. This associates the `SignUpPage` described above with `SignUpViewModel`, as well as avoiding the use of reflection for route discovery.

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

    > [!NOTE]  
    > To ensure that navigating between tabs only updates the content region (and doesn’t replace the entire page or TabBar), nest each `TabBar` child route under the `Main` route. You do not need to declare **explicit** `RouteMap` entries for TabBar navigation to work, but if you do, they must be nested under Main.
  
* Importantly, the snippet above establishes a route name `SignUp` for `SignUpPage`. We can use this route name to navigate to the `SignUpPage` view element.

* Add a `TabBarItem` to the `TabBar` element with the `uen:Region.Name` attached property set to `SignUp`.

    ```xml
    <utu:TabBar.Items>
        <utu:TabBarItem uen:Region.Name="One" 
                        Style="{StaticResource MaterialBottomTabBarItemStyle}" />
        <utu:TabBarItem uen:Region.Name="Two" 
                        Style="{StaticResource MaterialBottomTabBarItemStyle}" />
        <utu:TabBarItem uen:Region.Name="Three" 
                        Style="{StaticResource MaterialBottomTabBarItemStyle}" />
        <!-- Sign up item -->
        <utu:TabBarItem uen:Region.Name="SignUp" 
                        Style="{StaticResource MaterialBottomTabBarItemStyle}" />
    </utu:TabBar.Items>
    ```

#### Using the `Navigation.Data` attached property

Sometimes, it is necessary to send data to your ViewModel from the previous page. This can be done using the `Navigation.Data` attached property. For example, if you want to send an `Entity` object from the `MainViewModel` to the `SignUpViewModel`:

```diff
<!-- Sign up item -->
<utu:TabBarItem uen:Region.Name="SignUp"
+               uen:Navigation.Data="{Binding Entity}"
                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
```

For the full setup and more information on using the `Navigation.Data` attached property, refer to the documentation in the [How-To: Navigate in XAML](xref:Uno.Extensions.Navigation.HowToNavigateInXAML#2-navigationdata) guide.

> [!NOTE]  
> You also need to set up a `DataViewMap`. For more information on `ViewMap` and `DataViewMap`, refer to the **ViewMap** documentation in the [How-To: Define Routes](xref:Uno.Extensions.Navigation.HowToDefineRoutes#viewmap) guide.

### 6. Putting it all together

* When a `TabBarItem` is selected, the content which corresponds to the route name of the item will be displayed, with the `Visibility` property changed if needed.

* If that route name represents a `Page` element, a `Frame` will be created upon navigation to host the `Page` element. This `Frame` will be added to the visual tree in order to support subsequent navigation to other `Page` elements.

* Because the navigation service maintains an instance of the view, users can leave this new `SignUpPage` and return to it _without_ losing any state such as form data.

* Now that you have a functional tab bar navigation system, you can run it to see the results. Your completed `MainPage.xaml` should look like the code example below.

#### Code example

```xml
<Page x:Class="UsingTabBar.Views.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:UsingTabBar.Views"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      mc:Ignorable="d"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI">

    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                            Style="{StaticResource MaterialNavigationBarStyle}" />
        <Grid uen:Region.Attached="True"
              uen:Region.Navigator="Visibility"
              Grid.Row="1">
            <Grid uen:Region.Name="One" 
                  Visibility="Collapsed">
                <TextBlock Text="One"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid uen:Region.Name="Two" 
                  Visibility="Collapsed">
                <TextBlock Text="Two"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
            <Grid uen:Region.Name="Three" 
                  Visibility="Collapsed">
                <TextBlock Text="Three"
                           FontSize="24"
                           HorizontalAlignment="Center"
                           VerticalAlignment="Center" />
            </Grid>
        </Grid>
        <utu:TabBar Grid.Row="2"
                    uen:Region.Attached="True"
                    VerticalAlignment="Bottom">
            <utu:TabBar.Items>
                <utu:TabBarItem uen:Region.Name="One" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem uen:Region.Name="Two" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem uen:Region.Name="Three" 
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
                <utu:TabBarItem uen:Region.Name="SignUp" 
                                uen:Navigation.Data="{Binding Entity}"
                                Style="{StaticResource MaterialBottomTabBarItemStyle}" />
            </utu:TabBar.Items>
        </utu:TabBar>
    </Grid>
</Page>
```
