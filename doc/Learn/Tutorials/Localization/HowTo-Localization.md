# How-To: Configure and Use Localization

`Uno.Extensions.Localization` uses the locale-specific resources from `resw` files placed in folders corresponding to the well-known language tag (eg en-US). By opting into localization, an implementation of `IStringLocalizer` is registered with your application's `IServiceCollection`.

## Step-by-steps

### 1. Opt into localization

* Organize your application's localized `resw` resources into folders corresponding to a language tag

* Call the `UseLocalization()` method to register the implementation of `IStringLocalizer` with the DI container:

    ```csharp
    private IHost Host { get; }

    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseLocalization()
            .Build();
        // ........ //
    }
    ```

### 2. Use the localization service to resolve localized text

* Add a constructor parameter of `IStringLocalizer` type to a view model you registered with the service collection:

    ```cs
    public class MainViewModel
    {
        private readonly IStringLocalizer stringLocalizer;

        public MainViewModel(IStringLocalizer stringLocalizer)
        {
            this.stringLocalizer = stringLocalizer;
        }
    }
    ```

* You can then resolve the text either as a `string` or `LocalizedString`:

    ```csharp
    // Using IStringLocalizer as a dictionary
    string myString = stringLocalizer["MyKey"];

    // You can get a LocalizedString object too
    LocalizedString myString = stringLocalizer["MyKey"];
    var isResourceNotFound = myString.ResourceNotFound;
    ```

### 3. Update the UI culture with `LocalizationSettings`

* Add a constructor parameter of `IWritableOptions<LocalizationSettings>` type to a view model you registered with the service collection:

    ```cs
    public class MainViewModel
    {
        private readonly IWritableOptions<LocalizationSettings> localizationSettings;

        public MainViewModel(IWritableOptions<LocalizationSettings> localizationSettings)
        {
            this.localizationSettings = localizationSettings;
        }
    }
    ```

* Toggle the UI culture using the injected service:
    ```cs
    public void ToggleLocalization()
    {
        localizationSettings.Update(settings =>
        {
            settings.CurrentCulture = settings.CurrentCulture == "en-US" ? "fr-CA" : "en-US";
        });
    }
    ```

> [!TIP]
> This action requires an app restart before you're able to observe the changes
