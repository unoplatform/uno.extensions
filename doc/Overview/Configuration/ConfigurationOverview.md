---
uid: Overview.Configuration
---
# Configuration

`Uno.Extensions.Configuration` provides a uniform way to read or write configuration data from a number of distinct sources. The implmentation of `IOptions<T>` from [Microsoft.Extensions.Options](https://docs.microsoft.com/dotnet/api/microsoft.extensions.options) allows for [read-only](https://docs.microsoft.com/dotnet/core/extensions/configuration#concepts-and-abstractions) access to values organized into **configuration sections**. The [writable configuration](xref:Learn.Tutorials.Configuration.HowToWritableConfiguration) pattern supports the ability to update configuration values at runtime.

This feature uses [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration) for any configuration related work. For more documentation on configuration, read the references listed at the bottom.

## AppSettings

To use `appsettings.json` file packaged as **EmbeddedResource** in the application:  

```csharp
private IHost Host { get; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(e)
    .Configure(
        host => host
            .UseConfiguration(configure: builder => 
                builder.EmbeddedSource<App>( includeEnvironmentSettings: true ))
    );

    Host = appBuilder.Build();
...
```

To use `appsettings.json` file packaged as **Content** in the application:   

```csharp
private IHost Host { get; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(e)
    .Configure(
        host => host
            .UseConfiguration(configure: builder => 
                builder.ContentSource<App>( includeEnvironmentSettings: true ))
    );

    Host = appBuilder.Build();
...
```

By default, both `EmbeddedSource` and `ContentSource` methods will also create settings files that are specific to the current environment, `appsettings.<hostenvironment>.json` (eg `appsettings.development.json`). This can be disabled by setting the `includeEnvironmentSettings` argument to `false` (default value is `true`).

> [!TIP]
> It is recommended to ensure all configuration information is read before creation of the `IHost` instance by only using `EmbeddedSource`. This will allow configuration to determine which services are created.

## App Configuration 

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

## Configuration Access

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
```

The `IConfiguration` can then be accessed in any class created by the DI container:

```csharp
public class MyService : IMyService
{
    private readonly IConfiguration configuration;

    public MyService(IConfiguration configuration)
    {
        this.configuration = configuration;
    }
...
```

## Writable Configuration

Writable configuration allows for the ability to update configuration values at runtime. This is done by using the `IWritableOptions<T>` interface. This interface is registered as a service when the `UseConfiguration()` extension method is used. The recommended approach to using this feature is with configuration sections.

Register a configuration section for writable configuration does not require it to exist in any source. The section will be created if it does not exist.

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

- [Using IConfiguration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1)
- [Microsoft.Extensions.Options](https://docs.microsoft.com/dotnet/api/microsoft.extensions.options)
- [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration)