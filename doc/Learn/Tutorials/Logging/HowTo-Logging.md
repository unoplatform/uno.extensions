---
uid: Learn.Tutorials.Logging.UseLogging
---
# How-To: Enable and Use Logging

`Uno.Extensions.Logging` allows you to leverage logging capabilities tailored to your target platform to easily record events for XAML layout, Uno-internal messages, and custom events with severity and verbosity levels of your choice.

## Step-by-steps

> [!IMPORTANT]
> This guide assumes you used the template wizard or `dotnet new unoapp` to create your solution. If not, it is recommended that you follow the [instructions](xref:Overview.Extensions) for creating an application from the template.

### 1. Opt into logging

* Uno.Extensions offers a simple way to wire up platform-specific log providers such as `Uno.Extensions.Logging.OSLogLoggerProvider` for iOS and `Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider` for WASM as both debug and console logging. 

* Call the `UseLogging()` method to register the resultant implementation of `ILogger` with the DI container:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => {
                host.UseLogging();
            });
    ...
    ```

### 2. Use the injected service to log application events

* Add a constructor parameter of `ILogger` type to a view model you registered with the service collection:

    ```cs
    public class MainViewModel
    {
        private readonly ILogger logger;

        public MainViewModel(ILogger logger)
        {
            this.logger = logger;
        }
    ...
    ```

* You can now record application events using the injected `ILogger` service implementation:

    ```csharp
    logger.LogInformation("This is an information log.");
    ```