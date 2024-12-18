---
uid: Uno.Extensions.Localization.HowToUseLocalization
---
# How-To: Configure and Use Localization

`Uno.Extensions.Localization` uses the locale-specific resources from `resw` files placed in folders corresponding to the well-known language tag (eg en-US). By opting into localization, an implementation of `IStringLocalizer` is registered with your application's `IServiceCollection`.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Installation

* Add `Localization` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
        Extensions;
    +   Localization;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

### 2. Opt into localization

* Organize your application's localized `resw` resources into folders corresponding to a language tag

* Call the `UseLocalization()` method to register the implementation of `IStringLocalizer` with the DI container:

    ```csharp
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(hostBuilder =>
            {
                hostBuilder.UseLocalization();
            });
        ...
    }
    ```

### 3. Use the localization service to resolve localized text

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

### 4. Update the UI culture with `LocalizationSettings`

* Add a constructor parameter of `ILocalizationService` type to a view model you registered with the service collection:

    ```cs
    public class MainViewModel
    {
        private readonly ILocalizationService localizationService;

        public MainViewModel(ILocalizationService localizationService)
        {
            this.localizationService = localizationService;
        }
    }
    ```

* Toggle the UI culture using the injected service:

    ```cs
    public async Task ToggleLocalization()
    {
        var currentCulture = localizationService.CurrentCulture;

        var culture = localizationService.SupportedCultures.First(culture => culture.Name != currentCulture.Name);
        await localizationService.SetCurrentCultureAsync(culture);
    }
    ```

> [!TIP]
> This action requires an app restart before you're able to observe the changes
