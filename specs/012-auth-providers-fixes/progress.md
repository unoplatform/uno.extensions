# 012 — progress

Plan approved 2026-08-21 (audit + desktop-broker route confirmed by Steve). Order: test scaffolding
rides along with the first fix that needs it; every fix is red/fix/green with the failing test
committed alongside.

## Oidc

- [x] 1. Scaffold `Uno.Extensions.Authentication.Oidc.UI.Tests` (StubOidcServer, StubBrowser, harness) + wire into RuntimeTests head (not the slnf — MSAL.UI.Tests is not listed there either; the head’s ProjectReference is what CI builds)
- [x] 2. F1 red/fix/green: refresh must fail when the token endpoint errors; plumb ct into `RefreshTokenAsync`
- [x] 3. F2 red/fix/green: logout honors `LogoutResult.IsError` and the cancellation token
- [x] 4. F3 red/fix/green: `WebAuthenticatorBrowser` propagates cancellation; dispose CTSes

## Web

- [x] 5. Scaffold `Uno.Extensions.Authentication.UI.Tests` (stub `IWebAuthenticationBrokerProvider` via `ApiExtensibility.Register`)
- [x] 6. F4 red/fix/green: ct + interactive timeout plumbed to both broker paths (WinUIEx ct overload; `.AsTask(ct)`)
- [x] 7. F5 red/fix/green: `ResponseStatus.UserCancel` → OCE before any save; `ErrorHttp` → null
- [x] 8. F6 red/fix/green: logout honors the broker result
- [ ] 9. F7 red/fix/green: ephemeral-session setting via runtime dispatch (spec-010 pattern), no `#if __IOS__`

## Desktop broker (F8)

- [ ] 10. `DesktopWebAuthenticationBrokerProvider` in `Uno.Extensions.Authentication.UI`: loopback listener + system browser, per spec design (loopback-only validation, static response page, one-shot accept)
- [ ] 11. Runtime-gated `ApiExtensibility.Register` from `AddWeb`/`AddOidc` (desktop OSes only; never on Skia-mobile-substituted heads)
- [ ] 12. Desktop-lane end-to-end tests (fake browser = HTTP GET against the callback)

## Custom

- [ ] 13. `Uno.Extensions.Authentication.Tests` (plain net9.0): login/refresh/logout/cancellation coverage

## CI / docs

- [ ] 14. Widen `RuntimeTestsFilter` (and, separately, the WASM lane's narrow filter once green)
- [ ] 15. Docs: Skia Desktop support + loopback redirect registration; F9 packaged-only note; timeout setting (`HowTo-OidcAuthentication.md`, `HowTo-WebAuthentication.md`)

## Review log

(append per-item verification notes here)
- 2026-08-21, items 1+2: desktop head (net9.0-desktop, Windows) — red run: `When_RefreshRejected_Then_NotAuthenticated` failed with "Expected refreshed to be false ... but found True" while both happy-path tests passed; after the fix: 3/3 passed. Harness note: `ProviderInformation.Validate()` requires a `KeySet` even with signature validation opted out — empty `JsonWebKeySet` satisfies it.
- 2026-08-21, items 3+4: desktop head — red run: `When_LogoutCancelled_Then_StillAuthenticated` and `When_BrowserInvokeCancelled_Then_CancellationPropagates` failed while the 4 existing tests passed; after the fixes: 6/6 passed. F3's runtime-test covers the pre-launch cancellation check; the mid-flight broker path gets covered once item 5's stub broker exists.
- 2026-08-21, items 5-8: desktop head — red run: `When_LoginCancelled_Then_PreviousSessionSurvives`, `When_LogoutCancelled_Then_StillAuthenticated`, `When_LoginAlreadyCancelled_Then_NoPrompt` failed; 3 baselines passed. After the fixes: Web suite 6/6, full auth filter (MSAL+Oidc+Web) 35/35, 0 failures. Deviations from the item-6 sketch: no new `InteractiveTimeout` config knob for Web — the caller's ct is plumbed to both paths and Uno's broker already enforces `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout` (5 min default); the WinUIEx path now uses its ct overload. Registration seam note: `ModuleInitializer` is CA2255-banned in libraries — the stub broker registers from the harness instead (idempotent, before first broker touch). Stub gotcha: the provider parses the callback with `WebAuthenticationSettings`' OAuth-standard key defaults (`access_token`/`refresh_token`), not TokenCache key names.
