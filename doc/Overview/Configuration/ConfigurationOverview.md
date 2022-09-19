---
uid: Overview.Configuration
---
# Configuration and Settings

Uno.Extension.Configuration uses [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration) for any configuration related work.

For more documentation on configuration, read the references listed at the bottom.

## AppSettings

To use `appsettings.json` file packaged as `EmbeddedResource` in the application:  

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseConfiguration(configure: builder => 
            builder.EmbeddedSource<App>( includeEnvironmentSettings: true ))
        .Build();
    // ........ //
}
```  

To use `appsettings.json` file packaged as `Content` in the application:   

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseConfiguration(configure: builder => 
            builder.ContentSource<App>( includeEnvironmentSettings: true ))
        .Build();
    // ........ //
}
```

By default both `EmbeddedSource` and `ContentSource` methods will also add settings files that are specific to the current environment, `appsettings.<hostenvironment>.json` (eg `appsettings.development.json`). This can be disabled by setting the `includeEnvironmentSettings` argument to `false` (default value is `true`).

It is recommended to use `EmbeddedSource` as this ensures all configuration information is read prior to the `IHost` instance being created, allowing configuration to determine how services are created.

## App Configuration 

Map configuration section to class and register with DI

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseConfiguration(configure: builder => 
            builder
                .EmbeddedSource<App>()
                .Section<CustomIntroduction>())
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


## Configuration Access

The `IConfiguration` interface is registered as a service when the UseConfiguration extension method is used.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseConfiguration()
        .Build();
    // ........ //
}
```

The `IConfiguration` can then be accessed in any class created by the DI container

```csharp
// You can resolve the configuration in the constructor of a service using the IoC.
public class MyService(IConfiguration configuration) { ... }

// You can resolve the configuration from a view model using the IoC.
var configuration = this.GetService<IConfiguration>();
```

## Writable Configuration Section (a.k.a. Settings)

Register the configuration section the same way as you would for accessing a configuration section (the section doesn't have to exist in any configuration source)

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseConfiguration(configure: builder => 
            builder
                .EmbeddedSource<App>()
                .Section<DiagnosticSettings>())
        .Build();
    // ........ //
}

public record DiagnosticSettings( bool HasBeenLaunched );
```

Modify setting value by calling Update on an IWritableOptions instance

```csharp
public MainViewModel(IWritableOptions<DiagnosticSettings> debug)
{
    debug.Update(debugSetting =>
        debugSetting with {HasBeenLaunched = true}
        );
}
```

## References

- [Using IConfiguration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1)

