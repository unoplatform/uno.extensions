# Navigation - Getting Started

Enable navigation and support navigation using controls from the Uno.Toolkit

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseNavigation(RegisterRoutes)
        .UseToolkitNavigation()
        .Build();
    // ........ //
}

private static void RegisterRoutes(IRouteRegistry routes)
{
    routes.Register(new RouteMap("Shell", ViewModel: typeof(ShellViewModel),
		Nested: new[]
		{
			new RouteMap("Main", View: typeof(MainPage), ViewModel: typeof(MainPageViewModel)),
			new RouteMap("Second", View: typeof(SecondPage), ViewModel: typeof(SecondPageViewModel))
		}));
}

protected async override void OnLaunched(LaunchActivatedEventArgs args)
{
#if NET5_0 && WINDOWS
    _window = new Window();
    _window.Activate();
#else
	_window = Window.Current;
#endif

	_window.Content = Host.Services.NavigationHost();
	_window.Activate();

	await Task.Run(()=>Host.StartAsync());
}

```
Calling the NavigationHost method will create the root element for the application and navigate to the first route defined in the RegisterRoutes method. You can override this by specifying the initialRoute (ie route name), initialView (Type of view) or initialViewModel (Type of view model) to navigate to. 

## Navigation

Navigate by calling one of the navigation extension methods on an INavigator instance. The INavigator instance can be injected into the view model constructor, or can be set via the IInstance < INavigator > interface. 

```csharp
public class MainPageViewModel : ObservableValidator
{
    public MainPageViewModel(INavigator navigator)
    {
        Navigator = navigator;
    }

    private INavigator Navigator { get; }

    public void GoSecond()
    {
        Navigator.NavigateViewModelAsync<TSecondViewModel>(this);
    }
}
``` 

## Go Back 

Go back to previous page

```csharp
public class SecondPageViewModel : ObservableObject
{
    public SecondPageViewModel(INavigator navigator)
    {
        Navigator = navigator;
    }

    private INavigator Navigator { get; }

    public void GoBack()
    {
        Navigator.NavigatePreviousAsync(this);
    }
}
```




# Navigation - Details
Application is made up of a series of containers, such as a Frame, that support navigation. These are referred to as Regions

There are some containers that are currently supported by default, with others able to be supported by either implementing INavigator or extending the ControlNavigator base class:  
**ContentControlNavigator** - ContentControl - Supports forward navigation to show new content  
**PanelVisibilityNavigator** - Panel - Supports forward navigation to make children of the panel visible 
**FrameNavigator** - Frame - Supports forward and backward navigation between pages  
**NavigationViewNavigator** - NavigationView - Supports forward navigation between selected items in the NavigationView    
**TabBarNavigator** - TabBar (Toolkit) - Supports forward navigation between selected items in the TabBar
**ContentDialogNavigator** - ContentDialog - Supports forward navigation between tabs    
**MessageDialogNavigator** - MessageDialog - Supports forward navigation between tabs  
**PopupNavigator** - Popup - Supports forward navigation between tabs  



## Attributes
A region needs to be defined in XAML using one of these attached properties

**Region.Attached = true/false**
Indicates that a region should be attached to the specified FrameworkElement. 

**Region.Name = "region"**
This defines the name of any region that gets created below the FrameworkElement where the Region.Name attribute is set.

**Region.Navigator = "Visibility"**
Setting the Region.Navigator attribute controls what navigator type is used for a region. This may be required if two different navigators are registered for the same control type.

**Navigation.Request = "route"**
Specifies what route should be navigated to. The trigger for the navigation is specific to the type of control. For example a Button will navigate when the Click event is raised. There are handlers for Button (Click), NavigationView (ItemInvoked) and Selector (SelectionChanged or ItemClick for ListView)

**Navigation.Data = "{Binding MyDataProperty}"**
The data that should be attached to the navigation request. For Selector (including ListView) the selected item will automatically be attached to the navigation request.




## Navigation Interfaces and Extension methods

**INavigator**
```csharp
public interface INavigator
{
    Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}
```

**Navigator Extensions**
```csharp
public static class NavigatorExtensions
{
    // Navigate to a route eg Home
    NavigateRouteAsync(...);
    // Navigate to a route and expect a result of type TResult
    NavigateRouteForResultAsync<TResult>(...);
    // Navigate to a view of type TView
    NavigateViewAsync<TView>(...);
    // Navigate to a view of type TView and expect a result of type TResult
    NavigateViewForResultAsync<TView, TResult>(...);
    // Navigate to a view model of type TViewModel
    NavigateViewModelAsync<TViewModel>(...);
    // Navigate to a view model of type TViewModel and expect a result of type TResult
    NavigateViewModelForResultAsync<TViewModel, TResult>(...);
    // Navigate to the route that handles data of type TData
    NavigateDataAsync<TData>(...);
    // Navigate to the route that handles data of type TData and expect a result of type TResult
     NavigateDataForResultAsync<TData, TResultData>(...);
    // Navigate to the route that will return data of type TResultData
    NavigateForResultAsync<TResultData>(...);
    // Navigate to previous view (goback on frame or close dialog/popup)
    NavigatePreviousAsync(...);
    // Navigate to previous view (goback on frame or close dialog/popup) and provide response data
    NavigatePreviousWithResultAsync<TResult>(...);
    // Show MessageDialog
    ShowMessageDialogAsync(...);
}
```

## Route Schemes

The route takes on a number of attributes of a typical Uri. 

eg: ..

**None eg. Home or ProductDetails**
**../ eg. ../Home or ../ProductDetails**

Navigates the current region to the desired route. If the region is a Frame, this correlates to navigating to a new page. For selector based regions, such as NavigationView or TabBar, this correlates to selecting the item that is annotated with a Region.Name that matches the route. For other regions, the route is used to determine the type of view to load.
A single ../ scheme is

**../../ eg. ../../Home**
Navigates the parent region to the desired route

**./ eg. ./Details/ProductDetails**

Navigates a nested region to the desired route. The example navigates the nested named region, Details, to the route ProductDetails.
Note: If the name of the nested region, eg Details, correlates to a RouteMap, the current region will attempt to navigate to the View defined by the RouteMap

**-**

Navigates back or provides a response back. If the frame is a region with a backstack >0, the frame will be navigated back to previous page (as if GoBack was called on the frame). For all regions, if the last navigation requested a response, the - scheme will respond with either None or Some (if the navigation request had an associated Data).

**- eg. -Deals**

Navigates back to previous page, before navigating forward to the desired route

**- eg. -/Deals**

Navigates to the desired route and clears the back stack

**! eg. !MessageDialog  or  !CustomDialog

Navigates to the desired route as a dialog. MessageDialog is a predefined route used to display a MessageDialog. CustomDialog is the dialog that inherits from ContentDialog.

