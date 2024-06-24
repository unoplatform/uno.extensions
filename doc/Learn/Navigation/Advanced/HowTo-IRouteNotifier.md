---
uid: Uno.Extensions.Navigation.Advanced.IRouteNotifier
---
# How-To: Use IRouteNotifier to Handle Route Changes

The `IRouteNotifier` interface allows you to track and respond to route changes through the `RouteChanged` event. This guide will show you how to use `IRouteNotifier` to handle route changes and improve your app's navigation.

## Step-by-steps

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [Creating an application with Uno.Extensions article](xref:Uno.Extensions.HowToGettingStarted) for creating an application from the template.

### How to Use `IRouteNotifier` to Monitor Route Changes

The `IRouteNotifier` provides an effective way to monitor and respond to route changes within your application. To begin using `IRouteNotifier` Ensure your class has access to an instance of an `IRouteNotifier` implementation. Add a parameter of type `IRouteNotifier` to the constructor of your class where you want to monitor route changes.

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

<!-- TODO: Add IRouteNotifier, NavigationResponse, INavigator usage example -->