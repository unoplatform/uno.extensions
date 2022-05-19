# How-To: Enable Internal Logging Based on Your Hosting Environment

`Uno.Extensions.Logging` provides logging capabilities tailored to your target platform. It allows the recording of events for XAML layout, Uno-internal messages, and custom events with severity and verbosity levels of your choice.

> [!Tip] 
> This guide assumes your application has already opted into logging. To find out how to do this, refer to the tutorial [here](./HowTo-Logging.md)

## Step-by-steps

### 1. Enable Uno internal logging

* To log Uno-internal messages, invoke `EnableUnoLogging` on the `IHost` instance:

    ```csharp
    private IHost Host { get; }

    public App()
    {
        bool isDevelopment = false;

        Host = UnoHost
            .CreateDefaultBuilder()
            .UseLogging()
            .ConfigureServices((context, services) => {
                isDevelopment = context.HostingEnvironment.IsDevelopment();
            })
            .Build();

        if (isDevelopment)
        {
            Host.EnableUnoLogging();
        }
    }
    ```

### 2. Set verbosity level of XAML logging

* There are multiple log levels that correspond to differing degrees of severity:

  - **Trace** : Used for parts of a method to capture a flow.
  - **Debug** : Used for diagnostics information.
  - **Information** : Used for general successful information. _Generally the default minimum._
  - **Warning** : Used for anything causing application oddities. It is automatically recoverable.
  - **Error** : Used for anything fatal to the current operation but not to the whole process. It is potentially recoverable.
  - **Critical** : Used for anything forcing a shutdown to prevent data loss or corruption. It is not recoverable.

* To increase the verbosity of the events recorded when using the development hosting environment, you can tweak the minimum levels as well as those for the XAML layout.

* Add a call for `ConfigureLogging` to the `IHostBuilder` chain from above, and use the same technique to conditionally enable the recording of debug events depending on the hosting environment:

    ```csharp
    private IHost Host { get; }

    public App()
    {
        bool isDevelopment = false;

        Host = UnoHost
            .CreateDefaultBuilder()
            .UseLogging()
            .ConfigureServices((context, services) => {
                isDevelopment = context.HostingEnvironment.IsDevelopment();
            })
            .ConfigureLogging(logBuilder =>
            {
                if (isDevelopment)
                {
                    logBuilder
                        .SetMinimumLevel(LogLevel.Debug)
                        .XamlLogLevel(LogLevel.Debug)
                        .XamlLayoutLogLevel(LogLevel.Debug);
                }
            })
            .Build();

        if (isDevelopment)
        {
            Host.EnableUnoLogging();
        }
    }
    ```