# 009 — MSAL Authentication fixes (post uno#20601/#24055)

Context: unoplatform/uno PR #24055 fixes the Skia-renderer flavor selection for `Uno.UI.MSAL`
(uno#20601), which transitively heals the interactive-flow-dead-on-Skia failure for
`Uno.Extensions.Authentication.MSAL` — no change needed here for that. This spec tracks what is
still broken in this repo. Handoff: `HANDOFF-MSAL-AUTH.md` (repo root, untracked).

## Findings (investigation, 2026-08-12)

- **Shipped package (7.2.3)** carries libs for `net9.0`, `net9.0-android35.0`,
  `net9.0-browserwasm1.0`, `net9.0-desktop1.0`, `net9.0-ios18.0`, `net9.0-maccatalyst18.0`,
  `net9.0-windows10.0.19041`. `net9.0` and `net9.0-maccatalyst` are **stubs** (no
  `UNO_EXT_MSAL`); desktop/wasm/android/ios/windows are real providers.
  `Microsoft.Identity.Client.Extensions.Msal` is an explicit dependency in **every** group.
- **#2438** (desktop assembly-load failure) dates from extensions 4.1.x when the package had no
  desktop TFM. Current packaging ships `net9.0-desktop1.0` with the real provider → resolved by
  packaging; needs a scratch-app validation, not a code fix.
- **#3025** (macOS `keyChainServiceName` null crash): `SetupStorage` builds
  `StorageCreationPropertiesBuilder(CacheFileName, folderPath)` with **no** mac-keychain (or
  linux-keyring) properties. On macOS/Linux `MsalCacheHelper.CreateAsync` then throws
  `ArgumentNullException`. Error is caught + logged, but there is no fallback, no defaults, and no
  appsettings knob. Same failure exists on Linux desktop.
- **#2346** (wasm build break via implicit `Microsoft.Identity.Client.Extensions.Msal`): the
  original break (net8.0 group had the dependency removed while wasm heads consumed the net8.0
  lib) is gone — browserwasm TFM asset + explicit dependency exist today. Remaining: persistence
  is pointless on WASM; the `Microsoft.Identity.Client.Extensions.Msal.Wasm` vendored stubs under
  `src/Uno.Extensions.Authentication.MSAL/Microsoft.Identity.Client.Extensions.Msal/` are **dead
  code** (nothing references that namespace; the provider aliases the real package type).
- **Consumer-side define bug**: packaged `buildTransitive/…MSAL.WinUI.targets` gates
  `UNO_EXT_MSAL` on `$(_IsNetStd)/$(_IsMacOS)/$(_IsCatalyst)` — repo-internal properties that are
  **empty in consumer apps** (Uno.WinUI defines `_IsNetStdRef`, not these) → `UNO_EXT_MSAL` is
  defined in every consuming app on every TFM, including ones where the lib is a stub.
- **Mobile note**: on Android/iOS `MsalCacheHelper` takes the DPAPI (Windows) code path and throws
  `PlatformNotSupportedException` (caught, logged as error). MSAL.NET has its own persistent
  cache on those platforms — the helper should be skipped there.
- Uno.Sdk (6.8.0-dev.10) injects `Uno.Extensions.Authentication.MSAL.WinUI` unconditionally for
  `UnoFeatures=AuthenticationMsal`; `IsMSALSupported` (Wasm/iOS/Android/WinAppSdk only) gates only
  the plain-MSAL companion packages.

## Plan

- [x] 1. `SetupStorage` rework in `MsalAuthenticationProvider`:
  - browserwasm TFM: compile-time skip (`UNO_EXT_MSAL_NOSTORAGE` define) — MSAL in-memory cache.
  - Android/iOS: runtime skip (`OperatingSystem.IsAndroid()/IsIOS()`) — MSAL native cache.
  - macOS: default `WithMacKeyChain` (service name from config override → else derived from
    ClientId; account name default) — fixes #3025.
  - Linux: default `WithLinuxKeyring` equivalents.
  - `VerifyPersistence()` after `CreateAsync`; on `MsalCachePersistenceException` fall back to
    `WithUnprotectedFile()` with a loud warning; outer failure logs Error and continues with
    in-memory cache.
- [x] 2. `MsalConfiguration`: add `KeychainServiceName` / `KeychainAccountName` (appsettings-bindable).
- [x] 3. Extract testable defaults helper (`MsalStorageDefaults`, compiled on all TFMs).
- [x] 4. Delete dead vendored `Microsoft.Identity.Client.Extensions.Msal.Wasm` stubs + csproj glue.
- [x] 5. Fix consumer-side `UNO_EXT_MSAL` define in `build/Package.targets` (TFM-platform-based).
- [x] 6. Stop erasing exception type/stack in `InternalLoginAsync`; log failures at Error/Warning level.
- [x] 7. New `Uno.Extensions.Authentication.MSAL.Tests` (net9.0, MSTest, 7 tests, all green).
       Compiles `MsalStorageDefaults.cs` as linked source: the WinUI assembly can't load in a plain
       test host (Uno.WinUI-injected module initializer requires runtime Uno.UI). Registered in
       `Uno.Extensions.sln` + `Uno.Extensions-packageonly.slnf`.
- [x] 8. #2438 validated fixed by current packaging: scratch `net10.0-desktop` consumer of 7.2.3
       resolves `lib/net9.0-desktop1.0` (compile+runtime) and deploys
       `Uno.Extensions.Authentication.MSAL.WinUI.dll` + `Microsoft.Identity.Client*` + `Uno.UI.MSAL.dll`.
       Issue can be closed with a validation note.
- [x] 9. Docs: platform-support matrix + token-cache storage section added to
       `doc/Learn/Authentication/HowTo-MsalAuthentication.md`.
- [x] 10. Runtime red/green validation on real macOS keychain (scratch console app):
       RED = pre-fix shape reproduces `ArgumentNullException (keyChainServiceName)` (exact #3025
       error); GREEN = defaults → `CreateAsync` + `VerifyPersistence` + `RegisterCache` succeed
       (service `uno.extensions.msal.{clientId}`, account `MSALCache`).
- [x] 11. Release build of `Uno.Extensions-packageonly.slnf`: 0 errors, no new warnings (remaining
       warnings are the pre-existing CS1591 class + Uno.Sdk `Info.plist` notices).

## Review panel (2026-08-12)

All seven reviewer agents dispatched (architect, security, skeptic, quality, operability,
contract, performance). Worst-case verdict was **fix-first**; all actionable findings applied:

- **Unprotected-file fallback hardened** (skeptic High; security Medium): now opt-in via
  `MsalConfiguration.AllowUnprotectedTokenCacheFallback` (default **false** → in-memory cache +
  Error log with remediation). When enabled, the fallback uses a distinct file name
  (`msal.cache.plaintext-fallback`) so a recovered secure store never reads plaintext, re-applies
  the app's `Settings.Store` callback, and re-runs `VerifyPersistence`. Docs updated.
- **Failure latch removed** (operability/skeptic Medium): `_isCompleted` bool replaced with a
  latched `Task<bool>` that retries setup on the next call after a failed attempt.
- **Cancellation threaded** (operability Medium): `CancellationToken` now flows into
  `SetupStorage` and all MSAL `ExecuteAsync` calls; `OperationCanceledException` is rethrown
  explicitly on the login and silent-acquire paths (never escalates a cancelled login to an
  interactive prompt).
- **`ConfigureAwait(false)`** on all awaits in `SetupStorageCore` (performance Medium) —
  keychain/DPAPI round-trips no longer resume on the caller's (UI) context.
- **Vestigial `WINDOWS_UWP || !NET6_0_OR_GREATER` branch deleted** (quality Medium).
- **Define gates unified** (architect Medium): csproj and packaged `Package.targets` now share the
  same TFM-platform **allow-list** (android/ios/windows/desktop/browserwasm) — fail-safe for
  unknown future platforms; verified per-TFM via `msbuild -getProperty:DefineConstants` and in a
  net10.0-desktop consumer.
- **`MsalStorageDefaults`** compiled out on wasm (`#if !UNO_EXT_MSAL_NOSTORAGE`), `ApplyDefaults`
  returns void, header documents the linked-source/dependency-free constraint (skeptic/quality Low).
- **Log hygiene**: `ToJson`/`Count()` Information logs gated; account `Username` no longer logged
  (AGENTS §7 PII — was flagged by 3 reviewers as pre-existing; fixed while in the block).
- Contract's hand-off "desktop heads resolve the stub lib" was checked and is refuted: the scratch
  net10.0-desktop consumer resolves `lib/net9.0-desktop1.0` (real provider) for compile+runtime.

## PR notes (declare in the PR description)

- **Consumer MSBuild-surface change**: the packaged `UNO_EXT_MSAL` define is no longer defined in
  consumer projects on plain-.NET TFMs, macOS, or Mac Catalyst (it previously leaked on
  everywhere). Aligns the define with where the package actually ships a functional provider.
- **Removed type**: `Microsoft.Identity.Client.Extensions.Msal.Wasm.Storage` (public, browserwasm
  lib only) deleted — dead vendored code, never referenced or documented.
- **Exception contract**: `LoginAsync` now propagates original MSAL exception types instead of
  rewrapped `Exception` — intentional triage fix.

## Follow-ups (not this change)

- **REGRESSION found 2026-08-19, spec'd as `specs/010-msal-skia-mobile-runtime-dispatch/`**: the
  "Define gates unified" allow-list makes the plain `netX.0` lib a stub, but Uno.Sdk's
  `ReplaceUnoRuntime` substitutes exactly that lib on **Skia iOS/Android heads** (any package
  referencing Uno.UI) → `AddMsal` silently registers nothing →
  "No providers specified for the application" on every auth call. Verified live on the iOS
  simulator via the Uno.Samples testbed. Fix (runtime platform dispatch) is designed and ready to
  implement in spec 010. Also partially invalidates this spec's PR note about the consumer-side
  `UNO_EXT_MSAL` define (plain TFM must be functional, hence defined, again).
- ~~Hidden `ISettings` dependency in the auth token cache~~ — **fixed in this branch**
  (found 2026-08-12 while building the `authTestExt` rig): `TokenCache` → `IKeyValueStorage` →
  `ApplicationDataKeyValueStorage(..., ISettings)` requires `ISettings`, but the only
  registration lived in `UseToolkit()/UseThemeSwitching()` — a theme-switching API. Any app
  calling `UseAuthentication` without `UseToolkit()` got
  `InvalidOperationException: NoServiceRegistered (Uno.Extensions.ISettings)` on first
  `LoginAsync` (templates always call `UseToolkit`, which is why it never surfaced). Fix:
  `UnoHost.CreateDefaultBuilder` now `TryAdd`s `ISettings` alongside its `UseStorage()` call
  (`Storage.WinUI` can't self-register it — no `Core.WinUI` reference, and `Settings` is
  internal there); `UseThemeSwitching` switched to `TryAdd` for co-existence. Verified red/green
  with the `authTestExt` rig (no `UseToolkit`): pre-fix startup fails resolving the token cache,
  post-fix the `TokenCache`/`IsAuthenticated` traces appear. Unit-test exception (AGENTS §4):
  `Hosting.WinUI` can't load in a plain test host (Uno.WinUI module initializer requires a
  runtime Uno.UI), so coverage is via the live rig; a runtime-test in
  `Uno.Extensions.RuntimeTests` is the proper long-term home.

