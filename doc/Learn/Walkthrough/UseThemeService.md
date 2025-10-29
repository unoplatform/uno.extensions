---
uid: Uno.Extensions.ThemeService.UseThemeService
title: Switch Themes at Runtime
tags: [theme, ui, personalization]
---
# Switch themes at runtime and persist preferences

Use `IThemeService` to toggle between light and dark themes, persist the choice, and optionally register the service manually when you are not using Uno’s dependency injection.

## Verify registration

`IThemeService` is registered automatically when you enable the `Extensions` Uno feature (which adds DI). You only need manual registration if you are running without the standard host pipeline.

## Toggle themes from a view model

Inject `IThemeService` and call `SetThemeAsync` to flip between light and dark mode.

```csharp
public class SettingsViewModel
{
    private readonly IThemeService _themeService;

    public SettingsViewModel(IThemeService themeService) => _themeService = themeService;

    public async Task ToggleThemeAsync()
    {
        var next = _themeService.Theme == AppTheme.Dark
            ? AppTheme.Light
            : AppTheme.Dark;

        await _themeService.SetThemeAsync(next);
    }
}
```

The service updates the current window theme and stores the preference so it persists across sessions.

## Manually register the theme service

If DI is not enabled, add the service explicitly during host configuration.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseThemeSwitching();
        });
}
```

With manual registration in place you can inject `IThemeService` the same way as above.

## Resources

- [ThemeService source](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Core.UI/Toolkit/ThemeService.cs)
- [Extensions feature overview](xref:Uno.Extensions.Overview)
