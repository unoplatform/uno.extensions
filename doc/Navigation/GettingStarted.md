# Getting Started

Enable navigation and support navigation using controls from the Uno.Toolkit

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseNavigation(RegisterRoutes)
        .UseToolkitNavigation()
        .Build();
    // ........ //
}

private static void RegisterRoutes(IRouteRegistry routes)
{
    routes.Register(new RouteMap("Shell", ViewModel: typeof(ShellViewModel),
		Nested: new[]
		{
			new RouteMap("Main", View: typeof(MainPage), ViewModel: typeof(MainPageViewModel)),
			new RouteMap("Second", View: typeof(SecondPage), ViewModel: typeof(SecondPageViewModel))
		}));
}

protected async override void OnLaunched(LaunchActivatedEventArgs args)
{
#if NET5_0 && WINDOWS
    _window = new Window();
    _window.Activate();
#else
	_window = Window.Current;
#endif

	_window.Content = Host.Services.NavigationHost();
	_window.Activate();

	await Task.Run(()=>Host.StartAsync());
}

```
Calling the NavigationHost method will create the root element for the application and navigate to the first route defined in the RegisterRoutes method. You can override this by specifying the initialRoute (ie route name), initialView (Type of view) or initialViewModel (Type of view model) to navigate to. 

## Navigation

Navigate by calling one of the navigation extension methods on an INavigator instance. The INavigator instance can be injected into the view model constructor, or can be set via the IInstance < INavigator > interface. 

```csharp
public class MainPageViewModel : ObservableValidator
{
    public MainPageViewModel(INavigator navigator)
    {
        Navigator = navigator;
    }

    private INavigator Navigator { get; }

    public void GoSecond()
    {
        Navigator.NavigateViewModelAsync<TSecondViewModel>(this);
    }
}
``` 

## Go Back 

Go back to previous page

```csharp
public class SecondPageViewModel : ObservableObject
{
    public SecondPageViewModel(INavigator navigator)
    {
        Navigator = navigator;
    }

    private INavigator Navigator { get; }

    public void GoBack()
    {
        Navigator.NavigateBackAsync(this);
    }
}
```

