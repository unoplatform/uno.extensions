---
uid: Uno.Extensions.Logging.Overview
---

# Logging

Apps that record events typically do so for informational or diagnostic purposes, depending on the desired level of verbosity. **Logging** is the process of recording events to either a _persistent_ store such as a text file or database, or a _transient_ location like the standard output console. The `Uno.Extensions.Logging` library leverages logging capabilities tailored to the target platform to easily write entries for both app-specific and Uno internal events to one or more locations. These locations where logs can be written are referred to as **providers**. This feature enables a simple way to wire up custom log providers for locations not included by the runtime libraries or extensions.

It uses [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) for logging abstractions. For more documentation about logging, read the references listed at the bottom.

## Installation

> [!NOTE]
> If you already have `Extensions` in `<UnoFeatures>`, then `Logging` is already installed, as its dependencies are included with the `Extensions` feature.

`Logging` is provided as an Uno Feature. To enable `Logging` support in your application, add `Logging` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

## Platform Log Providers

To wire-up the platform-specific log providers for debug and console logging, use the extension method `UseLogging()` on the `IHostBuilder` instance:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host.UseLogging();
        });
    ...
}
```

## Logging

When the `UseLogging()` extension method is called, an `ILogger<T>` is registered in the service provider. This logger can be used to log messages to the console. It can then be injected into any class that needs to log messages. The `ILogger<T>` interface is generic, where `T` is the class that is logging the message. The `ILogger<T>` interface is generic, and it must be retrieved from the service provider this way. In order to scope the logger to a specific class, it cannot be used without the generic type parameter.

The library offers a number of extension methods to simplify logging messages for a multitude of situations.

## Log Levels

Sometimes it is necessary to write information to the log that varies in its verbosity or severity. For this reason, there are six log levels available to delineate the severity of an entry.

Use the following convention as a guide when logging messages:

| Level | Description |
|-------|-------------|
| Trace | Used for parts of a method to capture a flow. |
| Debug | Used for diagnostics information. |
| Information | Used for general successful information. Generally the default minimum. |
| Warning | Used for anything that can potentially cause application oddities. Automatically recoverable. |
| Error | Used for anything that is fatal to the current operation but not to the whole process. Potentially recoverable. |
| Critical | Used for anything that is forcing a shutdown to prevent data loss or corruption. Not recoverable. |

_These descriptions are adapted from those documented for the `LogLevel` enum [here](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.loglevel)._

Because these extension methods are scoped to a specific log level, the standard `Log()` method on the `ILogger<T>` interface does not need to be used. This example shows how to log a message using the `LogInformation()` extension method:

```csharp
public class MyClass
{
    private readonly ILogger<MyClass> _logger;

    public MyClass(ILogger<MyClass> logger)
    {
        _logger = logger;
    }

    public void MyMethod()
    {
        _logger.LogInformation("My message");
    }
}
```

## Configuring Logging Output

The `UseLogging()` extension method accepts an optional `Action<ILoggingBuilder>` parameter that can be used to configure the logging output.

### Setting the Minimum Log Level

The minimum log level can be set by calling the `SetMinimumLevel()` extension method on the `ILoggingBuilder` instance. If no LogLevel is specified, logging defaults to the `Information` level which may not always be desirable. The following example shows how to change the minimum log level to `Debug`:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
#if DEBUG
            .UseEnvironment(Environments.Development)
#endif
            .UseLogging(configure:
                (context, services) =>
                    services.SetMinimumLevel(
                    context.HostingEnvironment.IsDevelopment() ?
                        LogLevel.Trace :
                        LogLevel.Error)
                    )
        });
    ...
}
```

This example configures the minimum log level to `Trace` when the app is running in the `Development` environment, and `Error` otherwise. This is useful for increasing the amount of logging that occurs in that environment, and only emitting `Error` or `Critical` messages when in production.

## Serilog

Serilog is a third-party logging framework that is otherwise available as a [NuGet package](https://www.nuget.org/packages/Serilog). The benefit of using Serilog is that it provides a more robust logging framework that can be configured to log to a variety of different sinks, including the console and file. Examples of other sinks include Azure Application Insights, Seq, and many more.

An option to use Serilog is available by calling the `UseSerilog()` extension method on the `IHostBuilder` instance. This method will configure the Serilog logger to specifically not use the console or file sink by default. The following example shows how to enable Serilog:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseSerilog();
        });
    ...
}
```

For more information about Serilog, check out [Getting Started with Serilog](https://github.com/serilog/serilog/wiki/Getting-Started).

## Uno Internal Logging

The same logging system is wired up for Uno internal messages when `UseLogging()`  is called on the `IHost` instance. This is useful for debugging Uno internals like XAML parsing and layout. While this system is enabled as part of the standard logger, it is set by default to filter out messages with a level lower than `Warning`. This is to reduce noise in the log output.

The following table describes the aspects of this Uno internal logging system which can be configured for a more suitable experience while diagnosing issues during the development process.

| ILoggingBuilder Extension method | Filtered Events  | Affected Namespace(s) |
|------------------|-------------|---------------------|
| `XamlLogLevel()` | Messages emitted by specific XAML types | `Microsoft.UI.Xaml`, `Microsoft.UI.Xaml.VisualStateGroup`, `Microsoft.UI.Xaml.StateTriggerBase`, `Microsoft.UI.Xaml.UIElement`, `Microsoft.UI.Xaml.FrameworkElement` |
| `XamlLayoutLogLevel()` | Messages emitted by the layout system | `Microsoft.UI.Xaml.Controls`, `Microsoft.UI.Xaml.Controls.Layouter`, `Microsoft.UI.Xaml.Controls.Panel` |

## See also

- [Understanding logging providers](https://learn.microsoft.com/aspnet/core/fundamentals/logging/)
- [Getting started with Serilog](https://github.com/serilog/serilog/wiki/Getting-Started)
- [List of Serilog sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks)
