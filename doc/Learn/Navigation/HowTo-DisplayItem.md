---
uid: Uno.Extensions.Navigation.HowToDisplayItem
---
# How-To: Display Item Details

This topic walks through how to use Navigation to display the details of an item selected from a list. This demonstrates an important aspect of Navigation which is the ability to pass data as part of a navigation request.

[!include[getting-help](../includes/mvvm-approach.md)]

## Step-by-step

[!include[create-application](../includes/create-application.md)]

Often it is necessary to pass a data item from one page to another. This scenario will start with passing a newly created object along with the navigation request, and how the specified object can be accessed by the destination ViewModel.

### 1. Define the type of data to pass

- Define a `Widget` record (or class) for data to be passed between view models

    ```csharp
    public record Widget(string Name, double Weight);
    ```

- Change the `ViewMap` in `App.xaml.cs` that associates the `SecondPage` and `SecondViewModel`, to be a `DataViewMap` object that allows you to specify the `Widget` type.

    ```csharp
    new DataViewMap<SecondPage, SecondViewModel, Widget>()
    ```

### 2. Pass data when navigating

- Create a `Widget` inside the `GoToSecondPage` method in `MainViewModel.cs`, and pass it as `data` into the navigation method.

    ```csharp
    public async Task GoToSecondPage()
    {
        var widget = new Widget("CrazySpinner", 34.0);

        await _navigator.NavigateViewModelAsync<SecondViewModel>(this, data: widget);
    }
    ```

### 3. Receiving navigation data

- Update the `SecondViewModel` constructor to accept a `Widget` parameter.

    ```csharp
    public class SecondViewModel
    {
        public string Name { get; }

        public SecondViewModel(Widget widget)
        {
            Name = widget.Name;
        }
    }
    ```

- Add a `TextBlock` to `SecondPage.xaml`

    ```xml
    <TextBlock HorizontalAlignment="Center"
               VerticalAlignment="Center">
        <Run Text="Widget Name:" />
        <Run Text="{Binding Name}" />
    </TextBlock>
    ```

### 4. Navigating with data

- Because there's a mapping between the `Widget` and the `SecondViewModel` (in the `ViewMap` defined in `App.xaml.cs`), an alternative way to navigate is by calling the `NavigateDataAsync` and specifying the data object to pass in the navigation request. The type of the data object will be used to resolve which route to navigate to.

    ```csharp
    await _navigator.NavigateDataAsync(this, data: widget);
    ```

### 5. Navigating for selected value in a `ListView`

A common application scenario is to present a list of items, for example presented in a `ListView`. When the user selects an item, the application navigates to a new view in order to display the details of that item.

- Add a `Widgets` property to your `MainViewModel`

    ```csharp
    public Widget[] Widgets { get; } =
    [
        new Widget("NormalSpinner", 5.0),
        new Widget("HeavySpinner", 50.0)
    ];
    ```

- Update `MainPage.xaml` to replace the `Button` with a `ListView` which has the `ItemsSource` property data bound to the `Widgets` property. The `Navigation.Request` property defines the route that will be navigated to when an item in the `ListView` is selected.

    ```xml
    <ListView ItemsSource="{Binding Widgets}"
              uen:Navigation.Request="Second"
              Grid.Row="1"
              HorizontalAlignment="Center"
              VerticalAlignment="Center">
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

### 6. Navigating based on the type of data (again!)

If you have a `ListView` that has items of different types, the navigation route can be based on the type of selected item.

- Change the `Navigation.Request` property value to `""`. Navigation will use the type of the selected item to determine what route to use.

- Add two additional records, `BasicWidget` and `AdvancedWidget`, that derive from `Widget`.

    ```csharp
    public record Widget(string Name, double Weight);

    public record BasicWidget(string Name, double Weight) : Widget(Name, Weight);

    public record AdvancedWidget(string Name, double Weight) : Widget(Name, Weight);
    ```

- Change the `Widgets` property in `MainViewModel` to include an array of different types of widgets.

    ```csharp
    public Widget[] Widgets { get; } = 
    [
        new BasicWidget("NormalSpinner", 5.0),
        new AdvancedWidget("HeavySpinner", 50.0)
    ];
    ```

- Clone the `SecondPage.xaml` and `SecondPage.xaml.cs` files, and rename the files to `ThirdPage.xaml` and `ThirdPage.xaml.cs` respectively. Make sure you also change the class name in both files from `SecondPage` to `ThirdPage`, as well as the `Content` property of the `NavigationBar` to read "Third Page".
- Clone `SecondViewModel.cs` and rename to `ThirdViewModel.cs`. Also rename the class from `SecondViewModel` to `ThirdViewModel`
- Change the constructor of both the `SecondViewModel` and `ThirdViewModel` to accept widgets of different types

    ```csharp
    public class SecondViewModel
    {
        public string Name { get; }

        public SecondViewModel(BasicWidget widget)
        {
            Name = widget.Name;
        }
    }

    public class ThirdViewModel
    {
        public string Name { get; }

        public ThirdViewModel(AdvancedWidget widget)
        {
            Name = widget.Name;
        }
    }
    ```

- Add a `DataViewMap` and `RouteMap` for `ThirdPage` and `ThirdViewModel`, specifying the `AdvancedWidget`. Also, update `DataViewMap` for `SecondViewModel` to be `BasicWidget` instead of `Widget`

    ```csharp
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>(),
            new DataViewMap<SecondPage, SecondViewModel, BasicWidget>(),
            new DataViewMap<ThirdPage, ThirdViewModel, AdvancedWidget>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("Main", View: views.FindByViewModel<MainViewModel>()),
                    new ("Second", View: views.FindByViewModel<SecondViewModel>()),
                    new ("Third", View: views.FindByViewModel<ThirdViewModel>()),
                ]
            )
        );
    }
    ```

- Picking an item from the list will either open `SecondPage` or `ThirdPage` based on the type of item selected.
