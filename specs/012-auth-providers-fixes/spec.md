# 012 — Oidc / Web / Custom auth providers: port the 009–011 MSAL fixes, close the Skia Desktop sign-in gap

**Status: in progress.** Written 2026-08-21 on branch `dev/sb/auth-providers-fixes` (forked from
`dev/sb/msal-auth-fixes`). Read `specs/009-msal-auth-fixes/`, `specs/010-msal-skia-mobile-runtime-dispatch/`
and `specs/011-wasm-msal-token-cache/` first — this spec applies the same classes of fix to the three
non-MSAL providers and reuses the 009 test architecture (stub identity server + stub interactive surface,
hosted in a `*.UI.Tests` project that rides the runtime-test lanes).

## Scope

Three providers, audited 2026-08-21 against the MSAL branch's fixes:

| Provider | Assembly | Interactive surface |
| --- | --- | --- |
| Oidc (`OidcAuthenticationProvider`) | `Uno.Extensions.Authentication.Oidc.WinUI` | Duende OidcClient → `IBrowser` → `WebAuthenticationBroker` (WinUIEx on `WINDOWS`) |
| Web (`WebAuthenticationProvider`) | `Uno.Extensions.Authentication.WinUI` | `WebAuthenticationBroker` (WinUIEx on `WINDOWS`) |
| Custom (`CustomAuthenticationProvider`) | `Uno.Extensions.Authentication` | none (caller-supplied delegates) |

Already inherited from the parent branch with no work needed here: build-each-provider-exactly-once
(`ProviderFactory` lazy), the storage stack (WASM key-value stores, DPAPI unpackaged fix, truthful
`IsEncrypted`), and the `AuthenticationService` provider-dictionary fix.

## Findings

### F1 — Oidc: silent refresh reports success when the token endpoint returns an error

`OidcAuthenticationProvider.InternalRefreshAsync` never checks `result.IsError`. It then tests the
*stale* local `token` variable (the old refresh token, always non-null at that point) instead of the
result, and stores `result.AccessToken` — null on error — into the credentials dictionary. Net effect:
a failed refresh leaves the user looking authenticated with a dead token. Mirror of MSAL fix
`b3a3e3093` ("stop reporting success when a silent refresh fails"). Also: `RefreshTokenAsync(token)`
is not passed the `CancellationToken`.

### F2 — Oidc: logout ignores the result and the cancellation token

`InternalLogoutAsync` fires `_client.LogoutAsync()` with no `CancellationToken`, never inspects
`LogoutResult.IsError`, and returns `true` unconditionally — the local cache is flushed even when the
end-session round trip failed or the user cancelled it. Related to MSAL fix `a260ebf75`.

### F3 — Oidc: `WebAuthenticatorBrowser` swallows cancellation

The catch-all in `WebAuthenticatorBrowser.InvokeAsync` converts `OperationCanceledException` into
`BrowserResultType.UnknownError`, so a caller-cancelled login surfaces as "login failed" instead of
propagating cancellation. The two `CancellationTokenSource`s are also never disposed.

### F4 — Web: no cancellation or timeout reaches the interactive surface

Neither `InternalLoginAsync` nor `InternalLogoutAsync` passes a `CancellationToken` to the broker:
the `WINDOWS` path calls the ct-less `WinUIEx.WebAuthenticator.AuthenticateAsync` overload (the
vendored copy *has* a ct overload), and the broker path calls `.AsTask()` with no token. Uno's broker
has its own 5-minute default (`WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout`);
the WinUIEx path waits forever. Port MSAL's interactive-timeout treatment (`534fcec`, `MsalConfiguration.InteractiveTimeout`).

### F5 — Web: a cancelled login wipes the previous session's tokens

On user-cancel the broker returns non-Success and `ResponseData` is empty; the provider still returns
an **empty (non-null) dictionary**, and `TokenCache.SaveAsync` unconditionally `ClearAllAsync`s before
writing. So cancelling a re-login destroys the tokens the user still had. MSAL semantics (after
`a260ebf75`): user-cancel must surface as `OperationCanceledException` *before* any save, leaving the
cache untouched; a genuine error returns null. The provider must inspect `WebAuthenticationResult.ResponseStatus`
(`UserCancel` → throw OCE; `ErrorHttp` → null) instead of ignoring it.

