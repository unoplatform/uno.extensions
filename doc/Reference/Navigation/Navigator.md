---
uid: Reference.Navigation.Navigator
---

# INavigator

The `INavigator` is an interface that handles navigating between different views or pages in an application. It manages how you move from one part of your app to another, often keeping track of routes and handling navigation-related tasks.

The `NavigateAsync` method on the `INavigator` interface accepts a NavigationRequest parameter and returns a Task that can be awaited in order to get a NavigationResponse.

```csharp
public interface INavigator
{
    Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}
```

The `CanNavigate` method checks if the app can navigate to a specific route. It takes a `Route` parameter and returns a `Task<bool>`, which tells you whether navigation is possible. Before navigating, you can use CanNavigate to see if it's allowed or makes sense.

```csharp
public interface INavigator
{
    Task<bool> CanNavigate(Route route);
}
```

## `INavigator` Extension Methods

There are `INavigator` extension methods that accept a variety of parameters, depending on the intent, which are mapped to a corresponding combination of Route and Result values.

### Navigates to the specified route

Navigates to the view associated with the route as defined in the `RouteMap`.

```csharp
Task<NavigationResponse?> NavigateRouteAsync(this INavigator navigator, object sender, string route, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
```

Usage:

```csharp
var navigationResponse = await _navigator.NavigateRouteAsync(this, route: "MyExample");
```

### Navigates to the specified view

Navigates to the specified view.

```csharp
Task<NavigationResponse?> NavigateViewAsync<TView>(this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
```

Usage:

```csharp
var response = await _navigator.NavigateViewAsync<MyExamplePage>(this);
```

### Navigates to the view through the viewmodel

Navigates to the view associated with the viewmodel as defined in the `ViewMap`.

```csharp
Task<NavigationResponse?> NavigateViewModelAsync<TViewViewModel>(this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
```

Usage:

```csharp
var response = await _navigator.NavigateViewModelAsync<MyExampleViewModel>(this);
```

### Navigates to the view associated with the data type

Navigates to the view associated with the data type as defined in the `DataViewMap`. The type of the data object will be used to resolve which route to navigate to.

```csharp
Task<NavigationResponse?> NavigateDataAsync<TData>(this INavigator navigator, object sender, TData data, string qualifier = Qualifiers.None, CancellationToken cancellation = default)
```

Usage:

```csharp
await _navigator.NavigateDataAsync<MyData>(this);
```

### Navigates to a Route and Retrieving Result Data

Navigates to a route and get the result of the specified data type, as defined in the `ResultDataViewMap`.

```csharp
Task<NavigationResultResponse<TResult>?> NavigateForResultAsync<TResult>(this INavigator navigator, object sender, string qualifier = Qualifiers.None, object? data = null, CancellationToken cancellation = default)
```

Usage:

```csharp
var returnObject = await _navigator.NavigateForResultAsync<MyObject>(this);
```

Alternatively the `GetDataAsync<TResult>()` could be used:

```csharp
var returnObject = await _navigator.GetDataAsync<MyObject>(this);
```

All methods mentioned also have `ForResultAsync` variations available. These variations can be used if you need to retrieve data while navigating. For example:

```csharp
NavigateRouteForResultAsync<TResult>()
NavigateViewForResultAsync<TView, TResult>()
NavigateViewModelForResultAsync<TViewViewModel, TResult>()
NavigateDataForResultAsync<TData, TResult>()
```

The return type of `NavigateForResultAsync` and all its variations is `NavigationResultResponse<TResult>`. To get the actual value, use `.SomeOrDefault()` on the result:

```csharp
var result = await navigator.NavigateRouteForResultAsync<Widget>(this, "Second", data: widget).AsResult();
var actualResult = result.SomeOrDefault();
```

## NavigationResponse

The `NavigationResponse` object encapsulates the result of a navigation operation. It includes:

- **Route**: The route that was navigated to.
- **Success**: Indicates whether the navigation was successful.
- **Navigator**: The `INavigator` instance that processed the final segment of the route.

## NavigationRequest

The `NavigationRequest` object represents a request for navigation. It includes:

- **Sender**: The originator of the navigation request.
- **Route**: The route to navigate to.
- **Cancellation**: An optional `CancellationToken` to cancel the navigation.
- **Result**: An optional type for the result of the navigation.
- **Source**: An optional `INavigator` instance that initiated the navigation.
