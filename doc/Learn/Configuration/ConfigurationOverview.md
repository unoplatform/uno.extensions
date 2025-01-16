---
uid: Uno.Extensions.Configuration.Overview
---
# Configuration

`Uno.Extensions.Configuration` provides a uniform way to read or write configuration data from a number of distinct sources. The implementation of `IOptions<T>` from [Microsoft.Extensions.Options](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options) allows for [read-only](https://learn.microsoft.com/dotnet/core/extensions/configuration#concepts-and-abstractions) access to values organized into **configuration sections**. The [writable configuration](xref:Uno.Extensions.Configuration.HowToWritableConfiguration) pattern supports the ability to update configuration values at runtime.

This feature uses [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration) for any configuration related work. For more documentation on configuration, read the references listed at the bottom.

## Installation

> [!NOTE]
> If you already have `Extensions` in `<UnoFeatures>`, then `Configuration` is already installed, as its dependencies are included with the `Extensions` feature.

`Configuration` is provided as an Uno Feature. To enable `Configuration` support in your application, add `Configuration` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

## Using Configuration

The `IConfiguration` interface is registered as a service when the `UseConfiguration()` extension method is used.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(e)
    .Configure(
        host => host
            .UseConfiguration( /* ... */)
    );

    Host = appBuilder.Build();
    ...
}
```

`IConfiguration` is then available to be accessed by any class instantiated by the dependency injection (DI) container:

```csharp
public class MyService : IMyService
{
    private readonly IConfiguration configuration;

    public MyService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }
    ...
}
```

## App settings file sources

To use `appsettings.json` file packaged as **EmbeddedResource** in the application:

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(e)
        .Configure(
            host => host
                .UseConfiguration(
                    builder => builder
                        .EmbeddedSource<App>()
                )
        );

    Host = appBuilder.Build();
    ...
}
```

The recommended approach to specifying a configuration file source, especially when targeting Web Assembly (WASM), is to register it as an **EmbeddedResource** as described above. Configuration data read from embedded resources has the benefit of being available to the application immediately upon startup. However, it is still possible to package the file source as **Content** instead:

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    // NOT RECOMMENDED FOR WEB ASSEMBLY
    var appBuilder = this.CreateBuilder(e)
        .Configure(
            host => host
                .UseConfiguration(
                    builder => builder
                        .ContentSource<App>()
                )
        );

    Host = appBuilder.Build();
    ...
}
```

Both `EmbeddedSource` and `ContentSource` methods will also create settings files that are specific to the current environment by default, `appsettings.<hostenvironment>.json` (eg `appsettings.development.json`). This can be disabled by setting the `includeEnvironmentSettings` argument to `false` (default value is `true`):

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(e)
        .Configure(
            host => host
                .UseConfiguration(
                    builder => builder
                        .EmbeddedSource<App>(includeEnvironmentSettings: false)
                )
        );

    Host = appBuilder.Build();
}
```

> [!TIP]
> It is recommended to ensure all configuration information is read before creation of the `IHost` instance by only using `EmbeddedSource`. This will allow configuration to determine which services are created.

## Sections

Map configuration section to class and register with dependency injection (DI):

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(e)
        .Configure(
            host => host
                .UseConfiguration(
                    builder => builder.EmbeddedSource<App>()
                        .Section<CustomIntroduction>()
                )
        );
    ...
}
```

This can be accessed from any class created by the DI container:

```csharp
public class MainViewModel : ObservableObject
{
    private readonly IOptions<CustomIntroduction> customConfig;

    public MainViewModel(IOptions<CustomIntroduction> customConfig)
    {
        this.customConfig = customConfig;
    }
}
```

## Updating configuration values at runtime

The **writable configuration** feature enables the ability to update configuration values at runtime. This pattern may also be referred to as the _settings pattern_ in certain documentation.  With the `UseConfiguration()` extension method, `IWritableOptions<T>` is registered as a service. The recommended approach when specifying a set of settings for this feature is to use **configuration sections**.

Registering a configuration section for writable configuration does not require it to exist in any source. The section will be created if it does not exist.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(e)
        .Configure(
            host => host
                .UseConfiguration(builder =>
                    builder
                        .EmbeddedSource<App>()
                        .Section<DiagnosticSettings>()
                )
        );
    ...
}
```

`DiagnosticSettings` can be defined as a record:

```csharp
public record DiagnosticSettings(bool HasBeenLaunched);
```

Modify a setting value by calling `Update()` on the injected `IWritableOptions` instance:

```csharp
public MainViewModel(IWritableOptions<DiagnosticSettings> debug)
{
    debug.Update(debugSetting =>
        debugSetting with { HasBeenLaunched = true }
    );
}
```

## References

- [Using IConfiguration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration)
- [Microsoft.Extensions.Options](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options)
- [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)
