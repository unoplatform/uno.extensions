---
name: uno-navigation-code
description: Navigate programmatically in Uno Platform using INavigator extension methods. Use when implementing navigation from ViewModels, code-behind, or services. Covers NavigateRouteAsync, NavigateViewModelAsync, NavigateViewAsync, NavigateDataAsync, NavigateBackAsync, and result-based navigation.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Code-Based Navigation

This skill covers programmatic navigation using the INavigator interface in Uno Platform.

## Accessing INavigator

### From a Page or UserControl

```csharp
var navigator = this.Navigator();
await navigator.NavigateRouteAsync(this, "Second");
```

### From a ViewModel (Dependency Injection)

```csharp
public class MainViewModel
{
    private readonly INavigator _navigator;

    public MainViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public async Task NavigateToSecond()
    {
        await _navigator.NavigateRouteAsync(this, "Second");
    }
}
```

## Navigation Methods

### NavigateRouteAsync

Navigate to a registered route by name:

```csharp
// Simple navigation
await _navigator.NavigateRouteAsync(this, "Products");

// With data
await _navigator.NavigateRouteAsync(this, "ProductDetail", data: myProduct);

// With qualifier (clear back stack)
await _navigator.NavigateRouteAsync(this, "Login", qualifier: Qualifiers.ClearBackStack);
```

### NavigateViewModelAsync

Navigate to the view associated with a ViewModel type:

```csharp
// Simple navigation
await _navigator.NavigateViewModelAsync<ProductsViewModel>(this);

// With data
await _navigator.NavigateViewModelAsync<ProductDetailViewModel>(this, data: myProduct);

// With qualifier
await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
```

### NavigateViewAsync

Navigate to a specific View type:

```csharp
// Navigate to view
await _navigator.NavigateViewAsync<ProductsPage>(this);

// With qualifier for dialog
await _navigator.NavigateViewAsync<SamplePage>(this, qualifier: Qualifiers.Dialog);
```

### NavigateDataAsync

Navigate based on data type (uses DataViewMap registration):

```csharp
var product = new Product("Widget", 29.99);
await _navigator.NavigateDataAsync(this, product);
```

The navigation framework resolves the route based on the data type's DataViewMap registration.

### NavigateBackAsync

Navigate to the previous page:

```csharp
await _navigator.NavigateBackAsync(this);
```

### NavigateBackWithResultAsync

Navigate back and return data to the previous page:

```csharp
// Return typed result
await _navigator.NavigateBackWithResultAsync(this, data: selectedProduct);

// From a filter page returning to search
await _navigator.NavigateBackWithResultAsync(this, data: updatedFilter);
```

## Navigation with Results

### GetDataAsync

Navigate and await a result:

```csharp
// Navigate to a picker and get selected item
var selectedProduct = await _navigator.GetDataAsync<Product>(this);

if (selectedProduct is not null)
{
    // Use the selected product
}
```

### NavigateForResultAsync

Navigate and get a typed result response:

```csharp
var response = await _navigator.NavigateForResultAsync<Product>(this);
var product = response?.Result?.SomeOrDefault();
```

### NavigateRouteForResultAsync

Navigate to a specific route and get result:

```csharp
var result = await _navigator.NavigateRouteForResultAsync<Widget>(this, "WidgetPicker", data: initialWidget);
var selectedWidget = result?.AsResult().SomeOrDefault();
```

### NavigateViewModelForResultAsync

Navigate to a ViewModel and get result:

```csharp
var product = await _navigator.GetDataAsync<ProductsViewModel, Product>(this);
```

## Multi-Page Navigation

Navigate through multiple pages in one call:

```csharp
// Navigate to Sample with Second in the back stack
await _navigator.NavigateRouteAsync(this, "Second/Sample");
```

This:
- Navigates to SamplePage
- Injects SecondPage into the back stack
- SecondPage is created only when user navigates back

## Checking Navigation Possibility

```csharp
var route = Route.PageRoute("Products");
var canNavigate = await _navigator.CanNavigate(route);

if (canNavigate)
{
    await _navigator.NavigateRouteAsync(this, "Products");
}
```

## Method Signatures Reference

```csharp
// Route-based navigation
Task<NavigationResponse?> NavigateRouteAsync(
    object sender, 
    string route, 
    string qualifier = Qualifiers.None, 
    object? data = null, 
    CancellationToken cancellation = default)

// ViewModel-based navigation
Task<NavigationResponse?> NavigateViewModelAsync<TViewModel>(
    object sender, 
    string qualifier = Qualifiers.None, 
    object? data = null, 
    CancellationToken cancellation = default)

// View-based navigation
Task<NavigationResponse?> NavigateViewAsync<TView>(
    object sender, 
    string qualifier = Qualifiers.None, 
    object? data = null, 
    CancellationToken cancellation = default)

// Data-based navigation
Task<NavigationResponse?> NavigateDataAsync<TData>(
    object sender, 
    TData data, 
    string qualifier = Qualifiers.None, 
    CancellationToken cancellation = default)

// Back navigation
Task<NavigationResponse?> NavigateBackAsync(
    object sender, 
    string qualifier = Qualifiers.None, 
    CancellationToken cancellation = default)

// Back with result
Task<NavigationResponse?> NavigateBackWithResultAsync<TResult>(
    object sender, 
    string qualifier = Qualifiers.None, 
    Option<TResult>? data = null, 
    CancellationToken cancellation = default)

// Get data from navigation
Task<TResult?> GetDataAsync<TResult>(
    object sender, 
    string qualifier = Qualifiers.None, 
    object? data = null, 
    CancellationToken cancellation = default)
```

## Best Practices

1. **Use `this` as sender** in code-behind, or `this` in ViewModels when using INavigator directly

2. **Prefer NavigateViewModelAsync** for type-safe navigation when Views and ViewModels are properly registered

3. **Use NavigateDataAsync** when the data type determines the destination

4. **Always await navigation calls** to ensure proper navigation flow

5. **Handle null results** from GetDataAsync - user may cancel or navigate away
