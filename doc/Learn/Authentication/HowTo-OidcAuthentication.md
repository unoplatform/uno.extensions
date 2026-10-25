---
uid: Uno.Extensions.Authentication.HowToOidcAuthentication
---
# How-To: Get Started with Oidc Authentication

> **UnoFeatures:** `AuthenticationOidc` (add to `<UnoFeatures>` in your `.csproj`)

`OidcAuthenticationProvider` is a specific implementation of `IAuthenticationProvider` that allows your users to sign in using their identities from a participating identity provider. It provides seamless integration with any [OpenID Connect](https://openid.net/connect/) backend, such as [Duende.IdentityServer](https://duendesoftware.com/products/identityserver). By acting as an adapter, it integrates OpenID Connect authentication into the Uno.Extensions ecosystem, allowing you to leverage a unified approach across platforms.

Under the hood, `OidcAuthenticationProvider` relies on [Duende.IdentityModel.OidcClient](https://docs.duendesoftware.com/identitymodel-oidcclient/), for all non-windows targets, as a widely-used .NET library that handles the core OpenID Connect and OAuth 2.0 protocols. This library manages the complex operations like token handling and user authentication.

> [!NOTE]
> It may be useful to familiarize yourself with the [OpenID Connect](https://openid.net/connect/) protocol before proceeding.
> You can also explore the [Uno Tutorial for Oidc Authentication](xref:Uno.Tutorials.OpenIDConnect) to learn more about the particularities for different platforms.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Prepare for OIDC authentication

- For this type of authentication, the application must already be registered with the desired identity provider.

- A client id (and client secret) will be provided to you.

- Add `AuthenticationOidc` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
    +   AuthenticationOidc;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

### 2. Set up OIDC authentication

- Use the `UseAuthentication()` extension method to configure the `IHostBuilder` to use an authentication provider. In our case, we will be using the `OidcAuthenticationProvider`.

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

- Add the `OidcAuthenticationProvider` using the `AddOidc()` extension method which configures the `IAuthenticationBuilder` to use it.

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
                    builder.AddOidc();
                });
            });
        ...
    }
    ```

- The `IAuthenticationBuilder` is responsible for managing the lifecycle of the associated provider that was built.

- Because it is configured to use OpenID Connect, the user will eventually be prompted to sign in to their identity provider account when they use your application. `OidcAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.

### 3. Configure the OidcAuthenticationProvider

- While the `AddOidc()` extension method will configure the `IAuthenticationBuilder` to use the `OidcAuthenticationProvider`, it will not configure the provider itself. The `OidcAuthenticationProvider` can be configured using a configuration section.

