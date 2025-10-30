---
uid: Uno.Extensions.Navigation.Walkthrough.NavigateXAML
title: Navigate Between Pages using Navigation Attached Properties in XAML
tags: [uno, uno-platform, uno-extensions, navigation, XAML, Navigation.Request, Navigation.Data, attached-properties, declarative-navigation, no-code-behind, data-binding, ListView, Button, event-binding, auto-binding, DataViewMap, ViewMap, RouteMap, element-binding, selection-navigation, back-navigation, route-based-navigation]
---

# Navigate Between Pages using Navigation Attached Properties in XAML

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
