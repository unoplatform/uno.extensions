# Configuration and Settings

Uno.Extension.Configuration uses [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration) for any configuration related work.

For more documentation on configuration, read the references listed at the bottom.

## AppSettings

To add appsettings.json file packaged as an embedded resource.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseAppSettings<App>()
        .Build();
    // ........ //
}
```

To add appsettings.<environment>.json file packaged as an embedded resource.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseAppSettings<App>()
        .UseAppSettingsForHostConfiguration<App>()
        .Build();
    // ........ //
}
```

## App Configuration 

Map configuration section to class and register with DI

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseConfigurationSectionInApp<CustomIntroduction>("configsectionname")
        .Build();
    // ........ //
}
```

Access this in class created by DI container

```csharp
public MainPageViewModel(IOptions<CustomIntroduction> settings)
{
    // ........ //
}
```


## Custom Configuration Access

Register host configuration for access within the app

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseHostConfigurationForApp()
        .Build();
    // ........ //
}
```

The `IConfiguration` interface is registered as a service.

```csharp
// You can resolve the configuration in the constructor of a service using the IoC.
public class MyService(IConfiguration configuration) { ... }

// You can resolve the configuration from a view model using the IoC.
var configuration = this.GetService<IConfiguration>();
```

## Writable Configuration Section (aka Settings)

Register the configuration section (doesn't have to exist in configuration files)

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseWritableSettings<DiagnosticSettings>(ctx => ctx.Configuration.GetSection("sectionname"))
        .Build();
    // ........ //
}
```

Modify setting value by calling Update on an IWritableOptions instance

```csharp
public MainViewModel(IWritableOptions<DiagnosticSettings> debug)
{
    debug.Update(settings =>
    {
        debug.HasBeenLaunched = true;
    });
}
```

## References

- [Using IConfiguration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1)

