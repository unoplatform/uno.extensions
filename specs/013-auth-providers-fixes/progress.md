# 013 — progress

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

- 2026-08-21, testbeds: `Uno.Samples` branch `dev/sb/web-oidc-ext-samples` adds `UI/Authentication.OidcExtensionsDemo` (AddOidc) and `UI/Authentication.WebExtensionsDemo` (AddWeb + PKCE/code-exchange callbacks), both against the public Duende demo server (bob/bob), all four heads on SkiaRenderer, consuming 255.255.255.255-local from the sibling feed. Desktop/wasm/android heads build; desktop heads launch and run the startup silent path clean. These are the vehicles for the outstanding interactive validation (desktop loopback broker end-to-end without the test stub; Skia-mobile substitution incl. F7's reflection branch). Sample-order finding: Duende OidcClient 6 requires either an IIdentityTokenValidator or `Policy.RequireIdentityTokenSignature = false` - AddOidc configures neither, so out-of-the-box logins against a signing IdP throw; the sample opts out explicitly. Consider referencing the validator package from Uno.Extensions.Authentication.Oidc as follow-up work.
- 2026-08-21, sample debugging: "Sign in does nothing" in the Web demo was a sample bug, not a provider bug - `UseHttp` only registers `IHttpClientFactory` when named/typed clients are added, so navigation's `MainModel -> WebFlowService(IHttpClientFactory)` activation threw, the page bound no DataContext, and the code-behind handlers null-check the view model and silently no-op (fixed with `services.AddHttpClient()` in both demos, Uno.Samples `de933828`). Two wins from the diagnosis: (1) `When_TypedCallbacksOnly_Then_LoginUsesThem` now guards the sample-shaped typed AddWeb configuration in-lane (passed first run - provider path was never at fault); (2) the instrumented run validated the desktop loopback broker through the REAL registration path (no test stub): the provider derived `http://localhost:{port}/authentication-callback` and opened the system browser at the demo server - closing the "registration path end-to-end" gap noted under items 10-12. Debugging lesson (general, added to specs/lessons.md): a silent click handler that null-checks its view model hides DI failures; and `build | tail -1` hides compile errors - gate on the error count, not elapsed time.
- 2026-08-22, F11 (found via the OidcSteve sample): the Oidc provider never passed the cached id_token as the end-session `id_token_hint`, so the IdP could not trust the post-logout redirect - it prompted for confirmation and never redirected back, leaving desktop logout hung on the loopback listener until the 5-minute broker timeout with the UI stuck. Red/fix/green: `When_Logout_Then_EndSessionCarriesIdTokenHint`; full Oidc suite 7/7 on the desktop head.
- 2026-08-24, F12: the desktop loopback broker never saw URL fragments (browsers do not send them to servers), so implicit-flow responses - the only shape the basic AddWeb can consume without app-side exchange code - could not complete on Skia Desktop. A bare callback hit now serves a static relay page whose script re-requests the callback with the fragment as a marked query (`?uno-fragment=1&...`, or `?uno-no-fragment=1` when bare), and the broker restores the original fragment shape in ResponseData. Red/fix/green: `When_FragmentResponse_Then_RelayedAndReturned` + `When_NoQueryAndNoFragment_Then_CompletesEmpty`; full Authentication.UI.Tests suite 13/13. Enables a zero-code live demo of AddWeb against demo.duendesoftware.com's `interactive.implicit` client (id_token on the fragment, arbitrary redirect URIs accepted).
- 2026-08-24, spec 015 (Web redirect defaults): AddWeb now falls back to `WebAuthenticationBroker.GetCurrentApplicationCallbackUri()` when no callback is configured, and replaces the literal `{RedirectUri}` token in LoginStartUri/LogoutStartUri with the URL-encoded effective callback - one static configuration now serves every platform (WinAppSDK excluded: the WinRT broker answers ms-app://, wrong for the WinUIEx flow, so it keeps requiring explicit config). Red/fix/green: `When_RedirectUriPlaceholder_Then_BrokerCallbackSubstituted` + `When_NoCallbackConfigured_Then_BrokerCallbackUsed`; Web suite 15/15. Details in specs/015-web-redirect-defaults/spec.md.
- 2026-08-24, documentation pass over specs 013/015: HowTo-WebAuthentication rewritten for coherence (full config-key table incl. token keys and per-key defaults, {RedirectUri} placeholder in the walkthrough, corrected "web view" and "auto-refresh" claims, fragment-relay note replacing the stale "implicit flows cannot work over a loopback redirect" claim - also fixed in DesktopWebAuthenticationBrokerProvider's XML remarks); HowTo-OidcAuthentication gains sign-out (id_token_hint, cancel-keeps-session, local-only alternative), silent-refresh, and id_token-validation notes; AuthenticationOverview gains the Skia Desktop broker note; UpdatingExtensions.md gains the "Web and OIDC Authentication behavior changes" section for anyone upgrading to 7.0 covering every deliberate behavior change from specs 013 and 015.

## Rebuilt on main (2026-08-26)

The branch had drifted 36 commits behind `main`, and `main` had meanwhile absorbed roughly forty of
its own 90 commits through the msal-1..4 and storage-fixes split PRs - reworded and squashed, so
`git cherry` recognized none of them and a literal `git rebase main` conflicted on commit 1 of 90,
replaying work `main` already shipped. Rebuilt instead as the net diff: a fresh branch off `main`
carrying the 21 non-merge commits from `898e864f1` onward (everything after the last commit `main`
already had), which is exactly the Oidc/Web/Custom/broker/http work plus its docs and specs.

Resolutions worth knowing, in case this is ever redone:

- **`.azure-pipelines.yml` `RuntimeTestsFilter`** - merged from both sides rather than either one:
  `'Storage.UI.Tests | Uno.Extensions.Authentication.'`. `main` had added the storage suite; the
  branch had widened to the whole auth namespace. Both are wanted.
- **`stage-runtime-tests-wasm.yml`** - took `main`. The branch still carried the narrow
  `Given_BrowserTokenCacheStorage`-only filter with its StubEntra explanation; `main` fixed the
  underlying cause (the provider's Builder callback now runs after `WithUnoHelpers()`, so the stub
  applies in the browser) and runs the full filter there.
- **`MsalAuthenticationProvider` / `MsalStorageDefaults`** - kept `main`'s structure (the
  `SetupBrowserStorage` / `SetupDesktopStorage` split and the `MsalSecureStore` / `ForCurrentOS()`
  refactor) and grafted this branch's persistence-check behavior into it: `filePath` hoisted,
  `VerifyPersistenceIfNeeded` in place of the unconditional `VerifyPersistence()`,
  `ArmWriteVerification` after `RegisterCache`, and the four new members moved from the branch's
  `#if !UNO_EXT_MSAL_NOSTORAGE` guard into `main`'s non-browser branch, where `MsalCacheHelper`
  actually exists. The unprotected fallback keeps verifying unconditionally.
- **`specs/lessons.md`** - both sides kept; the two additions were unrelated.
- **Spec renumbering** - this spec was 012 on the branch, but `main`'s msal work took 012 when it was
  split out (it was 009 here). 012->013, 013->014, 014->015, 015->016.
- **Duplicate solution entry** - `Uno.Extensions.Authentication.Tests` ended up registered twice
  (`main`'s auth-core-fixes plus this branch's Custom-provider commit, each with its own GUID), which
  MSBuild rejects outright with MSB5004. `main`'s GUID kept.

Verification on the rebuilt branch:

| Check | Result |
| --- | --- |
| `Uno.Extensions-packageonly.slnf` Release (MSBuild) | 0 errors |
| Unit tests, `dotnet test` over the same filter package CI uses | **1612 passed, 0 failed** (19 pre-existing Reactive skips) |
| Runtime tests, Skia desktop head, the exact CI filter | **48/48 passed** |

They cover the Storage default-store suite, the desktop-broker suite (including the fragment relay),
the Web suite (including the `{RedirectUri}` placeholder and the broker-derived default), the Oidc
suite (including the `id_token_hint` end-session case) and the MSAL suite. What is still unconfirmed
is unchanged by the rebuild: the Android, iOS and WebAssembly lanes have never run the Oidc/Web
suites.

## Review panel (2026-08-27)

The seven-lens panel (`/review-panel`) on PR #3169 returned fix-first. Findings addressed in one
follow-up commit, keyed to the panel's numbering:

- [x] H1 — `__WASM__` is not defined by Uno.Sdk, so the desktop-broker suite would have compiled
  into the browser head and thrown `PlatformNotSupportedException` on the WASM lane. The test csproj
  now defines it for browserwasm, as `RuntimeTests.Core` does.
- [x] H2 — Oidc cancelled login wiped the session (F5 was fixed for Web only). `WebAuthenticatorBrowser`
  maps `ResponseStatus` to `BrowserResultType` (UserCancel with a null error, Timeout via the
  broker's error detail, HttpError otherwise); the provider throws `OperationCanceledException` on
  `Error == "UserCancel"`. Red test `When_LoginCancelled_Then_PreviousSessionSurvives`.
- [x] H3 — MSAL persistence watchdog: read check when the probe is skipped, retry bounded to one,
  `MsalCacheHelper` trace forwarded into the logger (spec 016, "Review-panel follow-ups").
- [x] H4 — Oidc refresh signed the user out on any error, including offline. Only token-endpoint
  error codes (RFC 6749 §5.2) end the session now; transport errors keep the tokens with a Warning.
  Red test `When_RefreshFailsOffline_Then_SessionKept`. (Web's `ErrorHttp` still returns null and
  the service-level wipe on a failed login stays out of scope, as the spec records.)
- [x] H5 — the desktop broker logs: Error on bind failure naming the prefix, Warning on timeout
  naming the callback and timeout, Debug on stray requests and dropped connections. Ambient
  `this.Log()`, since the broker is built outside DI.
- [x] M1 — `{State}` placeholder with per-sign-in verification (spec 015).
- [x] M2 — timeout vs cancel: the broker marks its timeout with `ResponseErrorDetail = 408`; both
  providers log Information and word the `OperationCanceledException` accordingly; upgrade notes
  list the `WebAuthenticatorBrowser` result types.
- [x] M3 — the broker builds the result before writing the completion page and treats a dropped
  connection as Debug, not as a failed sign-in; a failed relay-page write keeps listening.
- [x] M4 — `response_mode=form_post` is read from the POST body; a callback that carries its own
  query still relays fragments (bare = "no parameters beyond the callback's own"); `127.0.0.1`
  binds without elevation on Windows (checked empirically with a scratch `HttpListener`).
- [x] M5 — `TryRegister` returns `bool`; docs state the `#if !WINDOWS` guard.
- [x] M6 — `AddWeb`/`AddOidc` document the first-wins ordering rule (no opt-out added).
- [x] M7 — stale `Spec 012`/`Spec 014` breadcrumbs corrected.
- [x] M8 — callback resolution deduplicated (`ExtractRedirectUri`, `ResolveCallbackUri`).
- [x] M9 — token-leak assertions in the Web and Oidc suites, a Web `Refresh` callback case, the
  nested `FakeKeyValueStorage` removed, `CapturingLoggerProvider` linked into the Oidc suite.
- [x] L1 — the broker refuses non-http(s) request URIs before launching anything.
- [x] L2 — `TokenCache` does not warn for `InMemoryKeyValueStorage`.
- [x] L3 — the single-generic lifetime overload rejects interfaces at registration; the typed
  factory lambda is shared. The `AddClient<T>(context, default)` ambiguity is accepted and noted.
- [x] L4 — response data is matched to the callback as URIs (case/default-port normalized).
- [x] L5 — a trimmed/missing `PrefersEphemeralWebBrowserSession` setter logs Warning, not Debug.
- [x] L6 — abandoned `GetContextAsync` observed, `ConfigureAwait(false)` throughout the broker,
  `Process` handle disposed, response pages pre-encoded.
- [x] L7 — browser tasks in the broker tests are awaited so their assertions fail the test.
- [x] L8 — `HttpOverview.md` samples now match real overloads.
- [ ] Not done: an opt-out for the broker registration (M6), the MSAL double warning on WASM (L2),
  the third `CapturingLoggerProvider` copy in the MSAL suite, and the interface/implementation
  `HttpClient` naming difference between transient and non-transient `AddClient` (L3) — follow-ups.

A second commit (`b5563ca16 fix(auth): address the code-quality bot comments on PR #3169`)
answered the three `github-code-quality` inline comments: `using var` on the port-probe
`TcpListener`; the broad catch in `TryGetBrokerCallbackUri` kept but filtered
(`when (ex is not OperationCanceledException)`) with the rationale in a comment - what the broker
throws with no callback is platform-specific and the handler swallows nothing; and the form_post
test's `FormUrlEncodedContent` disposed at method scope, because the bot's suggested `using` inside
the `OnLaunch` lambda would dispose the body while the POST is still in flight.

## Continuation notes (2026-08-29)

Everything needed to pick this up in a fresh session. The untracked `HANDOFF.md` at the repo root
duplicates some of this; this section is the checked-in copy.

### State

| | |
| --- | --- |
| Branch | `dev/sb/auth-providers-onmain`, 29 commits on `main` (`8548c462c`), tip `b5563ca16`, pushed |
| PR | [#3169](https://github.com/unoplatform/uno.extensions/pull/3169) against `main`, open, no issue linked (the earlier split PRs each had a sub-issue under #3155 - create one and fill the `closes #` line if the checklist item matters) |
| Superseded branch | `origin/dev/sb/auth-providers-fixes` at `981f2f9e9`, untouched; #3166 and #3167 merged into it and are included here |
| Split out | `dev/sb/macos-keychain-storage` (local only, `main` + `b5768670a`): the macOS keychain-backed `IKeyValueStorage`, spec 017. Removed from this PR because it makes an unexecuted `SecKeychain*` P/Invoke the default store for every Skia macOS app. Needs a first run on a real Mac, then its own PR |

### CI

- Build 230485 (`e607a6d67`, the review-panel commit): **every Azure lane green**, including the
  first-ever Android-emulator, iOS-simulator and WebAssembly runs of the Oidc/Web suites.
- Build 230609 (`b5563ca16`): green except the Skia Desktop hot-reload job, which failed on its
  results-publish step with `Too Many Requests` (Azure DevOps throttling, zero test failures);
  that skipped the "Runtime Tests - Devices" stage. The commit changes nothing those lanes run.
  **"Rerun failed jobs" on 230609** in the Azure portal clears it.
- Builds 230470/230474 (pre-panel) failed on the WebAssembly runtime lane: the `__WASM__` gate
  (H1 above). Fixed.
- The GitHub "Azure Static Web Apps CI/CD" check fails on every PR branch with *"This Static Web
  App already has the maximum number of staging environments"* - an Azure quota on PR previews,
  fixed only by deleting stale staging environments in the portal.

### Left on the PR

1. Rerun build 230609's failed job (portal) so the device stage runs on the final commit.
2. SWA staging-environment quota (portal).
3. Optional: link an issue.
4. The four review-panel follow-ups listed under "Not done" above.

### Follow-up work, ranked

1. **OIDC out-of-the-box sign-in throws against any signing IdP.** Duende `OidcClient` 6 needs an
   `IIdentityTokenValidator` or `Policy.RequireIdentityTokenSignature = false`, and `AddOidc`
   configures neither. Pre-existing on `main` (the 6.0.1 bump is there), documented as a workaround
   in `HowTo-OidcAuthentication.md`. If Web/OIDC is the headline of the release, fix before the
   release note says "works out of the box".
2. **Token storage hardening** - the audit below has cleartext cells that are closable here:
   - Skia Windows access-token store: DPAPI via P/Invoke `CryptProtectData`/`CryptUnprotectData` in
     the plain/desktop build, gated on `OperatingSystem.IsWindows()`, same `Protect`/`Unprotect`
     override shape as the macOS store. Small, no new dependency, fully verifiable on a Windows box
     (the Skia desktop runtime-test head runs there). Do this first.
   - Skia iOS/Android access-token store: the `KeyChain`/`KeyStore` stores exist but live in
     `Storage.UI`, which the Uno SDK substitutes with its plain build on Skia heads. Preferred
     route: move them (and `BaseKeyValueStorageWithCaching`) into an assembly that does not
     reference Uno.UI and multi-targets `net9.0-ios`/`-android`, so it is not substituted -
     MSAL itself proves non-Uno.UI packages load their platform build on Skia heads. First step is
     confirming the SDK substitutes *Uno.UI-referencing* packages specifically. Alternative:
     `SecItem*` CoreFoundation interop in the plain build (iOS/Catalyst/macOS only). Spec 018.
   - Skia Linux: libsecret P/Invoke, key in the Secret Service, same AES-GCM envelope as the
     macOS store; `MsalCacheHelper` is the reference implementation. No Linux lane.
   - Android's MSAL cache (plain `SharedPreferences`) is MSAL.NET's and not redirectable
     ("custom serialization isn't available on mobile platforms"); WebAssembly has no secure store.
     Both are documented, not fixable here.
3. **Unpackaged WinAppSDK Web/OIDC** is documented as unsupported, not fixed - the WinRT broker
   answers `ms-app://`, wrong for the WinUIEx flow.
4. macOS keychain store (spec 017, other branch): first run on a Mac, then decide `SecKeychain*`
   vs going straight to `SecItem*`; key rotation and deletion are unaddressed.
5. Linux desktop key-value storage cleartext fall-through is asserted by a test so it cannot
   regress silently; `MsalStorageDefaults`' libsecret constants are the precedent.

### Token storage across platforms (audit, 2026-08-26)

Two caches, answered per cache and per head. "MSAL cache" is MSAL.NET's own (refresh + ID tokens);
"access token" is what `IAuthenticationService` keeps in the host's default `IKeyValueStorage`,
which the Web/OIDC/Custom providers use for everything.

| Head | MSAL's own cache | Access token via `IKeyValueStorage` |
| --- | --- | --- |
| Windows, WinAppSDK | DPAPI encrypted file | DPAPI (`EncryptedApplicationData`) |
| Skia Desktop, Windows | DPAPI encrypted file | **cleartext `ApplicationData`** |
| Skia Desktop, macOS | Keychain via `MsalCacheHelper` | **cleartext** (keychain store is on the split-out branch) |
| Skia Desktop, Linux | Keyring / libsecret | **cleartext `ApplicationData`** |
| iOS, native renderer | iOS Keychain | iOS Keychain |
| Android, native renderer | **plain `SharedPreferences`** | Android `KeyStore` |
| iOS, `SkiaRenderer` | iOS Keychain | **cleartext `ApplicationData`** |
| Android, `SkiaRenderer` | **plain `SharedPreferences`** | **cleartext `ApplicationData`** |
| Mac Catalyst | not supported (`AddMsal` throws) | iOS Keychain |
| WebAssembly | browser storage, cleartext | browser storage, cleartext |

Evidence for the MSAL rows: `SetupDesktopStorage` early-returns on Android/iOS/Catalyst, so those
rows are properties of `Microsoft.Identity.Client` 4.87.0. Its `lib/net8.0-ios18.0` uses
`SecKeyChain`/`SecRecord` (real keychain, entitlement required); its `lib/net8.0-android34.0` uses
`SharedPreferences` with no encryption primitives at all. The Skia rows lose the secure stores
because `Storage.UI` registers them under `#if __ANDROID__`/`#if __IOS__` and the SDK loads the
plain build on Skia heads - forced, since the stores are built on Xamarin bindings. Since this
branch, `TokenCache` logs a Warning naming the store whenever the default is unencrypted, so none
of the cleartext cells is silent any more.

Sites where the `#if` handling is *correct* and must not be "fixed": `MsalRedirectDefaults`
(`#if WINDOWS` is compile-time on purpose - `IsWindows()` is also true on Skia desktop),
`WebAuthenticationProvider.ApplyPrefersEphemeralWebBrowserSession` (runtime dispatch plus
reflection), `DesktopWebAuthenticationBrokerProvider.TryRegister` (runtime allow-list), and
`SetupDesktopStorage`'s mobile early-return (runtime, because a Skia Android head must not fall
through to `MsalCacheHelper`).

### Environment

See `specs/lessons.md`: `dotnet build` cannot build the XAML-bearing solutions (use `MSBuild.exe`
from VS 18), gate on exit codes not filtered output, the runtime-test engine's filter needs
`'A | B'` with spaces, a desktop-only runtime-test build invalidates the package build's restore
(`-t:Restore,Build` afterwards, never concurrently), and `jq` is not installed - use `gh --jq` or
PowerShell for JSON. `127.0.0.1` binds without elevation on Windows `HttpListener`.

Verification loop that matches CI (57 runtime tests as of `b5563ca16`; docs lint is the two
commands in `build/ci/stage-docs-validations.yml`):

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe" `
  Uno.Extensions-packageonly.slnf -t:Restore,Build -p:Configuration=Release -v:m -nologo -m
dotnet test Uno.Extensions-packageonly.slnf -c Release --no-build --filter "FullyQualifiedName!~UI.Tests"

dotnet build Uno.Extensions-runtimetests.slnf -p:Build_Android=false -p:Build_iOS=false `
  -p:Build_Windows=false -p:Build_MacCatalyst=false -p:Build_Web=false -c Debug -p:GeneratePackageOnBuild=false
$env:UNO_RUNTIME_TESTS_RUN_TESTS = '{"Filter": {"Value": "Storage.UI.Tests | Uno.Extensions.Authentication."}}'
$env:UNO_RUNTIME_TESTS_OUTPUT_PATH = "<some path>\results.xml"
Push-Location src\Uno.Extensions.RuntimeTests\Uno.Extensions.RuntimeTests\bin\Uno.Extensions.RuntimeTests\Debug\net9.0-desktop
dotnet Uno.Extensions.RuntimeTests.dll; Pop-Location   # then parse the NUnit XML, not the console
```
