---
uid: Uno.Extensions.Navigation.Walkthrough.RegisterRoutes
title: Register Navigation Routes and Associate Views to ViewModels
tags: [uno, uno-platform, uno-extensions, navigation, routes, ViewMap, RouteMap, IViewRegistry, IRouteRegistry, view-registration, route-registration, ViewModel-association, FindByViewModel, RegisterRoutes, navigation-setup, type-safe-navigation, performance-optimization, view-mapping, route-mapping]
---

# Register Navigation Routes and Associate Views to ViewModels

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

## Defining RouteMap and ViewMap

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>(),
        new ViewMap<SamplePage, SampleViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new("Main", View: views.FindByViewModel<MainViewModel>()),
                new("Sample", View: views.FindByViewModel<SampleViewModel>()),
            ]
        )
    );
}
```
