---
uid: Uno.Extensions.Navigation.Advanced.ResponsiveShell
---
# How-To: Build a Responsive Layout using NavigationView and TabBar

Apps that scale across multiple devices and form factors need to be able to adapt their layout to the available screen real estate. When your app is running on narrow devices, such as phones, you may want to hide the navigation pane and show a hamburger menu button instead.

It makes sense to allow `TabBar` to be the dominant navigation surface on these devices. On larger devices, such as tablets and desktops, you may want to show the navigation pane and hide the hamburger menu button. Using the `TabBar` would be a poor choice for navigation on these devices. The platform includes a `VisualStateManger` that allows you to define different visual states for different screen sizes in XAML, showing the navigation pane and hamburger menu button as appropriate.

This tutorial will show you how to build a responsive layout with multiple navigation controls such as `NavigationView` and the [Uno Toolkit](https://github.com/unoplatform/uno.toolkit.ui) `TabBar` which use the _same_ navigation service behind the scenes.

## Step-by-step

[!include[create-application](../../includes/create-application.md)]

### 1. Add necessary XAML namespaces

* Update the `Page` element in `MainPage.xaml` to include XAML namespace mappings for Navigation, WinUI, and Uno Toolkit:

    ```xml
    <Page x:Class="ResponsiveShell.Views.MainPage"
          xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
          xmlns:uen="using:Uno.Extensions.Navigation.UI"
          xmlns:utu="using:Uno.Toolkit.UI"
    ...
    ```

### 2. Define the view's layout

* Add necessary `RowDefinitions` to the root `Grid` in `MainPage.xaml`:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
    </Grid>
    ```

* In the root `Grid`, add a `NavigationView` and a few simple `MenuItems`:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <muxc:NavigationView Grid.Row="1"
                             x:Name="NavView">
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem Content="One" />
                <muxc:NavigationViewItem Content="Two" />
                <muxc:NavigationViewItem Content="Three" />
            </muxc:NavigationView.MenuItems>
        </muxc:NavigationView>
    </Grid>
    ```

* Add multiple sectors of distinct content to the `NavigationView` content area:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <muxc:NavigationView Grid.Row="1"
                             x:Name="NavView">
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem Content="One" />
                <muxc:NavigationViewItem Content="Two" />
                <muxc:NavigationViewItem Content="Three" />
            </muxc:NavigationView.MenuItems>
            <muxc:NavigationView.Content>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>
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
                </Grid>
            </muxc:NavigationView.Content>
        </muxc:NavigationView>
    </Grid>
    ```

    It's important to make each element that represents a sector of app content have it's `Visibility` explicitly set to `Collapsed`. Uno.Extensions will handle toggling it back to `Visible` when necessary.

**Built for complex layout scenarios:**
While the WinUI `NavigationView` control by itself is a good choice for a responsive shell layout because of its adaptability to different screen sizes and breakpoints, this guide will demonstrate Uno.Extensions navigation features using the `TabBar` control together with it.

### 3. Complementing the NavigationView with a TabBar

* Add a `TabBar` to the `NavigationView` content area:

    ```xml
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <muxc:NavigationView Grid.Row="1"
                             x:Name="NavView">
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem Content="One" />
                <muxc:NavigationViewItem Content="Two" />
                <muxc:NavigationViewItem Content="Three" />
            </muxc:NavigationView.MenuItems>
            <muxc:NavigationView.Content>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>
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
                    <utu:TabBar Grid.Row="1"
                                x:Name="Tabs"
                                VerticalAlignment="Bottom">
                        <utu:TabBar.Resources>
                            <x:String x:Key="Icon_bolt">...</x:String>
                            <x:String x:Key="Icon_person">...</x:String>
                            <x:String x:Key="Icon_storefront">...<x:String>
                        </utu:TabBar.Resources>
                        <utu:TabBar.Items>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_storefront}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_bolt}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_storefront}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                        </utu:TabBar.Items>
                    </utu:TabBar>
                </Grid>
            </muxc:NavigationView.Content>
        </muxc:NavigationView>
    </Grid>
    ```

### 4. Set up regions and specify navigator type

* Use the `Region.Attached="True"` attached property to enable regions on the parent element for each of the following:
  * **Participating controls:** The root grid of the `Page`
  * **Selectable items:** Both the `TabBar` and `NavigationView` controls contain selectable items
  * **Sectors of content:** Collapsed `Grid` elements containing item content

    ```xml
    <Grid Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <muxc:NavigationView Grid.Row="1"
                             Region.Attached="True"
                             x:Name="NavView">
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem Content="One" />
                <muxc:NavigationViewItem Content="Two" />
                <muxc:NavigationViewItem Content="Three" />
            </muxc:NavigationView.MenuItems>
            <muxc:NavigationView.Content>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>
                    <Grid Region.Attached="True">
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
                    <utu:TabBar Grid.Row="1"
                                x:Name="Tabs"
                                Region.Attached="True"
                                VerticalAlignment="Bottom">
                        <utu:TabBar.Resources>
                            <x:String x:Key="Icon_bolt">...</x:String>
                            <x:String x:Key="Icon_person">...</x:String>
                            <x:String x:Key="Icon_storefront">...<x:String>
                        </utu:TabBar.Resources>
                        <utu:TabBar.Items>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_storefront}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_bolt}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_storefront}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                        </utu:TabBar.Items>
                    </utu:TabBar>
                </Grid>
            </muxc:NavigationView.Content>
        </muxc:NavigationView>
    </Grid>
    ```

* Name both the content itself and associated navigation control items using the `Region.Name` attached property:

    ```xml
    <Grid Region.Attached="True">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition />
        </Grid.RowDefinitions>
        <utu:NavigationBar Content="Main Page"
                           Style="{StaticResource MaterialNavigationBarStyle}" />
        <muxc:NavigationView Grid.Row="1"
                             Region.Attached="True"
                             x:Name="NavView">
            <muxc:NavigationView.MenuItems>
                <muxc:NavigationViewItem Content="One"
                                         uen:Region.Name="One" />
                <muxc:NavigationViewItem Content="Two"
                                         uen:Region.Name="Two" />
                <muxc:NavigationViewItem Content="Three"
                                         uen:Region.Name="Three" />
            </muxc:NavigationView.MenuItems>
            <muxc:NavigationView.Content>
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>
                    <Grid Region.Attached="True">
                        <Grid Visibility="Collapsed"
                              uen:Region.Name="One">
                            <TextBlock Text="One"
                                       FontSize="24"
                                       HorizontalAlignment="Center"
                                       VerticalAlignment="Center" />
                        </Grid>
                        <Grid Visibility="Collapsed"
                              uen:Region.Name="Two">
                            <TextBlock Text="Two"
                                       FontSize="24"
                                       HorizontalAlignment="Center"
                                       VerticalAlignment="Center" />
                        </Grid>
                        <Grid Visibility="Collapsed"
                              uen:Region.Name="Three">
                            <TextBlock Text="Three"
                                       FontSize="24"
                                       HorizontalAlignment="Center"
                                       VerticalAlignment="Center" />
                        </Grid>
                    </Grid>
                    <utu:TabBar Grid.Row="1"
                                x:Name="Tabs"
                                Region.Attached="True"
                                VerticalAlignment="Bottom">
                        <utu:TabBar.Resources>
                            <x:String x:Key="Icon_bolt">...</x:String>
                            <x:String x:Key="Icon_person">...</x:String>
                            <x:String x:Key="Icon_storefront">...<x:String>
                        </utu:TabBar.Resources>
                        <utu:TabBar.Items>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}"
                                            uen:Region.Name="One">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_storefront}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}"
                                            uen:Region.Name="Two">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_bolt}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                            <utu:TabBarItem Style="{StaticResource MaterialBottomTabBarItemStyle}"
                                            uen:Region.Name="Three">
                                <utu:TabBarItem.Icon>
                                    <PathIcon Data="{StaticResource Icon_storefront}" />
                                </utu:TabBarItem.Icon>
                            </utu:TabBarItem>
                        </utu:TabBar.Items>
                    </utu:TabBar>
                </Grid>
            </muxc:NavigationView.Content>
        </muxc:NavigationView>
    </Grid>
    ```

* Specify the navigator type as `Visibility` using the `Region.Navigator` attached property on the parent element of your collapsed content `Grid` definitions:

    ```xml
    <Grid uen:Region.Attached="True"
          uen:Region.Navigator="Visibility">
    ...
    ```

### 5. Toggle visibility for responsive design

* Finally, specify groups for the `VisualStateManager` to adjust the page layout based on how close the window size is to a couple of defined breakpoints:

    ```xml
    <Page x:Class="ResponsiveShell.Views.MainPage">
        <Grid uen:Region.Attached="True">
            <VisualStateManager.VisualStateGroups>
                <VisualStateGroup>
                    <VisualState x:Name="Narrow">
                        <VisualState.Setters>
                            <Setter Target="Tabs.Visibility"
                                    Value="Visible" />
                            <Setter Target="NavView.IsPaneToggleButtonVisible"
                                    Value="false" />
                            <Setter Target="NavView.PaneDisplayMode"
                                    Value="LeftMinimal" />
                            <Setter Target="NavView.IsPaneOpen"
                                    Value="False" />
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="Normal">
                        <VisualState.StateTriggers>
                            <AdaptiveTrigger MinWindowWidth="700" />
                        </VisualState.StateTriggers>
                        <VisualState.Setters>
                            <Setter Target="Tabs.Visibility"
                                    Value="Collapsed" />
                            <Setter Target="NavView.IsPaneToggleButtonVisible"
                                    Value="True" />
                            <Setter Target="NavView.IsPaneVisible"
                                    Value="true" />
                            <Setter Target="NavView.PaneDisplayMode"
                                    Value="Auto" />
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="Wide">
                        <VisualState.StateTriggers>
                            <AdaptiveTrigger MinWindowWidth="1000" />
                        </VisualState.StateTriggers>
                        <VisualState.Setters>
                            <Setter Target="Tabs.Visibility"
                                    Value="Collapsed" />
                            <Setter Target="NavView.IsPaneToggleButtonVisible"
                                    Value="True" />
                            <Setter Target="NavView.IsPaneVisible"
                                    Value="true" />
                            <Setter Target="NavView.PaneDisplayMode"
                                    Value="Auto" />
                        </VisualState.Setters>
                    </VisualState>
                </VisualStateGroup>
            </VisualStateManager.VisualStateGroups>
    ...
    ```

* When a `TabBarItem` or `NavigationViewItem` is selected, the associated content region will now have its `Visibility` toggled to `Visible`
