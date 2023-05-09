---
uid: Overview.Logging
---

# Logging

Apps that record events typically do so for informational or diagnostic purposes. **Logging** is the process of recording events to either a _persistent_ store such as a text file or database, or a _transient_ location like the standard output console. The `Uno.Extensions.Logging` library leverages logging capabilities tailored to the target platform to easily record events for XAML layout, Uno-internal messages, and custom events. It allows for control over specific severity or verbosity levels. This feature also allows for a simple way to wire up custom log providers. It uses [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) for logging abstractions.

For more documentation about logging, read the references listed at the bottom.

## Platform Log Providers

This library comes with two platform specific log providers:

  - **Uno.Extensions.Logging.OSLogLoggerProvider** : Used for iOS
  - **Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider** : Used for WebAssembly (WASM)

To wire up platform specific log providers for debug and console logging, use the extension method `UseLogging()` on the `IHostBuilder` instance:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host.UseLogging();
        });
...
```

## Logging

When the `UseLogging()` extension method is called, an `ILogger<T>` is registered in the service provider. This logger can be used to log messages to the console. It can then be injected into any class that needs to log messages. The `ILogger<T>` interface is generic, where `T` is the class that is logging the message. While the `ILogger<T>` interface is generic, it is not required to be used in this way. It can be used without the generic type parameter, but it is recommended to use the generic type parameter to scope the logger to a specific class.

The library offers a number of extension methods to simplify the logging of messages for a specific log level. The following example shows how to log a message using the `LogInformation()` extension method:

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

Because these extension methods are scoped to a specific log level, the standard `Log()` method on the `ILogger<T>` interface does not need to be used.

## Configuring Logging Output

The `UseLogging()` extension method accepts an optional `Action<ILoggingBuilder>` parameter that can be used to configure the logging output.

### Setting the Minimum Log Level

The minimum log level can be set by calling the `SetMinimumLevel()` extension method on the `ILoggingBuilder` instance. If no LogLevel is specified, logging defaults to the `Information` level which may not always be desirable. The following example shows how to change the minimum log level to `Debug`:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host.UseLogging(
                builder => {
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
        });
...
```

### Setting the XAML Log Level

Sometimes it's necessary to filter messages recorded for specific XAML types to reduce noise. When a preference is specified for the **XAML log level**, the logger will change the verbosity of events from a set of specific XAML-related namespaces and types:
- Microsoft.UI.Xaml
- Microsoft.UI.Xaml.VisualStateGroup
- Microsoft.UI.Xaml.StateTriggerBase
- Microsoft.UI.Xaml.UIElement
- Microsoft.UI.Xaml.FrameworkElement

It can be set by calling the `XamlLogLevel()` extension method on the `ILoggingBuilder` instance. The following example shows how to set the XAML log level to `Information`:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host.UseLogging(
                builder => {
                    builder.XamlLogLevel(LogLevel.Information);
                });
        });
...
```

### Setting the Layout Log Level

Similar to the _XAML Log Level_ described above, the **layout log level** can be used to filter messages recorded for specific types to reduce noise. When a preference is specified for the layout log level, the logger will change the verbosity of events from a set of specific layout-related XAML namespaces and types:

- Microsoft.UI.Xaml.Controls
- Microsoft.UI.Xaml.Controls.Layouter
- Microsoft.UI.Xaml.Controls.Panel

It can be set by calling the `XamlLayoutLogLevel()` extension method on the `ILoggingBuilder` instance. The following example shows how to set the XAML layout log level to `Information`:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host.UseLogging(
                builder => {
                    builder.XamlLayoutLogLevel(LogLevel.Information);
                });
        });
...
```

## Serilog

Serilog is a third-party logging framework that is otherwise available as a [NuGet package](https://www.nuget.org/packages/Serilog). The benefit of using Serilog is that it provides a more robust logging framework that can be configured to log to a variety of different sinks, including the console and file. Examples of other sinks include Azure Application Insights, Seq, and many more.

An option to use Serilog is available by calling the `UseSerilog()` extension method on the `IHostBuilder` instance. This method will configure the Serilog logger to specifically not use the console or file sink by default. The following example shows how to enable Serilog:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseLogging()
            .UseSerilog();
        });
...
```

For more information about Serilog, check out [Getting started with Serilog](https://github.com/serilog/serilog/wiki/Getting-Started).

## Uno Internal Logging

To use the same logging system for Uno internal messages use `ConnectUnoLogging()` on the built `IHost` instance:

```csharp
private IHost Host { get; }

protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseLogging()
        });
    Host = appBuilder.Build();
    // Connect Uno internal logging to the same logging provider
    Host.ConnectUnoLogging();
...
```

## See also

- [Understanding logging providers](https://learn.microsoft.com/aspnet/core/fundamentals/logging/)
- [Getting started with Serilog](https://github.com/serilog/serilog/wiki/Getting-Started)
- [List of Serilog sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks)
