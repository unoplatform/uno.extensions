# Navigation

Register routes which provide mapping between view model and page

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseRouting<RouterConfiguration, LaunchMessage>(() => _frame)
        .Build();
    // ........ //
}


public class RouterConfiguration : IRouteDefinitions
{
    public IReadOnlyDictionary<string, (Type, Type)> Routes { get; } = new Dictionary<string, (Type, Type)>()
        .RegisterPage<MainPageViewModel, MainPage>(string.Empty)
        .RegisterPage<SecondPageViewModel, SecondPage>();
}
```

The LaunchMessage will navigate to the page that's registered with `string.Empty` in the Routes dictionary.

## Navigation

Navigate by raising a RoutingMessage, specifying the view model you want to navigate to.

```csharp
public class MainPageViewModel : ObservableValidator
{
    public MainPageViewModel(IRouteMessenger messenger)
    {
        Messenger = messenger;
    }

    private IRouteMessenger Messenger { get; }

    public void GoSecond()
    {
        Messenger.Send(new RoutingMessage(this, typeof(SecondPageViewModel).AsRoute()));
    }
}
``` 

## Go Back 

Close the current page and navigate to previous page by raising the CloseMessage

```csharp
public class SecondPageViewModel : ObservableObject
{
    public SecondPageViewModel(
        IRouteMessenger messenger)
    {
        Messenger = messenger;
    }

    private IRouteMessenger Messenger { get; }

    public void GoBack()
    {
        Messenger.Send(new CloseMessage(this));
    }
}
```