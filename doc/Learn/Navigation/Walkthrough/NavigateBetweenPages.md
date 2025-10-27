---
uid: Uno.Extensions.Navigation.Walkthrough.NavigateBetweenPages
title: Navigate Between Pages
summary: How to navigate between two pages in Uno Platform using INavigator and RouteMap.
tags: [uno, navigation, INavigator, MVUX, MVVM, NavigateViewAsync, NavigateBackAsync, RouteMap]
---

# Navigate Between Pages

## Purpose:
Demonstrates how to navigate between two pages using Uno Platform’s frame-based navigation with `INavigator`.

## Prerequisites

 * Add `Navigation` support in your app’s .csproj file:

    ```diff
    <UnoFeatures>
        Material;
        Extensions;
    +   Navigation;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

## Create Pages

Create two pages:

* `MainPage.xaml` — the start page

* `SamplePage.xaml` — the destination

* Example Button in MainPage.xaml:

    ```xml
    <Button Content="Go to SamplePage"
            Click="GoToSamplePageClick" />
    ```

* Code-behind (`MainPage.xaml.cs`):

    ```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewAsync<SamplePage>(this);
    }
    ```
* `NavigateViewAsync<T>` pushes the new page onto the navigation frame stack.

## Navigate Back

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

* `NavigateBackAsync` pops the current page off the stack and returns to the previous view.

## Define ViewMap and RouteMap

Navigation uses `reflection` by default.
For better performance, explicitly register your `pages` and `routes`.

* In `App.xaml.cs`:

    ```csharp
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>(),
            new DataViewMap<SecondPage, SecondViewModel, Entity>(),
            new ViewMap<SamplePage>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new("Main", View: views.FindByViewModel<MainViewModel>()),
                    new("Second", View: views.FindByViewModel<SecondViewModel>()),
                    new("Sample", View: views.FindByView<SamplePage>()),
                ]
            )
        );
    }
    ```

* Add a `ViewModel`:

    ```csharp
    public class SampleViewModel
    {
        public string Title => "Sample Page";
    }
    ```

* Register it with the page:

    ```csharp
    new ViewMap<SamplePage, SampleViewModel>()
    ```

* In `SamplePage.xaml`:

    ```xml
    <TextBlock Text="{Binding Title}" />
    ```

* Navigate from ViewModel

    You can perform navigation directly from a ViewModel.

    ```csharp
    public class SampleViewModel
    {
        private readonly INavigator _navigator;

        public SampleViewModel(INavigator navigator)
        {
            _navigator = navigator;
        }

        public Task GoBack() => _navigator.NavigateBackAsync(this);
    }
    ```

* Expose the DataContext for `x:Bind` in `SamplePage.xaml.cs`:

    ```csharp
    public SampleViewModel? ViewModel => DataContext as SampleViewModel;
    ```

* Update the XAML:

    ```xml
    <Button Content="Go Back (View Model)"
            Click="{x:Bind ViewModel.GoBack}" />
    ```