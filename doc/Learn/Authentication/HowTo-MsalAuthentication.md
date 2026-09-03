---
uid: Uno.Extensions.Authentication.HowToMsalAuthentication
---
# How-To: Get Started with MSAL Authentication

> **UnoFeatures:** `AuthenticationMsal` (add to `<UnoFeatures>` in your `.csproj`)

`MsalAuthenticationProvider` allows your users to sign in using their Microsoft identities. It wraps the [MSAL library](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) from Microsoft into an implementation of `IAuthenticationProvider`. This tutorial will use MSAL authorization to validate user credentials.

## Platform support

| Target | Interactive sign-in | Token cache persistence |
| --- | --- | --- |
| Windows (WinAppSdk) | ✅ WAM broker (requires a `Window` — see below) | ✅ Encrypted file (DPAPI) |
| Desktop (Skia) — Windows | ✅ System browser | ✅ Encrypted file (DPAPI) |
| Desktop (Skia) — macOS | ✅ System browser | ✅ Keychain |
| Desktop (Skia) — Linux | ✅ System browser | ✅ Keyring/libsecret |
| Android | ✅ Browser / custom tab | ⚠️ Plain `SharedPreferences` file, written by MSAL — app sandbox only, not encrypted, see [below](#android) |
| iOS | ✅ Web authentication session | ✅ iOS Keychain, written by MSAL — [keychain entitlement required](#ios-keychain-access-group) |
| WebAssembly | ✅ Popup | ✅ Browser storage, `localStorage` by default — cleartext, see [below](#webassembly-token-cache) |
| Mac Catalyst | ❌ Not supported (`AddMsal` throws `PlatformNotSupportedException`) | — |

MSAL's own cache (refresh and ID tokens) is what the last column describes. The access token that `IAuthenticationService` hands to HTTP handlers is kept separately, in the host's default `IKeyValueStorage` — `KeyStore` / Keychain on native Android and iOS, but plain `ApplicationData` on Android and iOS heads built with `UnoFeatures=SkiaRenderer`, where the Uno SDK loads the storage package's plain `netX.0` build. When the default store is not encrypted, the token cache logs a `Warning` naming the store at startup, so the downgrade is visible in the app's log output. See [Key-value storage](xref:Uno.Extensions.Storage.Overview#key-value-storage).

The set of identity scenarios (Microsoft accounts, work/school accounts, B2C, sovereign clouds, ...) is determined by MSAL itself — see [MSAL.NET supported platforms and scenarios](https://learn.microsoft.com/entra/msal/dotnet/getting-started/scenarios) for details.

## Prerequisites

- **If you target Android, iOS, or WebAssembly with `UnoFeatures=SkiaRenderer`**, you need an Uno Platform version containing the fix for [unoplatform/uno#20601](https://github.com/unoplatform/uno/issues/20601) ([PR #24055](https://github.com/unoplatform/uno/pull/24055)). Before it, Uno's build-time runtime-asset selector replaced `Uno.UI.MSAL.dll` with its no-op Skia flavor on every head, so `WithUnoHelpers()` silently did nothing — no parent `Activity` on Android (MSAL fails with `activity_required`), no `UIViewController` on iOS, and no `WasmWebUi`/`WasmHttpFactory` on WebAssembly, so the sign-in UI never appeared at all. Two things make this easy to miss:
  - The fix lives in `Uno.WinUI`'s build tasks, not in `Uno.WinUI.MSAL`. These packages declare a `Uno.WinUI` floor that includes it, so an older Uno shows up as an NU1605 package-downgrade error rather than a silent no-op. If you pin or override `Uno.WinUI` below that floor, sign-in goes back to doing nothing, with no diagnostic pointing at the cause.
  - It landed after the 6.7.x releases, so no 6.7.x version has it. Use 6.8.0 or later (or a `6.8.0-dev` build), unless it is backported to a 6.7.x servicing release.
- An app registration on the Microsoft identity platform, with each platform's redirect URI registered — see [Redirect URIs](#4-redirect-uris) for the value the provider applies on each target.
- **If you target WebAssembly**, the browser redirect URI must be registered under the **`spa`** platform of the app registration, not as a public-client/native URI. This is not a formality:
  - MSAL.NET issues the token request from the browser over `fetch`, which CORS only permits for a redirect URI registered as `spa`.
  - A `spa` registration caps refresh tokens at **24 hours, non-sliding** — as opposed to the 90-day sliding tokens a public-client registration issues. That cap is what makes it acceptable to keep the token cache in browser storage at all, since browser storage is readable by any script on the origin. See [Refresh tokens](https://learn.microsoft.com/entra/identity-platform/refresh-tokens) and [the SPA cookie reference](https://learn.microsoft.com/entra/identity-platform/reference-third-party-cookies-spas).
  - Consequence to design for: re-authentication has to happen in the top-level frame, and users sign in again at least daily.

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
> `/authentication-callback`, and it must be registered under the app registration's **`spa`**
> platform (see [Prerequisites](#prerequisites)). If your registered redirect uses a different path,
> set `RedirectUri` in configuration or from the `Builder(...)` callback — both now win over the
> broker-derived default.

### 5. Android and iOS: platform setup

On desktop the redirect comes back over a local HTTP listener, on WebAssembly through the
`WebAuthenticationBroker`, and on WinAppSDK through the WAM broker — the provider handles the whole
round-trip. On Android and iOS the redirect arrives as an *OS event delivered to the app*, so the
app has to hand it to MSAL itself, using MSAL's own helper,
[`AuthenticationContinuationHelper`](https://learn.microsoft.com/dotnet/api/microsoft.identity.client.authenticationcontinuationhelper).
Without these entries the browser opens and sign-in succeeds there, but the app's `LoginAsync`
never completes — the most common MSAL symptom on mobile. iOS needs one more thing: an entitlement
granting the keychain group MSAL stores its token cache in.

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

MSAL.NET keeps its Android token cache — refresh token included — in a plain `SharedPreferences`
file under the app's private data directory. The app sandbox is its only protection: the .NET
accessor does not encrypt it (the Java MSAL library does, MSAL.NET's does not), so anything that can
read the app's data can read the tokens, and by default that includes device backups. Set
`android:allowBackup="false"` on the `<application>` element of `AndroidManifest.xml` to keep the
file out of backups, and treat a rooted device as compromised.

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

#### iOS: keychain access group

MSAL keeps its token cache in the iOS keychain, under the access group `com.microsoft.adalcache`
unless told otherwise, and iOS only lets an app reach a group its entitlements grant. The Uno
single-project template ships `Platforms/iOS/Entitlements.plist` with an empty `<dict/>`, so
without this step sign-in itself succeeds and the first token save fails:

```console
MsalClientException: missing_entitlements
The application does not have keychain access groups enabled in the Entitlements.plist ...
The keychain access group '{TeamId}.com.microsoft.adalcache' is not enabled.
```

Depending on the MSAL version the failure can come earlier still, as
`cannot_access_publisher_keychain`, while MSAL probes the keychain for the publisher's Team ID as
it builds the `IPublicClientApplication`.

Grant the group in `Platforms/iOS/Entitlements.plist`:

```xml
<key>keychain-access-groups</key>
<array>
    <string>$(AppIdentifierPrefix)$(CFBundleIdentifier)</string>
    <string>$(AppIdentifierPrefix)com.microsoft.adalcache</string>
</array>
```

`$(AppIdentifierPrefix)` is substituted at build time with the Team ID from the provisioning
profile, so the second entry resolves to exactly the group MSAL derives on its own — nothing in
code has to name it. The Uno SDK picks `Platforms/iOS/Entitlements.plist` up as
`CodesignEntitlements` on its own, and entitlements are part of the signed app, so the change
takes effect on the next deploy rather than at runtime.

The order is deliberate. Once this entitlement exists, its first entry becomes the default access
group for every keychain item the app writes without naming one, so keeping the bundle's own group
first leaves anything else the app stores there — including `ITokenCache` on a native (non-Skia)
iOS head, where the default `IKeyValueStorage` is the keychain — where it already was.

Development provisioning profiles carry `keychain-access-groups` `{TeamId}.*`, so this signs
without any extra capability; a distribution build needs Keychain Sharing enabled on the App ID.
See [Add required entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements) for the
Apple Developer portal side of that.

To use a different group — private to this app, or shared for single sign-on with other apps signed
by the same team — put it in the entitlement and pass the same value to MSAL. The provider owns the
`IPublicClientApplication`, so that goes through the `Builder(...)` callback:

```csharp
builder.AddMsal(window, msal =>
    msal.Builder(pca => pca.WithIosKeychainSecurityGroup("com.example.myapp")));
```

There is no configuration key for it: the `Msal` section binds to MSAL's
`PublicClientApplicationOptions`, which has no keychain-group property.

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
override what those set. `Builder(...)` follows the same rule: it runs after the platform redirect
URI, the Windows broker and `WithUnoHelpers()` have been applied, so anything it sets — including an
`HttpClient` factory on WebAssembly — wins.

On desktop (Skia) heads, an interactive sign-in that isn't completed times out after **5 minutes**
by default — the system-browser flow cannot detect a closed browser window, so without a timeout
an abandoned sign-in would leave the awaiting login command busy forever. The Windows broker, the
mobile browsers and the WebAssembly popup all report a dismissed sign-in themselves, so no default
applies there. Configure it on any platform via `InteractiveTimeout` in the `Msal` configuration
section (a `TimeSpan`; zero or negative waits indefinitely):

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

The provider checks once that the secure store can round-trip the cache, the first time it persists
one at a given location. It does **not** re-check on every launch: on macOS the check probes with a keychain entry whose service name MSAL randomizes on
every run, so re-checking asks the user to grant keychain access on every single start and there is
no entry for "Always Allow" to be remembered against. Two cheaper checks stand in for it: when the
probe is skipped, the first cache read — which every sign-in performs anyway — runs during setup,
so a store that has since become unreadable (a Linux session without its keyring, a revoked macOS
grant) takes the same in-memory or unprotected-file fallback a failed probe does; and a write the
store silently rejects is caught — the provider notices the cache never reached disk, logs an error
alongside the cause `MsalCacheHelper` reported, and retries the setup once.

Set `VerifyCachePersistence` in the `Msal` configuration section to change when the check runs:

| Value | Behavior |
| --- | --- |
| `Auto` (default) | Check only when nothing has been persisted at this location yet. |
| `Always` | Check on every storage setup. Expect a keychain prompt on every launch on macOS. |
| `Never` | Never check, not even on a first run. A store that cannot be written to is reported after the first real write is attempted. |

```json
{
  "Msal": {
    "ClientId": "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b",
    "VerifyCachePersistence": "Always"
  }
}
```

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

On Android and iOS the token cache is persisted natively by MSAL — no configuration is needed (or honored) on those platforms. On iOS it lands in a keychain access group the app has to grant first; see [iOS: keychain access group](#ios-keychain-access-group).

#### WebAssembly token cache

In the browser there is no protected store to write to, so the cache goes to browser storage in cleartext and what bounds the exposure is *lifetime*, not encryption.

The default is `localStorage` — the store WebAssembly already used for the token cache before this setting existed, so upgrading the package never relocates an app's data. It is **not** MSAL.js's default: msal-browser defaults to `sessionStorage`, and if you are starting fresh that is the tighter choice, because the serialized cache is dropped when the tab closes rather than persisting across browser restarts. Opt into it explicitly:

It is a **storage** setting, not an MSAL one — it selects the host's single default key-value store, which the token cache shares with everything else built on it:

```json
{
  "KeyValueStorageConfiguration": {
    "BrowserCacheLocation": "SessionStorage"
  }
}
```

| Value | Behavior |
| --- | --- |
| `LocalStorage` (default) | Survives a page reload, closing the tab, and restarting the browser — the widest window in which a stolen cache stays usable. The default only because it is what WebAssembly already used. |
| `SessionStorage` | Survives a page reload; cleared when the tab closes. Matches MSAL.js and is the tighter choice for credentials. |
| `MemoryStorage` | Nothing is written to browser storage; the user signs in again after every reload. |

An invalid value throws while the host is being built rather than silently falling back.

One setting covers the Uno token cache (the access token) and the MSAL cache (the refresh and ID tokens) — splitting them would let the access token outlive both the tab and the refresh token. Because it belongs to storage rather than to a provider, it applies whatever you name your provider (`AddMsal(window, name: "MyMsal")`) and whichever provider you use. It is ignored on every other platform, where the platform's own protected store applies.

Signing out removes both: `LogoutAsync` removes every signed-in account and then deletes the serialized MSAL cache. Note this clears *our* storage, not the identity provider's session cookie — the next "Sign in" may complete without a prompt because the IdP still recognises the browser. Use the `end_session_endpoint` if you need a full sign-out. Clearing `ITokenCache` directly has the same storage effect.

> [!IMPORTANT]
> Whichever persistent option you pick, the serialized cache holds the **refresh token** in cleartext, readable by any script on the origin. What bounds the exposure is that your WebAssembly redirect URI must be registered under the Entra **`spa`** platform (see [Prerequisites](#prerequisites)), which caps refresh tokens at **24 hours, non-sliding**. A public-client registration issues a **90-day sliding** token instead, and nothing in the library can detect which one your tenant issued — the provider logs a warning naming the store on every unprotected persist, but the registration type is yours to get right. See [Refresh tokens in the Microsoft identity platform](https://learn.microsoft.com/entra/identity-platform/refresh-tokens).

For full control over the storage properties on desktop targets, use the `Storage()` extension method to configure the underlying `StorageCreationPropertiesBuilder`:

```csharp
builder.AddMsal(window, msal =>
    msal.Storage(store => store.WithMacKeyChain("com.contoso.myapp.msal", "MyAppCache")));
```

### 8. Use the provider in your application

> [!IMPORTANT]
> Sign-in must be given an `IDispatcher`: call
> `IAuthenticationService.LoginAsync(dispatcher, ...)`, not the `LoginAsync(credentials, provider, cancellationToken)`
> convenience overload. That overload passes no dispatcher, and the MSAL provider throws
> `ArgumentNullException` — including when the sign-in would have completed silently from a cached
> account, because the check happens before the silent attempt. Inject `IDispatcher` into your view
> model and pass it through.
>
> Sign-*out* needs no dispatcher: `LogoutAsync(cancellationToken)` works.

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
