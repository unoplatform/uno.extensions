---
uid: Uno.Extensions.Authentication.HowToWebAuthentication
---
# How-To: Get Started with Web Authentication

> **UnoFeatures:** `Authentication` (add to `<UnoFeatures>` in your `.csproj`)

`WebAuthenticationProvider` provides an implementation that opens the platform's browser surface for the user to log in. After login, the identity provider redirects back to the application, and the provider reads any tokens carried on that redirect. This tutorial will use web authorization to validate user credentials.

> [!TIP]
> The Web provider is deliberately protocol-agnostic: it drives the browser and stores whatever tokens your endpoint (or your callbacks) hand back, which is what makes it fit any browser-based login. If your identity provider speaks OpenID Connect, prefer the [OIDC provider](xref:Uno.Extensions.Authentication.HowToOidcAuthentication) instead — it owns the whole protocol (discovery, PKCE, code exchange, refresh) for you.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Prepare for web authentication

- For this type of authentication, the application must already be registered with the desired identity provider.

- A client id (and client secret) will be provided to you.

- Add `Authentication` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
    +   Authentication;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

### 2. Set up web authentication

- Use the `UseAuthentication()` extension method to configure the `IHostBuilder` to use an authentication provider. In our case, we will be using the `WebAuthenticationProvider`.

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host
                .UseAuthentication(builder =>
                {
                    // Add the authentication provider here
                });
            });
        ...
    }
    ```

- Add the `WebAuthenticationProvider` using the `AddWeb()` extension method which configures the `IAuthenticationBuilder` to use it.

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host
                .UseAuthentication(builder =>
                {
                    builder.AddWeb();
                });
            });
        ...
    }
    ```

- The `IAuthenticationBuilder` is responsible for managing the lifecycle of the associated provider that was built.

- `WebAuthenticationProvider` will store the user's access token in credential storage.

### 3. Configure the provider

- While the `WebAuthenticationProvider` is added using the `AddWeb()` extension method, you will need to add a configuration section for basic settings to appsettings.json.

- We will be using the default name of `Web` for the configuration section (a custom provider name passed to `AddWeb(name: ...)` selects a section of that name instead).

    ```json
    {
        "Web": {
            "LoginStartUri": "https://idp.example/authorize?client_id=...&response_type=token&redirect_uri={RedirectUri}",
            "LogoutStartUri": "URI_TO_LOGOUT"
        }
    }
    ```

