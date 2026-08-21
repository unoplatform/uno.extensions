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
- [x] 9. F7: ephemeral-session setting via runtime dispatch (spec-010 pattern) — exceptions note: no red/green possible in-repo, see review log

## Desktop broker (F8)

- [x] 10. `DesktopWebAuthenticationBrokerProvider` in `Uno.Extensions.Authentication.UI`: loopback listener + system browser, per spec design (loopback-only validation, static response page, one-shot accept)
- [x] 11. Runtime-gated `ApiExtensibility.Register` from `AddWeb`/`AddOidc` (desktop OSes only; never on Skia-mobile-substituted heads)
- [x] 12. Desktop-lane end-to-end tests (fake browser = HTTP GET against the callback)

## Custom

- [x] 13. `Uno.Extensions.Authentication.Tests` (plain net9.0): login/refresh/logout/cancellation coverage

## CI / docs

- [x] 14. Widen `RuntimeTestsFilter` (and, separately, the WASM lane's narrow filter once green)
- [x] 15. Docs: Skia Desktop support + loopback redirect registration; F9 packaged-only note; timeout setting (`HowTo-OidcAuthentication.md`, `HowTo-WebAuthentication.md`)

## Review log

(append per-item verification notes here)
- 2026-08-21, items 1+2: desktop head (net9.0-desktop, Windows) — red run: `When_RefreshRejected_Then_NotAuthenticated` failed with "Expected refreshed to be false ... but found True" while both happy-path tests passed; after the fix: 3/3 passed. Harness note: `ProviderInformation.Validate()` requires a `KeySet` even with signature validation opted out — empty `JsonWebKeySet` satisfies it.
- 2026-08-21, items 3+4: desktop head — red run: `When_LogoutCancelled_Then_StillAuthenticated` and `When_BrowserInvokeCancelled_Then_CancellationPropagates` failed while the 4 existing tests passed; after the fixes: 6/6 passed. F3's runtime-test covers the pre-launch cancellation check; the mid-flight broker path gets covered once item 5's stub broker exists.
- 2026-08-21, items 5-8: desktop head — red run: `When_LoginCancelled_Then_PreviousSessionSurvives`, `When_LogoutCancelled_Then_StillAuthenticated`, `When_LoginAlreadyCancelled_Then_NoPrompt` failed; 3 baselines passed. After the fixes: Web suite 6/6, full auth filter (MSAL+Oidc+Web) 35/35, 0 failures. Deviations from the item-6 sketch: no new `InteractiveTimeout` config knob for Web — the caller's ct is plumbed to both paths and Uno's broker already enforces `WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout` (5 min default); the WinUIEx path now uses its ct overload. Registration seam note: `ModuleInitializer` is CA2255-banned in libraries — the stub broker registers from the harness instead (idempotent, before first broker touch). Stub gotcha: the provider parses the callback with `WebAuthenticationSettings`' OAuth-standard key defaults (`access_token`/`refresh_token`), not TokenCache key names.
- 2026-08-21, item 9 (F7) — exceptions process (AGENTS §4): red/fix/green is not achievable in this repo. Constraint: the buggy path only exists when Uno's runtime-asset selector substitutes the plain-TFM lib into a Skia iOS consumer app; CI lanes use project references, which are never substituted, so the iOS lane compiles the `#if __IOS__` branch and the plain branch is unreachable in-process. Impact: the reflection branch (`Type.GetType("Uno.WinRTFeatureConfiguration+WebAuthenticationBroker, Uno")`) is verified by inspection + all-TFM Release compile (msbuild, 0 errors: ios/android/catalyst/windows/wasm/desktop/net9.0), not by a failing-then-passing test. Mitigation: validate on the Uno.Samples Skia-iOS testbed alongside spec 010's item 8; the miss path logs at Debug and no-ops. Also verified: Web suite still 6/6 on the desktop head after the refactor.
- 2026-08-21, item 13: `Uno.Extensions.Authentication.Tests` (plain net9.0, bare Microsoft host + hand-rolled `FakeKeyValueStorage` via `SetDefaultInstance` - the product's InMemory storage is internal): 6/6 green via `dotnet test`. Coverage tests, not bug repros (F10 found the Custom provider sound), so no red phase. Added to `Uno.Extensions.sln` + `Uno.Extensions-packageonly.slnf` so package CI builds and discovers it.
- 2026-08-21, item 14: `RuntimeTestsFilter` widened to `'Uno.Extensions.Authentication.'` (35 tests, verified green on the desktop head); wasm lane filter extended with the OIDC and Web suites. Engine filter discovery, verified empirically and in `UnitTestFilter.cs` (uno.ui.runtimetests.engine 2.0.0-dev.79): `;`/`|` are OR but the parser drops the character before an operator, so terms need a trailing space - `'A|B'` silently runs only `A` (truncated); `'A | B'` works. Desktop probe of the exact wasm string: 20 tests, 0 failures.
- 2026-08-21, items 10-12: `DesktopWebAuthenticationBrokerProvider` (public, in Authentication.WinUI, `#if !WINDOWS`): loopback-only validation, root-prefix listener with manual path match (HttpListener prefixes need a trailing slash the IdP redirect never has), static completion page, launch via Process.Start with ArgumentList (no shell parsing), first-use ephemeral port cached per process, honors `WinRTFeatureConfiguration.WebAuthenticationBroker` timeout/DefaultReturnUri/DefaultCallbackPath. Registered from `AddWeb`/`AddOidc` behind a runtime desktop-OS allow-list (spec-010 hazard: compile-time gating can't work on substituted plain-TFM builds). 4 broker tests + 6 Web tests green on the desktop head (10/10); all-TFM Release compile clean after fixing CA1416 with explicit OS guards. Not separately verified: the registration path end-to-end without the test stub (first-wins registration means the stub always shades it in-process) - validate on a real Skia Desktop app alongside the docs work.
- 2026-08-21, item 15: platform-support sections appended to HowTo-WebAuthentication.md (full matrix + Skia Desktop loopback details + WinAppSDK packaged-only note + broker timeout) and HowTo-OidcAuthentication.md (cross-link to the matrix + the two notes that differ). No TOC change needed - both pages already exist.

## Review section

All 15 items complete. Fixes: Oidc refresh IsError (F1), Oidc logout result+ct (F2), WebAuthenticatorBrowser cancellation (F3), Web ct plumbing (F4), Web cancelled-login token wipe (F5), Web logout result (F6), ephemeral-session runtime dispatch (F7, exceptions-process item), Skia Desktop loopback broker (F8, feature). Docs cover F9 (packaged-only WinAppSDK). Custom provider needed tests only (F10).
Test infrastructure added: Oidc.UI.Tests (StubOidcServer/StubBrowser, 6), Authentication.UI.Tests (StubWebAuthenticationBroker + desktop broker suite, 10), Authentication.Tests (Custom, 6). Desktop head: 41 auth runtime tests green under the exact CI filter (plus 6 Custom unit tests via dotnet test). CI: all four lanes now run the full auth namespace (wasm keeps MSAL excluded per its standing StubEntra limitation).
Outstanding for CI to confirm: the new suites on the Android/iOS/wasm lanes (first run happens on this branch's pipeline); the desktop broker's registration path end-to-end in a real Skia Desktop app (in-process tests always see the stub via first-wins registration).
