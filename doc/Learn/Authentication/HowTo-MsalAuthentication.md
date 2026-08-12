---
uid: Uno.Extensions.Authentication.HowToMsalAuthentication
---
# How-To: Get Started with MSAL Authentication

> **UnoFeatures:** `AuthenticationMsal` (add to `<UnoFeatures>` in your `.csproj`)

`MsalAuthenticationProvider` allows your users to sign in using their Microsoft identities. It wraps the [MSAL library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) from Microsoft into an implementation of `IAuthenticationProvider`. This tutorial will use MSAL authorization to validate user credentials.

## Platform support

| Target | Interactive sign-in | Token cache persistence |
|---|---|---|
| Windows (WinAppSdk) | ✅ WAM broker (requires a `Window` — see below) | ✅ Encrypted file (DPAPI) |
| Desktop (Skia) — Windows | ✅ System browser | ✅ Encrypted file (DPAPI) |
| Desktop (Skia) — macOS | ✅ System browser | ✅ Keychain |
| Desktop (Skia) — Linux | ✅ System browser | ✅ Keyring/libsecret |
| Android | ✅ Browser / custom tab | ✅ Handled natively by MSAL |
| iOS | ✅ Web authentication session | ✅ Handled natively by MSAL |
| WebAssembly | ✅ Popup | In-memory only (tokens don't survive a page reload) |
| Mac Catalyst | ❌ Not supported (`AddMsal` is a no-op) | — |

> [!NOTE]
> On Android, iOS, and WebAssembly heads that use `UnoFeatures=SkiaRenderer`, interactive sign-in requires an Uno Platform version containing the fix for [unoplatform/uno#20601](https://github.com/unoplatform/uno/issues/20601); with earlier versions the sign-in UI never appears on those targets.

The set of identity scenarios (Microsoft accounts, work/school accounts, B2C, sovereign clouds, ...) is determined by MSAL itself — see [MSAL.NET supported platforms and scenarios](https://learn.microsoft.com/entra/msal/dotnet/getting-started/scenarios) for details.

## Step-by-step

[!include[create-application](../includes/create-application.md)]

### 1. Prepare for MSAL authentication

- For this type of authentication, the application must be registered with the Microsoft identity platform. For more information, see [Register an application with the Microsoft identity platform](https://learn.microsoft.com/azure/active-directory/develop/quickstart-register-app).

- The identity provider will provider you with a client ID and guidance on scopes to use.

- Add `AuthenticationMsal` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

    ```diff
    <UnoFeatures>
        Material;
    +   AuthenticationMsal;
        Toolkit;
        MVUX;
    </UnoFeatures>
    ```

### 2. Set up MSAL authentication

- Use the `UseAuthentication()` extension method to configure the `IHostBuilder` to use an authentication provider. In our case, we will be using the `MsalAuthenticationProvider`.

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

- Use the `Configure` method overload that provides access to a `Window` instance. Add the `MsalAuthenticationProvider` using the `AddMsal()` extension method which configures the `IAuthenticationBuilder` to use it.

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure((host, window) =>
            {
                host
                .UseAuthentication(builder =>
                {
                    builder.AddMsal(window);
                });
            });
        ...
    }
    ```

> [!IMPORTANT]
> The `AddMsal()` method requires a `Window` instance, which the `MsalAuthenticationProvider` uses to set up the authentication dialog. You can access the `Window` instance through the `Configure()` method overload that provides it.
> **Note:** Failing to pass a valid `Window` instance could result in a `MsalClientException` with the message:
> *"Only loopback redirect uri is supported, but <your_redirect_uri> was found. Configure http://localhost or http://localhost:port both during app registration and when you create the PublicClientApplication object. See https://aka.ms/msal-net-os-browser for details."*

- The `IAuthenticationBuilder` is responsible for managing the lifecycle of the associated provider that was built.

- Because it is configured to use MSAL, the user will eventually be prompted to sign in to their Microsoft account when they use your application. `MsalAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.

### 3. Configure the provider

- While `MsalAuthenticationProvider` is added using the `AddMsal()` extension method, you will need to add a configuration section to your appsettings.json file with your client ID and scopes.

    The following example shows how to configure the provider using the default section name:

    ```json
    {
      "Msal": {
        "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
        "Scopes": [ "Tasks.Read", "User.Read", "Tasks.ReadWrite" ]
      }
    }
    ```

    This configuration can also be done in the root App.cs file:

    ```csharp
    private IHost Host { get; set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure((host, window) =>
            {
                host
                .UseAuthentication(builder =>
                {
                    builder.AddMsal(window, msal =>
                        msal
                        .Builder(msalBuilder => 
                            msalBuilder.WithClientId("161a9fb5-3b16-487a-81a2-ac45dcc0ad3b"))
                        .Scopes(new[] { "Tasks.Read", "User.Read", "Tasks.ReadWrite" })
                    );
                });
            });
        ...
    }
    ```

    > [!WARNING]
    > A ClientId of GUID format is required for MSAL Authentication to work. You can specify it in the appsettings.json file, or in the code itself.
    > If the ClientId cannot be found, the app will crash with the following error:

    ```xml
    Exception thrown: 'Microsoft.Identity.Client.MsalClientException' in Microsoft.Identity.Client.dll
    No ClientId was specified.
    ```

### 4. Redirect URIs

`MsalAuthenticationProvider` applies the platform's conventional redirect URI for you, so a
cross-platform app does not need a per-platform `#if` block in its `Builder(...)` callback:

| Platform | Redirect URI applied |
| --- | --- |
| Android | `msal{ClientId}://auth` |
| iOS | `msauth.{BundleId}://auth` |
| WebAssembly | the `WebAuthenticationBroker` callback URI |
| Desktop (Windows, macOS, Linux) | MSAL's `WithDefaultRedirectUri()` — `http://localhost` on .NET, for the system-browser flow |
| WinAppSDK (`net9.0-windows10.*`) | none — the WAM broker owns the redirect URI |

Each value still has to be registered in your app registration, and Android and iOS additionally
require the app to declare the matching platform entry — an `Activity` deriving from
`BrowserTabActivity` with an intent filter for scheme `msal{ClientId}` and host `auth` on Android,
and a `CFBundleURLTypes` entry in `Info.plist` on iOS. The provider derives exactly the value those
entries declare; it cannot create them for you.

Precedence, lowest to highest:

1. The platform default above.
2. `RedirectUri` in the `Msal` configuration section.
3. Whatever the app's `Builder(...)` callback sets — it runs last and simply overwrites.

So overriding for a single platform stays a one-liner:

```csharp
builder.AddMsal(window, msal =>
    msal.Builder(msalBuilder => msalBuilder.WithRedirectUri("myapp://auth")));
```

To suppress the default entirely and take whatever MSAL itself would use, set:

```json
{
  "Msal": {
    "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
    "UseDefaultPlatformRedirectUri": false
  }
}
```

> [!NOTE]
> On WebAssembly the callback URI comes from Uno's `WebAuthenticationBroker`, whose default path is
> `/authentication-callback`. If your registered SPA redirect uses a different path, set
> `RedirectUri` in configuration or from the `Builder(...)` callback — both now win over the
> broker-derived default.

### 5. Token cache storage (optional)

On desktop targets, `MsalAuthenticationProvider` persists the MSAL token cache so that users stay signed in across app restarts:

- **Windows** — an encrypted (DPAPI) cache file in the app's data folder.
- **macOS** — the cache is protected by the macOS Keychain. By default the keychain entry uses the service name `uno.extensions.msal.{ClientId}` and the account name `MSALCache`. Both can be overridden in the `Msal` configuration section:

    ```json
    {
      "Msal": {
        "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
        "KeychainServiceName": "com.contoso.myapp.msal",
        "KeychainAccountName": "MyAppCache"
      }
    }
    ```

- **Linux** — the cache is stored in the default keyring collection via `libsecret`.

If the platform's secure storage isn't available (for example, a Linux session without a keyring), the provider logs an error and keeps the token cache in memory for the session — sign-in still works, but the user has to sign in again after an app restart. To persist the cache in an **unprotected (plaintext) file** in that situation instead, opt in explicitly:

```json
{
  "Msal": {
    "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
    "AllowUnprotectedTokenCacheFallback": true
  }
}
```

With the fallback enabled, the provider logs a warning (including the fallback file path) whenever it has to downgrade, and the plaintext cache uses a separate file from the protected one.

On Android and iOS the token cache is persisted natively by MSAL, and on WebAssembly tokens are cached in memory only — no configuration is needed (or honored) on those platforms.

For full control over the storage properties on desktop targets, use the `Storage()` extension method to configure the underlying `StorageCreationPropertiesBuilder`:

```csharp
builder.AddMsal(window, msal =>
    msal.Storage(store => store.WithMacKeyChain("com.contoso.myapp.msal", "MyAppCache")));
```

### 6. Use the provider in your application

- Update the `MainPage` to include a `Button` labeled to sign in with Microsoft.

    ```xml
    <Page
        x:Class="MyApp.MainPage"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="using:MyApp"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d">

        <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
            <Button x:Name="SignInButton" Content="Sign in with Microsoft" Command="{x:Bind ViewModel.Authenticate}" />
        </Grid>
    </Page>
    ```

- Because the `IAuthenticationService` instance will be injected into our view models, we can now update the `MainViewModel` to include a `Command` that will use that service to sign in a user.

    ```csharp
    public class MainViewModel : ObservableObject
    {
        private readonly IAuthenticationService _authenticationService;

        public MainViewModel(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
            Authenticate = new AsyncRelayCommand(AuthenticateAsync);
        }

        public ICommand Authenticate { get; }

        private async Task AuthenticateAsync()
        {
            await _authenticationService.LoginAsync(/* ... */);
        }
    }
    ```

- Finally, we can run the application and sign in with our Microsoft account. The user will be prompted to sign in to their Microsoft account when they tap the button in the application.

- `MsalAuthenticationProvider` will then store the user's access token in credential storage. The token will be automatically refreshed when it expires.
