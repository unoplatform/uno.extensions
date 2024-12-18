---
uid: Uno.Extensions.Logging.UseInternalLogging
---
# How-To: Enable Internal Logging Based on Your Hosting Environment

`Uno.Extensions.Logging` provides logging capabilities tailored to your target platform. It allows the recording of events for XAML layout, Uno-internal messages, and custom events with severity and verbosity levels of your choice.

## Step-by-step

> [!IMPORTANT]
> This guide assumes your application has already opted into logging. To find out how to do this, refer to the tutorial [here](xref:Uno.Extensions.Logging.UseLogging)

### 1. Enable Uno internal logging

* Add `Logging` to the `<UnoFeatures>` property in the Class Library (.csproj) file. If you already have `Extensions` in `<UnoFeatures>`, then `Logging` is already installed, as its dependencies are included with the `Extensions` feature.

    ```diff
    <UnoFeatures>
        Material;
    +   Logging;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

* To log Uno-internal messages, you first need to call `UseLogging()` on the `IHost` instance, passing `true` in for the enableUnoLogging parameter:

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => {
                host
                .UseLogging(enableUnoLogging: true)
            });

        Host = appBuilder.Build();
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

* Conditionally enable the recording of debug events depending on the hosting environment:

    ```csharp
    private IHost Host { get; set; }

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

        Host = appBuilder.Build();
    ...
    ```

### 3. Enable specific types of internal logging

#### Setting the XAML Log Level

* Filter out messages recorded for specific XAML types by setting the **XAML log level**.

* To adjust the verbosity of logged events raised by a set of XAML-related types, you should call the `XamlLogLevel()` extension method on the `ILoggingBuilder` instance. The following example shows how to set the XAML log level to `Information`:

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

#### Setting the Layout Log Level

* The **layout log level** can be used to filter messages recorded from a set of layout-related types.

* To set this up, call the `XamlLayoutLogLevel()` extension method on the `ILoggingBuilder` instance. The following example shows how to set the XAML layout log level to `Information`:

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
    }
    ```

### 4. Testing the logging configuration

* Run the application in debug mode and open the **Output** window in Visual Studio.

* Since you have configured Uno-internal logging, messages with a severity level of `Information` or higher will now be recorded for the specified categories.
