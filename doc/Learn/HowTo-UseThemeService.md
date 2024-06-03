---
uid: Uno.Extensions.ThemeService
---

# How to use Theme Service

The Uno Extensions library includes a `ThemeService` that facilitates dynamic theme switching across your Uno Platform applications. This service is especially useful for applications requiring runtime theme changes and for persisting user theme preferences.

## Registering ThemeService

To utilize the `ThemeService` in your application, you must register it during the app startup phase. Here’s how to set up the `ThemeService`:

### App Startup Configuration

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
            var currentTheme = _themeService.CurrentTheme;
            var newTheme = currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
            await _themeService.SetThemeAsync(newTheme);
        }
    }
    ```
## Source Code

[ThemeService Implementation](https://github.com/unoplatform/uno.extensions/blob/51c9c1ef14f686363f946588733faecc5a1863ff/src/Uno.Extensions.Core.UI/Toolkit/ThemeService.cs)
