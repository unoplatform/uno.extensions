---
uid: Uno.Extensions.Navigation.HowToNavigateInXAML
---
# How-To: Navigate in XAML

This topic walks through controlling Navigation from XAML. This includes specifying data that should be attached to the navigation request.

[!include[getting-help](../includes/mvvm-approach.md)]

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Navigation.Request

Navigation can be defined in XAML by placing the `Navigation.Request` attached property on a specific XAML element. The string value specified in the `Navigation.Request` is the route to be navigated to.
Depending on the type of the XAML element, the `Navigation.Request` property will attach to an appropriate event in order to trigger navigation. For example, on a `Button`, the `Click` event will be used to trigger navigation, whereas the `SelectionChanged` event on a `ListView` is used. If you place a `Navigation.Request` property on a static element, such as a `Border`, `Image`, or `TextBlock`, the `Tapped` event will be used to trigger navigation.

- Add a new `Page` to navigate to, `SamplePage.xaml`

- Add a new class, `SampleViewModel`, to the class library project

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

- In `MainPage.xaml` update the `Button` to use the `Navigation.Request` attached property instead of the `Click` event handler.

    ```xml
    <Button Content="Go to SamplePage"
            uen:Navigation.Request="Sample" />
    ```

    > [!TIP]
    > As Navigation.Request attached property exists in the `Uno.Extensions.Navigation.UI` namespace you will need to import this namespace on the `Page` element with

    ```csharp
    <Page x:Class="NavigateInXAML.Views.SamplePage"
        ...
        xmlns:uen="using:Uno.Extensions.Navigation.UI">
    ```

- In `SamplePage.xaml` add a `Button`, again with the `Navigation.Request` attached property. The "-" navigation route is used to navigate back.

    ```xml
    <Button Content="Go Back"
            uen:Navigation.Request="-" />
    ```

    > [!TIP]
    > While this works, it relies on reflection to convert the request path "Sample" to the corresponding view, i.e. `SamplePage`. It's better to define `ViewMap` and `RouteMap`

- Add a `ViewMap` and a `RouteMap` for the `SamplePage` into the `RegisterRoutes` method in the `App.xaml.cs` file

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
                    new ("Main", View: views.FindByViewModel<MainViewModel>()),
                    new ("Second", View: views.FindByViewModel<SecondViewModel>()),
                    new ("Sample", View: views.FindByViewModel<SampleViewModel>()),
                ]
            )
        );
    }
    ```

### 2. Navigation.Data

In addition to specifying the route to navigate to, the Navigation.Data attached property can be used to define the data to be attached to the navigation request. The data can be accessed by the view model associated with the route using constructor injection.

- Define a record (or class), `Widget`, that is the type of data that will be attached to the navigation request.

    ```csharp
    public record Widget(string Name, double Weight);
    ```

- Add a property, `Widgets`, to `MainViewModel` that returns an array of predefined `Widget` instances.

    ```csharp
    public Widget[] Widgets { get; } =
    [
        new Widget("NormalSpinner", 5.0),
        new Widget("HeavySpinner", 50.0)
    ];
    ```

- Replace the `Button` with a `ListView` in `MainPage.xaml` that has the `ItemsSource` property data bound to the `Widgets` property.

    ```xml
    <StackPanel Grid.Row="1"
                HorizontalAlignment="Center"
                VerticalAlignment="Center">
        <ListView ItemsSource="{Binding Widgets}"
                  x:Name="WidgetsList"
                  SelectionMode="Single">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal"
                                Padding="10">
                        <TextBlock Text="{Binding Name}" />
                        <TextBlock Text="{Binding Age}" />
                    </StackPanel>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </StackPanel>
    ```

- Add a `Button` to the `StackPanel` on  `MainPage.xaml` that has both `Navigation.Request`, that specified the navigation route, and the `Navigation.Data` properties. In this case the `Navigation.Data` attached property is data bound to the `SelectedItem` property on the named element `WidgetsList` (which matches the `x:Name` set on the previously added `ListView`)

    ```xml
    <Button Content="Go to Sample Page"
            uen:Navigation.Request="Sample"
            uen:Navigation.Data="{Binding SelectedItem, ElementName=WidgetsList}"/>
    ```

- Update `SecondViewModel` to accept a `Widget` as the second constructor parameter

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

- Add a `TextBlock` to `SecondPage.xaml` that shows the name of the `Widget` supplied during navigation.

    ```xml
    <TextBlock HorizontalAlignment="Center"
               VerticalAlignment="Center">
        <Run Text="Widget Name:" />
        <Run Text="{Binding Name}" />
    </TextBlock>
    ```

- In order for the `Widget` to be injected into the `SampleViewModel` during navigation, a `DataMap` has to be added to the `ViewMap`. Therefore, we can change the `ViewMap` instantiation to `DataViewMap` and provide the `Widget` as a generic argument:

    ```csharp
    new DataViewMap<SamplePage, SampleViewModel, Widget>()
    ```

### 3. Navigating To SelectedItem

Instead of having to select an item in the `ListView` and then clicking on the `Button`, Navigation can be triggered when the user selects an item in the `ListView`.

- Add the `Navigation.Request` property to the `ListView`. The `Navigation.Data` property is not required as the selected item will automatically be attached to the navigation request. Also remove the `SelectionMode` property as it is no longer necessary for the `ListView` to track the selected item.

    ```xml
    <ListView ItemsSource="{Binding Widgets}"
              uen:Navigation.Request="Sample">
        <ListView.ItemTemplate>
            <DataTemplate>
                <StackPanel Orientation="Horizontal"
                            Padding="10">
                    <TextBlock Text="{Binding Name}" />
                    <TextBlock Text="{Binding Age}" />
                </StackPanel>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
    ```
