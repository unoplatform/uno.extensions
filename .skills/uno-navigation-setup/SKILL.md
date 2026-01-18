---
name: uno-navigation-setup
description: Set up and configure Uno Platform Navigation Extensions in an application. Use when creating new navigation-enabled projects, adding navigation to existing Uno apps, or configuring the navigation host and routes registration. Covers UnoFeatures, App.xaml.cs configuration, UseNavigation and UseToolkitNavigation setup.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Uno Platform Navigation Extensions Setup

This skill covers setting up Navigation Extensions in Uno Platform applications.

## Prerequisites

- Uno Platform 5.x or later application
- .NET 8.0 or later

## Installation

### Step 1: Add UnoFeatures

Add `Navigation` to the `<UnoFeatures>` property in your Class Library (.csproj) file:

```xml
<UnoFeatures>Navigation;Toolkit</UnoFeatures>
```

> **Note:** Adding `Toolkit` enables TabBar and NavigationBar controls with navigation support.

### Step 2: Configure App.xaml.cs

In your `App.xaml.cs`, configure the navigation host:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .UseToolkitNavigation()  // Enable Toolkit navigation support
        .Configure(host => host
            .UseNavigation(RegisterRoutes)
        );
    // ...
}

private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new RouteMap("Main", View: views.FindByViewModel<MainViewModel>())
            ]
        )
    );
}
```

### Step 3: XAML Namespace

Add the navigation namespace to your XAML files:

```xml
<Page xmlns:uen="using:Uno.Extensions.Navigation.UI"
      xmlns:utu="using:Uno.Toolkit.UI">
```

## Key Components

### INavigator Interface

The `INavigator` interface is the core navigation service:

```csharp
public interface INavigator
{
    Route? Route { get; }
    Task<bool> CanNavigate(Route route);
    Task<NavigationResponse?> NavigateAsync(NavigationRequest request);
}
```

Access `INavigator` through dependency injection or the extension method:

```csharp
// From a Page or UserControl
var navigator = this.Navigator();

// From a ViewModel (via constructor injection)
public class MyViewModel
{
    private readonly INavigator _navigator;
    
    public MyViewModel(INavigator navigator)
    {
        _navigator = navigator;
    }
}
```

## Shell Setup

Create a Shell that hosts the Frame for navigation:

**Shell.xaml:**
```xml
<UserControl x:Class="MyApp.Shell"
             xmlns:uen="using:Uno.Extensions.Navigation.UI">
    <Grid uen:Region.Attached="True">
        <Frame />
    </Grid>
</UserControl>
```

**Shell.xaml.cs:**
```csharp
public sealed partial class Shell : UserControl
{
    public Shell()
    {
        this.InitializeComponent();
    }
}
```

## Common Configuration Patterns

### With Localization

```csharp
.Configure(host => host
    .UseLocalization()
    .UseNavigation(RegisterRoutes)
)
```

### With Configuration and Logging

```csharp
.Configure(host => host
    .UseConfiguration()
    .UseLogging()
    .UseNavigation(RegisterRoutes)
)
```

## Best Practices

1. **Use `UseToolkitNavigation()`** when using TabBar or NavigationBar controls
2. **Register all views and routes** in the `RegisterRoutes` method
3. **Use ViewMap for view-viewmodel associations** to enable navigation by viewmodel type
4. **Keep Shell simple** - it should only host the navigation frame
5. **Do not use `Region.Attached="True"`** inside Shell.xaml or ExtendedSplashScreen content

## Troubleshooting

- If navigation doesn't work, verify `UseNavigation` is called in `Configure`
- Ensure views are registered before routes
- Check that `Region.Attached="True"` is set on navigation controls
