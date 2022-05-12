
# Introduction

## What is Navigation?
Navigation needs to encompass a range of UI concepts:  
* Navigation between pages in a <a href="https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.frame" target="_blank">Frame</a> (forward and backwards)  
* Switching between menu items in a <a href="https://docs.microsoft.com/en-us/windows/winui/api/microsoft.ui.xaml.controls.navigationview?view=winui-3.0" target="_blank">NavigationView</a>, or between tab items in a <a href="https://platform.uno/uno-toolkit/" target="_blank">TabBar</a>  
* Loading content into a <a href="https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentcontrol" target="_blank">ContentControl</a>  
* Loading and toggling visibility of child elements in a <a href="https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.grid" target="_blank">Grid</a>  
* Displaying a <a href="https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.primitives.popup" target="_blank">Popup</a> or <a href="https://docs.microsoft.com/en-us/windows/apps/design/controls/dialogs-and-flyouts/flyouts" target="_blank">Flyout</a>
* Prompt a <a href="https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialoghttps://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.contentdialog" target="_blank">ContentDialog</a> or <a href="https://docs.microsoft.com/en-us/uwp/api/windows.ui.popups.messagedialog" target="_blank">MessageDialog</a>

## What triggers Navigation?
Navigation can be triggered for a number of reasons:
* Change view  
Either based on the type of View or the type of ViewModel to show
* Display data  
The view to show is based on the type of data to display (eg display ProductX)
* Prompt, or request, for data  
The view to show is based on the type of data being requested (eg Country picker)  
Prompt the user using a flyout or content dialog to get a response


# Architecture

## Objectives
Navigation needs to be accessible from anywhere
* View (Code behind)  
i.e. in context of a page/usercontrol  
* View (XAML)  
i.e. using attached properties 
* ViewModel (Presentation)  
i.e. in a context that doesn't have access to the UI layer  

Navigation needs to make use of available data  
* Uri  
Used to share links to the app (eg deeplink)
* DTO  
If an instance of an entity is already in memory, navigation should support passing the existing entity between views  
* ViewModel  
The type of viewmodel (associated with the view) to navigate to  
* View  
The type of view to navigate to


# Design

## Navigation Controls
An application typically has one or more views responsible for controlling navigation. Eg a Frame that navigates between pages, or a TabBar that switches tabs  

Navigation controls can be categorized in three distinct groups with different Navigation goals. 


| Content-Based        | Has a content area that's used to display the current view                                                             |
|----------------------|------------------------------------------------------------------------------------------------------------------------|
| ContentControl       | Navigation creates an instance of a control and sets it as the Content                                                 |
| Panel (eg Grid)      | Navigation sets a child element to Visible, hiding any previously visible child. Two scenarios:<br>	- An existing child is found. The child is set to Visible<br>	- No child is found. A new instance of a control is created and added to the Panel. The new instance is set to visible<br>Note that when items are hidden, they're no removed from the visual tree |
| Frame                | Forward navigation adds a new page to the stack based <br>Backward navigation pops the current page off the stack<br>Combination eg forward navigation and clear back stack |
|                      |                                                                                                                        |
| **Selection-Based**      | **Has selectable items**                                                                                                 |
| NavigationView       | Navigation selects the NavigationViewitem with the correct Region.Name set                                             |
| TabBar               | Navigation selects the TabBarItem with the correct Region.Name set                                                     |
|                      |                                                                                                                        |
| **Prompt-Based (Modal)** | **Modal style prompt, typically for capturing input from user**                                                            |
| ContentDialog        | Forward navigation opens a content dialog <br>Backward navigation closes the dialog                                    |
| MessageDialog        | Forward navigation opens a MessageDialog<br>Backward navigation closes the MessageDialog                               |
| Popup                | Forward navigation opens the popup<br>Backward navigation closes the popup                                             |
| Flyout               | Forward navigation opens the flyout<br>Backward navigation closes the flyout                                           |



## Regions
A region is the abstraction of the view responsible for handling navigation. 

Regions are structured into a logical hierarchical representation that shadows the navigation-aware controls in the visual hierarchy. The hierarchy allows navigation requests to be propagated up to parent and down to child regions as required. 

Regions are specified by setting Region.Attached="true" on a navigation control (eg Frame, ContentControl, Grid). 

```csharp
<ContentControl uen:Region.Attached="true" />
```


