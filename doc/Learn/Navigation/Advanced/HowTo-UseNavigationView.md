---
uid: Uno.Extensions.Navigation.Advanced.NavigationView
---
# How-To: Use a NavigationView to Switch Views

Choosing the right control for your navigation needs is important, and one common choice is `NavigationView`. This control adapts to different screen sizes and offers a uniform top-level navigation experience. `NavigationView` is a great option for adaptive, customizable, and mobile-friendly navigation. The Uno Platform extensions for navigation provide built-in support for using `NavigationView` and `NavigationViewItem` to switch between views. This tutorial will show you how to configure a `NavigationView` for use with the navigation extensions.

[!include[getting-help](../../includes/mvvm-approach.md)]

## Step-by-step

[!include[create-application](../../includes/create-application.md)]

### 1. Add XAML namespace mapping

* Add the following namespace mapping to the root element of your XAML page:

  ```xml
  xmlns:uen="using:Uno.Extensions.Navigation.UI"
  ```

### 2. Define the view's layout

* Add a `Grid` element to the root of your XAML page. This will be the container for the `NavigationView` and the content area.

  ```xml
  <Grid>
      ...
  </Grid>
  ```

* Add a `NavigationView` control to the `Grid`. This will be the menu for the app.

  ```xml
  <NavigationView>
      ...
  </NavigationView>
  ```

* Add a `Grid` element to the control. `NavigationView` contains two sections for content:

  * A pane which contains a list of navigation `MenuItems`
  * The content area intended to correspond with the selected `NavigationViewItem`.

  For this tutorial, `Grid` should be placed in the `Content` area.

  ```xml
  <Grid>
      ...
  </Grid>
  ```

### 3. Add the navigation view items

* Add the `NavigationView.MenuItems` collection to the `NavigationView` and add a `NavigationViewItem` for each view you want to navigate to.

  ```xml
  <NavigationView.MenuItems>
      <NavigationViewItem Content="One" />
      <NavigationViewItem Content="Two" />
      <NavigationViewItem Content="Three" />
  </NavigationView.MenuItems>
  ```

### 4. Add the navigation view content

* Inside the `Grid` element of the `NavigationView`, add a `Grid` element to represent the content of each view you can to navigate to.

  ```xml
  <Grid>
      <Grid>
          <TextBlock Text="One"
                     FontSize="24"
                     HorizontalAlignment="Center"
                     VerticalAlignment="Center" />
      </Grid>
      <Grid>
          <TextBlock Text="Two"
                     FontSize="24"
                     HorizontalAlignment="Center"
                     VerticalAlignment="Center" />
      </Grid>
      <Grid>
          <TextBlock Text="Three"
                     FontSize="24"
                     HorizontalAlignment="Center"
                     VerticalAlignment="Center" />
      </Grid>
  </Grid>
  ```

### 5. Set up navigation

* Components with navigation enabled will have their visual trees considered by the navigation service when a request is made. The matching **region** will be shown and the rest will be hidden.

* Add the `uen:Region.Attached` attached property to each of the following elements:

  * The `Grid` element that contains the `NavigationView`.
  * The `NavigationView` element.
  * The `Grid` element that contains the content area.

  ```xml
  uen:Region.Attached="True"
  ```

* Setting this to `True` will enable the navigation extensions for the element.

* Add the `uen:Region.Name` attached property to each of the `NavigationViewItem` elements. This will define the name of the view that the `NavigationViewItem` will navigate to.

  ```xml
  uen:Region.Name="One"
  ```

  The full code for the `NavigationViewItem` elements should look like the code example below:

  ```xml
  <NavigationView.MenuItems>
    <NavigationViewItem Content="One"
                        uen:Region.Name="One" />
    ...
  </NavigationView.MenuItems>
  ```

* Add the `uen:Region.Navigator` attached property to the `Grid` element that contains the content area. This will set the type of navigation to adjust the visibility of the content area's children.

  ```xml
  uen:Region.Navigator="Visibility"
  ```

  > [!IMPORTANT]
  > Even if you're not utilizing the `<Grid>` element to arrange the content (ie, you're navigating between pages defined with routes), you must still include the `<Grid>` element with the `uen:Region.Navigator="Visibility"` and `uen:Region.Attached="True"` attached properties. These properties are essential for enabling the navigator functionality.

* Add the `uen:Region.Name` attached property to each of the `Grid` elements that contain the content area. This will define the name of the view that the `Grid` will represent.

  ```xml
  uen:Region.Name="One"
  ```

  The full code for the `Grid` elements should look like the code example below:

  ```xml
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
  ```

* Set the `Visibility` of the `Grid` elements to `Collapsed` to hide the content area's children beforehand:

  ```xml
  Visibility="Collapsed"
  ```

#### Navigating to Page elements