- The section supports the following values:

  | Key | Purpose | Default when absent |
  | --- | --- | --- |
  | `LoginStartUri` | The login page opened in the platform's browser surface. Required. | — |
  | `LoginCallbackUri` | The redirect the flow completes on. | Extracted from `redirect_uri` in `LoginStartUri` when present; otherwise the platform default (see [One configuration for every platform](#one-configuration-for-every-platform)). |
  | `AccessTokenKey` / `RefreshTokenKey` | The query/fragment keys the tokens are read from on the redirect. | `access_token` / `refresh_token` |
  | `LogoutStartUri` | The logout page for `LogoutAsync`. When absent, logout fails rather than silently clearing — clear `ITokenCache` directly for a local-only sign-out. | — |
  | `LogoutCallbackUri` | The redirect the logout flow completes on. | `LoginCallbackUri`, then the same fallbacks as login. |
  | `PrefersEphemeralWebBrowserSession` | iOS/Mac Catalyst: use a cookie-less `ASWebAuthenticationSession`. | `false` |

- The literal token `{RedirectUri}` inside `LoginStartUri` (and `LogoutStartUri`) is replaced at sign-in time with the URL-encoded effective callback, so one static URI works on every platform.

- After the user successfully logs in, the identity provider redirects back to the application; the provider reads the tokens from the redirect's query string **or URL fragment** using the configured keys, and stores them in credential storage. If the redirect carries something else — such as an authorization code that still needs exchanging — process it with the `PostLogin` callback below.

### 4. Process post-login tokens

- You can process the user's returned response for tokens by registering a delegate with the `WebAuthenticationProvider`.

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host =>
            {
                host
                .UseAuthentication(builder =>
                {
                    builder.AddWeb(options =>
                    {
                        options.PostLogin(async (authService, tokens, ct) =>
                        {
                            // Process the response here
                            return tokens;
                        });
                    });
                });
            });
        ...
    }
    ```

- The `PostLogin` delegate will be invoked after the user has successfully logged in. The delegate will be passed the `WebAuthenticationProvider` instance, the user's tokens, and a cancellation token.

- The delegate should return the user's tokens.

### 5. Use the provider in your application

- Update `MainPage` to include a button that will be used to login.

    ```xml
    <Page
        x:Class="Uno.Extensions.Authentication.Sample.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:Uno.Extensions.Authentication.Sample"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d">

        <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
            <Button x:Name="LoginButton" Content="Login" Command="{x:Bind ViewModel.Authenticate}" />
        </Grid>
    </Page>
    ```

- Update `MainViewModel` to include a command that will be used to login.

    ```csharp
    public class MainViewModel : ObservableCollection
    {
        private readonly IAuthenticationService _authService;

        public MainViewModel(IAuthenticationService authService)
        {
            _authService = authService;
        }

        public ICommand Authenticate => new DelegateCommand(async () =>
        {
            await _authService.LoginAsync(/* ... */);
        });
    }
    ```

- Finally, we can pass the login credentials to the `LoginAsync()` method and authenticate with the identity provider. The user will be prompted to sign in to their account when they tap the button in the application.

- `WebAuthenticationProvider` will then store the user's access token in credential storage.

- Calling `IAuthenticationService.RefreshAsync()` re-serves the stored tokens by default; the provider is protocol-agnostic, so actual token renewal only happens when you supply a `Refresh` callback (`AddWeb(web => web.Refresh(...))`) that redeems the stored refresh token against your endpoint.

## Platform support

`WebAuthenticationProvider` drives the interactive flow through each platform's authentication surface:

| Target | Sign-in surface | Notes |
| --- | --- | --- |
| Android | Custom Tabs via `WebAuthenticationBroker` | Redirect URI uses a custom scheme registered for the app. |
| iOS / Mac Catalyst | `ASWebAuthenticationSession` via `WebAuthenticationBroker` | Redirect URI uses a custom scheme declared in `Info.plist`. |
| WebAssembly | Browser popup/redirect via `WebAuthenticationBroker` | Redirect URI must share the app's origin (no custom schemes). |
| Skia Desktop (Windows, macOS, Linux) | System browser + loopback listener | See below. |
| Windows (WinAppSDK) | System browser via protocol activation | **Packaged apps only**: the OAuth redirect scheme must be declared as a Protocol in `Package.appxmanifest`; unpackaged apps are not supported. |

On the `WebAuthenticationBroker`-backed targets the interactive flow is bounded by `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout` (5 minutes by default), and the `CancellationToken` passed to `LoginAsync`/`LogoutAsync` cancels the flow.

### Skia Desktop

Uno Platform has no built-in `WebAuthenticationBroker` on Skia Desktop, so `AddWeb()` (and `AddOidc()`) automatically register a loopback broker: the sign-in page opens in the system browser and the redirect returns to a one-shot HTTP listener on `localhost`, per [RFC 8252 §7.3](https://www.rfc-editor.org/rfc/rfc8252#section-7.3).

- The redirect URI **must** be a loopback HTTP address, e.g. `http://localhost:5001/authentication-callback`, and be registered with your identity provider. If you rely on the default (`WebAuthenticationBroker.GetCurrentApplicationCallbackUri()`), a free port is picked on first use — your identity provider must then allow variable-port loopback redirects (Microsoft Entra and Duende IdentityServer do).
- Responses on the URL **fragment** (implicit-style flows) work too: browsers never send fragments to a server, so when the redirect arrives bare, the listener serves a small static relay page whose script re-requests the callback carrying the fragment — the app then receives the response in its original fragment shape. Query-string responses (authorization-code flow) complete in a single request, unchanged.
- Apps that call `WebAuthenticationBroker` directly (without `AddWeb`/`AddOidc`) can register the broker themselves during startup with `DesktopWebAuthenticationBrokerProvider.TryRegister()`.

## One configuration for every platform

The callback URI differs per platform, but a single configuration can serve all of them — the provider derives the platform callback at runtime:

- When no `LoginCallbackUri` is configured (and `LoginStartUri` carries no `redirect_uri` value), the provider falls back to `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` — the custom scheme on Android/iOS, the app origin on WebAssembly, and the loopback listener on Skia Desktop. (WinAppSDK keeps requiring explicit configuration: its flow uses a protocol scheme the broker does not model.)
- The literal token `{RedirectUri}` inside `LoginStartUri`/`LogoutStartUri` is replaced at sign-in/out time with the URL-encoded effective callback:

  ```json
  "Web": {
    "LoginStartUri": "https://idp.example/authorize?client_id=...&response_type=token&redirect_uri={RedirectUri}"
  }
  ```

Remember to register each platform's callback with the identity provider (see the table above).