Pushing a view to this region:  
	`navigator.NavigateRouteAsync(this,"ProductDetails");`  
or  
	`navigator.NavigateViewAsync<ProductDetailsControl>(this);`  
or  
	`navigator.NavigateViewModelAsync<ProductDetailsViewModel(this);`  
or    
    `navigator.NavigateDataAsync(this, selectedProduct);`  
    
    

## Region Name

Regions can be named by specifying the Region.Name="XXX" property. 

For selection-based regions, the selectable items (NavigationViewItem, TabBarItem, …) are identified using the Region.Name property

```csharp
<muxc:NavigationView uen:Region.Attached="true">
	<muxc:NavigationView.MenuItems>
		<muxc:NavigationViewItem Content="Products" uen:Region.Name="Products" />
		<muxc:NavigationViewItem Content="Deals" uen:Region.Name="Deals" />
		<muxc:NavigationViewItem Content="Profile" uen:Region.Name="Profile" />
	</muxc:NavigationView.MenuItems>
</muxc:NavigationView>
```

Switching selected item:  
	`naviator.NavigateRouteAsync(this,"Deals");`



## INavigator

The NavigateAsync method on the INavigator  interface accepts a NavigationRequest parameter and returns a Task that can be awaited in order to get a NavigationResponse. 


```csharp
public interface INavigator
{
    Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}
```


There are INavigator extension methods that accept a variety of parameters, depending on the intent, which are mapped to a corresponding combination of Route and Result values.


## Examples - Code (either code behind or view model)

The following examples assume the following application structure

```
Frame  
  - LoginPage  
  - HomePage  
      TabBar  
        - Products
            - Details
        - Deals
```
There is a Frame located at the root of the application which is used to navigate between the Login and Home pages. Inside the Home page there's a TabBar which is used to switch between a list of Products and a list of Deals. Clicking on Products navigates forward to the Details page.

| Method                                                                                                               | Notes                                                                                                                                                                                                                                           |
|----------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| NavigateRouteAsync(this, "Home")                                                                                     | Navigates to the HomePage                                                                                                                                                                                                                       |
| NavigateRouteAsync(this, "Home/Products")                                                                            | Navigates to the HomePage and then the Products tab                                                                                                                                                                                             |
| NavigateRouteAsync(this, "Home/Products/Details?id=5")                                                               | Navigates to the HomePage, then the Products tab, and then to the Details page and displays product with id=5                                                                                                                                   |
| NavigateRouteAsync(this, data: selectedProduct )                                                                     | Navigates to the Details page<br>The type of the selecteProduct is used to determine which view to navigate to<br>The selectedProduct object is passed into the ToQuery function in order to extract query paramters                            |
| NavigateRouteAsync(this, "Deals")                                                                                 | Navigates to the Deals tab<br>(assuming this is executed from the Details page, inside a Products tab on the HomePage)                                                                                                                          |
| NavigateViewAsync< HomePage >(this)                                                                                    | Navigates to the HomePage<br>The HomePage type is used to find the RouteMap with a View of HomePage where the Path is used to set the Base of the Route                                                                                         |
| NavigateViewModelAsync< HomeViewModel >(this)                                                                          | Navigates to the HomePage<br>The HomeViewModel type is used to fine the RouteMap with a ViewModel of HomeViewModel where the Path is used to set the Base of the Route                                                                          |
| NavigateRouteAsync(this, "-") or NavigateBackAsync(this)                                                         | This navigates to previous page in a frame, or closes a dialog                                                                                                                                                                                  |
| NavigateRouteAsync(this, "-", data: selectedProduct) or NavigateBackWithResultAsync(this, data: selectedProduct) | This navigation passes data as result of a prior navigation request (it will also navigate to previous page on a frame, or close a dialog). The selectedProduct object is passed into the ToQuery function in order to extract query parameters |
| NavigateRouteAsync(this, "/Login") or NavigateRouteAsync(this, "Login", scheme: Schemes.Root)                        | Navigates to the Login page from the root NavigationRegion. Irrespective of which INavigator instance you call NavigateAsync on, the Root scheme will cause the hierarchy to be traversed up to the first NavigationRegion                      |
| ShowMessageDialogAsync(this,"Warning about something","Alert")                                                       | Displays a MessageDialog with title "Alert" and content of "Warning about something"                                                                                                                                                            |


## RouteMap

