---
uid: Uno.Extensions.Configuration.Configuration.HowTo
title: Load Configuration Sections
tags: [configuration, appsettings, options]
---

> **UnoFeature:** Configuration

# Load configuration sections from embedded JSON

Use Uno configuration to hydrate strongly typed options from embedded `appsettings.json` files and inject them into your services.

## Enable configuration support

Add the `Configuration` UnoFeature (included automatically when `Extensions` is enabled).

```diff
<UnoFeatures>
    Material;
+   Configuration;
    Toolkit;
    MVUX;
</UnoFeatures>
```

## Register embedded appsettings files

Call `UseConfiguration` during host setup and point to embedded JSON files.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseConfiguration(config =>
                config.EmbeddedSource<App>()            // appsettings.json
                      .EmbeddedSource<App>("platform")); // appsettings.platform.json
        });
}
```

`EmbeddedSource<T>` reads an `appsettings*.json` resource from the assembly containing `T`.

## Model the configuration section

Create a record or class that mirrors the JSON shape you expect.

```csharp
public record Auth
{
    public string? ApplicationId { get; init; }
    public string[]? Scopes { get; init; }
    public string? RedirectUri { get; init; }
    public string? KeychainSecurityGroup { get; init; }
}
```

## Bind the section to options

Add `.Section<Auth>()` so the container exposes `IOptions<Auth>`.

```csharp
host.UseConfiguration(config =>
    config.EmbeddedSource<App>()
          .Section<Auth>());
```

## Inject the configuration into services

Request `IOptions<T>` anywhere you need the values.

```csharp
public class AuthenticationService : IAuthenticationService
{
    private readonly Auth _settings;

    public AuthenticationService(IOptions<Auth> options)
    {
        _settings = options.Value;
    }

    // use _settings.ApplicationId, etc.
}
```

The options snapshot reflects the contents of your embedded configuration at startup.

## Resources

- [Writable configuration](xref:Uno.Extensions.Configuration.HowToWritableConfiguration)
- [Configuration overview](xref:Uno.Extensions.Configuration.Overview)
