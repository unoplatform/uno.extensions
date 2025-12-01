---
uid: Uno.Extensions.Localization.Localization.HowTo
title: Localize Your App
tags: [localization, globalization, string-localizer]
---

> **UnoFeature:** Localization

# Localize strings and switch cultures at runtime

Register Uno localization, resolve localized strings through `IStringLocalizer`, and update the active culture from your view models.

## Enable localization

Add the `Localization` feature so `Uno.Extensions.Localization` is available.

```diff
<UnoFeatures>
    Material;
    Extensions;
+   Localization;
    Toolkit;
    MVUX;
</UnoFeatures>
```

Organize your `resw` files under language-tag folders (for example `Strings/en-US/Resources.resw`) so the resource manager can locate them.

## Register localization with the host

Call `UseLocalization` during host configuration.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseLocalization();
        });
}
```

This registers `IStringLocalizer` and `ILocalizationService` against the DI container.

## Resolve localized strings

Inject `IStringLocalizer` wherever you need localized text.

```csharp
public class MainViewModel
{
    private readonly IStringLocalizer _localizer;

    public MainViewModel(IStringLocalizer localizer) => _localizer = localizer;

    public string Title => _localizer["MainPage_Title"];
}
```

You can access additional metadata via `LocalizedString` when needed.

```csharp
var localized = _localizer["MainPage_Title"];
if (localized.ResourceNotFound)
{
    // fallback logic
}
```

## Switch cultures programmatically

Use `ILocalizationService` to enumerate supported cultures and set the active culture.

```csharp
public class SettingsViewModel
{
    private readonly ILocalizationService _localization;

    public SettingsViewModel(ILocalizationService localization) => _localization = localization;

    public async Task SetCultureAsync(string cultureName)
    {
        var target = _localization.SupportedCultures
            .FirstOrDefault(c => c.Name == cultureName);

        if (target is not null)
        {
            await _localization.SetCurrentCultureAsync(target);
        }
    }
}
```

> [!TIP]
> Most platforms require an app restart to display the new culture. Plan your UX accordingly (for example, show a prompt to restart).

## Resources

- [Localization overview](xref:Uno.Extensions.Localization.Overview)
- [Globalization and localization in .NET](https://learn.microsoft.com/dotnet/standard/globalization-localization/)