### F6 — Web: logout ignores the broker result

`InternalLogoutAsync` discards `userResult` entirely and returns `true` — same shape as F2.

### F7 — Web: `PrefersEphemeralWebBrowserSession` dies on Skia iOS heads (spec-010 mechanism)

The setting is applied under `#if __IOS__`. Both `Authentication.WinUI` and `Authentication.Oidc.WinUI`
reference `Uno.UI` and ship plain-TFM siblings, so Uno's runtime-asset selector substitutes the **plain
`net9.0` lib** onto Skia iOS/Android heads (verified mechanism in spec 010) — and the plain lib has the
`#if __IOS__` block compiled out. Unlike MSAL-before-010 this is not a total stub (the rest of the
provider is functional; broker calls bind to the native `Uno.dll` at runtime), but the ephemeral-session
setting silently no-ops. Fix per spec 010: runtime dispatch — apply the setting when
`OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()` (verify `WinRTFeatureConfiguration`'s
property is present on the plain TFM at implementation time), no `#if`.

### F8 — Platform gap: no sign-in on Skia Desktop for Oidc and Web

Verified in unoplatform/uno source (`src/Uno.UWP/Security/Authentication/Web/`): the Skia flavor of
`WebAuthenticationBrokerProvider` **throws `NotImplementedException`** from `AuthenticateAsyncCore`
and `GetApplicationCustomSchemes`, and nothing in the Uno repo registers a desktop implementation.
`WebAuthenticationBroker`'s static ctor resolves the provider via
`ApiExtensibility.CreateInstance<IWebAuthenticationBrokerProvider>` first, falling back to the built-in.
So on `net9.0-desktop`, Oidc and Web sign-in throw today. MSAL is unaffected only because MSAL ships
its own system-browser + loopback-listener flow.

**Decision: fix by shipping a desktop `IWebAuthenticationBrokerProvider`** — see design below.

### F9 — Windows (WinAppSDK) is packaged-only

The vendored WinUIEx authenticator requires a packaged app with a protocol declaration
(`Package.Current` + `AppxManifest.xml` probing). Unpackaged apps fail. Out of scope to fix here
(WinUIEx upstream constraint); document it in `HowTo-OidcAuthentication.md` / `HowTo-WebAuthentication.md`.

### F10 — Custom provider: sound

No platform surface, no MSAL-class bugs. Only gap is test coverage (none exists — there is no plain
`Uno.Extensions.Authentication.Tests` project at all).

## Platform support matrix (current state, before this spec's fixes)

| Lane | Custom | Web | Oidc |
| --- | --- | --- | --- |
| Android (native + Skia renderer) | ✅ | ✅ CustomTabs | ✅ |
| iOS (native + Skia renderer) | ✅ | ✅ (F7 on Skia) | ✅ |
| WASM | ✅ | ✅ popup/redirect | ✅ (IdP must permit CORS) |
| net9.0-desktop (Skia Desktop) | ✅ | ❌ F8 | ❌ F8 |
| Windows (WinAppSDK) | ✅ | ⚠️ F9 packaged-only | ⚠️ F9 packaged-only |

## Design: desktop `IWebAuthenticationBrokerProvider` (F8)

One implementation covers both Oidc and Web (and any consumer app calling `WebAuthenticationBroker`
directly), because both route their non-`WINDOWS` interactive flow through the broker statics.

- **Shape**: loopback listener + system browser, per RFC 8252 §7.3 (native-app loopback redirect).
  `AuthenticateAsync(options, requestUri, callbackUri, ct)`:
  1. Validate `callbackUri` is loopback HTTP (`localhost` / `127.0.0.1`); reject anything else with a
     clear exception — never listen on a non-loopback interface.
  2. Bind an `HttpListener` to the exact callback URI, launch the system browser at `requestUri`
     (prefer `Windows.System.Launcher.LaunchUriAsync` if implemented on Skia Desktop; else
     `Process.Start(UseShellExecute: true)` / `open` / `xdg-open` per OS).
  3. Await exactly one request matching the callback path (404 anything else), honoring `ct` and
     `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout`.
  4. Respond with a static "you can close this window" page — **static HTML only, never echo query
     content** (reflected-XSS guard), then return `WebAuthenticationResult` with the full callback URL
     as `ResponseData`, `Success` status; `ct` fired → `UserCancel`.