In order for navigation to support both view and viewmodel based navigation it is necessary to have some way to define a mapping, or association, between a view and viewmodel (for example MainPage is mapped to MainViewModel, and vice versa).  However, given the different intents and behaviours we needed to support, navigation supports a more complex mapping that is referred to as a RouteMap.

A RouteMap is made up of the following components:  

| Component  | Description                                                                                                                      |
|------------|----------------------------------------------------------------------------------------------------------------------------------|
| Path       | The name of the route. When processing a NavigationRequest the Base is used to look up the RouteMap with the corresponding Path.<br>This is used to match to the Region.Name attribute for PanelVisibilityNavigator, NavigationViewNavigator and TabBarNavigator<br>NavigateRouteAsync(sender, Path) to navigate to the RouteMap with matching Path.<br>That Path is also used to populate the deep link at any point in the application. |
| View       | The type of view to be created (or in the case of Frame, navigated to)<br>NavigateViewAsync<Tview>(sender) to navigate to the RouteMap with the matching View (type) |
| ViewModel  | The type of view model to be created, and set as DataContext for the current view of the region<br>NavigateViewModelAsync<TViewModel>(sender) to navigate to the RouteMap with the matching ViewModel (type) |
| Data       | The type of data being sent in the navigation request<br>NavigateDataAsync(sender, Data: data) to navigate to the RouteMap with matching Data (type) |
| ResultData | The type of data to be returned in the response to the navigation request<br>NavigateForResultAsync<TResultData>(sender) to navigate to the RouteMap with matching ResultData(type) |
| IsDefault  | Determines which child route should, if any, be used as the default route                                                        |
| Init       | Callback function to be invoked prior to navigation for a particular route                                                       |
| ToQuery    | Callback function to convert a data object into query parameters (eg Product -> [{"ProductId", "1234"}] )                        |
| FromQuery  | Callback function to convert query parameters into a data object (eg [{"ProductId", "1234"}] -> Product )                        |
| Nested     | Child routes - currently only used to specify default views for nested regions                                                   |


## Common Scenarios

### 1. Navigating between pages (code behind)

Navigate forward to a new page by calling NavigateRouteAsync with the route to navigate to

**XAML**
```xml
<Page x:Class="Playground.Views.HomePage">
    <Button Click="{x:Bind GoToSecondPageClick}"
	        Content="Go to Second Page - Code behind" />
</Page>
```

**C#**  
```csharp
public sealed partial class HomePage : Page
{
	public async void GoToSecondPageClick()
	{
		var nav = this.Navigator();
		await nav.NavigateRouteAsync(this, "Second");
	}
}
```
Navigate back to the previous page by calling NavigateBackAsync

**XAML**
```xml
<Page x:Class="Playground.Views.SecondPage">
	<Button Click="{x:Bind GoBackClick}"
			Content="Go Back" />
</Page>
```

**C#**
```csharp
public sealed partial class SecondPage : Page
{
	public async void GoBackClick()
	{
		var nav = this.Navigator();
		await nav.NavigateBackAsync(this);
	}
}
```
### 2. Navigating between pages (view model)

Navigate forward to a new page by calling NavigateRouteAsync with the route to navigate to

**XAML**
```xml
<Page x:Class="Playground.Views.HomePage">
			<Button Click="{x:Bind ViewModel.GoToSecondPageClick}"
					Content="Go to Second Page - View Model" />
</Page>
```


**C#**
```csharp
public sealed partial class HomeViewModel
{
	private readonly INavigator _navigator;
	public HomeViewModel(INavigator navigator)
	{
	    _navigator = navigator;
	}
	
	public async void GoToSecondPageClick()
	{
		await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
	}
}
```


Navigate back to the previous page by calling NavigateBackAsync

**XAML**
```xml
<Page x:Class="Playground.Views.SecondPage">
			<Button Click="{x:Bind ViewModel.GoBackClick}"
					Content="Go Back" />
</Page>
```


**C#**
```csharp
public sealed partial class SecondViewModel
{

	private readonly INavigator _navigator;
	public SecondViewModel(INavigator navigator)
	{
	    _navigator = navigator;
	}
	
	public async void GoBackClick()
	{
		await _navigator.NavigateBackAsync(this);
	}
}
```


### 3. Navigating between pages (XAML)

