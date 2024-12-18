---
uid: Uno.Extensions.Navigation.HowToNavigateInCode
---
# How-To: Navigate in Code

This topic walks through controlling Navigation from code, either in the code-behind file of a Page or in the corresponding view model. One of the Navigation objectives was a single navigation construct that applies wherever you choose to write your navigation code.

[!include[getting-help](../includes/mvvm-approach.md)]

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Navigating to a New Page

Navigation can be invoked in the code-behind file of a `Page` by using the `Navigator` extension method to get an `INavigator` instance.

- Add a new `Page` to navigate to, `SamplePage.xaml`
- In `MainPage.xaml` update the `Button` to the following XAML, which includes a handler for the `Click` event

    ```xml
    <Button Content="Go to SamplePage"
            Click="GoToSamplePageClick" />
    ```

- In the `GoToSamplePageClick` method, use the `Navigator` extension method to get a reference to an  `INavigator` instance and call `NavigateViewAsync` to navigate to the `SamplePage`. This will push a new instance of the `SamplePage` onto the current frame, pushing the `MainPage` to the back-stack.

    ```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewAsync<SamplePage>(this);
    }
    ```

### 2. Navigating Back to the Previous Page

- In `SamplePage.xaml` add a `Button` with a handler for the `Click` event

    ```xml
    <Button Content="Go Back"
            Click="GoBackClick" />
    ```

- Again, use the `Navigator` extension method to access the `INavigator` instance and call `NavigateBackAsync`. This will cause the frame to navigate to the previous page on the back-stack and releasing the `SamplePage` instance.

    ```csharp
    private void GoBackClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateBackAsync(this);
    }
    ```

### 3. Navigate to a ViewModel

The `NavigateViewAsync` method uses the type of the view, i.e. `SamplePage`, to determine the view to navigate to. By associating a view model with a view, Navigation can be defined based on the type of view model to navigate to. This means that the Navigation logic isn't dependent on the UI layer of the application. The Navigation logic can then be moved into a view model and thus making it easier to test.

- Create a new class `SampleViewModel` in the `ViewModels` folder of the class library project

    ```csharp
    public class SampleViewModel
    {
        public SampleViewModel()
        {
        }
    }
    ```

- Add `ViewMap` and `RouteMap` instances in the `RegisterRoutes` method in `App.xaml.cs`. This associates the `SampleViewModel` with the `SamplePage`, as well as avoiding the use of reflection for route discovery.

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

- Now that there's an association between `SamplePage` and `SampleViewModel` the code in `MainPage.xaml.cs` can be updated to use the `NavigateViewModelAsync` method.

    ```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewModelAsync<SampleViewModel>(this);
    }
    ```

### 4. View Model Navigation

- The logic for navigating back from `SamplePage` to `MainPage` can be moved into the `SampleViewModel`. Add the `GoBack` method to `SampleViewModel` that uses the `INavigator` instance that's injected via the constructor.

    ```csharp
    public SampleViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public Task GoBack()
    {
        return _navigator.NavigateBackAsync(this);
    }

    private readonly INavigator _navigator;
    ```

- During Navigation from `MainPage` to `SamplePage` an instance of the `SampleViewModel` will get created and assigned as the `DataContext` of the newly created `SamplePage`. In order to `x:Bind` to properties and methods on the `SampleViewModel`, expose a `ViewModel` property that returns the `DataContext` property as a `SampleViewModel`.

    ```csharp
    public SampleViewModel? ViewModel => DataContext as SampleViewModel;

    public SamplePage()
    {
        this.InitializeComponent();
    }
    ```

- Update the `Button` in `SamplePage.xaml` to use `x:Bind` to define the event handler for the `Click` event.

    ```xml
    <Button Content="Go Back (View Model)"
            Click="{x:Bind ViewModel.GoBack}" />
    ```

> [!TIP]
> The logic to navigate from `MainPage` to `SamplePage` can also be refactored into the `MainViewModel`. Irrespective of whether the logic is in the code-behind or in the view model, it would use the same `NavigateViewModelAsync<SampleViewModel>` method call.

There are many other extension methods on the `INavigator` interface that can be used from either the code-behind or view model. Here are a few of the key navigation methods:
**NavigateRouteAsync** - Navigates to a route specified as a string
**NavigateViewAsync** - Navigates to a route that matches the view type specified
**NavigateViewModelAsync** - Navigates to a route that matches the view model type specified
**NavigateDataAsync** - Navigates to a route that is registered for the data type specified
**NavigateForResultAsync** - Navigates to a route that is registered to return the result data type specified
