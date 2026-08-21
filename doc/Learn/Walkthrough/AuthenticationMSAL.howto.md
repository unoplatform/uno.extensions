---
uid: Uno.Extensions.Authentication.Msal.HowTo
title: Authenticate Users with MSAL
tags: [authentication, msal, navigation]
---

> **UnoFeatures:** `AuthenticationMsal` (add to `<UnoFeatures>` in your `.csproj`)

# Sign in with Microsoft account (MSAL)

This how-to shows how to let users sign in with their Microsoft identity in an Uno Platform app using **Uno.Extensions** and **MSAL**.

Requires the `AuthenticationMsal` UnoFeature

> [!TIP]
> **For comprehensive guides:**
>
> - See [Complete MSAL Setup Guide](xref:Uno.Extensions.Authentication.HowToMsalSetup) for detailed Azure AD configuration and all options
> - See [MSAL Troubleshooting Guide](xref:Uno.Extensions.Authentication.HowToMsalTroubleshooting) for common issues and solutions

## 1. Register the app in Entra ID

- Go to the [Microsoft identity platform](https://portal.azure.com/#view/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/~/RegisteredApps) and create an app registration.
- Copy the **Application (client) ID**.
- Add **Redirect URI**: `http://localhost` or `http://localhost:{port}`.
- Under **Authentication**, configure platform as **Mobile and desktop applications**.
- Enable **Public client flows** under Advanced settings.

> [!TIP]
> For detailed Azure AD configuration steps, see the [Complete MSAL Setup Guide](xref:Uno.Extensions.Authentication.HowToMsalSetup).

(Why? Uno’s MSAL provider will throw if the redirect isn’t loopback, same as MSAL itself.)

## 2. Turn on MSAL in the Uno features

In your **.csproj**:

```xml
<PropertyGroup>
  <UnoFeatures>
    Material;
    AuthenticationMsal;
    Toolkit;
    MVUX;
  </UnoFeatures>
</PropertyGroup>
````

This makes sure the host knows MSAL is a thing.

## 3. Add MSAL to the host

In **App.xaml.cs** (or your entry point):

```csharp
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure((host, window) =>
        {
            host
                .UseAuthentication(auth =>
                {
                    // must pass window
                    auth.AddMsal(window);
                });
        });

    Host = builder.Build();
}
```

**Important:** `AddMsal(...)` needs the **Window**. If you don’t pass it, MSAL cannot show the system browser dialog and you can get `MsalClientException` about loopback URIs.

## 4. Add configuration

In **appsettings.json**:

```json
{
  "Msal": {
    "ClientId": "00000000-0000-0000-0000-000000000000",
    "Scopes": [ "User.Read" ]
  }
}
```

You can also do it in code (see separate how-to).

## 5. Show a sign-in button

In **MainPage.xaml**:

```xml
<Page
    x:Class="MyApp.MainPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Button
            Content="Sign in with Microsoft"
            Command="{x:Bind ViewModel.Authenticate}" />
    </Grid>
</Page>
```

## 6. Call the authentication service

In **MainViewModel.cs**:

```csharp
public class MainViewModel : ObservableObject
{
    private readonly IAuthenticationService _authenticationService;

    public MainViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        Authenticate = new AsyncRelayCommand(AuthenticateAsync);
    }

    public IAsyncRelayCommand Authenticate { get; }

    private async Task AuthenticateAsync()
    {
        await _authenticationService.LoginAsync();
    }
}
```

This triggers the MSAL flow. Token is cached and refreshed automatically.

## Troubleshooting

If you encounter issues during authentication, refer to the [MSAL Troubleshooting Guide](xref:Uno.Extensions.Authentication.HowToMsalTroubleshooting) which covers:

- Configuration validation
- Common AADSTS error codes and solutions
- Azure AD verification using Azure CLI
- Platform-specific issues

## Next Steps

- **[Complete MSAL Setup Guide](xref:Uno.Extensions.Authentication.HowToMsalSetup)** - Learn about all configuration options, token retrieval, and Microsoft Graph integration
- **[MSAL Troubleshooting](xref:Uno.Extensions.Authentication.HowToMsalTroubleshooting)** - Systematic debugging approach and common issues
