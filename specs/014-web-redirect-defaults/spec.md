# 014 — Web provider: broker-derived redirect defaults and the {RedirectUri} placeholder

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
