---
uid: Uno.Extensions.Configuration.HowToWritableConfiguration
---

# How-To: Writable Configuration

**Writable Configuration** extends the standard, [read-only](xref:Uno.Extensions.Configuration.HowToConfiguration) pattern by allowing for programmatic writing of configuration values at runtime. This is useful for scenarios where you want to persist user preferences or other trivial information that may be changed over time. `Uno.Extensions.Configuration` extends the `IOptionsSnapshot<T>` interface from [Microsoft.Extensions.Options](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options) to support this.

A special interface called `IWritableOptions<T>` is registered as a service when you use the `UseConfiguration()` extension method. In this tutorial, we will walk through how to use the `UpdateAsync()` method on this interface to modify values of a specific configuration section. For a refresher on configuration sections, see [Sections](xref:Uno.Extensions.Configuration.Overview#sections).

> [!NOTE]
> It is common to see this referred to as the _settings_ pattern in certain documentation. This is because the `IOptions<T>` interface is often used to represent settings that can be changed by the user.

## Step-by-step

### 1. Prepare for writing configuration values

* Add `Configuration` to the `<UnoFeatures>` property in the Class Library (.csproj) file. If you already have `Configuration` in `<UnoFeatures>`, then `Configuration` is already installed, as its dependencies are included with the `Extensions` feature.

    ```diff
    <UnoFeatures>
        Material;
    +   Configuration;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

* To enable configuration, you first need to call `UseConfiguration()` on the `IHostBuilder` instance:

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => {
                host
                .UseConfiguration()
            });

        Host = appBuilder.Build();
        ...
    }
    ```

* Use the `EmbeddedSource<T>()` extension method to load configuration information from a JSON file called `appsettings.json` embedded inside the `App` assembly:

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => {
                host
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                );
            });

        Host = appBuilder.Build();
        ...
    }
    ```

### 2. Define a configuration section

* To model the configuration section you want to write to, author a new class or record with related properties:

    ```csharp
    public partial record ToDoApp
    {
        public bool? IsDark { get; init; }
        public string? LastTaskList { get; init; }
    }
    ```

* For instance, the `IsDark` property could be used to toggle between light and dark themes, while `LastTaskList` could be used to persist the last task list the user was viewing.

* Register the newly-defined configuration section by calling `Section<T>()` on `IConfigBuilder`:

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => {
                host
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<ToDoApp>()
                );
            });

        Host = appBuilder.Build();
        ...
    }
    ```

### 3. Write configuration values

* From any view model registered with the dependency injection (DI) container, you can now inject an instance of `IWritableOptions<T>` to write configuration values:

    ```csharp
    public class SettingsViewModel
    {
        private readonly IWritableOptions<ToDoApp> _appSettings;

        public SettingsViewModel(IWritableOptions<ToDoApp> appSettings)
        {
            _appSettings = appSettings;
        }
        ...
    }
    ```

* To update the `IsDark` property, create a method that calls `UpdateAsync()` on the injected instance:

    ```csharp
    public class SettingsViewModel
    {
        private readonly IWritableOptions<ToDoApp> _appSettings;

        public SettingsViewModel(IWritableOptions<ToDoApp> appSettings)
        {
            _appSettings = appSettings;
        }

        public async Task ToggleTheme()
        {
            await _appSettings.UpdateAsync(settings => settings with {
                IsDark = !settings.IsDark
            });
        }
        ...
    }
    ```

* Note that the `with` expression is used to create a new instance of the `ToDoApp` class with the updated value. This is because the `UpdateAsync()` method expects a function that returns a new instance of the class.

* The configuration section that was registered is not required to exist in any source beforehand. Sections like these will be created automatically when you call `UpdateAsync()`.

## See also

* [Configuration](xref:Uno.Extensions.Configuration.Overview)
* [Microsoft.Extensions.Configuration](https://docs.microsoft.com/dotnet/api/microsoft.extensions.configuration)
* [`IOptionsSnapshot<T>`](https://docs.microsoft.com/dotnet/api/microsoft.extensions.options.ioptionssnapshot-1)
