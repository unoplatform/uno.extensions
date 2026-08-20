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
| Mac Catalyst | ❌ Not supported (`AddMsal` throws `PlatformNotSupportedException`) | — |

> [!NOTE]
> On Android, iOS, and WebAssembly heads that use `UnoFeatures=SkiaRenderer`, interactive sign-in requires an Uno Platform version containing the fix for [unoplatform/uno#20601](https://github.com/unoplatform/uno/issues/20601); with earlier versions the sign-in UI never appears on those targets.

> [!NOTE]
> On Skia iOS/Android heads, the Uno.Sdk build substitutes the package's plain `netX.0` library
> for the platform one, and the provider selects platform behavior at runtime
> (`OperatingSystem.IsAndroid()`/`IsIOS()`), so everything above still applies. Your *app* keeps
> its platform TFM, so `#if ANDROID` / `#if IOS` blocks in your own code — including `Builder(...)`
> callbacks — behave normally. Library authors whose packages reference `Uno.UI` are subject to the
> same substitution and must not assume the TFM implies the OS.

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
entries declare; it cannot create them for you. Section 5 shows the complete set.

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

### 5. Android and iOS: deliver the sign-in callback

On desktop the redirect comes back over a local HTTP listener, on WebAssembly through the
`WebAuthenticationBroker`, and on WinAppSDK through the WAM broker — the provider handles the whole
round-trip. On Android and iOS the redirect arrives as an *OS event delivered to the app*, so the
app has to hand it to MSAL itself, using MSAL's own helper,
[`AuthenticationContinuationHelper`](https://learn.microsoft.com/dotnet/api/microsoft.identity.client.authenticationcontinuationhelper).
Without these entries the browser opens and sign-in succeeds there, but the app's `LoginAsync`
never completes — the most common MSAL symptom on mobile.

#### Android

Declare the activity that catches the redirect. Intent filters are declared with attributes, which
only accept compile-time constants, so the client id is repeated here literally — keep it in sync
with `Msal:ClientId` in `appsettings.json`:

```csharp
// Platforms/Android/MsalActivity.Android.cs
using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

[Activity(Exported = true, LaunchMode = LaunchMode.SingleTask, NoHistory = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryBrowsable, Intent.CategoryDefault],
    DataScheme = "msal161a9fb5-3b16-487a-81a2-ac45dcc0ad3b", // msal{ClientId}
    DataHost = "auth")]
public class MsalActivity : BrowserTabActivity
{
}
```

Then forward activity results from your `MainActivity`, which is what resumes the pending
`AcquireTokenInteractive` call:

```csharp
// Platforms/Android/MainActivity.Android.cs
protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
{
    base.OnActivityResult(requestCode, resultCode, data);
    Microsoft.Identity.Client.AuthenticationContinuationHelper
        .SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
}
```

#### iOS

Register the callback URL scheme in `Platforms/iOS/Info.plist` — the scheme is
`msauth.` followed by your bundle identifier:

```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLName</key>
        <string>com.microsoft.msal</string>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>msauth.com.example.myapp</string>
        </array>
    </dict>
</array>
```

With Skia rendering the `App` class is no longer the `UIApplicationDelegate`, so URL handling goes
in a type deriving from Uno's `UnoUIApplicationDelegate`:

```csharp
// Platforms/iOS/MsalAppDelegate.iOS.cs
using Foundation;
using Microsoft.Identity.Client;
using UIKit;

public class MsalAppDelegate : Uno.UI.Runtime.Skia.AppleUIKit.UnoUIApplicationDelegate
{
    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
        => AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url)
            || base.OpenUrl(application, url, options);
}
```

registered from the entry point:

```csharp
// Platforms/iOS/Main.iOS.cs
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseAppleUIKit(builder => builder.UseUIApplicationDelegate<MsalAppDelegate>())
    .Build();
```

> [!NOTE]
> Recent iOS SDKs flag `OpenUrl` as deprecated (`CA1422`) in favor of the UIScene lifecycle. Apps
> without a `UIApplicationSceneManifest` in `Info.plist` still run the application-delegate
> lifecycle, so suppress the warning with `#pragma warning disable CA1422`; scene-based apps
> forward the same call from their scene delegate's `OpenUrlContexts` instead.

A complete working reference for all of the above is the
[MSAL authentication sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/Authentication.MsalExtensionsDemo).

### 6. Customizing the interactive sign-in request (optional)

`Builder(...)` configures the `PublicClientApplicationBuilder`, which MSAL builds once. The
modifiers that apply to a *single* interactive sign-in — `WithPrompt`, `WithLoginHint`,
`WithExtraScopeToConsent`, `WithSystemWebViewOptions` — hang off `AcquireTokenInteractiveParameterBuilder`
instead, and are unreachable from `Builder(...)`. Use `InteractiveBuilder(...)` for those:

```csharp
builder.AddMsal(window, msal => msal
    .InteractiveBuilder(interactive => interactive
        .WithPrompt(Prompt.SelectAccount)
        .WithLoginHint("user@contoso.com")));
```

The callback runs on every interactive sign-in, after the Uno helpers have been applied, so it can
override what those set.

An interactive sign-in that isn't completed times out after **5 minutes** by default — the
system-browser flow used on desktop cannot detect a closed browser window, so without a timeout an
abandoned sign-in would leave the awaiting login command busy forever. Configure it via
`InteractiveTimeout` in the `Msal` configuration section (a `TimeSpan`; zero or negative waits
indefinitely):

```json
{
  "Msal": {
    "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
    "InteractiveTimeout": "00:02:00"
  }
}
```

### 7. Token cache storage (optional)

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

### 8. Use the provider in your application

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
