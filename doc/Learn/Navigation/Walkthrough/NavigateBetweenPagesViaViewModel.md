---
uid: Uno.Extensions.Navigation.Walkthrough.NavigateViewModel
title: Navigate Between Pages via ViewModel
tags: [uno, uno-platform, uno-extensions, navigation, INavigator, ViewModel, MVVM, dependency-injection, DI, constructor-injection, NavigateViewModelAsync, NavigateBackAsync, NavigateRouteAsync, testable-navigation, unit-testing, UI-independent, separation-of-concerns, ViewMap, RouteMap, x-bind, data-binding, navigation-logic]
---

# Navigate Between Pages via ViewModel

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

## Navigate to another page

```csharp
public class MainViewModel
{
    private readonly INavigator _navigator;

    public MainViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }

    public async Task GoToSamplePage()
    {
        await _navigator.NavigateViewModelAsync<SampleViewModel>(this);
    }
}
```

```xml
<Button Content="Go to Sample Page"
        Click="{x:Bind ViewModel.GoToSamplePage}" />
```

Inject `INavigator` via constructor for testable, UI-independent navigation.

## Navigate back to previous page

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

```xml
<Button Content="Go Back"
        Click="{x:Bind ViewModel.GoBack}" />
```

## Navigate using route string

```csharp
public async Task GoToSamplePage()
{
    await _navigator.NavigateRouteAsync(this, "Sample");
}
```

Route name must match the name registered in RouteMap.
