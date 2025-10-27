---
uid: Uno.Extensions.Navigation.Walkthrough.NavigateViaXAML
title: Navigate via XAML
summary: How to control navigation directly from XAML using Navigation.Request and Navigation.Data attached properties.
tags: [uno, navigation, XAML, Navigation.Request, Navigation.Data, attached-properties, data-binding, ListView]
---

# Navigate via XAML

## Purpose
Demonstrates declarative navigation from XAML using attached properties, including passing data with navigation requests.

## Prerequisites

* Add `Navigation` support in your app's .csproj file:

    ```diff
    <UnoFeatures>
        Material;
        Extensions;
    +   Navigation;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

* Import the Navigation namespace in your XAML:

    ```xml
    <Page xmlns:uen="using:Uno.Extensions.Navigation.UI">
    ```

## Navigation.Request Attached Property

Use `Navigation.Request` to trigger navigation declaratively from any XAML element.

* Create a destination page `SamplePage.xaml` and ViewModel:

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

* Add a Button with `Navigation.Request` in `MainPage.xaml`:

    ```xml
    <Button Content="Go to SamplePage"
            uen:Navigation.Request="Sample" />
    ```

* The route string `"Sample"` maps to the registered route for `SamplePage`.

### Auto-Attached Events

`Navigation.Request` automatically attaches to appropriate events based on control type:

* **Button** — `Click` event
* **ListView** — `SelectionChanged` event
* **Border/Image/TextBlock** — `Tapped` event

### Navigate Back

* Use `"-"` as the route to navigate back:

    ```xml
    <Button Content="Go Back"
            uen:Navigation.Request="-" />
    ```

## Register Routes

Define `ViewMap` and `RouteMap` to avoid reflection:

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>(),
        new DataViewMap<SecondPage, SecondViewModel, Entity>(),
        new ViewMap<SamplePage, SampleViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new("Main", View: views.FindByViewModel<MainViewModel>()),
                new("Second", View: views.FindByViewModel<SecondViewModel>()),
                new("Sample", View: views.FindByViewModel<SampleViewModel>()),
            ]
        )
    );
}
```

## Navigation.Data Attached Property

Pass data with navigation requests using `Navigation.Data`.

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

* Add a Button with both `Navigation.Request` and `Navigation.Data`:

    ```xml
    <Button Content="Go to Sample Page"
            uen:Navigation.Request="Sample"
            uen:Navigation.Data="{Binding SelectedItem, ElementName=WidgetsList}"/>
    ```

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

## Navigate on ListView Selection

Trigger navigation automatically when selecting a ListView item.

* Add `Navigation.Request` directly to the ListView:

    ```xml
    <ListView ItemsSource="{Binding Widgets}"
              uen:Navigation.Request="Sample">
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

* `Navigation.Data` is not needed — the selected item is automatically attached to the request.
* `SelectionMode` can be removed as selection is no longer manually tracked.

## Key Concepts

* **Navigation.Request** — Attached property specifying the navigation route
* **Navigation.Data** — Attached property passing data with the navigation request
* **Event Auto-Binding** — Different controls use appropriate events automatically
* **Constructor Injection** — Navigation data is injected into destination ViewModel
* **DataViewMap** — Registers the data type for injection mapping

## Best Practices

* Always import `xmlns:uen="using:Uno.Extensions.Navigation.UI"` in XAML
* Use `DataViewMap<TView, TViewModel, TData>` when passing data
* Define explicit ViewMap and RouteMap to avoid reflection
* Use `"-"` route for back navigation
* Let ListView handle selection automatically when navigating on SelectionChanged
