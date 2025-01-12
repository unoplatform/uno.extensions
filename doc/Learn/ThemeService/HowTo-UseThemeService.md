---
uid: Uno.Extensions.ThemeService.Overview
---
# How to use Theme Service

This topic explains how to use the `ThemeService` for runtime theme switching and persisting user theme preferences.

## Automatic Registration with DI

> [!NOTE]
> The `ThemeService` is automatically registered when you enable DI (Dependency Injection) or Extensions UnoFeature in your Uno Platform project. This means you typically *do not* need to explicitly register it using `UseThemeSwitching` unless you are not using DI.

## Step-by-step (Typical Usage with DI)

1. **Consume ThemeService**: Inject the `ThemeService` into your view models or other services where you need to manipulate the theme.

    ```csharp
    public class SettingsViewModel
    {
        private readonly IThemeService _themeService;
        
        public SettingsViewModel(IThemeService themeService)
        {
            _themeService = themeService;
        }

        public async Task ToggleThemeAsync()
        {
            var currentTheme = _themeService.Theme;
            var newTheme = currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            await _themeService.SetThemeAsync(newTheme);
        }
    }
    ```

## Step-by-step (Manual Registration without DI or Advanced Scenarios)

If you are *not* using DI, Extensions, or require more control over the registration process, you can manually register the `ThemeService`:

1. When using the Uno.Sdk, follow this guide on how to add `ThemeService` [UnoFeature](xref:Uno.Features.Uno.Sdk#managing-the-unosdk-version).

1. **Register ThemeService**: Add the `ThemeService` to your project's host builder configuration using `UseThemeSwitching`.

    ```csharp
    public partial class App : Application
    {
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var builder = this.CreateBuilder(args)
                .Configure(host => host
                    .UseThemeSwitching()
                );
        }
    }
    ```

1. **Consume ThemeService:** (Same as the DI usage) Inject the `ThemeService` as shown in the previous example.

## Source Code

[ThemeService Implementation](https://github.com/unoplatform/uno.extensions/blob/51c9c1ef14f686363f946588733faecc5a1863ff/src/Uno.Extensions.Core.UI/Toolkit/ThemeService.cs)
