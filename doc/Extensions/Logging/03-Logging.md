# Logging
Uno.Extensions.Logging use [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) for logging abstractions.

For more documentation on logging, read the references listed at the bottom.


## Platform Log Providers

Wire up platform specific log providers (iOS: Uno.Extensions.Logging.OSLogLoggerProvider, WASM: Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider), Debug and Console logging.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseLogging()
        .Build();
    // ........ //
}
```

## Log levels

We use the following convention for log levels:

  - **Trace** : Used for parts of a method to capture a flow.
  - **Debug** : Used for diagnostics information.
  - **Information** : Used for general successful information. Generally the default minimum.
  - **Warning** : Used for anything that can potentially cause application oddities. Automatically recoverable.
  - **Error** : Used for anything that is fatal to the current operation but not to the whole process. Potentially recoverable.
  - **Critical** : Used for anything that is forcing a shutdown to prevent data loss or corruption. Not recoverable.

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseLogging()
        .ConfigureLogging(logBuilder =>
                {
                    logBuilder
                        .SetMinimumLevel(LogLevel.Debug)
                        .XamlLogLevel(LogLevel.Information)
                        .XamlLayoutLogLevel(LogLevel.Information);
                })
        .Build();
    // ........ //
}
```

## Serilog

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseLogging()
        .UseSerilog()
        .Build();
    // ........ //
}
```

## Logging

To log, you simply need to get a `ILogger` and use the appropriate methods.

```csharp
var myLogger = myServiceProvider.GetService<ILogger<MyType>>();

myLogger.LogInformation("This is an information log.");
```

Alternatively you can add `ILogger` as a constructor parameter for any type that's being created by the dependency injection container.


## Uno Internal Logging

To use the same logging for Uno internal messages call EnableUnoLogging after the Build method on the IHost instance. 

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseLogging()
        .Build()
        .EnableUnoLogging();
    // ........ //
}
```

## References
- [Understanding logging providers](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.0)
- [Getting started with Serilog](https://github.com/serilog/serilog/wiki/Getting-Started)
- [List of Serilog sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks)
