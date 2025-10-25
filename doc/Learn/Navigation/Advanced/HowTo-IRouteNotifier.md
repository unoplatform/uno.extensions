---
uid: Uno.Extensions.Navigation.Advanced.IRouteNotifier
---
# How-To: Use `IRouteNotifier` to Handle Route Changes

> **UnoFeatures:** `Navigation` (add to `<UnoFeatures>` in your `.csproj`)

The `IRouteNotifier` interface allows you to track and respond to route changes through the `RouteChanged` event. This guide will show you how to use `IRouteNotifier` to handle route changes and improve your app's navigation.

## Step-by-steps

[!include[create-application](../../includes/create-application.md)]

### 1. How to implement the RouteChanged-Event from `IRouteNotifier` to Monitor Route Changes

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

    private void RouteChanged(object? sender, RouteChangedEventArgs e)
    {
        // Implement your logic to handle route change here
    }
}
```

### 2. Access `INavigator` through `IRouteNotifier` and get the current Route Name

It's possible to access an `INavigator` through the `RouteChanged` event provided by the `IRouteNotifier`. This can be particularly useful when you need to handle navigation within dynamic scenarios, such as

- managing modals or dialogs
- conditional navigation flows
- getting the current Route name.

```diff
private void RouteChanged(object? sender, RouteChangedEventArgs e)
{
    var navigator = e.Navigator;
    
+   var currentRouteName = e.Navigator.Route?.ToString();

    // Other Logic to use `INavigator` here
}
```

### Chaining `IRouteNotifier` up with Localization support

By listening to Route Changes like this, we can for example set the Root Page Title, which could be shown in a `NavigationView` Header, dependant on the current Route name. But it's rarely happening that our Page Headline does match the Route Name since those can not contain Characters like Spaces and nobody would want to change the RouteMap just to have another localization culture supported.

For the following sample, let's assume, we are working on MainPage and it's corresponding Model is named `MainModel`. 

#### [Mvux](#tab/mvux)

1. Because of the `IRouteNotifier` doesn't have a direct Property of `INavigator`, we do need to aquire an Instance of this through the Constructor of our Model:

    ```diff
    public partial record MainModel
    {
        private readonly IRouteNotifier _routeNotifier;
    +   private readonly INavigator _navigator;

        public MainModel(
            IRouteNotifier routeNotifier,
    +       INavigator navigator)
        {
            _routeNotifier = routeNotifier;
            _routeNotifier.RouteChanged += Main_OnRouteChanged;
    +       _navigator = navigator;
        }
    }
    ```
2. Add your Route Names to the `./Strings/[locID]/Resources.resw` and add suffix each of them with `Title`.
3. Get an Instance of `IStringLocalizer` through your Model Constructor
        
    ```diff
    public partial record MainModel
    {
        private readonly IRouteNotifier _routeNotifier;
        private readonly INavigator _navigator;
    +   private readonly IStringLocalizer _stringLocalizer;

        public MainModel(
            IRouteNotifier routeNotifier,
            INavigator navigator,
    +       IStringLocalizer stringLocalizer)
        {
            _routeNotifier = routeNotifier;
            _routeNotifier.RouteChanged += Main_OnRouteChanged;
            _navigator = navigator;
    +       _stringLocalizer = stringLocalizer;
        }
    }
    ```

4. Create a `IState<string>` in your Model with an initial Value of the Route name by requesting the previously gotten `INavigator`:

    ```csharp
    public IState<string> Title => State<string>.Value(this, () => _navigator.Route?.ToString() ?? string.Empty);
    ```

    > [!NOTE]
    > The `ToString()` of the `Route`-Type does have an overwritten Behaviour, to return the Name of the current Route as `string`.
    > Since the `Route` Property of the `INavigator` is defined as nullable, we need to use `?` and the coaleszenz Operator `??` to provide a non-null Value for our State in this case.

5. Now we can use the Event Handler, to update our Title each time it gets called:

    ```diff
    private async void RouteChanged(object? sender, RouteChangedEventArgs e)
    {
        var navigator = e.Navigator;
        
       var currentRouteName = e.Navigator.Route?.ToString();
        +   await Title.SetAsync(currentRouteName]);

        // Other Logic to use `INavigator` here
    }
    ```

    > [!NOTE]
    > The automatic Eventhandler refactoring command in the Visual Studio IDE defaults to give us an `private void` as you seen above this sample. But as we need to `await` the State to set the new Value in Mvux, this Eventhandler needs to become an `private async void`. Remark here, that if you just type `await` infront of the Title line, the IDE will potentially auto format your previous `private void` to `private async Task`, which will promt you the error message, that the Definition of the Eventhandler is **incompartible like this** with the registration we made for this in the Constructor of our Model before, so make sure to check this if you run into such error.

6. Let's combine the `RouteNotifier` with some [Localization support](xref:Uno.Extensions.Localization.Overview)! Update the Route Changed Eventhandler contained line to request our `IStringLocalizer` for the value of our Route name with the suffix of `Title`.

    ```diff
    private async void RouteChanged(object? sender, RouteChangedEventArgs e)
    {
        var navigator = e.Navigator;

        var currentRouteName = e.Navigator.Route?.ToString();
    +   await Title.SetAsync(_stringLocalizer[currentRouteName + "Title"]);

        // Other Logic to use `INavigator` here
    }
    ```

    > [!TIP]
    > By using a Suffix, you could easily scale the variants and could also have a `+ "SubTitle"` as simplest Example.

7. Your corresponding UI could then be set up like this with an `NavigationView` suitable to a Navigation related display for example:

    ```xaml
    <NavigationView uen:Region.Attached="True"
                    Header="{Binding Title}">
        <NavigationView.MenuItems>
            <NavigationViewItem Content="Home"
                                uen:Region.Name="Dashboard"
                                Icon="Home" />
            <NavigationViewItem Content="Create new List"
                                uen:Region.Name="CreateNewList"
                                Icon="AddFriend" />
        </NavigationView.MenuItems>
        <NavigationView.Content>
            <Grid uen:Region.Attached="True"
                  uen:Region.Navigator="Visibility"
                  Visibility="Visible"/>
        </NavigationView.Content>
    </NavigationView>

#### [Mvvm](#tab/mvvm)



### Use the `IRouteNotifyer` from everywhere in your App

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

