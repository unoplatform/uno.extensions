---
uid: Learn.Tutorials.DependencyInjection.HowToCommunityToolkit
---
## CommunityToolkit.Mvvm

> [!TIP]
> This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions-net6` template to create the solution. Instructions for creating an application from the template can be found [here](../GettingStarted/UsingUnoExtensions.md)

If you want to access the DI container via the Ioc.Default API exposed via the <a href="https://www.nuget.org/packages/CommunityToolkit.Mvvm" target="_blank">CommunityToolkit.Mvvm</a>, you need to configure the service collection after building the Host.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .Build();
    Ioc.Default.ConfigureServices(Host.Services);
    // ........ //
}
```

