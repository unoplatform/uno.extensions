# 009 — MSAL Authentication fixes (post uno#20601/#24055)

**Status: implemented — `progress.md` carries the plan, the two review-panel outcomes (2026-08-12,
2026-08-23) and the verification trail.** Written after the fact (2026-08-23) so the folder has the
`spec.md` AGENTS.md asks for; the content was in `progress.md`'s Findings and Plan from the start.

## Problem

unoplatform/uno#24055 fixed the Skia-renderer flavor selection for `Uno.UI.MSAL` (uno#20601),
which heals the interactive-flow-dead-on-Skia failure for `Uno.Extensions.Authentication.MSAL`
without a change here. What remained broken in this repo:

- **#3025** — on macOS (and Linux desktop) `SetupStorage` built `StorageCreationPropertiesBuilder`
  with no keychain/keyring properties, so `MsalCacheHelper.CreateAsync` threw
  `ArgumentNullException (keyChainServiceName)`; caught and logged, no fallback, no defaults, no
  settings knob.
- **#2438** — desktop assembly-load failure from the 4.1.x era, when the package had no desktop
  TFM; needed validation against current packaging, not code.
- **#2346** — the WASM build break via the implicit `Microsoft.Identity.Client.Extensions.Msal`
  dependency was already gone; what was left was a dead vendored
  `Microsoft.Identity.Client.Extensions.Msal.Wasm` stub namespace and a pointless persistence
  path on the browser.
- **Consumer-side define bug** — the packaged `buildTransitive/…MSAL.WinUI.targets` gated
  `UNO_EXT_MSAL` on repo-internal properties that are empty in consumer apps, so the symbol was
  defined in every consuming app on every TFM, including ones where the lib is a stub.
- **Mobile** — on Android/iOS `MsalCacheHelper` took the DPAPI path and threw; MSAL.NET persists
  natively there and the helper should be skipped.

## Scope

`src/Uno.Extensions.Authentication.MSAL/` (provider, storage defaults, consumer `Package.targets`),
the new `Uno.Extensions.Authentication.MSAL.Tests` and `.UI.Tests` projects, the runtime-test CI
stages that exercise them, and `doc/Learn/Authentication/HowTo-MsalAuthentication.md`. Platform
token-cache persistence became platform-aware (keychain/keyring defaults with an opt-in
unprotected-file fallback; native cache on Android/iOS; `IKeyValueStorage` on the browser via
spec 011); platform selection later moved to runtime dispatch (spec 010).

Out of scope here: #2640 (WebAuthenticationBroker for Desktop/Skia, re-scoped to OIDC/Web), and
the browser token-cache persistence itself (spec 011).

## Status

Implemented; see `progress.md` for the checklist, review-panel findings, the live-testing bugs
found afterwards (logout dispatcher guard, empty-token refresh — both fixed), and what is still
open. Related: `specs/010-msal-skia-mobile-runtime-dispatch/spec.md`,
`specs/011-wasm-msal-token-cache/spec.md`.
