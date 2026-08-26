# 015 — Web provider: broker-derived redirect defaults and the {RedirectUri} placeholder

**Status: in progress.** Written 2026-08-24 on branch `dev/sb/auth-providers-fixes`, following the
spec-012 work. Motivation: PR #3139 gave MSAL per-platform redirect derivation and AddOidc has
`AutoRedirectUriFromWebAuthenticationBroker`, but `AddWeb` still requires the app to supply the
callback per platform - either a per-head configuration value or hand-written `Prepare*`
callbacks calling `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()`.

## Design

Two additive behaviors in `WebAuthenticationProvider`, no new API surface:

1. **Broker-derived callback default.** When no callback can be resolved from configuration
   (`LoginCallbackUri` absent, and no usable `redirect_uri` inside `LoginStartUri`), the provider
   falls back to `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` - the platform's
   custom scheme on Android/iOS, the app origin on WebAssembly, the loopback listener on Skia
   Desktop. Same fallback for the logout callback chain. Not applied on the WinAppSDK path
   (`#if WINDOWS` - a compile-time gate is correct there, since windows10 TFMs are never
   substituted): the WinRT broker would answer `ms-app://…`, which is wrong for the WinUIEx
   protocol-activation flow, so WinAppSDK keeps requiring explicit configuration.

2. **`{RedirectUri}` placeholder.** After the callback is resolved, the literal token
   `{RedirectUri}` inside `LoginStartUri`/`LogoutStartUri` is replaced with the URL-encoded
   callback. This is what makes the default usable from pure configuration: an authorize URL must
   carry the redirect too, and a static config value cannot know the per-platform callback.

   ```json
   "Web": {
     "LoginStartUri": "https://idp.example/authorize?client_id=...&redirect_uri={RedirectUri}"
   }
   ```

   A `redirect_uri={RedirectUri}` pair inside `LoginStartUri` is ignored by the existing
   extract-callback-from-start-uri logic (it is a placeholder, not a value).

Behavior change (documented): a configuration with no resolvable callback used to fail login with
a warning; it now attempts the flow with the broker-derived callback. Strictly wider - the failing
case had no working configuration to preserve.

## Tests

Web runtime suite (stub broker): placeholder substitution reaches the browser URL encoded and the
broker is handed the derived callback; a config with no callback at all now signs in via the
broker default. Red first, per AGENTS.

## Diagnostics

**Item 3 - the dead end names its cause.** When neither configuration nor the start URI nor the broker
yields a callback, the flow cannot start. The warning used to say only that `LoginCallbackUri`
was unset and `redirect_uri` was missing from `LoginStartUri` - which reads as a configuration
slip even when the start URI carries `{RedirectUri}` and the real cause is a platform with no
callback to derive. The broker's own failure message was logged at `Debug`, i.e. invisible at
the log level an app actually runs with, so the one line explaining the dead end never appeared.

The warning now names all three sources tried and carries the broker's message, plus what makes
a broker-derived callback possible (a custom scheme registered for the app: `CFBundleURLTypes`
in `Info.plist` on iOS/Mac Catalyst, an intent filter on Android). Found the hard way: an iOS
head whose `Info.plist` declared `CFBundleURLSchemes` at the root instead of inside
`CFBundleURLTypes` - so iOS ignored the scheme entirely - reported only "redirect_uri not set",
which is the one thing that was not wrong.

Guarded by `Given_WebAuthentication.When_BrokerCannotDeriveCallback_Then_WarningNamesBroker`
(stub broker throwing from `GetCurrentApplicationCallbackUri`).
