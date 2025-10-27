---
uid: Uno.Extensions.Navigation.Walkthrough.NavigateInCode
title: Navigate in Code
summary: How to control navigation from code-behind or ViewModel using INavigator.
tags: [uno, navigation, INavigator, code-behind, ViewModel, NavigateViewAsync, NavigateBackAsync, NavigateViewModelAsync, NavigateRouteAsync]
---

# Navigate in Code

## Purpose
Demonstrates how to control navigation from code (code-behind or ViewModel) using a unified `INavigator` interface.

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

## Navigate from Code-Behind

Use the `Navigator()` extension method to access `INavigator` from any `Page`.

* Create a destination page `SamplePage.xaml`

* In `MainPage.xaml`, add a Button:

    ```xml
    <Button Content="Go to SamplePage"
            Click="GoToSamplePageClick" />
    ```

* In `MainPage.xaml.cs`:

    ```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewAsync<SamplePage>(this);
    }
    ```

* `NavigateViewAsync<T>` navigates to the specified view type and pushes it onto the frame stack.

## Navigate Back from Code-Behind

* In `SamplePage.xaml`:

    ```xml
    <Button Content="Go Back"
            Click="GoBackClick" />
    ```

* In `SamplePage.xaml.cs`:

    ```csharp
    private void GoBackClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateBackAsync(this);
    }
    ```

* `NavigateBackAsync` pops the current page and returns to the previous one.

## Navigate to a ViewModel

Navigation can target ViewModels instead of Views, enabling testable, UI-independent navigation logic.

* Create a ViewModel:

    ```csharp
    public class SampleViewModel
    {
        public SampleViewModel()
        {
        }
    }
    ```

* Register View-ViewModel mapping in `App.xaml.cs`:

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

* Navigate using `NavigateViewModelAsync`:

    ```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewModelAsync<SampleViewModel>(this);
    }
    ```

* This approach decouples navigation logic from the UI layer.

## Navigate from ViewModel

Move navigation logic entirely into the ViewModel for better testability.

* Update `SampleViewModel` to inject `INavigator`:

    ```csharp
    public class SampleViewModel
    {
        private readonly INavigator _navigator;

        public SampleViewModel(INavigator navigator)
        {
            _navigator = navigator;
        }

        public Task GoBack()
        {
            return _navigator.NavigateBackAsync(this);
        }
    }
    ```

* Expose the ViewModel in `SamplePage.xaml.cs`:

    ```csharp
    public SampleViewModel? ViewModel => DataContext as SampleViewModel;

    public SamplePage()
    {
        this.InitializeComponent();
    }
    ```

* Bind to the ViewModel method in `SamplePage.xaml`:

    ```xml
    <Button Content="Go Back (View Model)"
            Click="{x:Bind ViewModel.GoBack}" />
    ```

* The ViewModel instance is automatically created and assigned as `DataContext` during navigation.

## Key Navigation Methods

The `INavigator` interface provides several extension methods:

* **NavigateRouteAsync** — Navigate to a route specified as a string
* **NavigateViewAsync** — Navigate to a route matching the view type
* **NavigateViewModelAsync** — Navigate to a route matching the ViewModel type
* **NavigateDataAsync** — Navigate to a route registered for a data type
* **NavigateBackAsync** — Navigate back to the previous page
* **NavigateForResultAsync** — Navigate to a route that returns a result

All methods work consistently from code-behind or ViewModel.

## Best Practices

* Use **NavigateViewModelAsync** for testable, UI-independent navigation
* Define **ViewMap** and **RouteMap** to avoid reflection overhead
* Inject **INavigator** into ViewModels via constructor dependency injection
* Keep navigation logic in ViewModels rather than code-behind when possible
