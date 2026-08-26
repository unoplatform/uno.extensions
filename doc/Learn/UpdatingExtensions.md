---
uid: Uno.Extensions.Migration
---

# Upgrading Extensions Version

## Upgrading to Extensions 7.4

### Minimum Uno Platform version

Extensions 7.4 requires **Uno Platform 6.8**. Every `*.WinUI` package declares an `Uno.WinUI` floor
of `6.8.0-dev.46` — that is, while 6.8 is in preview, a **`6.8.0-dev` build** is required (this
release was built against `Uno.Sdk` `6.8.0-dev.21`); once a stable 6.8.0 ships it satisfies the floor
too. An older Uno fails at restore with a clear error rather than misbehaving at runtime:

```console
error NU1605: Detected package downgrade: Uno.WinUI from 6.8.0-dev.46 to 6.7.24
```

Update the Uno SDK version in your `global.json` — see [updating your Uno.Sdk](xref:Uno.Development.UpgradeUnoNuget):

```diff
 {
   "msbuild-sdks": {
-    "Uno.Sdk": "6.7.24"
+    "Uno.Sdk": "6.8.0-dev.21"
   }
 }
```

For apps using [MSAL authentication](xref:Uno.Extensions.Authentication.HowToMsalAuthentication) this
is not a formality. Interactive sign-in on Android, iOS and WebAssembly heads built with
`UnoFeatures=SkiaRenderer` depends on the fix for
[unoplatform/uno#20601](https://github.com/unoplatform/uno/issues/20601), which shipped in `Uno.WinUI`
after the 6.7.x releases. Without it `WithUnoHelpers()` silently does nothing and the sign-in UI never
appears at all.

`Microsoft.Identity.Client` also moves from 4.72.1 to 4.87.0, and `System.Text.Json` from 8.0.x to
9.0.x. If your app pins either package explicitly, raise the pin or remove it and let the Uno SDK
supply it.

### MSAL authentication behavior changes

These are binary-compatible but observable. Read them if your app calls `AddMsal`.

- **Mac Catalyst: `AddMsal` now throws.** MSAL has no Catalyst build, so the provider was always a
  stub there; `AddMsal` used to return silently and every later auth call failed with
  "No providers specified". It now throws `PlatformNotSupportedException` at host build, so a shared
  `App.xaml.cs` that registers MSAL crashes at startup on a Catalyst head. Guard the registration:

  ```csharp
  .UseAuthentication(auth =>
  {
      if (!OperatingSystem.IsMacCatalyst())
      {
          auth.AddMsal(window);
      }
  })
  ```

- **WebAssembly: your redirect URI is no longer overridden.** The provider used to apply Uno's
  `WebAuthenticationBroker` callback URI *after* your `Builder(...)` callback, so a
  `WithRedirectUri(...)` or a configured `Msal:RedirectUri` was silently replaced in the browser. It
  now applies the broker URI only when you set nothing — the
  [documented precedence](xref:Uno.Extensions.Authentication.HowToMsalAuthentication#4-redirect-uris).
  If you set a redirect URI unconditionally for another platform (for example an Android scheme),
  either remove it — the provider derives the Android and iOS values — or guard it with
  `OperatingSystem.IsBrowser()`; otherwise WebAssembly sign-in fails with a redirect-URI mismatch.

- **WebAssembly: the token cache is persisted by default.** Before 7.4 the MSAL cache lived in
  memory only, so a page reload meant signing in again. It is now serialized through the host's
  default `IKeyValueStorage` — `localStorage`, under the key `MsalCache_{ClientId}` — and therefore
  holds the **refresh token** in cleartext browser storage. Register the redirect URI under the
  Entra `spa` platform so that token is capped at 24 non-sliding hours (see the how-to's
  [prerequisites](xref:Uno.Extensions.Authentication.HowToMsalAuthentication#prerequisites)). To
  keep the pre-7.4 behavior set `KeyValueStorageConfiguration:BrowserCacheLocation` to
  `MemoryStorage`; `SessionStorage` is the middle ground. Note that switching to `MemoryStorage`
  (or downgrading) does not delete an entry a previous run left in `localStorage` — sign out first,
  or clear the site's data.

- **`Builder(...)` runs last.** Your `PublicClientApplicationBuilder` callback now runs after the
  platform redirect URI, the Windows broker and `WithUnoHelpers()` have been applied, so what it sets
  wins. Previously `WithUnoHelpers()` ran after it and, on WebAssembly, replaced an `HttpClient`
  factory set from the callback.

- **Interactive sign-in timeout.** Desktop (Skia) heads now cancel an interactive sign-in that has
  not completed after 5 minutes — the system-browser flow cannot see a closed browser window. No
  default applies on Windows, Android, iOS or WebAssembly. Set `Msal:InteractiveTimeout` to change
  or disable it on any platform.

- **iOS: MSAL's keychain entitlement is now reachable.** On Skia iOS heads `AddMsal` used to
  register a stub, so nothing ever touched MSAL's cache. Now that sign-in runs there,
  `Platforms/iOS/Entitlements.plist` has to grant `keychain-access-groups`
  `$(AppIdentifierPrefix)com.microsoft.adalcache` — the template ships that file empty, and without
  the group the first token save fails with `missing_entitlements`. See
  [iOS: keychain access group](xref:Uno.Extensions.Authentication.HowToMsalAuthentication#ios-keychain-access-group).

- **Sign-out removes every MSAL account**, not just the first, and also deletes the serialized cache
  entry on WebAssembly.

- **A refresh that cannot renew the session signs the user out.** When the refresh token has expired
  or been revoked, `RefreshAsync` now returns `false`, clears the token cache and raises
  `IAuthenticationService.LoggedOut` — the same as an explicit sign-out. It used to keep the user
  "authenticated" with an empty access token. A refresh that fails for a *transient* reason (the
  token endpoint unreachable, a 5xx) keeps the current tokens and does not sign out.

- **`LoginAsync` rethrows MSAL exceptions untouched.** They used to be re-wrapped — an
  `MsalClientException` into a new one carrying only the code and message, anything else into a
  plain `Exception` with only the message. Callers now get the original type, error code and stack,
  so `catch (MsalServiceException)` / `catch (MsalUiRequiredException)` work; a `catch (Exception)`
  keeps working.

- **Removed:** the vendored `Microsoft.Identity.Client.Extensions.Msal.Wasm.Storage` type that the
  `browserwasm` build of `Uno.Extensions.Authentication.MSAL.WinUI` used to carry. It was dead code
  — nothing in the package referenced it — and the browser cache now goes through
  `IKeyValueStorage` as described above.

### Storage

- `IKeyValueStorage.IsEncrypted` is now reported truthfully: the Windows `EncryptedApplicationDataKeyValueStorage` (DPAPI) and `PasswordVaultKeyValueStorage` (Credential Locker) return `true`; the browser and plain `ApplicationData` stores return `false`. Code that branched on it to decide whether a store is safe for tokens gets the right answer now.
- `ISettings` (used by the stores on unpackaged Windows) is registered by `UseStorage` itself, so storage no longer depends on the app also calling `UseToolkit` / `UseThemeSwitching`. An `ISettings` the app registers itself still wins.
- On WebAssembly the default store is selectable via `KeyValueStorageConfiguration:BrowserCacheLocation` (`LocalStorage` — the default — `SessionStorage` or `MemoryStorage`); see [Key-value storage](xref:Uno.Extensions.Storage.Overview#key-value-storage). The default is what WebAssembly already used, so upgrading never relocates an app's data.

## Upgrading to Extensions 7.0

### Web and OIDC Authentication behavior changes

The Web and OIDC providers received a set of deliberate behavior corrections and platform additions. Most apps need no code changes — the differences show up as more truthful outcomes:

- **Failed silent refresh reports failure (OIDC).** A refresh the identity provider rejected used to be reported as success with an empty access token, leaving `IsAuthenticated` true with nothing to send. It now returns `false` and clears the session.
- **Cancelled sign-in preserves the session (Web).** Backing out of the sign-in UI now surfaces as `OperationCanceledException` and leaves previously cached tokens untouched; it used to return an empty token set that wiped them. A failed flow (HTTP error) returns `false`.
- **Cancelled or failed sign-out keeps the session (Web and OIDC).** `LogoutAsync` now returns `false` and keeps the token cache when the end-session flow is dismissed or fails; it used to clear the cache regardless.
- **OIDC logout sends `id_token_hint`.** The cached id_token is passed to the end-session endpoint, so compliant identity providers skip the logout confirmation prompt and redirect back to the app. Without it, desktop logout could hang until the broker timeout.
- **Cancellation reaches the interactive flow.** The `CancellationToken` passed to `LoginAsync`/`LogoutAsync` now cancels the browser interaction on every platform.
- **Skia Desktop sign-in works out of the box.** `AddWeb()`/`AddOidc()` automatically register a loopback `WebAuthenticationBroker` (system browser + `localhost` listener), including relay support for URL-fragment responses. See [Web Authentication: Platform support](xref:Uno.Extensions.Authentication.HowToWebAuthentication#platform-support).
- **Web callback defaults (behavior change).** When no callback is configured, `AddWeb` now derives it from `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` instead of failing with a warning, and the literal `{RedirectUri}` token in `LoginStartUri`/`LogoutStartUri` is replaced with the URL-encoded effective callback. A `redirect_uri={RedirectUri}` pair is no longer treated as a literal callback value. See [Web Authentication: One configuration for every platform](xref:Uno.Extensions.Authentication.HowToWebAuthentication#one-configuration-for-every-platform).
- **`PrefersEphemeralWebBrowserSession` is honored on Skia iOS heads**, where it was previously lost to build-time platform selection.
- **Unencrypted token stores are reported.** The token cache behind `IAuthenticationService` now logs a `Warning` at startup naming the store when the host's default `IKeyValueStorage` does not encrypt — plain `ApplicationData` on Skia Desktop and on Skia-renderer Android/iOS heads, browser storage on WebAssembly. Where tokens go is unchanged; the warning makes the downgrade visible so a protected store can be registered instead. See [Key-value storage](xref:Uno.Extensions.Storage.Overview#key-value-storage).

### OidcClient Authentication

When upgrading to Uno.Extensions 7.0 or later, the NuGet Package Dependency, before known as `IdentityModel.OidcClient`, which is used in the [Oidc Authentication Extension](xref:Uno.Extensions.Authentication.HowToOidcAuthentication), has been [rebranded](https://github.com/DuendeSoftware/foss/blob/main/README.md#relationship-to-identitymodel).

When upgrading to later versions, you must make sure to update the Namespaces in your App, to match the new ones. e.g. in your `GlobalUsings.cs` file at your Project root directory:

```diff
- global using IdentityModel.OidcClient
+ global using Duende.IdentityModel.OidcClient
```

### MSAL Authentication behavior changes

The MSAL provider received a set of deliberate behavior corrections and platform additions. Most apps need no code changes — the differences show up as more truthful outcomes and working platforms:

- **Minimum Uno Platform 6.8.** Below it, the runtime-asset selector replaces `Uno.UI.MSAL` with a no-op build on Skia mobile and WebAssembly heads, so `WithUnoHelpers()` silently does nothing and interactive sign-in never appears. The package floor turns that silent no-op into an `NU1605` restore error. See [MSAL prerequisites](xref:Uno.Extensions.Authentication.HowToMsalAuthentication).
- **Redirect URIs are derived per platform** when configuration does not supply one: `msal{ClientId}://auth` on Android, `msauth.{BundleId}://auth` on iOS, the `WebAuthenticationBroker` URI on WebAssembly, and MSAL's default loopback redirect on desktop. WinAppSDK is untouched (the WAM broker owns it there). An app-supplied `RedirectUri` always wins.
- **The app's `Builder(...)` callback wins** over the platform defaults and `WithUnoHelpers()`; it used to be overwritten on WebAssembly.
- **Mac Catalyst throws at registration.** `AddMsal` now fails immediately with `PlatformNotSupportedException` instead of silently registering nothing and failing far from the cause.
- **The WebAssembly token cache is persisted by default**, so sign-in survives a page reload. Select the store with `KeyValueStorageConfiguration:BrowserCacheLocation` (`LocalStorage` default, `SessionStorage`, or `MemoryStorage`); browser storage is origin-readable, so prefer `SessionStorage` for the tightest lifetime. The WebAssembly redirect URI must be registered under the **spa** platform of the app registration.
- **An unrenewable refresh signs the user out.** When MSAL reports that interaction is required, `RefreshAsync` now returns `false`, clears the session, and raises `LoggedOut`; it used to report success with an empty access token, leaving `IsAuthenticated` true with nothing to send. Transient failures keep the tokens.
- **Sign-out removes every MSAL account** and the serialized cache — previously only the first account was removed, leaving the rest silently renewable — and the token cache is cleared even when the provider throws.
- **MSAL exceptions propagate untouched** (`MsalClientException`, `MsalServiceException`, …), so callers can inspect the MSAL error codes; they were previously flattened.
- **Abandoned interactive sign-ins are cancelled** after 5 minutes by default — closing the system browser is undetectable on desktop — configurable via `InteractiveTimeout` in the `Msal` configuration section.
- **The token-cache persistence check runs once per cache, not once per launch.** It used to probe the platform's secure store on every storage setup. On macOS that probe is a keychain entry whose service name MSAL randomizes each run, so the OS asked for keychain access on every single launch and "Always Allow" could never take effect. The check now runs only when nothing has been persisted yet, and a write that the store silently rejects is detected and reported instead. Restore the old behavior with `"VerifyCachePersistence": "Always"` in the `Msal` configuration section.

## Upgrading to Extensions 6.0

The Uno SDK dependencies for Uno Extensions have been updated to Uno SDK version 6.0. Ensure to [update your uno.sdk](xref:Uno.Development.UpgradeUnoNuget) to the latest version.

## Upgrading to Extensions 5.2

### MSAL Authentication

When upgrading to Uno.Extensions 5.2 or later, you must update your MSAL authentication setup in the host configuration. The `AddMsal()` method now includes an additional parameter to specify the `Window` instance used by the `MsalAuthenticationProvider` to configure the authentication dialog. You can obtain the `Window` instance from the `Configure()` method overload that provides it:

```diff
private IHost Host { get; set; }

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
-       .Configure(host =>
+       .Configure((host, window) =>
        {
            host
            .UseAuthentication(builder =>
            {
-               builder.AddMsal();
+               builder.AddMsal(window);
            });
        });
    ...
}
```

> [!IMPORTANT]
> Failing to pass a valid `Window` instance could result in a `MsalClientException` with the message:
> *"Only loopback redirect uri is supported, but <your_redirect_uri> was found. Configure `http://localhost` or `http://localhost:port` both during app registration and when you create the PublicClientApplication object. See `https://aka.ms/msal-net-os-browser` for details."*
