
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




