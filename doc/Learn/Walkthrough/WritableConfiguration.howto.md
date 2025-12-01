---
uid: Uno.Extensions.Configuration.WritableConfiguration.HowTo
title: Persist Writable Settings
tags: [configuration, writable, options]
---

> **UnoFeature:** Configuration

# Persist writable settings with `IWritableOptions`

Capture user preferences by updating configuration sections at runtime through Uno’s writable options support.

> [!NOTE]
> Writable options build on top of the read-only configuration described in [Load Configuration Sections](xref:Uno.Extensions.Configuration.HowToConfiguration).

## Enable configuration with writable support

Make sure the `Configuration` feature is enabled and register configuration sources plus the section you want to edit.

```diff
<UnoFeatures>
    Material;
+   Configuration;
    Toolkit;
    MVUX;
</UnoFeatures>
```

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseConfiguration(config =>
                config.EmbeddedSource<App>()
                      .Section<ToDoApp>());
        });
}
```

`Section<ToDoApp>()` ensures both `IOptions<ToDoApp>` and `IWritableOptions<ToDoApp>` are available from the container.

## Model the writable section

Use an immutable record to represent the settings you plan to update.

```csharp
public record ToDoApp
{
    public bool? IsDark { get; init; }
    public string? LastTaskList { get; init; }
}
```

## Update settings from a view model

Inject `IWritableOptions<T>` and call `UpdateAsync` whenever the user changes a preference.

```csharp
public class SettingsViewModel
{
    private readonly IWritableOptions<ToDoApp> _settings;

    public SettingsViewModel(IWritableOptions<ToDoApp> settings) => _settings = settings;

    public async Task ToggleThemeAsync() =>
        await _settings.UpdateAsync(current => current with
        {
            IsDark = !(current.IsDark ?? false)
        });
}
```

`UpdateAsync` receives the current snapshot and expects you to return a new instance with the modifications you want to persist.

## Create missing sections on demand

If the selected section does not exist in any backing store, Uno automatically creates it the first time `UpdateAsync` runs. This makes writable options a convenient way to build lightweight settings without pre-seeding JSON files.

## Resources

- [Configuration overview](xref:Uno.Extensions.Configuration.Overview)
- [Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration)
- [`IOptionsSnapshot<T>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.ioptionssnapshot-1)
