---
name: uno-navigation-xaml
description: Implement declarative navigation in Uno Platform XAML using Navigation.Request and Navigation.Data attached properties. Use when adding navigation to buttons, list items, or other controls without code-behind. Covers route syntax, qualifiers in XAML, and data binding for navigation.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# XAML-Based Navigation

This skill covers declarative navigation using attached properties in Uno Platform XAML.

## XAML Namespace

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI">
```

## Navigation.Request Property

The `Navigation.Request` attached property enables navigation from any control:

### Basic Navigation

```xml
<Button Content="Go to Products" 
        uen:Navigation.Request="Products" />
```

### Back Navigation

Use `-` to navigate back:

```xml
<Button Content="Go Back" 
        uen:Navigation.Request="-" />
```

### Clear Back Stack

Use `-/` prefix to clear the back stack when navigating:

```xml
<Button Content="Go to Login" 
        uen:Navigation.Request="-/Login" />
```

### Remove Current Page

Use `-` prefix (without slash) to remove current page from back stack:

```xml
<Button Content="Continue" 
        uen:Navigation.Request="-NextPage" />
```

### Dialog Navigation

Use `!` prefix to open as dialog/flyout:

```xml
<Button Content="Show Options" 
        uen:Navigation.Request="!Options" />
```

### Nested Region Navigation

Use `./` prefix for nested region navigation:

```xml
<Button Content="Show Products" 
        uen:Navigation.Request="./Products" />

<!-- Navigate to specific named region -->
<Button Content="Show Details" 
        uen:Navigation.Request="./Details/ProductInfo" />
```

### Multi-Page Navigation

Navigate through multiple pages:

```xml
<Button Content="Go to Sample via Second" 
        uen:Navigation.Request="Second/Sample" />
```

## Navigation.Data Property

Pass data during navigation:

```xml
<Button Content="View Product" 
        uen:Navigation.Request="ProductDetail"
        uen:Navigation.Data="{Binding SelectedProduct}" />
```

### Two-Way Data Binding

For round-trip data (like filters):

```xml
<Button Content="Edit Filters" 
        uen:Navigation.Request="!Filter"
        uen:Navigation.Data="{Binding Filter.Value, Mode=TwoWay}" />
```

When the dialog closes, updated data flows back to the source.

## List Navigation

### ListView/ItemsRepeater Selection

Enable automatic navigation on item selection:

```xml
<ListView ItemsSource="{Binding Products}"
          uen:Navigation.Request="">
    <ListView.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Name}" />
                <TextBlock Text="{Binding Price}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

Empty `Navigation.Request=""` enables data-based routing from selection.

### ItemsRepeater with Navigation

```xml
<ItemsRepeater ItemsSource="{Binding Categories}"
               uen:Navigation.Request="CategoryDetails">
    <ItemsRepeater.ItemTemplate>
        <DataTemplate x:DataType="models:Category">
            <StackPanel Margin="0,0,0,4">
                <TextBlock Text="{Binding Name}" />
            </StackPanel>
        </DataTemplate>
    </ItemsRepeater.ItemTemplate>
</ItemsRepeater>
```

Clicked item is automatically passed as navigation data.

### Return Selected Item

Navigate back and return the selected item:

```xml
<ListView ItemsSource="{Binding Widgets}"
          uen:Navigation.Request="-"
          HorizontalAlignment="Center">
    <ListView.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Padding="10">
                <TextBlock Text="{Binding Name}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

## Route Qualifiers Summary

| Qualifier | Description | Example |
|-----------|-------------|---------|
| (none) | Forward navigation | `Products` |
| `-` | Navigate back | `-` |
| `-/` | Clear back stack | `-/Login` |
| `-Route` | Remove current, go forward | `-NextPage` |
| `!` | Open as dialog/flyout | `!Options` |
| `./` | Nested region navigation | `./Details` |
| `A/B` | Multi-page navigation | `Second/Sample` |

## TabBarItem Navigation

```xml
<utu:TabBar uen:Region.Attached="True">
    <utu:TabBarItem Content="Home" 
                    uen:Region.Name="Home" />
    <utu:TabBarItem Content="Search" 
                    uen:Region.Name="Search" />
    <utu:TabBarItem Content="Profile" 
                    uen:Region.Name="Profile"
                    uen:Navigation.Data="{Binding User}" />
</utu:TabBar>
```

## NavigationViewItem Navigation

```xml
<NavigationView uen:Region.Attached="True">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Products" 
                            uen:Region.Name="Products" />
        <NavigationViewItem Content="Orders" 
                            uen:Region.Name="Orders"
                            uen:Navigation.Data="{Binding CurrentOrder}" />
    </NavigationView.MenuItems>
</NavigationView>
```

## Best Practices

1. **Always use `uen:Navigation.Request`** for declarative navigation (never use code-behind for simple navigation)

2. **Use `./` prefix** for navigating within regions on the same page

3. **Use empty string `""`** with lists to enable automatic data-based routing

4. **Combine `Navigation.Request` with `Navigation.Data`** to pass context

5. **Use `-/` to prevent back navigation** to pages like login or onboarding

6. **Use `!` for temporary UI** like filters, pickers, or confirmations

## Common Patterns

### Confirmation Before Action

```xml
<Button Content="Delete" 
        uen:Navigation.Request="!ConfirmDelete"
        uen:Navigation.Data="{Binding SelectedItem}" />
```

### Filter Dialog

```xml
<Button Content="Filters" 
        uen:Navigation.Request="!Filters"
        uen:Navigation.Data="{Binding CurrentFilter, Mode=TwoWay}" />
```

### Master-Detail Navigation

```xml
<ItemsRepeater ItemsSource="{Binding Items}"
               uen:Navigation.Request="./ItemDetail">
    <!-- Item template -->
</ItemsRepeater>

<ContentControl Grid.Column="1"
                uen:Region.Attached="True"
                uen:Region.Name="ItemDetail" />
```
