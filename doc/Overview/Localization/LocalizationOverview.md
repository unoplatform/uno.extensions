---
uid: Overview.Localization
---

# Localization

It is often necessary to adapt an application to a specific subset of users within a market. **Localization** includes the type of actions developers take to modify both user interface elements and content to adhere to more languages or cultures. Specifically, text **translation** is done by applying alternate strings of text at runtime which accomodate a user's language preference. Many apps store these pieces of text in dedicated resource files that the app parses and assigns as text throughout the application. `Uno.Extensions.Localization` provides a consistent way to resolve text of a specific culture or locale across platforms. This feature allows for modifying them to be applied upon app restart.

It uses [Microsoft.Extensions.Localization](https://www.nuget.org/packages/Microsoft.Extensions.Localization) for any localization related work. For documentation on the broader process of localization, read the references listed at the bottom.

## Resolving translated strings

Once locale specific resources are included in `.resw` files corresponding to the desired locale folder (eg en-US), the localization feature can be used to resolve those localized texts.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => 
        {
            host
            .UseLocalization()
        });
...
```

An implementation of `IStringLocalizer` (`ResourceLoaderStringLocalizer`) will be registered as a service. This service offers a consistent way to resolve localized strings. Behind the scenes, it will automatically use `ResourceManager` on Windows and `ResourceLoader` on other platforms.

```csharp
var stringLocalizer = serviceProvider.GetService<IStringLocalizer>();
```

Localized strings can be resolved using the indexer on the IStringLocalizer as a dictionary. This indexer takes a key and returns the localized string.

```csharp
string myString = stringLocalizer["MyKey"];
```

LocalizedString objects can also be resolved from the IStringLocalizer. These objects contain the localized string and a boolean indicating whether the resource was found.

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