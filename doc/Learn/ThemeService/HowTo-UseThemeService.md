---
uid: Uno.Extensions.ThemeService.Overview
---
<!--markdownlint-disable MD051 -->
# How to use Theme Service

This topic explains how to use the `ThemeService` for runtime theme switching and persisting user theme preferences.

## Automatic Registration with DI

> [!NOTE]
> The `ThemeService` is automatically registered when you enable DI (Dependency Injection) or Extensions UnoFeature in your Uno Platform project. This means you typically *do not* need to explicitly register it using `UseThemeSwitching` unless you are not using DI.

## Step-by-step (Typical Usage with DI)

### [MVVM](#tab/mvvm)

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

### [MVUX](#tab/mvux)

1. **Consume ThemeService**: Inject the `ThemeService` into your models or other services where you need to manipulate the theme.
2. **Bind to View**: Use a State or Command to bind your UI in your XAML. <!-- TODO: Add Links to each of the tabs -->

   ### [Model](#tab/mvux/csharp)

    In case you want to use it together with a State, e.g. to bind a control to the current theme trigger a Task with each Time, the Value changes, here is a simple example:

    ```cssharp
    namespace UnoAppName.Presentation.ViewModels;
    public partial record MainModel
    {
        private readonly IThemeService _themeService;

        public MainModel(
            IThemeService themeService)
        {
            _themeService = themeService;
        }

    public IState<bool> IsDarkMode => State<bool>.Value(this, () => _themeService.Theme == AppTheme.Dark)
                                                 .ForEach(SwitchThemeAsync);

    public async ValueTask SwitchThemeAsync(bool item,CancellationToken ctk = default)
    {

        _ = item switch
        {
            true => await _themeService.SetThemeAsync(AppTheme.Light),
            false => await _themeService.SetThemeAsync(AppTheme.Dark)
        };
    }
    ```

   ### [View](#tab/mvux/xaml)

    With this, you can bind the `IsDarkMode` state to a toggle switch in your XAML:

    ```xml
    <ToggleSwitch x:Name="ThemeSwitch"
                Grid.Row="1"
                HorizontalAlignment="Right"
                VerticalAlignment="Top"
                Margin="0,0,10,0"
                IsOn="{Binding Path=IsDarkMode, Mode=TwoWay}"
                Foreground="{ThemeResource OnPrimaryContainerBrush}"
                Style="{ThemeResource ToggleSwitchStyle}">
    <ToggleSwitch.OnContent>
        <FontIcon Glyph="&#xE708;"/>
    </ToggleSwitch.OnContent>
    <ToggleSwitch.OffContent>
        <FontIcon Glyph="&#xE706;"/>
    </ToggleSwitch.OffContent>
    </ToggleSwitch>
    ```

    <!-- TODO: Check if IsOn can get changed to CommandBinding Extensions usage of #1391 #1309 #1310 -->

---

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

[!code-csharp [ThemeService Implementation](https://github.com/unoplatform/uno.extensions/blob/51c9c1ef14f686363f946588733faecc5a1863ff/src/Uno.Extensions.Core.UI/Toolkit/ThemeService.cs)]