- **Platform redirect URIs — prototyped in this branch** (2026-08-12, prompted by "should the
  extension handle this?" about the rig's `#if ANDROID / #elif IOS / #else` block). The provider
  set a redirect URI on exactly one of four heads (wasm), so every app hand-wrote the other three.
  Now `MsalAuthenticationProvider.ApplyPlatformRedirectUri` derives them:
  `msal{ClientId}://auth` (Android), `msauth.{BundleId}://auth` (iOS), the broker URI (wasm),
  `WithDefaultRedirectUri()` → `http://localhost` (desktop). WinAppSDK (`#if WINDOWS`) is
  deliberately left alone — the WAM broker owns the redirect there and that path is unexercised.
  New pure helper `MsalRedirectDefaults` (platform passed in, linked into
  `Uno.Extensions.Authentication.MSAL.Tests` like `MsalStorageDefaults`), 9 tests in
  `Given_MsalRedirectDefaults`.
  - **Behavior change, not purely additive:** the defaults are applied *before* the app's
    `Builder(...)` callback, so the callback now wins. Previously `WithWebRedirectUri()` ran
    *after* it and stomped the app on wasm, forcing apps to mutate
    `WinRTFeatureConfiguration.WebAuthenticationBroker.DefaultReturnUri` before host build to
    change the wasm callback path. Precedence is now: platform default < `RedirectUri` from
    configuration < `Builder(...)` callback. Escape hatch:
    `MsalConfiguration.UseDefaultPlatformRedirectUri = false`.
  - Verified: all TFMs compile; `ANDROID`/`IOS` constants confirmed live (probe build + `NSBundle`
    present only in the ios assembly — note `-getProperty:DefineConstants` does *not* show them,
    the SDK merges `ImplicitDefineConstants` later, so don't re-chase that); desktop head logs
    `ApplyPlatformRedirectUri - Configuring MSAL default RedirectUri` with the rig's per-platform
    block deleted; 1540 unit tests green.
  - Not verified: interactive sign-in completion on any head; iOS and WinAppSDK not built.
- **MSAL auth test coverage — built out 2026-08-13.** The area had essentially no automated tests
  (two pure helpers; the one `Given_Msal` UI test was commented out). Now:
  - `Given_MsalRedirectDefaults_Apply` — precedence asserted through a real built
    `IPublicClientApplication`, so it also pins MSAL's own `WithDefaultRedirectUri()` behavior.
  - `Uno.Extensions.Authentication.MSAL.UI.Tests` / `Given_MsalAuthentication` — login, silent
    refresh, logout, cancellation, token-leak and redirect-URI coverage driven against a stubbed
    Entra tenant (`StubEntra` + `StubWebUi`). Unattended: no network, no tenant, no human. This
    replaces the "live Skia sweep needs a human at the prompt" blocker for everything except a
    real-Entra integration check.
  - Four runtime-test CI stages (desktop / wasm / android / ios) ported from `../studio.live`,
    plus `MobileRuntimeTestsAutostart` + the Android intent bridge in the RuntimeTests head.
  - **Two real token leaks fixed** (both pre-existing, found by the new guard):
    `TokenCache.LogKeyValues` logged raw token values at Trace; `HeaderHandler` logged the bearer
    token at Debug on every request.
  - Required one additive API: `IMsalAuthenticationBuilder.InteractiveBuilder(...)` — see
    `specs/lessons.md` for why `Builder(...)` could not reach `WithCustomWebUi`.
  - **Known limitation:** the device stages run a filter scoped to `Given_MsalAuthentication`, not
    `!_HotReload`. The wider runtime suite has 15 failing tests, verified identical at the
    pre-change baseline `31e7eb529` — rot accumulated while the runtime-test stage sat commented
    out. Widening `RuntimeTestsFilter` in `.azure-pipelines.yml` is blocked on fixing those.
  - Also unblocked, all pre-existing and all invisible because no CI stage built these heads:
    two `Page` subclasses missing `partial` (android CS0260), two unresolvable doc-comment cref attributes
    (android), and trim/AOT analysis failing the wasm build of every `*.UI.Tests` project (fixed
    once in `src/Directory.Build.props` under the existing `IsTestProject` condition).
- Live Skia sweep with interactive sign-in (needs patched uno.winui cache + human at the prompt).
- #2640 WebAuthenticationBroker for Desktop/Skia (feature).
- Measure actual WASM payload effect of `UNO_EXT_MSAL_NOSTORAGE` on a trimmed publish.
- Consider a nested storage-config section if Linux keyring settings are ever exposed.

## Not done here (needs a human / other repo)

- Live Skia sweep with interactive sign-in (login → cache → silent → logout) on
  Android/iOS/WASM heads — needs the patched `uno.winui` cache (see HANDOFF-MSAL-AUTH.md §3) and a
  human at the Entra sign-in prompt.
- #2640 — re-scoped (2026-08-12) to OIDC/Web only: retitled and commented
  (https://github.com/unoplatform/uno.extensions/issues/2640#issuecomment-5268202737). MSAL needs
  no WAB on desktop (system browser + loopback, validated); the remaining gap is
  `OidcAuthenticationProvider`/`WebAuthenticationProvider`, since Uno's Skia
  `WebAuthenticationBrokerProvider` throws `NotImplementedException`.
- Red/green exception: the exact #3025 crash can only be reproduced against a real macOS
  keychain, which CI (Windows) can't exercise; the committed unit tests guard the defaults logic,
  and the live keychain red/green was performed manually on this machine (item 10).

## Decisions

- Keep `Microsoft.Identity.Client.Extensions.Msal` as an explicit dependency on **all** TFMs
  (including wasm): removing it on wasm would drop `StorageCreationPropertiesBuilder` from the
  public `IMsalAuthenticationBuilder.Storage(...)` signature there → source-breaking for shared
  code. Compile-time skip of the persistence body still lets the trimmer drop the helper on wasm.
- Defaults applied *before* `Settings.Store` callback so user configuration always wins.
- Keychain service name defaults to `"uno.extensions.msal.{ClientId}"` — app-registration-unique,
  intentional cache sharing across apps of the same registration (SSO), no collision between
  different registrations.