* You may want to navigate to a `Page` view element represented by a route name. It is possible to do this without defining the view element alongside the other content areas. For instance, you may need to display a products page `ProductsPage` which will be defined in a separate XAML file.

* Add a new **Page** item to your app called `ProductsPage` with the following code:

  ```xml
  <Page x:Class="UsingNavigationView.Views.ProductsPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:Uno.Extensions.Navigation.UI.Samples"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

      <Grid>
          <TextBlock Text="Products"
                     FontSize="24"
                     HorizontalAlignment="Center"
                     VerticalAlignment="Center" />
      </Grid>
  </Page>
  ```

* For the purposes for this tutorial, `ProductsPage` will be associated with its own view model `ProductsViewModel`. Add a new **Class** item to your app called `ProductsViewModel` with the following code:

  ```csharp
  namespace UsingNavigationView.ViewModels;

  public class ProductsViewModel
  {
      public ProductsViewModel()
      {

      }
  }
  ```

* Register `ViewMap` and `RouteMap` instances inside the `RegisterRoutes` method in `App.xaml.cs`. This associates the `ProductsPage` described above with `ProductsViewModel`, as well as avoiding the use of reflection for route discovery.

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

  > [!NOTE]  
  > To ensure selecting a `NavigationViewItem` only updates the content region (and doesn’t replace the entire page or the `NavigationView` itself), nest each `NavigationViewItem` as a `RouteMap` under the `Main` `RouteMap`. You do not need to declare **explicit** `RouteMap` entries for navigation to work, but if you do, they must be nested under Main.

* Importantly, the snippet above establishes a route name `Products` for `ProductsPage`. We can use this route name to navigate to the `ProductsPage` view element.

* Add a `NavigationViewItem` to the `NavigationView` element with the `uen:Region.Name` attached property set to `Products`.

  ```xml
  <NavigationView.MenuItems>
      <NavigationViewItem Content="One"
                          uen:Region.Name="One" />
      <NavigationViewItem Content="Two"
                          uen:Region.Name="Two" />
      <NavigationViewItem Content="Three"
                          uen:Region.Name="Three" />
      <!-- Adds a products item -->
      <NavigationViewItem Content="Products"
                          uen:Region.Name="Products" />
  </NavigationView.MenuItems>
  ```

#### Using the `Navigation.Data` attached property

Sometimes, it is necessary to send data to your ViewModel from the previous page. This can be done using the `Navigation.Data` attached property. For example, if you want to send an `Entity` object from the `MainViewModel` to the `ProductsViewModel`:

```diff
<!-- Adds a products item -->
<NavigationViewItem Content="Products"
+                   uen:Navigation.Data="{Binding Entity}"
                    uen:Region.Name="Products" />
```

For the full setup and more information on using the `Navigation.Data` attached property, refer to the documentation in the [How-To: Navigate in XAML](xref:Uno.Extensions.Navigation.HowToNavigateInXAML#2-navigationdata) guide.

> [!NOTE]  
> You also need to set up a `DataViewMap`. For more information on `ViewMap` and `DataViewMap`, refer to the **ViewMap** documentation in the [How-To: Define Routes](xref:Uno.Extensions.Navigation.HowToDefineRoutes#viewmap) guide.

### 6. Putting it all together

* Observe how the `NavigationView` and the content area are now connected. When you select a `NavigationViewItem`, the corresponding `Grid` or `Page` will be shown.

* If the route name specified represents a `Page` element, a `Frame` will be created upon navigation to host the `Page` element. This `Frame` will be added to the visual tree in order to support subsequent navigation to other `Page` elements.

* Because the navigation service maintains an instance of the view, users can leave this new `ProductsPage` to view item details and return to it _without_ losing any state such as a product filter they have already specified.

* Now, you have written a UI layout capable of navigating to views with `NavigationView`. Your completed `MainPage.xaml` should look like the code example below.

#### Code example

```xml
<Page x:Class="UsingNavigationView.Views.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      mc:Ignorable="d"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      Background="{ThemeResource MaterialBackgroundBrush}">

    <Grid uen:Region.Attached="True">

        <NavigationView uen:Region.Attached="true">
            <NavigationView.MenuItems>
                <NavigationViewItem Content="One"
                                    uen:Region.Name="One" />
                <NavigationViewItem Content="Two"
                                    uen:Region.Name="Two" />
                <NavigationViewItem Content="Three"
                                    uen:Region.Name="Three" />
                <NavigationViewItem Content="Products"
                                    uen:Navigation.Data="{Binding Entity}"
                                    uen:Region.Name="Products" />
            </NavigationView.MenuItems>

            <Grid uen:Region.Attached="True"
                  uen:Region.Navigator="Visibility">
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
        </NavigationView>
    </Grid>
</Page>
```
