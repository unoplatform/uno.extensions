---
uid: Uno.Extensions.Hosting.HostingSetup
title: Configure Uno Hosting
tags: [hosting, dependency-injection, startup]
---
# Configure hosting for dependency injection

Create an Uno host so you can register services, windows, and features through a unified startup pipeline.

## Enable the hosting feature

Add `Hosting` to pull in `Uno.Extensions.Hosting` (already included when `Extensions` is enabled).

```diff
<UnoFeatures>
    Material;
+   Hosting;
    Toolkit;
    MVUX;
</UnoFeatures>
```

## Create the application builder

Expose a `Host` property and call `CreateBuilder` in `OnLaunched`.

```csharp
public sealed partial class App : Application
{
    public IHost Host { get; private set; } = default!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host =>
            {
                // register services, features, etc.
            });
```

Add `global using Microsoft.Extensions.Hosting;` to `GlobalUsings.cs` for convenience.

## Use the builder to create the window

Instead of instantiating `Window` yourself, use the builder’s window.

```csharp
        var app = appBuilder.Build();

        MainWindow = app.Window;
```

`Build()` returns an `AppHost` wrapper containing both the configured `IHost` and the initialized window.

## Keep a reference to the host

Store the host so you can resolve services later if needed.

```csharp
        Host = app.Host;
        MainWindow!.Activate();
    }
}
```

Any service registrations performed inside the `Configure` callback are available through `Host.Services`.

## Resources

- [Register and consume services](xref:Uno.Extensions.DependencyInjection.HowToDependencyInjection)
- [Navigation overview](xref:Uno.Extensions.Navigation.Overview)
