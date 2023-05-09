---
uid: Learn.Tutorials.Logging.UseInternalLogging
---
# How-To: Enable Internal Logging Based on Your Hosting Environment

`Uno.Extensions.Logging` provides logging capabilities tailored to your target platform. It allows the recording of events for XAML layout, Uno-internal messages, and custom events with severity and verbosity levels of your choice.

> [!IMPORTANT] 
> This guide assumes your application has already opted into logging. To find out how to do this, refer to the tutorial [here](xref:Learn.Tutorials.Logging.UseLogging)

## Step-by-steps

### 1. Enable Uno internal logging

* To log Uno-internal messages, use `ConnectUnoLogging()` on the built `IHost` instance:

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

### 2. Set verbosity level of XAML logging

* There are multiple log levels that correspond to differing degrees of severity:

| Level | Description |
|-------|-------------|
| Trace | Used for parts of a method to capture a flow. |
| Debug | Used for diagnostics information. |
| Information | Used for general successful information. Generally the default minimum. |
| Warning | Used for anything that can potentially cause application oddities. Automatically recoverable. |
| Error | Used for anything that is fatal to the current operation but not to the whole process. Potentially recoverable. |
| Critical | Used for anything that is forcing a shutdown to prevent data loss or corruption. Not recoverable. |

* To increase the verbosity of the events recorded when using the development hosting environment, you can adjust the minimum levels as well as those for the XAML layout.

* Add a call for `UseLogging` to the `IHostBuilder` chain from above, and conditionally enable the recording of debug events depending on the hosting environment:

    ```csharp
    private IHost Host { get; }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => {
                host
                .UseLogging(configure:
                    (context, services) =>
                        services.SetMinimumLevel(
                        context.HostingEnvironment.IsDevelopment() ?
                            LogLevel.Trace :
                            LogLevel.Error)
                        )
            });

        Host = appBuilder.Build();

    #if DEBUG
        Host.UseEnvironment(Environments.Development);
    #endif

        Host.ConnectUnoLogging();
    ...
    ```
