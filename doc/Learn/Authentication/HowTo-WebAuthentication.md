---
uid: Uno.Extensions.Authentication.HowToWebAuthentication
---
# How-To: Get Started with Web Authentication

> **UnoFeatures:** `Authentication` (add to `<UnoFeatures>` in your `.csproj`)

`WebAuthenticationProvider` provides an implementation that displays a web view in order for the user to login. After login, the web view redirects back to the application, along with any tokens. This tutorial will use web authorization to validate user credentials.

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

- We will be using the default name of `Web` for the configuration section.

    ```json
    {
        "Web": {
            "LoginStartUri": "URI_TO_LOGIN",
            "LogoutStartUri": "URI_TO_LOGOUT"
        }
    }
    ```

- `LoginStartUri`: The URI that will be used to start the login process. This is the URI that will be opened in the web view.

- `LogoutStartUri`: The URI that will be used to start the logout process.

- `WebAuthenticationProvider` will automatically redirect the user to the `LoginStartUri` when they are not authenticated. The `LoginStartUri` will then redirect the user to the identity provider's login page. After the user successfully logs in, the identity provider will redirect the user back to the application. The `WebAuthenticationProvider` will then store the user's access token in credential storage.

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

- `WebAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.

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
- Only query-string responses reach the app (authorization-code flow); a URL fragment never leaves the browser, so implicit flows cannot work over a loopback redirect.
- Apps that call `WebAuthenticationBroker` directly (without `AddWeb`/`AddOidc`) can register the broker themselves during startup with `DesktopWebAuthenticationBrokerProvider.TryRegister()`.
