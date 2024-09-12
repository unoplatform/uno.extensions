---
uid: Uno.Extensions.Navigation.HowToNavigateBetweenPages
---
# How-To: Navigate Between Pages

This topic covers using Navigation to navigate between two pages using frame-based navigation.

[!include[getting-help](../includes/mvvm-approach.md)]

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Installation

* Add `Navigation` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
        Extensions;
    +   Navigation;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

### 2. Navigating to a New Page

* Add a new `Page` to navigate to, `SamplePage.xaml`
* In `MainPage.xaml` replace the existing `Button` with the following XAML, which includes a handler for the Click event

    ```xml
    <Button Content="Go to SamplePage"
            Click="GoToSamplePageClick" />
    ```

* In the `GoToSamplePageClick` method, use the `Navigator` extension method to get a reference to an  `INavigator` instance and call `NavigateViewAsync` to navigate to the `SamplePage`. This will push a new instance of the `SamplePage` onto the current Frame, pushing the `MainPage` to the back-stack.

    ```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateViewAsync<SamplePage>(this);
    }
    ```

### 3. Navigating Back to the Previous Page

* In `SamplePage.xaml` add a `Button` with a handler for the `Click` event

    ```xml
    <Button Content="Go Back"
            Click="GoBackClick" />
    ```

* Again, use the `Navigator` extension method to access the `INavigator` instance and call `NavigateBackAsync`. This will cause the frame to navigate to the previous page on the back-stack and releasing the `SamplePage` instance.

    ```csharp
    private void GoBackClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateBackAsync(this);
    }
    ```

### 4. Defining ViewMap and RouteMap

At this point, if you inspect the Output window you'll see a line that says something similar to:
`For better performance (avoid reflection), create a mapping for for path 'Sample', view 'SamplePage', view model`
This warning exists because Navigation uses reflection as a fallback mechanism to associate types and the corresponding navigation route. This can be resolved by specifying a `ViewMap` and a `RouteMap` for the `SamplePage` to eliminate the need for reflection

* Update the `RegisterRoutes` method in the `App.xaml.cs` file

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
                    new ("Main", View: views.FindByViewModel<MainViewModel>()),
                    new ("Second", View: views.FindByViewModel<SecondViewModel>()),
                    new ("Sample", View: views.FindByView<SamplePage>()),
                ]
            )
        );
    }
    ```

### 5. Associating a View Model

By defining a `ViewMap` that associates a view with a view model, an instance of the view model can dynamically be created and is subsequently set as the `DataContext` on the view that's navigated to.

* Create a new class `SampleViewModel` in the Presentation folder

    ```csharp
    public class SampleViewModel
    {
        public string Title => "Sample Page";
        public SampleViewModel()
        {
        }
    }
    ```

* Update `ViewMap` in `App.xaml.cs` to include `SampleViewModel`

    ```csharp
    new ViewMap<SamplePage, SampleViewModel>()
    ```

* Add `TextBlock` to the `SamplePage.xaml` and data bind to the `Title` property

    ```xml
    <TextBlock Text="{Binding Title}" />
    ```

### 6. Navigating via View Models

The Navigation code can be moved from the `SamplePage.cs` code-behind file to the `SampleViewModel`.

* Update `SampleViewModel` to take a constructor dependency on `INavigator` and add a `GoBack` method that will call the `NavigateBackAsync` method

    ```csharp
    private readonly INavigator _navigator;
    public SampleViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public Task GoBack()
    {
        return _navigator.NavigateBackAsync(this);
    }
    ```

* In order to use `x:Bind` to invoke the `GoBack` method on the `SampleViewModel` the `SamplePage` needs to expose a property that returns the `DataContext` as a `SampleViewModel`.

    ```csharp
    public SampleViewModel? ViewModel  => DataContext as SampleViewModel;

    public SamplePage()
    {
        this.InitializeComponent();
    }
    ```

* Update the `Button` in `SamplePage.xaml` to set the `Click` method to `x:Bind` to the `GoBack` method

    ```xml
    <Button Content="Go Back (View Model)"
            Click="{x:Bind ViewModel.GoBack}" />
    ```
