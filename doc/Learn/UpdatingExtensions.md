---
uid: Uno.Extensions.Migration
---

# Upgrading Extensions Version

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
