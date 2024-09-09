---
uid: Uno.Extensions.Navigation.Advanced.IRouteNotifier
---
# How-To: Use `IRouteNotifier` to Handle Route Changes

The `IRouteNotifier` interface allows you to track and respond to route changes through the `RouteChanged` event. This guide will show you how to use `IRouteNotifier` to handle route changes and improve your app's navigation.

## Step-by-steps

[!include[create-application](../../includes/create-application.md)]

### How to Use `IRouteNotifier` to Monitor Route Changes

The `IRouteNotifier` provides an effective way to monitor and respond to route changes within your application. To begin using `IRouteNotifier`, ensure your class has access to an instance of an `IRouteNotifier` implementation. Add a parameter of type `IRouteNotifier` to the constructor of your class where you want to monitor route changes.

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
        // Implement your logic to handle route change here
    }
}
```

It's possible to access the `IRouteNotifier` service directly from `App.xaml.cs` or anywhere you have access to the `IHost` to retrieve services. This allows you to subscribe to route changes and respond accordingly in your application.

For example, in `App.xaml.cs`:

```csharp
...
Host = await builder.NavigateAsync<Shell>();

var notifier = Host.Services.GetService<IRouteNotifier>();
notifier.RouteChanged += (s, e) =>
{
    Debug.WriteLine($"Navigated to {e.Region?.Name}");
};
```

### Access `INavigator` through `IRouteNotifier`

It's possible to access an `INavigator` through the `RouteChanged` event provided by the `IRouteNotifier`. This can be particularly useful when you need to handle navigation within dynamic scenarios, such as managing modals, dialogs, or conditional navigation flows.

```csharp
private async void RouteChanged(object? sender, RouteChangedEventArgs e)
{
    var navigator = e.Navigator;
    // Logic to use `INavigator` here
}
```
