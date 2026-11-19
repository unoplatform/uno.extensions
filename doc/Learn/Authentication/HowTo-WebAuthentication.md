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
            "LoginCallbackUri": "URI_TO_RETURN_TO_APP",
            "LogoutStartUri": "URI_TO_LOGOUT"
        }
    }
    ```

- `LoginStartUri`: The URI that will be used to start the login process. This is the URI that will be opened in the web view.

- `LoginCallbackUri`: The URI the identity provider redirects to when the login completes. The web view closes when it navigates to this URI, and the tokens are read from it. It can be omitted only when `LoginStartUri` includes a `redirect_uri` query parameter, which is then used instead. Without either, the login fails.

- `LogoutStartUri`: The URI that will be used to start the logout process. Without it, `LogoutAsync` returns `false` and the stored tokens are kept.

- Calling `LoginAsync` opens the `LoginStartUri` in the web view. The `LoginStartUri` will then redirect the user to the identity provider's login page. After the user successfully logs in, the identity provider will redirect the user back to the application. The `WebAuthenticationProvider` will then store the user's access token in credential storage.

#### Skia Desktop (Windows, macOS, Linux)

Uno Platform has no built-in `WebAuthenticationBroker` on the Skia Desktop target, so `AddWeb()` registers one: `DesktopWebAuthenticationBrokerProvider`, which opens `LoginStartUri` in the system browser and receives the redirect on a loopback HTTP listener. It is registered through Uno's `ApiExtensibility` registry, where the first registration wins, so an app that supplies its own `IWebAuthenticationBrokerProvider` must register it before calling `AddWeb()`.

- `LoginCallbackUri` (and `LogoutCallbackUri`) must be a loopback HTTP address, for example `http://localhost:5001/authentication-callback`, registered with the identity provider as is. A custom scheme is not routed back to a desktop app.
- `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` returns `http://localhost:{port}/authentication-callback` on this target, with a port the broker picks once per process; set `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` to pin it. Do not call it before `AddWeb()` has run: the broker resolves its provider on first use, and anything that touches `WebAuthenticationBroker` earlier pins Uno's built-in provider, which throws on this target. A sign-in that fails with `NotImplementedException` is this case: move the earlier call after `UseAuthentication`. A provider the app registers *after* `AddWeb()` is ignored for the same reason: the registry keeps the first registration.
- Whether the loopback broker took the registration, or an earlier one kept it, is logged through the host's logger once the host is built (`Uno.Extensions.Authentication` category, Debug and Information). That line is written by an `IServiceInitialize`, which only the Uno host (`this.CreateBuilder(args)` or `UnoHost.CreateDefaultBuilder()`) runs, so a host built some other way never writes it. Independently of the host, the broker's first sign-in flow logs at Information on Uno's ambient logger that the loopback broker is handling it; if the first sign-in logs no such line, an app-supplied provider handled it. Both lines sit under the `Uno.` category, so they only show once `CoreLogLevel` is `Information` or lower. The broker's own diagnostics (a port that could not be bound, a timed-out flow, a browser that would not open) go to Uno's ambient logger, which `UseLogging()` only connects with `enableUnoLogging: true`; its per-flow lines are at Debug, below the `Uno` filter the usual log levels apply.
- The listener completes on the first request to the callback path, which any page in the same browser (or any local process) can reach, and neither the broker nor the Web provider binds that request to the sign-in it started. Bind it yourself: send a `state` value your backend echoes back on the redirect (via `PrepareLoginStartUri`) and check it in `PostLogin` before accepting the tokens, rather than trusting whatever arrives. The OIDC provider does this as part of the protocol.
- If no redirect arrives within `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout`, the flow is reported as cancelled with `ResponseErrorDetail` set to `DesktopWebAuthenticationBrokerProvider.TimeoutErrorDetail` (408).
- To open the page some other way (a specific browser, a private window), derive from `DesktopWebAuthenticationBrokerProvider`, override `LaunchBrowserAsync`, and register your type with `ApiExtensibility.Register` before `AddWeb()`. Apps that call `WebAuthenticationBroker` directly can register the broker themselves with `DesktopWebAuthenticationBrokerProvider.TryRegister()` during startup. The type only exists in the non-WinAppSDK builds of the package, so guard the reference with `#if !WINDOWS` in code shared with a WinAppSDK head.

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
                        options.PostLogin(async (tokens, ct) =>
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

- The `PostLogin` delegate will be invoked after the user has successfully logged in. The delegate will be passed the user's tokens and a cancellation token. Other overloads also receive the `IServiceProvider`, the `ITokenCache` and the credentials, and `AddWeb<TService>` passes an instance of `TService` as well.

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

- `WebAuthenticationProvider` will then store the user's access token in credential storage. To refresh the token when it expires, register a `Refresh` callback with `options.Refresh(...)`.
