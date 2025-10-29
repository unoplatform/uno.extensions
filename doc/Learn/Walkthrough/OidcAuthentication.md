---
uid: Uno.Extensions.Authentication.OidcAuthentication
title: Sign In with OpenID Connect
tags: [authentication, oidc, identityserver]
---
# Connect your app to an OpenID Connect provider

Integrate any OpenID Connect identity provider with `OidcAuthenticationProvider`, store tokens securely, and refresh them automatically across platforms.

> [!NOTE]
> Make sure the app is registered with your identity provider and you have the authority URL, client ID, client secret (if required), scopes, and redirect URI. Review the [OpenID Connect specification](https://openid.net/connect/) or the [Uno tutorial for OIDC authentication](xref:Uno.Tutorials.OpenIDConnect) if you need a refresher.

## Enable OIDC support

Add the `AuthenticationOidc` feature to reference `Uno.Extensions.Authentication.Oidc`.

```diff
<UnoFeatures>
    Material;
+   AuthenticationOidc;
    Toolkit;
    MVUX;
</UnoFeatures>
```

## Register the OIDC provider

Wire up `AddOidc` inside the host configuration.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseAuthentication(builder =>
            {
                builder.AddOidc();
            });
        });
}
```

The provider wraps `IdentityModel.OidcClient` (or MSAL on Windows) to handle redirects, token storage, and refresh.

## Configure authority, client, and redirect URIs

Provide identity provider settings in `appsettings.json`.

```json
{
  "Oidc": {
    "Authority": "https://demo.duendesoftware.com/",
    "ClientId": "interactive.confidential",
    "ClientSecret": "secret",
    "Scope": "openid profile email api offline_access",
    "RedirectUri": "oidc-auth://callback"
  }
}
```

- `Authority` is the base URL for discovery.
- `ClientId` and `ClientSecret` identify your application.
- `Scope` controls the claims you request (`openid` is required).
- `RedirectUri` tells the identity provider where to send the browser after login.

Call `.AutoRedirectUriFromAuthenticationBroker()` if you want Uno to derive the callback from `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()`. This is the default on WebAssembly and opt-in on other platforms:

```csharp
builder.AddOidc(oidc => oidc.AutoRedirectUriFromAuthenticationBroker());
```

For fine-grained control, adjust the underlying `OidcClientOptions`.

```csharp
builder.AddOidc()
    .ConfigureOidcClientOptions(options =>
    {
        options.DisablePushedAuthorization = false;
        options.Policy.RequireIdentityTokenOnRefreshTokenResponse = true;
        options.Policy.Discovery.ValidateIssuerName = false;
    });
```

## Start the OIDC login flow from the UI

Bind a button to `LoginAsync` to launch the provider UI.

```xml
<Grid>
    <Button Content="Sign in"
            Command="{x:Bind ViewModel.Authenticate}" />
</Grid>
```

```csharp
public class MainViewModel
{
    private readonly IAuthenticationService _authenticationService;

    public MainViewModel(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        Authenticate = new AsyncRelayCommand(AuthenticateAsync);
    }

    public IAsyncRelayCommand Authenticate { get; }

    private async Task AuthenticateAsync() =>
        await _authenticationService.LoginAsync();
}
```

`OidcAuthenticationProvider` opens the system browser or embedded web view, retrieves tokens, and keeps them refreshed.

## Customize the OIDC browser

Replace the default browser implementation by registering your own `IBrowser` from `IdentityModel.OidcClient`.

```csharp
.ConfigureServices((context, services) =>
{
    services.AddTransient<IBrowser, CustomBrowser>();
})
```

Use this hook to integrate with custom web views or to instrument the authentication experience.

## Resources

- [Authentication overview](xref:Uno.Extensions.Authentication.Overview)
- [OpenID Connect specification](https://openid.net/connect/)
- [IdentityModel OIDC client documentation](https://docs.duendesoftware.com/identitymodel-oidcclient/)
- [Uno tutorial for OIDC authentication](xref:Uno.Tutorials.OpenIDConnect)
