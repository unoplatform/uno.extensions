---
uid: Uno.Extensions.Navigation.Walkthrough.Advanced.IRouteNotifier
title: Track and Respond to Route Changes
tags: [uno, uno-platform, uno-extensions, navigation, IRouteNotifier, RouteChanged, RouteChangedEventArgs, event-handling, route-tracking, INavigator, navigation-monitoring, route-notification, navigation-events, dependency-injection, IHost, IServiceProvider, GetService, navigation-state, route-history, event-subscription, navigation-debugging, Region.Name, route-observer]
---

# Track and Respond to Route Changes

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

## Monitor route changes

```csharp
public class MyClass
{
    private readonly IRouteNotifier _notifier;

    public MyClass(IRouteNotifier notifier)
    {
        _notifier = notifier;
        _notifier.RouteChanged += RouteChanged;
    }

    private async void RouteChanged(object? sender, RouteChangedEventArgs e)
    {
        // Handle route change
    }
}
```

## Access globally

```csharp
Host = await builder.NavigateAsync<Shell>();

var notifier = Host.Services.GetService<IRouteNotifier>();
notifier.RouteChanged += (s, e) =>
{
    Debug.WriteLine($"Navigated to {e.Region?.Name}");
};
```

## Navigate from route change event

```csharp
private async void RouteChanged(object? sender, RouteChangedEventArgs e)
{
    var navigator = e.Navigator;
    
    if (SomeCondition)
    {
        await navigator.NavigateBackAsync(this);
    }
}
```

* **e.Navigator** — INavigator instance for navigation operations
* **e.Region** — Current navigation region
* **e.Route** — The route being navigated to

## Use Cases

### Navigation Analytics

```csharp
private void RouteChanged(object? sender, RouteChangedEventArgs e)
{
    _analytics.TrackPageView(e.Region?.Name ?? "Unknown");
}
```

### Navigation Guards

```csharp
private async void RouteChanged(object? sender, RouteChangedEventArgs e)
{
    if (e.Region?.Name == "SecurePage" && !_authService.IsAuthenticated)
    {
        await e.Navigator.NavigateRouteAsync(this, "Login");
    }
}
```

### State Management

```csharp
private void RouteChanged(object? sender, RouteChangedEventArgs e)
{
    _stateService.UpdateCurrentRoute(e.Route);
}
```

### Debug Logging

```csharp
private void RouteChanged(object? sender, RouteChangedEventArgs e)
{
    Debug.WriteLine($"Route changed: {e.Region?.Name} at {DateTime.Now}");
}
```
