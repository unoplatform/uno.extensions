---
uid: Learn.Tutorials.DependencyInjection.HowToCommunityToolkit
---
## CommunityToolkit.Mvvm

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

