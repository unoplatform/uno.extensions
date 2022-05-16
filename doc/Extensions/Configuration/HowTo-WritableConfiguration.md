# How-To: Writable Configuration

Writable configuration offers a method to read and update configuration data from your application's settings file(s). You can easily register new sections with the `IHostBuilder` to add hierarchy in the written data. The application is able to add or modify a setting value and have the information written to a corresponding key-value pair in a JSON file. Notably, this pattern also separates the written configuration, honoring your current hosting environment.

## Step-by-steps

### 1. Registering the configuration section
* Regardless of whether it already exists in the configuration file(s), register a section to delimit a group of related settings:
    ```csharp
    private IHost Host { get; }

    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseSettings<DiagnosticSettings>()
            .Build();
        // ........ //
    }
    ```

### 2. Modifying the value of a setting
* In your registered view model, call `Update` on the injected `IWritableOptions` instance
    ```csharp
    public MainViewModel(IWritableOptions<DiagnosticSettings> debug)
    {
        debug.Update(settings =>
        {
            debug.HasBeenLaunched = true;
        });
    }
    ```