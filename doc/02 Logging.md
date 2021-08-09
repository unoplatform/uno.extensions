# Logging
_[TBD - Review and update this guidance]_

We use [Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) for logging abstractions.

For more documentation on logging, read the references listed at the bottom.

## Log levels

We use the following convention for log levels:

  - **Trace** : Used for parts of a method to capture a flow.
  - **Debug** : Used for diagnostics information.
  - **Information** : Used for general successful information. Generally the default minimum.
  - **Warning** : Used for anything that can potentially cause application oddities. Automatically recoverable.
  - **Error** : Used for anything that is fatal to the current operation but not to the whole process. Potentially recoverable.
  - **Critical** : Used for anything that is forcing a shutdown to prevent data loss or corruption. Not recoverable.

## Log providers

We use [Serilog](https://www.nuget.org/packages/Serilog/) to implement log providers (called sinks).
  - We use [Serilog.Sinks.Xamarin](https://www.nuget.org/packages/Serilog.Sinks.Xamarin/) for native console logging (not supported by [Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console/)).
  - We use [Serilog.Sinks.File](https://www.nuget.org/packages/Serilog.Sinks.File) for file logging.
  - We use [Serilog.Settings.Configuration](https://www.nuget.org/packages/Serilog.Settings.Configuration) to load the log filters from [appsettings.json](../src/app/ApplicationTemplate.Shared/appsettings.json); _filters are different between environments_.
  - We use [Serilog.Extensions.Hosting](https://www.nuget.org/packages/Serilog.Extensions.Hosting/) to configure the loggers with `GenericHost`.

The loggers are configured inside the [Startup.cs](../src/app/ApplicationTemplate.Shared/Startup.cs) file.

## Logging

To log, you simply need to get a `ILogger` and use the appropriate methods.

```csharp
var myLogger = myServiceProvider.GetService<ILogger<MyType>>();

myLogger.LogInformation("This is an information log.");
```

## Diagnostics

Multiple logging features can be tested from the diagnostics screen. This is configured in [LoggersDiagnosticsViewModel](../src/app/ApplicationTemplate.Shared/Presentation/Diagnostics/LoggersDiagnosticsViewModel.cs) and [SummaryDiagnosticsViewModel](../src/app/ApplicationTemplate.Shared/Presentation/Diagnostics/SummaryDiagnosticsViewModel.cs)..

- You can test the different log levels / providers. 
- You can enable / disable console logging.
- You can enable / disable file logging.
- You can see if a log file exists.
- You can share the logs / app summary by email, etc.

## References
- [Understanding logging providers](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.0)
- [Getting started with Serilog](https://github.com/serilog/serilog/wiki/Getting-Started)
- [List of Serilog sinks](https://github.com/serilog/serilog/wiki/Provided-Sinks)
