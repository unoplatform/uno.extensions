---
uid: Learn.Tutorials.Navigation.Advanced.Panel
---

# How-To: Use a Panel to Switch Views

Sometimes your application may need to switch between multiple views without the overhead of controls like `Frame` which support a navigation stack. In this case, it makes sense to define sectors of potential view content as **regions** and use another control to toggle the `Visibility` of the multiple views directly. This tutorial will show you how to use a `Panel` to switch between views.

## Step-by-steps

### 1. Add necessary XAML namespaces
* We will be using the `NavigationBar` control from the Uno.Toolkit

* Update the `Page` element in `MainPage.xaml` to include XAML namespace mappings for Navigation and Uno Toolkit:

  ```xml
  xmlns:uen="using:Uno.Extensions.Navigation.UI"
  xmlns:utu="using:Uno.Toolkit.UI"
  ```

* Your `Page` element should now look like this:

  ```xml
  <Page x:Class="UsingPanelRegion.Views.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:UsingPanelRegion.Views"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        xmlns:uen="using:Uno.Extensions.Navigation.UI"
        xmlns:utu="using:Uno.Toolkit.UI"
  ...
  ```

### 2. Define the view's layout

* Add row definitions to the root `Grid` in `MainPage.xaml`:

  ```xml
  <Grid.RowDefinitions>
      <RowDefinition Height="Auto" />
      <RowDefinition Height="Auto" />
      <RowDefinition />
  </Grid.RowDefinitions>
  ```

* Add a `NavigationBar` control to the first row of the root `Grid`:

  ```xml
  <utu:NavigationBar Content="Main Page" 
                    Style="{StaticResource MaterialNavigationBarStyle}"/>
  ```

* Define the control responsible for switching between views. In this case, we will place multiple `Button` controls in a `StackPanel` to represent the possible views:

  ```xml
  <StackPanel Grid.Row="1" 
              HorizontalAlignment="Center" 
              Orientation="Horizontal">
      <Button Content="One" />
      <Button Content="Two" />
      <Button Content="Three" />
  </StackPanel>
  ```

* Add a `Grid` containing several child panels and their content to the third row of the root `Grid`. These will represent the view content, and not all of them will be visible:

  ```xml
  <Grid Grid.Row="2">
      <Grid>
          <TextBlock Text="One" 
                    FontSize="24" 
                    HorizontalAlignment="Center" 
                    VerticalAlignment="Center"/>
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

* Your `Page` element should now look like this:

  ```xml
  <Page x:Class="UsingPanelRegion.Views.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:UsingPanelRegion.Views"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        xmlns:uen="using:Uno.Extensions.Navigation.UI"
        xmlns:utu="using:Uno.Toolkit.UI"
        Background="{ThemeResource MaterialBackgroundBrush}">

      <Grid>
          <Grid.RowDefinitions>
              <RowDefinition Height="Auto" />
              <RowDefinition Height="Auto" />
              <RowDefinition />
          </Grid.RowDefinitions>

          <utu:NavigationBar Content="Main Page" 
                            Style="{StaticResource MaterialNavigationBarStyle}"/>

          <StackPanel Grid.Row="1" 
                      HorizontalAlignment="Center" 
                      Orientation="Horizontal">
              <Button Content="One" />
              <Button Content="Two" />
              <Button Content="Three" />
          </StackPanel>

          <Grid Grid.Row="2">
              <Grid>
                  <TextBlock Text="One" 
                            FontSize="24" 
                            HorizontalAlignment="Center" 
                            VerticalAlignment="Center"/>
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
      </Grid>
  </Page>
  ```

### 3. Set up navigation

* Set the `Navigation.Request` attached property on each `Button` control to what will be the route to a corresponding view:

  ```xml
  <StackPanel Grid.Row="1" 
              HorizontalAlignment="Center" 
              Orientation="Horizontal">
      <Button Content="One"
              uen:Navigation.Request="./One" />
      <Button Content="Two"
              uen:Navigation.Request="./Two" />
      <Button Content="Three"
              uen:Navigation.Request="./Three" />
  </StackPanel>
  ```

* This will automatically set up navigation to the corresponding view when the `Button.Click` event is raised.

* Next, we need to attach content regions for each view. The first step is to set the `Region.Attached` attached property to `True` on the parent `Grid` containing the views:
  
  ```xml
  <Grid Grid.Row="2" 
        uen:Region.Attached="True">
      <Grid>
          <TextBlock Text="One" 
                    FontSize="24" 
                    HorizontalAlignment="Center" 
                    VerticalAlignment="Center"/>
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

* Since we want to toggle visibility of the views, we need to set the `Region.Navigator` attached property to `Visibility`:

  ```xml
  <Grid Grid.Row="2" 
        uen:Region.Attached="True"
        uen:Region.Navigator="Visibility">
      <Grid>
          <TextBlock Text="One" 
                    FontSize="24" 
                    HorizontalAlignment="Center" 
                    VerticalAlignment="Center"/>
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

* Finally, we need to set the `Region.Name` attached property on each child `Grid` to the corresponding view name:

  ```xml
  <Grid Grid.Row="2" 
        uen:Region.Attached="True"
        uen:Region.Navigator="Visibility">
      <Grid uen:Region.Name="One">
          <TextBlock Text="One" 
                    FontSize="24" 
                    HorizontalAlignment="Center" 
                    VerticalAlignment="Center"/>
      </Grid>
      <Grid uen:Region.Name="Two">
          <TextBlock Text="Two"
                    FontSize="24"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center" />
      </Grid>
      <Grid uen:Region.Name="Three">
          <TextBlock Text="Three"
                    FontSize="24"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center" />
      </Grid>
  </Grid>
  ```

* Now, you have written a UI layout capable of navigating to views with `Panel`. Your completed `MainPage.xaml` should look like the code example below.

#### Code example

```xml
<Page x:Class="UsingPanelRegion.Views.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:UsingPanelRegion.Views"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      mc:Ignorable="d"
      xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI"
      Background="{ThemeResource MaterialBackgroundBrush}">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>

        <utu:NavigationBar Content="Main Page" 
                           Style="{StaticResource MaterialNavigationBarStyle}"/>

        <StackPanel Grid.Row="1" 
                    HorizontalAlignment="Center" 
                    Orientation="Horizontal">
            <Button Content="One"
                    uen:Navigation.Request="./One" />
            <Button Content="Two"
                    uen:Navigation.Request="./Two" />
            <Button Content="Three"
                    uen:Navigation.Request="./Three" />
        </StackPanel>

        <Grid uen:Region.Attached="True" 
              uen:Region.Navigator="Visibility" 
              Grid.Row="2">
            <Grid uen:Region.Name="One"
                  Visibility="Collapsed">
                <TextBlock Text="One" 
                           FontSize="24" 
                           HorizontalAlignment="Center" 
                           VerticalAlignment="Center"/>
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
    </Grid>
</Page>
```

### 4. Switching between views

* Now, you can switch between views by clicking on the `Button` controls - no `Frame` required! The `Panel` region will automatically toggle the visibility of the corresponding views.