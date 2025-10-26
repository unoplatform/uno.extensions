---
uid: Uno.Extensions.Hosting.HowToCustomWindow
---
# How-To: Use a Custom Window with Hosting

This guide demonstrates how to create and use a custom `Window`-derived type with the Uno Extensions Hosting builder.

## Overview

By default, when you call `CreateBuilder()`, it creates a standard `Window` instance. However, you might need to use a custom `Window`-derived type to:

- Add custom properties or methods to your Window
- Override Window behavior
- Implement custom initialization logic

## Step-by-step

### 1. Define Your Custom Window Class

Create a class that inherits from `Window`:

```csharp
public class MyCustomWindow : Window
{
    public MyCustomWindow()
    {
        // Custom initialization
        Title = "My Custom Application";
    }

    public string CustomProperty { get; set; } = "Default Value";

    public void CustomMethod()
    {
        // Your custom logic
    }
}
```

### 2. Use the Window Factory with CreateBuilder

In your `App.xaml.cs`, use the `CreateBuilder` overload that accepts a `Func<Window>`:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // Create builder with a custom Window factory
    var appBuilder = this.CreateBuilder(args, () => new MyCustomWindow())
        .Configure(host => {
            // Configure the host builder
            host.UseConfiguration(configure: configBuilder =>
                configBuilder
                    .EmbeddedSource<App>()
            );
        });

    // The Window property will be your MyCustomWindow instance
    MainWindow = appBuilder.Window;

    // You can cast it to access custom members
    if (MainWindow is MyCustomWindow customWindow)
    {
        customWindow.CustomProperty = "Configured Value";
        customWindow.CustomMethod();
    }

    Host = appBuilder.Build();
}
```

### 3. Alternative: Use with Custom Assembly

If you need to specify both a custom Window and a custom application assembly:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var customAssembly = typeof(MyApp).Assembly;

    var appBuilder = this.CreateBuilder(
        args, 
        customAssembly, 
        () => new MyCustomWindow()
    )
    .Configure(host => {
        // Configure the host builder
    });

    MainWindow = appBuilder.Window;
    Host = appBuilder.Build();
}
```

## Complete Example

Here's a complete example showing a custom Window with additional functionality:

```csharp
// MyCustomWindow.cs
public class MyCustomWindow : Window
{
    private int _clickCount = 0;

    public MyCustomWindow()
    {
        Title = "Custom Window Example";
        
        // Subscribe to window events
        Activated += OnWindowActivated;
        Closed += OnWindowClosed;
    }

    public int ClickCount => _clickCount;

    public void IncrementClickCount()
    {
        _clickCount++;
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        // Custom activation logic
        System.Diagnostics.Debug.WriteLine("Window activated");
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        // Custom cleanup logic
        System.Diagnostics.Debug.WriteLine("Window closed");
    }
}

// App.xaml.cs
public partial class App : Application
{
    private IHost? Host { get; set; }
    
    protected Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args, () => new MyCustomWindow())
            .Configure(host => {
                host
#if DEBUG
                    .UseEnvironment(Environments.Development)
#endif
                    .UseLogging(configure: (context, logBuilder) =>
                    {
                        logBuilder
                            .SetMinimumLevel(
                                context.HostingEnvironment.IsDevelopment() ?
                                    LogLevel.Information :
                                    LogLevel.Warning);
                    })
                    .UseConfiguration(configure: configBuilder =>
                        configBuilder.EmbeddedSource<App>()
                    );
            });

        // Get the window instance
        MainWindow = appBuilder.Window;

        // To access custom properties/methods, cast to your custom type
        if (MainWindow is MyCustomWindow customWindow)
        {
            // Now you can access custom members
            customWindow.IncrementClickCount();
        }

        Host = appBuilder.Build();
    }
}
```

> [!NOTE]
> When using a custom Window, the `Window` property from `IApplicationBuilder` returns your custom instance, but it's typed as `Window` for compatibility. Cast it to your custom type when you need to access custom properties or methods.

## Summary

The custom Window factory feature allows you to:

1. Pass a factory function `() => new CustomWindow()` to `CreateBuilder`
2. Use the returned `Window` instance, which will be your custom type
3. Access custom properties and methods by casting to your specific Window type when needed

This provides full flexibility to customize the Window behavior while still leveraging the Uno Extensions Hosting infrastructure.
