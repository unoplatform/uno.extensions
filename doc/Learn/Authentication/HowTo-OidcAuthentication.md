---
uid: Uno.Extensions.Authentication.HowToOidcAuthentication
---
# How-To: Get Started with Oidc Authentication

`OidcAuthenticationProvider` is a specific implementation of `IAuthenticationProvider` that allows your users to sign in using their identities from a participating identity provider. It provides seamless integration with any [OpenID Connect](https://openid.net/connect/) backend, such as [IdentityServer](https://duendesoftware.com/products/identityserver). By acting as an adapter, it integrates OpenID Connect authentication into the Uno.Extensions ecosystem, allowing you to leverage a unified approach across platforms.

Under the hood, `OidcAuthenticationProvider` relies on [IdentityModel.OidcClient](https://identitymodel.readthedocs.io/), for all non-Windows targets, as a widely-used .NET library that handles the core OpenID Connect and OAuth 2.0 protocols. This library manages the complex operations like token handling and user authentication.

> [!IMPORTANT]
> **Platform support:** The `OidcAuthenticationProvider` requires the `WebAuthenticationBroker` or a custom `IBrowser` implementation to open the login page. The `WebAuthenticationBroker` is currently supported on **Android, iOS, macOS, and WebAssembly**. It is **not available on Skia Desktop** (`net10.0-desktop`). Calling `LoginAsync` on Skia Desktop without a custom `IBrowser` will throw a `NotImplementedException`.
>
> **Workarounds:**
> - **For Microsoft Entra ID / Azure AD:** Use the [`MsalAuthenticationProvider`](xref:Uno.Extensions.Authentication.HowToMsalAuthentication) instead, which has built-in desktop browser support.
> - **For other OIDC providers on desktop:** Provide a custom `IBrowser` implementation that opens the system browser and listens on a loopback URI. See [Advanced Customizations](#advanced-customizations) below.

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

  > Common scopes include `openid`, `profile`, `email`, and `offline_access`. The `openid` scope is required for OpenID Connect authentication. The `profile` and `email` scopes are used to request additional user information, and the `offline_access` scope is used to request a refresh token. Your identity provider may provide additional scopes, such as `api`, to request access to specific APIs.

- `Authority`: The URL of the identity provider.

- `ClientId` and `ClientSecret`: The client ID and client secret that were provided to you.

- `Scope`: The scope of the access token.

- `RedirectUri`: The URL that the identity provider will redirect to after the user has authenticated.
  > It is also possible to populate this setting automatically from the WebAuthenticationBroker using the `.AutoRedirectUriFromAuthenticationBroker()` extension method, which will set the redirect URI to the value returned by the `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` method, which should discover the correct redirect URI for the application/platform. More information can be found in the [Web Authentication Broker documentation](xref:Uno.Features.WAB).
  >
  > When used, this setting will override the value set in the configuration file for both the redirect URI and the post-logout redirect URI.
  > **This setting is ON by default on WebAssembly but opt-in on other platforms.**

- **Advanced settings**: some advanced settings may need to access directly the `OidcClientOptions` from `IdentityModel.OidcClient` used by this extension. This can be done by using the `.ConfigureOidcClientOptions()` extension method.

    ```csharp
    builder.AddOidc()
        .ConfigureOidcClientOptions(options =>
        {
            // Example of advanced settings for the OidcClientOptions
            options.DisablePushedAuthorization  = false; // Disable the PAR endpoint
            options.Policy.RequireIdentityTokenOnRefreshTokenResponse = true; // Require an identity token on refresh token response
            options.Policy.Discovery.ValidateIssuerName = false; // Disable issuer name validation
        });
    ```

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

### Custom IBrowser implementation

You can use your own implementation of `IBrowser` (an interface from the `IdentityModel.OidcClient` library) to customize the browser behavior. This is **required on Skia Desktop** (`net10.0-desktop`), where no default browser implementation is provided.

Register your implementation using `.ConfigureServices()` in the `App.xaml.cs` file:

```csharp
.ConfigureServices((context, services) =>
{
    services.AddTransient<IBrowser, SystemBrowser>();
})
```

A typical `IBrowser` implementation for desktop opens the system browser and listens for the redirect on a loopback URI:

```csharp
using IdentityModel.OidcClient.Browser;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public class SystemBrowser : IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        // Listen on the redirect URI (loopback)
        using var listener = new HttpListener();
        var redirectUri = options.EndUrl;
        if (!redirectUri.EndsWith("/"))
            redirectUri += "/";
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        // Open the system browser
        Process.Start(new ProcessStartInfo
        {
            FileName = options.StartUrl,
            UseShellExecute = true
        });

        // Wait for the callback
        var context = await listener.GetContextAsync();
        var result = new BrowserResult
        {
            Response = context.Request.Url?.ToString() ?? string.Empty,
            ResultType = BrowserResultType.Success
        };

        // Send a response to the browser
        var response = context.Response;
        var buffer = System.Text.Encoding.UTF8.GetBytes("<html><body>Authentication complete. You can close this window.</body></html>");
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, cancellationToken);
        response.Close();

        return result;
    }
}
```

> [!NOTE]
> When using a custom `IBrowser` on desktop, set your OIDC `RedirectUri` to a loopback address (e.g. `http://127.0.0.1:{port}/callback`) and register this URI with your identity provider.
