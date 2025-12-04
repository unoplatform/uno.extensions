---
uid: Uno.Extensions.Navigation.Walkthrough.NavigateXAML
title: Navigate Between Pages using Navigation Attached Properties in XAML
tags: [uno, uno-platform, uno-extensions, navigation, XAML, Navigation.Request, Navigation.Data, attached-properties, declarative-navigation, no-code-behind, data-binding, ListView, Button, event-binding, auto-binding, DataViewMap, ViewMap, RouteMap, element-binding, selection-navigation, back-navigation, route-based-navigation]
---

# Navigate Between Pages using Navigation Attached Properties in XAML

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

* Import the Navigation namespace in XAML:

    ```xml
    <Page xmlns:uen="using:Uno.Extensions.Navigation.UI">
    ```

## Navigate to another page via XAML

Use `Navigation.Request` attached property to trigger navigation declaratively.

```csharp
public class SampleViewModel
{
    public SampleViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    private readonly INavigator _navigator;
}
```

```xml
<Button Content="Go to SamplePage"
        uen:Navigation.Request="Sample" />
```

* **Navigation.Request** — Attached property that specifies the route to navigate to
* Route string `"Sample"` maps to the registered route for `SamplePage`
* No Click event handler needed in code-behind

### Perform a backward navigation via XAML

```xml
<Button Content="Go Back"
        uen:Navigation.Request="-" />
```

## Navigate from ListView item selection

Use `Navigation.Request` on ListView to navigate when an item is selected.

```xml
<ListView ItemsSource="{Binding Cities}"
          uen:Navigation.Request="CityDetails">
    <ListView.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

Selected item is automatically passed as navigation data.

## Navigate from ItemsRepeater item click

Use `Navigation.Request` directly on ItemsRepeater to navigate when an item is clicked.

```xml
<ItemsRepeater ItemsSource="{Binding Cities}"
               uen:Navigation.Request="CityDetails">
    <ItemsRepeater.ItemTemplate>
        <DataTemplate x:DataType="models:City">
            <StackPanel Margin="0,0,0,8">
                <TextBlock Text="{x:Bind Name}" />
                <TextBlock Text="{x:Bind Population}" />
            </StackPanel>
        </DataTemplate>
    </ItemsRepeater.ItemTemplate>
</ItemsRepeater>
```

Clicked item is automatically passed as navigation data.

## Navigate from GridView item click

```xml
<GridView ItemsSource="{Binding Products}"
          uen:Navigation.Request="ProductDetails">
    <GridView.ItemTemplate>
        <DataTemplate>
            <Grid Width="150" Height="150">
                <Image Source="{Binding ImageUrl}" />
                <TextBlock Text="{Binding Name}" 
                           VerticalAlignment="Bottom" />
            </Grid>
        </DataTemplate>
    </GridView.ItemTemplate>
</GridView>
```

## Pass data between pages

Pass data with navigation requests using `Navigation.Data` attached property.

* Define a data type:

    ```csharp
    public record Widget(string Name, double Weight);
    ```

* Add data source to `MainViewModel`:

    ```csharp
    public Widget[] Widgets { get; } =
    [
        new Widget("NormalSpinner", 5.0),
        new Widget("HeavySpinner", 50.0)
    ];
    ```

* Create a ListView in `MainPage.xaml`:

    ```xml
    <ListView ItemsSource="{Binding Widgets}"
              x:Name="WidgetsList"
              SelectionMode="Single">
        <ListView.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal" Padding="10">
                    <TextBlock Text="{Binding Name}" />
                    <TextBlock Text="{Binding Weight}" />
                </StackPanel>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
    ```

    ```xml
    <Button Content="Go to Sample Page"
            uen:Navigation.Request="Sample"
            uen:Navigation.Data="{Binding SelectedItem, ElementName=WidgetsList}"/>
    ```

* **Navigation.Data** — Data-binds to the selected item and passes it with the navigation request

* The data is injected into the ViewModel constructor:

    ```csharp
    public class SampleViewModel
    {
        public string Title => "Sample Page";
        public string Name { get; }

        public SampleViewModel(Widget widget)
        {
            Name = widget.Name;
        }
    }
    ```

* Display the data in `SamplePage.xaml`:

    ```xml
    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center">
        <Run Text="Widget Name:" />
        <Run Text="{Binding Name}" />
    </TextBlock>
    ```

* Register the data mapping with `DataViewMap`:

    ```csharp
    new DataViewMap<SamplePage, SampleViewModel, Widget>()
    ```
