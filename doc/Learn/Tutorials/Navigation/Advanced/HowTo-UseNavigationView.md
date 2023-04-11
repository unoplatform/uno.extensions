---
uid: Learn.Tutorials.Navigation.Advanced.NavigationView
---
# How-To: Use a NavigationView to Switch Views

Choosing the right control for your navigation needs is important, and one common choice is the WinUI `NavigationView`. This control adapts to different screen sizes and offers a uniform top-level navigation experience. `NavigationView` is a great option for adaptive, customizable, and mobile-friendly navigation. The Uno Platform extensions for navigation provide built-in support for using `NavigationView` and `NavigationViewItem` to switch between views. This tutorial will show you how to configure a `NavigationView` for use with the navigation extensions.

## Step-by-steps

### 1. Add XAML namespace mappings

* Add the following namespace mappings to the root element of your XAML page:

  ```xml
  xmlns:uen="using:Uno.Extensions.Navigation.UI"
  xmlns:utu="using:Uno.Toolkit.UI"
  ```

* Make sure that the project you're building depends on Uno.WinUI. Only if you're building a UWP app (Uno.UI), you will need to take an additional step. Add the following namespace mapping and prefix every reference to `NavigationView` or `NavigationViewItem` with _muxc_ for this tutorial:

  ```xml
  xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
  ```

### 2. Define the view's layout

* Add a `Grid` element to the root of your XAML page. This will be the container for the `NavigationView` and the content area.

  ```xml
  <Grid>
      ...
  </Grid>
  ```

* Add a `RowDefinition` to the `Grid`'s `RowDefinitions` collection. This will define the height of the `NavigationView`'s menu.

  ```xml
  <Grid.RowDefinitions>
      <RowDefinition Height="Auto" />
      <RowDefinition />
  </Grid.RowDefinitions>
  ```

* Add the `NavigationBar` control to the `Grid`. This will be the header for the app name.

  ```xml
  <utu:NavigationBar Content="Main Page"
                    Style="{StaticResource MaterialNavigationBarStyle}" />
  ```

* Add a `NavigationView` control to the `Grid`. This will be the menu for the app.

  ```xml
  <NavigationView Grid.Row="1">
      ...
  </NavigationView>
  ```

* Add a `Grid` element to the `NavigationView`. This will be the container for the content area.

  ```xml
  <Grid Grid.Row="1">
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
  <Grid Grid.Row="1">
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

    * The `Grid` element that contains the `NavigationView` and the `Grid` element that contains the content area.
    * The `NavigationView` element.
    * The `Grid` element that contains the content area.

  ```xml
  uen:Region.Attached="True"
  ```

* Setting this to `True` will enable the navigation extensions for the element.

* Add the `uen:Region.Name` attached property to each of the `NavigationViewItem` elements. This will define the name of the view that the `NavigationViewItem` will navigate to.

  ```xml
  uen:Region.Name="One|Two|Three"
  ```

* Add the `uen:Region.Navigator` attached property to the `Grid` element that contains the content area. This will set the type of navigation to adjust the visibility of the content area's children.

  ```xml
  uen:Region.Navigator="Visibility"
  ```

* Add the `uen:Region.Name` attached property to each of the `Grid` elements that contain the content area. This will define the name of the view that the `Grid` will represent.

  ```xml
  uen:Region.Name="One|Two|Three"
  ```

* Observe how the `NavigationView` and the content area are now connected. When you select a `NavigationViewItem`, the corresponding `Grid` will be shown.

* Set the `Visibility` of the `Grid` elements to `Collapsed` to hide the content area's children beforehand:

  ```xml
  Visibility="Collapsed"
  ```

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
      xmlns:utu="using:Uno.Toolkit.UI"
      Background="{ThemeResource MaterialBackgroundBrush}">

    <Grid uen:Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <NavigationView uen:Region.Attached="true"
                        Grid.Row="1">
            <NavigationView.MenuItems>
                <NavigationViewItem Content="One"
                                    uen:Region.Name="One" />
                <NavigationViewItem Content="Two"
                                    uen:Region.Name="Two" />
                <NavigationViewItem Content="Three"
                                    uen:Region.Name="Three" />
            </NavigationView.MenuItems>
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
        </NavigationView>
    </Grid>
</Page>
```

### 6. Switching between views

* To switch between views, you can now use the `NavigationView`. When you select a `NavigationViewItem`, the corresponding `Grid` will be shown. The visibility of the corresponding views will be adjusted automatically.