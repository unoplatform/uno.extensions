# How-To: Navigate in XAML

This topic walks through controlling Navigation from XAML. This includes specifying data that should be attached to the navigation request.

> [!Tip] This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](../Extensions/GettingStarted/UsingUnoExtensions.md)

## Step-by-steps

### 1. Navigation.Request

Navigation can be defined in XAML by placing the `Navigation.Request` attached property on a specific XAML element. The string value specified in the `Navigation.Request` is the route to be navigated to. 
Depending on the type of the XAML element, the `Navigation.Request` property will attach to an appropriate event in order to trigger navigation. For example, on a `Button`, the `Click` event will be used to trigger navigation, where as the `SelectionChanged` event on a `ListView` is used. If you place a `Navigation.Request` property on an static element, such as a `Border`, `Image` or `TextBlock`, the `Tapped` event will be used to trigger navigation.

- Add a new `Page` to navigate to, `SamplePage.xaml`, in the UI (shared) project

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

- In `MainPage.xaml` add a `Button` with the `Navigation.Request` attached property

    ```xml
    <Button Content="Go to SamplePage"
            uen:Navigation.Request="Sample" />
    ```

- In `SamplePage.xaml` add a `Button`, again with the `Navigation.Request` attached property. The "-" navigation route is used to navigate back. 

    ```xml
    <Button Content="Go Back"
            uen:Navigation.Request="-" />
    ```

> [!Tip] Whilst this works, it relies on reflection to convert the request path "Sample" to the corresponding view, i.e. `SamplePage`. It's better to define `ViewMap` and `RouteMap`


- Add a `ViewMap` and a `RouteMap` for the `SamplePage` into the `RegisterRoutes` method in the `App.xaml.host.cs` file 

    ```csharp
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap<ShellControl,ShellViewModel>(),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<SecondPage, SecondViewModel>(),
            new ViewMap<SamplePage, SampleViewModel>()
        );
        
        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>() ,
                Nested: new RouteMap[]
                {
                    new RouteMap("Main", View: views.FindByViewModel<MainViewModel>()),
                    new RouteMap("Second", View: views.FindByViewModel<SecondViewModel>()),
                    new RouteMap("Sample", View: views.FindByViewModel<SampleViewModel>()),
        }));
    }
    ```

### 2. Navigation.Data

In addition to specifying the route to navigate to, the Navigation.Data attached property can be used to define the data to be attached to the navigation request. The data can be accessed by the view model associated with the route using constructor injection.  

- Define a record (or class), `Widget`, that is the type of data that will be attached to the navigation request. 

    ```csharp
    public record Widget(string Name, double Weight){}
    ```

- Add a property, `Widgets`, to `MainViewModel` that returns an array of predefined `Widget` instances.

    ```csharp
    public Widget[] Widgets { get; } = new[]
    {
        new Widget("NormalSpinner", 5.0),
        new Widget("HeavySpinner",50.0)
    };
    ```

- Add a `ListView` to `MainPage.xaml` that has the `ItemsSource` property data bound to the `Widgets` property.

    ```xml
    <ListView ItemsSource="{Binding Widgets}" x:Name="WidgetsList">
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
- Add a `Button` to `MainPage.xaml` that has both `Navigation.Request`, that specified the navigation route, and the `Navigation.Data` properties. In this case the `Navigation.Data` attached property is data bound to the `SelectedItem` property on the named element `WidgetsList` (which matches the `x:Name` set on the previously added `ListView`)

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
        private readonly INavigator _navigator;
    
        public string Name { get; }
    
        public SampleViewModel(INavigator navigator, Widget widget)
        {
            _navigator = navigator;
            Name = widget.Name;
        }
    }
    
    ```
- Add a `TextBlock` to `SecondPage.xaml` that shows the name of the `Widget` supplied during navigation.

    ```xml
    <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center">
        <Run Text="Widget Name:" /><Run Text="{Binding Name}" />
    </TextBlock>
    ```     

- In order for the `Widget` to be injected into the `SampleViewModel` during navigation, a `DataMap` has to be added to the `ViewMap`
    ```csharp
    new ViewMap<SamplePage, SampleViewModel>(Data: new DataMap<Widget>())
    ```

### 3. Navigating To SelectedItem

Instead of having to select an item in the `ListView` and then clicking on the `Button`, Navigation can be triggered when the user selects an item in the `ListView`.

- Add the `Navigation.Request` property to the `ListView`. The `Navigation.Data` property is not required as the selected item will automatically be attached to the navigation request.

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



