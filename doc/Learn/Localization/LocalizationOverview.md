---
uid: Uno.Extensions.Localization.Overview
---

# Localization

It is often necessary to adapt an application to a specific subset of users within a market. **Localization** includes the type of actions developers take to modify both user interface elements and content to adhere to more languages or cultures. Specifically, text **translation** is done by applying alternate strings of text at runtime which accommodate a user's language preference.

Many apps store these pieces of text in dedicated resource files that the app parses and assigns as text throughout the application. `Uno.Extensions.Localization` provides a consistent way to resolve the text of a specific culture or locale across platforms. This feature allows for modifying them to be applied upon app restart.

It uses [Microsoft.Extensions.Localization](https://www.nuget.org/packages/Microsoft.Extensions.Localization) for any localization-related work. For documentation on the broader process of localization, read the references listed at the bottom.

## Installation

`Localization` is provided as an Uno Feature. To enable `Localization` support in your application, add `Localization` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

## Set up localization

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseLocalization()
        });
    ...
}
```

An implementation of `IStringLocalizer` (`ResourceLoaderStringLocalizer`) will be registered as a service. This service offers a consistent way to resolve localized strings. Behind the scenes, it will automatically use `ResourceManager` on Windows and `ResourceLoader` on other platforms.

### Adding language-specific resources

The `ResourceLoaderStringLocalizer` will look for `.resw` files in folders corresponding to the well-known language tag (eg en-US). For example, if the current culture is `en-US`, the `ResourceLoaderStringLocalizer` will look for `.resw` files in the `en-US` folder. If the current culture is `fr-FR`, the `ResourceLoaderStringLocalizer` will look for `.resw` files in the `fr-FR` folder.

#### Planning to support different locales

The cultures which the app will support are enumerated in a specific section of the `appsettings.json` configuration file. The `LocalizationConfiguration` section of the file should look like the code example below:

```json
{
  "LocalizationConfiguration": {
    "Cultures": [ "fr", "en" ]
  },
  ...
}
```

#### Add resource files

To add a new resource file, right-click on the project and select **Add > New Item...**. Select **Resource File (.resw)** and name it `Resources.resw`. Resource files have a key-value pair structure. The key is used to identify the resource, and the value can represent any valid property value such as translated text, the width of an item, or a color.

### Resolving localized strings

Once local-specific resources are included, the localization feature can be used to resolve those localized values.

#### Using resources in XAML

The key contains a name that corresponds to the `x:Uid` and the intended property of the XAML element. The value contains the localized text.

For example, if the `x:Uid` property of a `TextBlock` is `MyTextBlock`, the key in the resource file should be `MyTextBlock.Text`. In XAML, assigning a localized value to an element with an `x:Uid` property looks like this:

```xml
<TextBlock x:Uid="MyTextBlock" />
```

#### Using resources in code-behind

Setting the `x:Uid` property in markup is not required to resolve localized resources like text. The `IStringLocalizer` service can be resolved by the service provider. This service can be used to resolve localized strings in code behind.

```csharp
var stringLocalizer = serviceProvider.GetService<IStringLocalizer>();
```

Strings can be resolved using the indexer on the `IStringLocalizer` as a dictionary. This indexer takes a key and returns the localized string value.

```csharp
string myString = stringLocalizer["MyKey"];
```

`LocalizedString` objects are the primary type resolved from `IStringLocalizer`. While these objects can be implicitly converted to strings, they also contain additional information which may be desirable. For instance, `LocalizedString` includes a boolean indicating whether the resource was not found. This can be used to determine whether a fallback value should be used.

```csharp
LocalizedString myString = stringLocalizer["MyKey"];
var isResourceNotFound = myString.ResourceNotFound;
```

## UI Culture

Current culture or locale can be changed using `ILocalizationService`. This action requires an app restart.

```csharp
public class MainViewModel
{
    private readonly ILocalizationService localizationService;

    public MainViewModel(ILocalizationService localizationService)
    {
        this.localizationService = localizationService;
    }

    public Task ToggleLocalizationAsync()
    {
        var currentCulture = localizationService.CurrentCulture;
        var culture = localizationService.SupportedCultures.First(culture => culture.Name != currentCulture.Name);

        return localizationService.SetCurrentCultureAsync(culture);
    }
}
```

## See also

- [Software Localization](https://learn.microsoft.com/globalization/localization/localization)
- [Localization](https://learn.microsoft.com/dotnet/core/extensions/localization)
- [Using IStringLocalizer](https://learn.microsoft.com/aspnet/core/fundamentals/localization)
