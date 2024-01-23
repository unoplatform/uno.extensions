---
uid: Reference.Navigation.Navigator
---
# Custom INavigator Implementation

## INavigator

The `NavigateAsync` method on the `INavigator` interface accepts a NavigationRequest parameter and returns a Task that can be awaited in order to get a NavigationResponse.

```csharp
public interface INavigator
{
    Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}
```

There are `INavigator` extension methods that accept a variety of parameters, depending on the intent, which are mapped to a corresponding combination of Route and Result values.

- Walk through a simple INavigator implementation
