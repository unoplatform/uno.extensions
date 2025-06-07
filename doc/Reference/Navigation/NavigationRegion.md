---
uid: Reference.Navigation.Regions
---

# What is a Navigation Region

## Regions

A region is a part of the user interface that manages navigation.

Regions are organized in a hierarchy that mirrors the structure of the navigation controls in the user interface. This hierarchy allows navigation commands to move up to parent regions or down to child regions as needed.

To specify a region, you set `Region.Attached="true"` on a navigation control (like Frame, ContentControl, or Grid).

```xml
<ContentControl uen:Region.Attached="true" />
```

## Region Name

You can name a region by setting the `Region.Name="RegionName"` property.

In selection-based regions, the selectable items (like `NavigationViewItem`, `TabBarItem`, etc.) are identified using the Region.Name property.

```xml
<muxc:NavigationView uen:Region.Attached="true">
    <muxc:NavigationView.MenuItems>
        <muxc:NavigationViewItem Content="Products" uen:Region.Name="Products" />
        <muxc:NavigationViewItem Content="Deals" uen:Region.Name="Deals" />
        <muxc:NavigationViewItem Content="Profile" uen:Region.Name="Profile" />
    </muxc:NavigationView.MenuItems>
</muxc:NavigationView>
```

`SettingsItem` in `NavigationView` is generated automatically by the control, so it is not possible to set its region name in XAML. Instead, you can do so in code behind on `Loaded`:

```xml
public MainPage()
{
    this.InitializeComponent();
    this.Loaded += MainPage_Loaded;
}

private void MainPage_Loaded(object sender, RoutedEventArgs e)
{
    var item = (NavigationViewItem)MyNavigationView.SettingsItem;
    Region.SetName(item, "MyRegionName");
}
```

Switching selected item:

  ```csharp
  navigator.NavigateRouteAsync(this, "Deals");
  ```

  or

  ```csharp
  navigator.NavigateViewAsync<DealsControl>(this);
  ```

  or

  ```csharp
  navigator.NavigateViewModelAsync<DealsViewModel>(this);
  ```

  or

  ```csharp
  navigator.NavigateDataAsync(this, selectedDeal);
  ```
