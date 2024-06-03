---
uid: Uno.Extensions.ThemeService.Overview
---

# How to use Theme Service

This topic explains how to use the `ThemeService` for runtime theme switching and persisting user theme preferences.

## Step-by-steps

1. **Register ThemeService**:
    Add the `ThemeService` to your project's host builder configuration.

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

2. **Consume ThemeService**:
    Inject the ThemeService into your view models or other services where you need to manipulate the theme.

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

## Source Code

[ThemeService Implementation](https://github.com/unoplatform/uno.extensions/blob/51c9c1ef14f686363f946588733faecc5a1863ff/src/Uno.Extensions.Core.UI/Toolkit/ThemeService.cs)