Navigate forward to new page by specifying the route in the Navigation.Request attached property  
**XAML**
```xml
<Page x:Class="Playground.Views.HomePage">
			<Button uen:Navigation.Request="Second"
					Content="Go to Second Page - XAML" />
</Page>
```

Navigate to previous page by specifying the back qualifier in the Navigation.Request attached property  
**XAML** 
```xml
<Page x:Class="Playground.Views.SecondPage">
			<Button uen:Navigation.Request="-"
					Content="Go Back" />
</Page>
```


### 4. Navigating between pages (XAML)

Navigate forward to new page and clearing backstack by specifying the route in the Navigation.Request attached property with the Clear Backstack qualifier ("-/")  
**XAML**
```xml
<Page x:Class="Playground.Views.HomePage">
		<Button uen:Navigation.Request="-/Second"
				Content="Go to Second Page - XAML" />
</Page>
```


### 5. Prompt user - Message Dialog

Prompt the user with a message using the ShowMessageDialogAsync method. Await the Result property in order to access the response from the user  
**C#**
```csharp
public sealed partial class HomePage : Page
{
	public async void PromptWithMessageDialogClick()
	{
		var nav = this.Navigator();
		var response = await nav.ShowMessageDialogAsync(this,"Warning about something","Alert");
		var messgaeResult = await response.Result;
	}
}
```


### 6. Switching items in NavigationView

Define selectable navigation view items by setting the Region.Name. When an item is selected, the child element of the Grid with the same Region.Name will be set to Visible (and the others set back to Collapsed)  
**XAML**  
```xml
<Page x:Class="Playground.Views.NavigationViewPage">

	<Grid>
		<muxc:NavigationView uen:Region.Attached="true">
			<muxc:NavigationView.MenuItems>
				<muxc:NavigationViewItem Content="Products"
										 uen:Region.Name="Products" />
				<muxc:NavigationViewItem Content="Deals"
										 uen:Region.Name="Deals" />
				<muxc:NavigationViewItem Content="Profile"
										 uen:Region.Name="Profile" />
			</muxc:NavigationView.MenuItems>
			<Grid uen:Region.Attached="True">
				<StackPanel uen:Region.Name="Products"
					  Visibility="Collapsed">
					<TextBlock Text="Products" />
				</StackPanel>
				<StackPanel uen:Region.Name="Deals"
					  Visibility="Collapsed">
					<TextBlock Text="Deals" />
				</StackPanel>
				<StackPanel uen:Region.Name="Profile"
					  Visibility="Collapsed">
					<TextBlock Text="Profile" />
				</StackPanel>
			</Grid>
		</muxc:NavigationView>
	</Grid>
</Page>
```


### 7. Displaying content in a ContentControl

Specify the Region.Name of the nested control and the route of the UserControl to display in the Navigation.Request attached property  
**XAML**
```xml
<Page x:Class="Playground.Views.ContentControlPage">
	<Grid>
		<Grid.RowDefinitions>
			<RowDefinition />
			<RowDefinition Height="Auto" />
		</Grid.RowDefinitions>
			<Button Content="Show profile"
						uen:Navigation.Request="./Info/Profile" />
			<ContentControl uen:Region.Attached="True"
							uen:Region.Name="Info"
							Grid.Row="1" />
	</Grid>
</Page>

<UserControl x:Class="Playground.Views.ProfileUserControl">
…
</UserControl>
```


## Qualifiers

| Qualifier |                                                              | Example          |                                                              |
|-----------|--------------------------------------------------------------|------------------|--------------------------------------------------------------|
| ""        | Navigate to page in frame, or open popup                     | "Home"           | Navigate to the HomePage                                     |
| /         | Forward request to the root region                           | "/"<br>"/Login"  | Navigate to the default route at the root of navigation<br>Navigate to LoginPage at the root of navigation |
| ./        | Forward request to child region                              | "./Info/Profile" | Navigates to the Profile view in the child region named Info |
| !         | Open a dialog or flyout                                      | "!Cart"          | Shows the Cart flyout                                        |
| -         | Back (Frame), Close (Dialog/Flyout) or respond to navigation | "-"<br>"--Profile"<br>"-/Login" | Navigate back one page (in a frame)<br>Navigate to Profile page and remove two pages from backstack<br>Navigate to Login page and clear backstack |





# Getting Started

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
        Navigator.NavigateBackAsync(this);
    }
}
```