- **`GetCurrentApplicationCallbackUri()`**: pick a free ephemeral port on first call and cache it for
  the process lifetime → `http://localhost:{port}/authentication-callback`. Supports Oidc
  `AutoRedirectUri` on desktop (requires the IdP to allow variable-port loopback redirects, which
  RFC 8252 mandates and Entra/Duende honor). Explicitly configured redirect URIs bypass this.
- **Where it lives**: `Uno.Extensions.Authentication.UI` (ships in `Uno.Extensions.Authentication.WinUI`,
  which Oidc already references). Compiled without `#if` into the shared body.
- **Registration**: imperative `ApiExtensibility.Register` at `AddWeb`/`AddOidc` time — **not** an
  assembly-level `[ApiExtension]` attribute. Rationale (spec-010 hazard): the plain-TFM lib is
  substituted onto Skia *mobile* heads, where the native broker works and must not be displaced; an
  assembly attribute is discovered at build time and can't distinguish. Gate the registration at
  runtime: only when running on Windows/macOS/Linux desktop (`!IsAndroid() && !IsIOS() && !IsBrowser()`
  and not the `WINDOWS` TFM path, which uses WinUIEx), and only when no provider is already registered.
  Registration happens during host building — before anything can touch `WebAuthenticationBroker`'s
  static ctor.
- **Test seam**: the browser launch is overridable (virtual or injected delegate), so the desktop-lane
  runtime test drives the "browser" by issuing a plain HTTP GET to the callback with the expected
  query — no real browser in CI.

## Test architecture (mirrors 009)

- **`Uno.Extensions.Authentication.Oidc.UI.Tests`** (new, shape of `Authentication.MSAL.UI.Tests`,
  referenced by the RuntimeTests head + `Uno.Extensions-runtimetests.slnf`):
  - `StubOidcServer`: `HttpMessageHandler` behind `OidcClientOptions.HttpClientFactory`, plus
    `ProviderInformation` to skip discovery. Token endpoint mints unsigned-validated JWTs
    (`Policy.RequireIdentityTokenSignature = false`) with correct `iss`/`aud`/`nonce`/`exp`; refresh
    grant switchable to `invalid_grant` for the F1 red test.
  - `StubBrowser : IBrowser` registered in DI *after* `AddOidc` (last registration wins): parses
    `state`/`redirect_uri` from `options.StartUrl`, returns the canned `?code=…&state=…` response.
  - Runs on all four lanes (Duende OidcClient is platform-neutral; no window needed — `AddOidc`
    takes none).
- **`Uno.Extensions.Authentication.UI.Tests`** (new, for the Web provider): stub
  `IWebAuthenticationBrokerProvider` registered via `ApiExtensibility.Register` before first broker
  use. Valid on all four CI lanes (none compile `#if WINDOWS`); the desktop lane needs it regardless
  (F8). Desktop-broker end-to-end tests also live here, desktop-gated.
- **`Uno.Extensions.Authentication.Tests`** (new, plain net9.0): Custom provider
  login/refresh/logout/cancellation through the hosting API — no UI host needed since the Custom
  provider lives in the core package. Picked up by package CI's `**/*.Tests.dll` filter automatically
  (keep it discoverable — `TreatNoTestsAsError=true`).
- **CI**: widen `RuntimeTestsFilter` in `build/ci/.azure-pipelines.yml` from
  `'Authentication.MSAL.UI.Tests'` to cover the new namespaces (e.g. `'Authentication'`); the WASM
  lane's deliberately narrower filter gets widened in its own commit once the new suites are green
  there (see the comment in `stage-runtime-tests-wasm.yml`).

Every bug fix follows red/fix/green; the failing test is committed with the fix.

## Out of scope

- F9 (unpackaged WinAppSDK) — docs only.
- Making `TokenCache.SaveAsync`'s clear-before-write semantics safer service-wide (behavior is shared
  with MSAL and predates this branch; F5 is fixed at the provider level to match MSAL semantics).
- tvOS (`WebAuthenticationBroker` is compiled out there entirely).