- We will use the default section name `Oidc` in this example:

  ```json
  {
      "Oidc": {
          "Authority": "https://demo.duendesoftware.com/",
          "ClientId": "interactive.confidential",
          "ClientSecret": "secret",
          "Scope": "openid profile email api offline_access",
          "RedirectUri": "oidc-auth://callback",
      }
  }
  ```

  On Skia Desktop the redirect URI must be a loopback HTTP address instead of a custom scheme; see [Skia Desktop](#skia-desktop-windows-macos-linux) below.

  > Common scopes include `openid`, `profile`, `email`, and `offline_access`. The `openid` scope is required for OpenID Connect authentication. The `profile` and `email` scopes are used to request additional user information, and the `offline_access` scope is used to request a refresh token. Your identity provider may provide additional scopes, such as `api`, to request access to specific APIs.

- `Authority`: The URL of the identity provider.

- `ClientId` and `ClientSecret`: The client ID and client secret that were provided to you.

- `Scope`: The scope of the access token.

- `RedirectUri`: The URL that the identity provider will redirect to after the user has authenticated.
  > It is also possible to populate this setting automatically from the WebAuthenticationBroker using the `.AutoRedirectUriFromWebAuthenticationBroker()` extension method (`builder.AddOidc(oidc => oidc.AutoRedirectUriFromWebAuthenticationBroker())`), which will set the redirect URI to the value returned by the `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` method, which should discover the correct redirect URI for the application/platform. More information can be found in the [Web Authentication Broker documentation](xref:Uno.Features.WAB).
  >
  > When used, this setting will override the value set in the configuration file for both the redirect URI and the post-logout redirect URI.
  > **This setting is ON by default on WebAssembly but opt-in on other platforms.**

- **Advanced settings**: some advanced settings may need to access directly the `OidcClientOptions` from `IdentityModel.OidcClient` used by this extension. This can be done by using the `.ConfigureOidcClientOptions()` extension method.

    ```csharp
    builder.AddOidc(oidc => oidc
        .ConfigureOidcClientOptions(options =>
        {
            // Example of advanced settings for the OidcClientOptions
            options.DisablePushedAuthorization = true; // Disable the PAR endpoint
            options.Policy.RequireIdentityTokenOnRefreshTokenResponse = true; // Require an identity token on refresh token response
            options.Policy.Discovery.ValidateIssuerName = false; // Disable issuer name validation
        }));
    ```

#### Skia Desktop (Windows, macOS, Linux)

Uno Platform has no built-in `WebAuthenticationBroker` on the Skia Desktop target, so `AddOidc()` registers one: `DesktopWebAuthenticationBrokerProvider`, which opens the sign-in page in the system browser and receives the redirect on a loopback HTTP listener. It is registered through Uno's `ApiExtensibility` registry, where the first registration wins, so an app that supplies its own `IWebAuthenticationBrokerProvider` must register it before calling `AddOidc()`.

- The `RedirectUri` (and `PostLogoutRedirectUri`) must be a loopback HTTP address, for example `http://localhost:5001/authentication-callback`, and that exact value must be registered with the identity provider. A custom scheme is not routed back to a desktop app.
- `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` returns `http://localhost:{port}/authentication-callback` on this target, with a port the broker picks once per process; set `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` to pin it. Do not call it before `AddOidc()` has run: the broker resolves its provider on first use, and anything that touches `WebAuthenticationBroker` earlier pins Uno's built-in provider, which throws on this target. A provider the app registers *after* `AddOidc()` is ignored for the same reason: the registry keeps the first registration.
- The broker's diagnostics (which broker handles sign-in, a port that could not be bound, a timed-out flow) go to Uno's ambient logger, which `UseLogging()` only connects with `enableUnoLogging: true`.
- Sign-out sends the cached ID token as `id_token_hint`, which the identity provider needs before it will redirect back to `PostLogoutRedirectUri`. Some providers redirect at once; others, such as the Duende demo server, show a signed-out page with a link back to the app, and the sign-out completes when the user follows it.
- If no redirect arrives within `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout`, the flow is reported as cancelled with `ResponseErrorDetail` set to `DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail` (408).
- To open the page some other way (a specific browser, a private window), derive from `DesktopWebAuthenticationBrokerProvider`, override `LaunchBrowserAsync`, and register your type with `ApiExtensibility.Register` before `AddOidc()`. The type only exists in the non-WinAppSDK builds of the package, so guard the reference with `#if !WINDOWS` in code shared with a WinAppSDK head.

### 4. Use the provider in your application

- Update the `MainPage` to include a button that will allow the user to sign in with your desired service.

    ```xml
    <Page
        x:Class="UnoOidcAuthentication.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:UnoOidcAuthentication"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d">

        <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
            <Button x:Name="SignInButton" Content="Sign In" Command="{x:Bind ViewModel.Authenticate}" />
        </Grid>
    </Page>
    ```

- Update the `MainViewModel` to include a command that will allow the user to sign in with your desired service.

    ```csharp
    public class MainViewModel : ObservableObject
    {
        private readonly IAuthenticationService _authenticationService;

        public MainViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public ICommand Authenticate => new AsyncRelayCommand(AuthenticateImpl);

        private async Task AuthenticateImpl()
        {
            await _authenticationService.LoginAsync(/* ... */);
        }
    }
    ```

- Finally, we can pass the login credentials to the `LoginAsync()` method and authenticate with the identity provider. The user will be prompted to sign in to their account when they tap the button in the application.

- `OidcAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.

## Advanced Customizations

- You can use your own implementation of `IBrowser` (an interface from the `IdentityModel.OidcClient` library) to customize the browser behavior. This should be done by creating a class that implements the interface and register it using the `.ConfigureServices()` in the `App.xaml.cs` file.

  ```csharp
    .ConfigureServices((context, services) =>
    {
        services.AddTransient<IBrowser, CustomBrowser>();
    })

  ```
