---
uid: Uno.Extensions.Localization.Localization.HowTo
title: Localize Your App
tags: [localization, globalization, string-localizer]
---

> **UnoFeatures:** `Localization` (add to `<UnoFeatures>` in your `.csproj`)

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

## Use x:Uid in XAML

When localization is enabled, user-facing text should use `x:Uid` attributes to reference localized strings.

### Example: LoginPage.xaml

```xml
<StackPanel Spacing="16">
    <TextBlock x:Uid="LoginPage.TextBlock.WelcomeBack"
               Style="{StaticResource TitleLargeTextBlockStyle}"
               HorizontalAlignment="Center" />
    
    <TextBlock x:Uid="LoginPage.TextBlock.SignInPrompt"
               Style="{StaticResource BodyMediumTextBlockStyle}"
               Opacity="0.7" />
    
    <TextBox x:Uid="LoginPage.TextBox.Email" />
    
    <PasswordBox x:Uid="LoginPage.PasswordBox.Password" />
    
    <Button x:Uid="LoginPage.Button.Login"
            Command="{Binding LoginCommand}" />
</StackPanel>
```

### Create Resources.resw entries

In `Strings/en-US/Resources.resw`:

| Name | Value |
|------|-------|
| `LoginPage.TextBlock.WelcomeBack.Text` | Welcome Back |
| `LoginPage.TextBlock.SignInPrompt.Text` | Sign in to continue |
| `LoginPage.TextBox.Email.PlaceholderText` | Email |
| `LoginPage.TextBox.Email.Header` | Email Address |
| `LoginPage.PasswordBox.Password.PlaceholderText` | Password |
| `LoginPage.PasswordBox.Password.Header` | Password |
| `LoginPage.Button.Login.Content` | Login |

In `Strings/fr-FR/Resources.resw`:

| Name | Value |
|------|-------|
| `LoginPage.TextBlock.WelcomeBack.Text` | Bon Retour |
| `LoginPage.TextBlock.SignInPrompt.Text` | Connectez-vous pour continuer |
| `LoginPage.TextBox.Email.PlaceholderText` | E-mail |
| `LoginPage.TextBox.Email.Header` | Adresse E-mail |
| `LoginPage.PasswordBox.Password.PlaceholderText` | Mot de passe |
| `LoginPage.PasswordBox.Password.Header` | Mot de passe |
| `LoginPage.Button.Login.Content` | Connexion |

> [!NOTE]
> The recommended naming convention is `PageName.ElementType.Purpose` with the appropriate property suffix. Common property suffixes include `.Text` for TextBlock, `.Content` for Button and NavigationViewItem, and `.PlaceholderText` or `.Header` for TextBox and PasswordBox.

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
