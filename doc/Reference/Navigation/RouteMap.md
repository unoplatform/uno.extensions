---
uid: Reference.Navigation.RouteMap
---
# What is a RouteMap

## RouteMap

In order for navigation to support both view and viewmodel based navigation it is necessary to have some way to define a mapping, or association, between a view and viewmodel (for example MainPage is mapped to MainViewModel, and vice versa).  However, given the different intents and behaviors we needed to support, navigation supports a more complex mapping that is referred to as a RouteMap.

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

- Explain what a RouteMap is and how it's used to define the route hierarchy in the app
