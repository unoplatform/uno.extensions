---
uid: Uno.Extensions.Logging.Logging.HowTo
title: Add Structured Logging
tags: [logging, diagnostics, observability]
---
# Add structured logging to your Uno app

Enable Uno logging so platform-specific providers capture diagnostic information, then inject `ILogger` into your services and view models.

## Enable logging support

Add the `Logging` feature to include `Uno.Extensions.Logging`.

```diff
<UnoFeatures>
    Material;
+   Logging;
    Toolkit;
    MVUX;
</UnoFeatures>
```

## Register logging providers

Call `UseLogging` during host configuration to wire up platform defaults (OSLog on iOS, console logging on WebAssembly, etc.).

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseLogging();
        });
}
```

Customize providers or minimum levels by supplying a callback:

```csharp
host.UseLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Information);
    logging.AddConsole();
});
```

## Inject loggers into your components

Request `ILogger<T>` (or `ILogger`) through constructor injection.

```csharp
public class MainViewModel
{
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(ILogger<MainViewModel> logger) => _logger = logger;

    public void Load()
    {
        _logger.LogInformation("Loading main view model at {Time}", DateTimeOffset.Now);
    }
}
```

Log at the appropriate level (`LogDebug`, `LogWarning`, `LogError`, etc.) to control verbosity across environments.

## Resources

- [Logging overview](xref:Uno.Extensions.Logging.Overview)
- [Internal logging configuration](xref:Uno.Extensions.Logging.UseInternalLogging)
- [Microsoft.Extensions.Logging documentation](https://learn.microsoft.com/dotnet/core/extensions/logging)
