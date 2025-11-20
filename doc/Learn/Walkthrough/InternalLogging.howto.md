---
uid: Uno.Extensions.Logging.InternalLogging.HowTo
title: Tune Internal Logging
tags: [logging, diagnostics, uno-internals]
---
# Tune Uno internal logging for diagnostics

Enable Uno’s internal logging pipeline, adjust verbosity per environment, and control XAML layout logs so you capture the details you need without overwhelming your output.

> [!IMPORTANT]
> Start with the base logging setup described in [Add Structured Logging](xref:Uno.Extensions.Logging.UseLogging) before enabling Uno-specific diagnostics.

## Enable Uno logging

Pass `enableUnoLogging: true` when configuring logging to capture Uno platform messages (rendering, navigation, bindings, etc.).

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseLogging(enableUnoLogging: true);
        });
}
```

This adds additional log providers and categories under `Uno.*`.

## Adjust log levels by environment

Set different minimum levels for development and production to balance noise and insight.

```csharp
host.UseLogging((context, logging) =>
{
    var minimum = context.HostingEnvironment.IsDevelopment()
        ? LogLevel.Trace
        : LogLevel.Warning;

    logging.SetMinimumLevel(minimum);
});
```

Combine with `UseEnvironment(Environments.Development)` in debug builds to ensure `IsDevelopment()` returns true.

## Control XAML and layout logging

Tune verbosity for specific XAML categories to watch binding or layout issues without globally increasing noise.

```csharp
host.UseLogging(logging =>
{
    logging.XamlLogLevel(LogLevel.Information);
    logging.XamlLayoutLogLevel(LogLevel.Warning);
});
```

Use lower levels (e.g., `Trace`) when diagnosing rendering problems, and raise them back once resolved.

## Verify internal logs

Run the app under the debugger and open the Output window (or platform console) to confirm Uno categories are emitting messages. Adjust levels iteratively until the output shows the detail you need.

## Resources

- [Add structured logging](xref:Uno.Extensions.Logging.UseLogging)
- [Logging overview](xref:Uno.Extensions.Logging.Overview)
- [Microsoft logging levels](https://learn.microsoft.com/dotnet/core/extensions/logging)
