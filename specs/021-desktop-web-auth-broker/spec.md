# 021 - Skia Desktop: a loopback WebAuthenticationBroker for the Web and OIDC providers

**Status: implemented in PR #3261.** `progress.md` carries the review-panel findings, the red/green
runs and the verification trail; this file states the design the code is held to.

## Symptom

Uno Platform has no `WebAuthenticationBroker` on the Skia Desktop target (`net10.0-desktop` on
Windows, macOS and Linux): Uno's `WebAuthenticationBrokerProvider.skia.cs` throws
`NotImplementedException`, and no desktop runtime package registers a replacement. `AddOidc()`
routes every interactive step through `WebAuthenticatorBrowser`, and `AddWeb()` calls the broker
directly, so both providers fail every sign-in on desktop (`Error logging in:
System.NotImplementedException`, `LoginAsync` returns `false`). MSAL is unaffected: MSAL.NET brings
its own system browser and loopback listener.

## Decision

Ship `DesktopWebAuthenticationBrokerProvider` in `Uno.Extensions.Authentication.WinUI` (compiled out
on WinAppSDK, `#if !WINDOWS`, where WinUIEx handles activation) and register it from `AddWeb()` and
`AddOidc()`. It follows RFC 8252 section 7.3 (loopback redirect for native apps):

1. Open the request URI in the system browser (`LaunchBrowserAsync`, `protected virtual`).
2. Listen with `HttpListener` on the callback URI's loopback port, and complete the flow from the
   first request to the callback path.
3. Hand the response back as a `WebAuthenticationResult`, rebuilt from the callback URI as the
   caller wrote it (not the normalized `Uri`), so callers that match the result textually keep
   working.

### Registration

- `TryRegister()` registers through `ApiExtensibility.Register(typeof(IWebAuthenticationBrokerProvider), ...)`.
  The registry is first-wins (`TryAdd`), so an app that registers its own provider *before*
  `AddWeb`/`AddOidc` keeps it, and one registered after is ignored. After registering, the
  registry is resolved once to confirm which provider won, since `Register` is a silent no-op when
  another provider got in first.
- Registration is gated at runtime on `OperatingSystem.IsWindows() || IsLinux() || IsMacOS()`, not
  at compile time: the plain-TFM build of the assembly is what Skia mobile heads load, and Android,
  iOS and the browser must keep their native brokers.
- It runs at host-builder time, because Uno's static `WebAuthenticationBroker` resolves its provider
  once, in its static constructor; anything that touches it earlier pins the throwing stub for the
  process lifetime. `WebAuthenticatorBrowser` turns that stub's `NotImplementedException` into an
  error naming the registration order.
- The outcome is logged through the host's logger once the host is built (an `IServiceInitialize`,
  run by the Uno host only); the broker itself logs to Uno's ambient logger because the registry,
  not DI, creates it.

### Callback URI

- Loopback HTTP only (`IsLoopback` and `http`), otherwise `ArgumentException`: the callback carries
  the authorization response and must never be served on another interface. Requests whose
  `RemoteEndPoint` is not loopback get a 404, since http.sys routes by `Host` header.
- `GetCurrentApplicationCallbackUri()` returns `http://localhost:{port}/authentication-callback`
  with a port probed free on IPv4 and IPv6 loopback once per process, or
  `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` when set. A bind failure on
  the default port forgets it, so the next flow probes again.
- The request URI must be `http` or `https`: the OS URL handler dispatches on scheme.

### Response shapes

- Query string (authorization-code flows): one request.
- `response_mode=form_post`: the urlencoded body, read up to 32 KB, returned in query form.
  Oversized or truncated bodies are answered 400 and the flow keeps listening.
- URL fragment (implicit-style flows): browsers never send fragments, so a bare callback hit gets a
  static relay page whose script re-requests the callback with the fragment as a marked query (or a
  no-fragment sentinel); the result is restored to its fragment shape.
- Every page served is static (no reflection of request data) and `Cache-Control: no-store`.

### Flow control

- One flow at a time per broker instance (there is one per process): a second concurrent flow
  throws `InvalidOperationException` instead of failing to bind.
- The caller's `CancellationToken` cancels the flow. The broker's own
  `AuthenticationTimeout` returns `UserCancel` with `ResponseErrorDetail = TimeoutErrorDetail`
  (408), since WinRT has no timeout status.
- A failed browser launch never logs or rethrows the request URL (it can carry `login_hint` and
  `id_token_hint`).

### OIDC provider changes (every platform)

- `WebAuthenticatorBrowser` maps the broker result to `BrowserResultType` (`Success`, `UserCancel`,
  `Timeout` for both timeouts, `HttpError`) instead of collapsing failures into `UnknownError`.
- The caller's cancellation propagates as `OperationCanceledException` out of
  `IAuthenticationService.LoginAsync`/`LogoutAsync`, where a cancelled login used to return `false`
  and clear the stored tokens. This is a behavior change, documented in `UpdatingExtensions.md`.
- Sign-out sends the cached id_token as `id_token_hint`, which identity providers need before they
  redirect back to `PostLogoutRedirectUri`.
- With `AutoRedirectUri`, the redirect is re-read from the broker before each flow, so a re-probed
  port is followed.

## Out of scope

- Binding the Web provider's plain token-on-redirect response to the flow that started it (a
  `state` value): neither the broker nor the Web provider does it; the How-To page tells apps to,
  and #3169 tracks doing it in the provider.
- An opt-out for when Uno ships its own desktop broker.

## Tests

`Uno.Extensions.Authentication.WinUI.Tests` (a `*.UI.Tests` project, run by the desktop runtime-test
lane through `RuntimeTestsFilter`; the broker suite compiles for the desktop TFMs only):
`Given_DesktopWebAuthenticationBroker` (listener round trip for each response shape, callback
validation, cancellation, timeout detail, default-port re-probe, launch failure, token-leak guard on
the log),
`Given_DesktopWebAuthenticationBrokerRegistration`, `Given_WebAuthenticatorBrowser`, and
`Given_OidcAuthenticationProvider`.
