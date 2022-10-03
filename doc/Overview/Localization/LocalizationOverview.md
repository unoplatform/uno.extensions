---
uid: Overview.Localization
---
# Localization
Uno.Extensions.Localization uses [Microsoft.Extensions.Localization](https://www.nuget.org/packages/Microsoft.Extensions.Localization) for any localization related work.

For more documentation on localization, read the references listed at the bottom.

## Text localization

Save locale specific resources in `.resw` files in folder corresponding to locale (eg en-US).

An implementation of `IStringLocalizer` (`ResourceLoaderStringLocalizer`) is registered as service.

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

We use `IStringLocalizer` to resolve those localized texts

```csharp
var stringLocalizer = serviceProvider.GetService<IStringLocalizer>();

// Using IStringLocalizer as a dictionary
string myString = stringLocalizer["MyKey"];

// You can get a LocalizedString object too
LocalizedString myString = stringLocalizer["MyKey"];
var isResourceNotFound = myString.ResourceNotFound;
```

## UI Culture

Current culture/locale can be changed using the ILocalizationService. This requires an app restart. 


```csharp
public class MainViewModel
{
    private readonly ILocalizationService localizationService;

    public MainViewModel(ILocalizationService localizationService)
    {
        this.localizationService = localizationService;
    } 
    
    public async Task ToggleLocalization()
    {
        var currentCulture = localizationService.CurrentCulture;
        
        var culture = localizationService.SupportedCultures.First(culture => culture.Name != currentCulture.Name);
        await localizationService.UpdateCurrentCulture(culture);
    }
}
```

## References

- [Using IStringLocalizer](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-3.1)