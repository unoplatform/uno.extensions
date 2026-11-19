# 021 - Desktop WebAuthenticationBroker: review-panel follow-up

Branch `dev/sb/desktop-web-auth-broker`, PR #3261. The feature commit adds
`DesktopWebAuthenticationBrokerProvider` (system browser + loopback `HttpListener`) and registers
it from `AddWeb`/`AddOidc` through Uno's `ApiExtensibility` registry. The seven-reviewer panel
returned **fix-first**; this note records each finding and what was done about it.

## Facts established while fixing

- Uno's static `WebAuthenticationBroker` resolves its provider **once, in its static constructor**
  (`ApiExtensibility.CreateInstance`, else the built-in provider, whose Skia build throws
  `NotImplementedException`). There is one provider instance per process, so the broker's
  per-instance one-flow-at-a-time guard is live in production, and anything that touches the
  static broker before `AddWeb`/`AddOidc` pins the throwing stub for good.
- `ApiExtensibility.Register` is `TryAdd` in the pinned Uno (`7.0.0-dev.2162`): first registration
  wins, later ones are ignored silently. `IsRegistered<T>()` is public.
- `this.Log()` (`Uno.Core.Extensions.Logging.Singleton`) caches one logger per closed type at
  first access and never re-reads the ambient factory; `typeof(T).Log()` does not cache. The
  runtime-test host installs an ambient factory with a `"Uno" -> Warning` filter.
- `OidcAuthenticationSettings.AutoRedirectUri` defaults to `PlatformHelper.IsWebAssembly`, so on
  desktop the OIDC redirect follows the broker's port only when the app opts in.
- Linux TIME_WAIT (skeptic): the managed `HttpListener` sets no `SO_REUSEADDR`, but five
  consecutive rebinds of the same loopback port succeed on macOS, and the Skia Desktop (ubuntu)
  lane of build 241186 passed with the suite running every flow on the one static port. Not a
  blocker; an ADO query for the per-test results needs sign-in, so the lane's own report is the
  evidence.
- Local baseline before the changes: 12/12 on macOS through the runtime-tests head
  (`UNO_RUNTIME_TESTS_RUN_TESTS` filter `Authentication.UI.Tests`).

## Findings and status

| # | Severity | Finding (reviewers) | Status |
| --- | --- | --- | --- |
| 1 | high | Registration path untested; early touch of the static broker fails silently (skeptic, quality, contract, operability, architect) | Fixed: `TryRegister` checks `IsRegistered<>` first, keeps a static flag so repeat calls return `true`, logs at Information when another provider won, and documents the `<returns>`. Tests drive the static broker end to end (callback, success, timeout detail, second-flow rejection, cancellation) through a shared test double registered first; `When_ProviderRegisteredFirst_Then_TryRegisterKeepsIt` covers first-wins. The positive case (registering on an empty registry) is unobservable in-process and stays uncovered. |
| 2 | high | Port-reset self-heal defeated by the OIDC provider's one-time callback snapshot (skeptic, operability) | Fixed: `OidcAuthenticationProvider` re-reads `GetCurrentApplicationCallbackUri()` before every login/logout when `AutoRedirectUri`. Red/green: `Given_OidcAuthenticationProvider.When_AutoRedirectUri_Then_RedirectFollowsBrokerCallbackOnEveryFlow`. The bind-failure log also says to restart when the redirect was captured at startup. |
| 3 | high | TIME_WAIT rebind on the shared port, needs a Linux run (skeptic) | Resolved by evidence (see above). |
| 4 | high | Broker diagnostics silent on the default `UseLogging()` (operability) | Partly fixed: protected `DesktopWebAuthenticationBrokerProvider(ILogger?)` lets a derived broker carry a DI logger; the XML remark and both HowTo pages say `enableUnoLogging: true` is needed for the default path. Handing the host's factory to the registry-built instance is left as a follow-up. |
| 5 | high | `AddWeb`/`AddOidc` side effect is a behavior change; `TryRegister` ran before the null guard (contract, architect) | Fixed: registration moved below the `hostBuilder is null` return in all three builder methods; `UpdatingExtensions.md` states the negative case (provider registered after is ignored). |
| 6 | medium | `{State}` placeholder reference to #3169; Web provider has no request binding on desktop (security, quality) | Fixed in docs and remarks: the dangling reference is gone, the Web page says plainly that neither the broker nor the Web provider binds the response and shows the `PrepareLoginStartUri`/`PostLogin` pattern. Per-flow `state` inside the Web provider stays with #3169. |
| 7 | medium | Address-family mismatch in the default-port probe (skeptic, security) | Fixed: the probe requires the port free on IPv4 and IPv6 loopback (bounded retries). |
| 8 | medium | Loopback enforced on the URI, not the socket; Windows/macOS untested (security, skeptic, architect) | Partly fixed: requests from a non-loopback `RemoteEndPoint` get a 404. Windows http.sys (URL ACL for non-admin) and macOS remain unexercised by CI; follow-up. |
| 9 | medium | No token-leak guard test (security, contract) | Fixed: `When_ResponsesCarrySecrets_Then_NoneLogged` runs the query, relayed-fragment, form-post, port-in-use and timeout paths with secrets and asserts none reach the captured log (and that the capture saw the broker's own lines). |
| 10 | medium | `TimeoutErrorDetail` dead surface: `WebAuthenticatorBrowser` dropped the status (skeptic, contract, quality) | Fixed: `WebAuthenticatorBrowser.ToBrowserResult` maps Success/UserCancel/timeout/ErrorHttp to `BrowserResultType`; `Given_WebAuthenticatorBrowser` covers all four. Test comment reworded. |
| 11 | medium | Wall-clock tests mutate process-wide statics (quality, contract, architect) | `[DoNotParallelize]` on the three classes; the timeout is a global Uno setting and cannot be injected, noted in the helper. |
| 12 | medium | No per-flow log between announce and outcome; launcher failures unnamed (operability) | Fixed: Debug "opened in the system browser", Debug "redirect reached ... as a {shape}", Debug on second-flow rejection, Error naming the launcher on `Win32Exception`; `_announced` now set after `IsEnabled`. |
| 13 | medium | `AddWeb<TService>` missing the first-wins `<remarks>` (quality, operability) | Fixed. |
| 14 | medium | Docs tell consumers to derive from a type absent on the WINDOWS TFM (contract) | Fixed: `#if !WINDOWS` caveat on both derive bullets. |
| 15 | medium | Suite could be a plain `*.Tests` project, run on every PR (architect) | Not done; follow-up (would add Windows coverage on the package agent). |
| 16 | low | `Process.Start`/`listener.Start()` on the caller's thread (performance) | Default `LaunchBrowserAsync` runs the launch on the thread pool. |
| 17 | low | 64 KB `char[]` per POST (performance) | `ArrayPool<char>`, sliced to exactly the limit + 1. |
| 18 | low | `TryRegister` return semantics (contract) | Fixed with #1. |
| 19 | low | Namespace drift, file under `Web/` (architect) | Moved to `src/Uno.Extensions.Authentication.UI/DesktopWebAuthenticationBrokerProvider.cs`. |
| 20 | low | Duplicate `Observe` catches, unread `TryRespondAsync` bool, magic strings (quality) | Fixed: one catch, `Task` return, `HttpMethod.Post.Method`/`MediaTypeNames`. |
| 21 | low | Test hardcodes relay markers; `options` unused (contract, architect) | Not changed: the markers are the served page's protocol; `options` is the interface parameter. |
| 22 | info | net9.0 comment, `Cache-Control: no-store`, OIDC sample pointer, relay drops the callback's own query, `__MACCATALYST__` dead | Comment fixed; `no-store` added and asserted; sample pointer added. The relay/query edge case and the dead symbol are left as is. |

## Second review-panel round (fix-first)

The panel re-ran on the branch with the first round's fixes in place and returned **fix-first**
(security `fix-before-merge`, skeptic `fix-first`, the other five `approve-with-changes`). The
agreed fix set and what was done:

| # | Severity | Finding (reviewers) | Status |
| --- | --- | --- | --- |
| H1 | high | On Windows Skia Desktop a failed `Process.Start(url) { UseShellExecute = true }` throws a `Win32Exception` whose message is the request URL (`login_hint`, and `id_token_hint` on sign-out); logged at Error by the broker and again as `LoginResult.Error` by the OIDC provider (security) | Fixed at the broker's one catch: the log carries the launcher's name and `NativeErrorCode` only, and a fresh `Win32Exception(code, fixed message)` without inner exception is rethrown. Red/green: `When_BrowserLaunchFails_Then_RequestUriNotLoggedAndNextFlowRuns` (also proves the one-flow guard is released). Lesson added. |
| H2 | high | `WebAuthenticatorBrowser` collapsed every exception into `UnknownError`; with both timeouts at five minutes the OIDC client's fires first, so the broker's `Timeout` mapping never ran in production; a caller cancel read as a failed sign-in; CTSs never disposed; `Debug.WriteLine` (skeptic, operability, performance) | Fixed: the caller's cancellation propagates as `OperationCanceledException` (so a cancelled sign-out takes `AuthenticationService`'s exception path, which clears tokens with a live token); the client's own timeout maps to `Timeout`; both CTSs disposed; `ILogger<WebAuthenticatorBrowser>` injected (parameterless ctor kept, public type); `Error = ex.Message`. Red/green: `When_OidcTimeoutElapsesFirst_Then_TimeoutResult`, `When_CallerCancels_Then_CancellationPropagates`; `When_LogoutCancelled_Then_CancellationPropagates` proves the OIDC client does not swallow it. |
| M1 | medium | Pooled form-post buffer returned uncleared (performance, security) | `Return(buffer, clearArray: true)`. |
| M2 | medium | The "another provider is already registered" line ran at builder time, before any logger exists, so it reached `NullLogger` in every real app (operability, architect) | Fixed without moving the registration later (it must stay ahead of any static-broker touch; MSAL's `WithWebRedirectUri` runs at provider-build time, so builder time is safe): `RegisterDesktopWebAuthenticationBroker` registers at builder time and adds an `IServiceInitialize` that logs the outcome through the host's `ILogger` once built (Debug when registered, Information when another provider won). One line per host. The architect's "move into `ConfigureServices`" was not taken for that reason. Covered by `Given_DesktopWebAuthenticationBrokerRegistration.When_AnotherProviderRegisteredFirst_Then_LoggedOnceThroughHostLogger`, which builds a real host with `AddOidc().AddWeb()` and a capturing logging provider. |
| M3 | medium | `TryRegister` returns `true` after an early static-broker touch pinned Uno's stub; the test helper's assertion had the same gap (operability, skeptic) | `WebAuthenticatorBrowser` maps `NotImplementedException` to an error naming `TryRegister` and `AddOidc`; `TryRegister`'s `<returns>` states the limit; both HowTo pages name the symptom; `EnsureRegistered` asserts the static broker's callback is the double's. Red/green: `When_BrokerNotImplemented_Then_ErrorNamesRegistrationOrder`. |
| M7 | medium | No guard for the no-id_token branch of the logout hint (quality) | `When_LogoutWithoutIdToken_Then_NoHintSent` (`null`, empty, whitespace). |
| L6 | low | `Rent(MaxFormPostLength + 1)` = 32769 chars landed in the 65536-char bucket, on the LOH (performance) | `MaxFormPostLength = 32 * 1024 - 1`; comment corrected. |
| L7 | low | Public parameterless ctor with no external caller (contract) | One `protected DesktopWebAuthenticationBrokerProvider(ILogger? logger = null)`. |
| L8 | low | `WebAuthenticatorBrowser` had no XML docs (contract, quality) | Added. |
| L10 | low | Negative platform list dead beside the positive allow-list (quality) | Removed. |
| M6 | medium | `UpdatingExtensions.md` covered only the desktop broker (contract) | The cross-platform OIDC changes (result types, cancellation propagation, `id_token_hint`) added to the Authentication bullet. |

Left for follow-up, as agreed: M4 (Web provider ignores the token and `ResponseStatus`; #3169), M5 (an
opt-out once Uno ships its own desktop broker), L1-L5, L9, L11-L15 and the info items.

### Verification

- [x] Red run (tests written first, product untouched): 28 pass, 4 fail - H1, H2 (two), M3 - for
      the predicted reasons; M7 and the cancellation-propagation guard already pass.
- [x] Green run after the fixes: 33/33 in the desktop head (macOS, `net10.0-desktop`,
      `Authentication.UI.Tests` filter), the registration-announcer case included.
- [x] Release build of `Uno.Extensions.Authentication.Oidc.WinUI` (with `Authentication.WinUI`):
      no warnings in the touched files.

## PR review (Xiaoy312, code-quality bot)

Approved with inline comments; the branch was rebased onto `main` first, which renumbered this
folder from `020` to `021` (`main` now has `020-uno-ui-lottie-reference`).

| # | Comment | Status |
| --- | --- | --- |
| R1 | The cancellation change alters behavior on every platform (WinAppSDK included): a cancelled `LoginAsync` now throws instead of returning `false`, and no longer clears the stored tokens | Kept here, as the reviewer accepted it: called out as a behavior change in the PR description, and `UpdatingExtensions.md` now states both effects. |
| R2 | `GetLeftPart` normalizes the callback (host lowercased, default port dropped), so the Web provider's `StartsWith(loginCallbackUri)` misses and the token is lost silently | Fixed: the result is rebuilt from `OriginalString` without its query or fragment. Red/green: `When_CallbackNotNormalized_Then_ResultKeepsCallbackAsWritten` (red: `http://localhost:...` vs the configured `http://LocalHost:...`). |
| R3 | `IsRegistered` then `Register` is not atomic: a provider registered in between wins, yet `Registered` is returned | Fixed: after `Register`, the registry is resolved (`CreateInstance`, null owner, as the static broker does) and `Registered` is returned only when the instance is exactly this type. Re-checking `IsRegistered` would not close the gap (it is true either way). No deterministic repro for the window; the existing registration tests cover both outcomes of the early check. |
| R4 | Only the Uno host runs `IServiceInitialize`, so the registration line is never written in other hosts | Both How-To pages say so, point at the broker's first-flow Information line as the host-independent signal, and note both lines need `CoreLogLevel` at `Information` or lower. |
| R5 | Spec numbering, missing `spec.md`, "spec 017 F11" clashing with `main`'s 017 | Renumbered to `021`, `spec.md` added, the reference now names #3169's own spec. |
| R6 | Static field written from an instance method (bot, twice) | `TryForgetDefaultPort(int)` static helper; behavior unchanged. |

Verification: 34/34 in the desktop head (macOS, `net10.0-desktop`, `Authentication.UI.Tests`
filter, `Attempts: 1`); red run before the R2 fix: 20 passed, 1 failed for the predicted reason.

## Also in this branch

- Docs validation on `main` is red (`specs/018` unknown words, MD012 double blank lines in
  `specs/019` and `specs/lessons.md`); fixed here so PR #3261 can go green. The GitHub
  "Build and Deploy Job" failure is an Azure Static Web Apps staging-slot quota, unrelated.

## Found while running the Uno.Samples OIDC demo against this branch

- Sign-out never returned to the app: `OidcAuthenticationProvider` called `LogoutAsync()` with no
  `id_token_hint`, so the identity provider (Duende demo) stopped on its signed-out page with no link
  back, and the loopback broker waited out its five-minute timeout before the app cleared its tokens.
  Fixed: the cached id_token is passed as `IdTokenHint`, and the caller's `CancellationToken` reaches
  `LogoutAsync`. Red/green: `Given_OidcAuthenticationProvider.When_Logout_Then_CachedIdTokenSentAsHint`.
  The same change is in #3169 (finding F11 of the spec that PR adds), which also honors
  `LogoutResult.IsError` (its F2); that behavior change stays there.
- Live check: the sample app on macOS desktop, built against packages packed from this branch.
  Before the fix sign-out returned after 5:00.38; after it, the demo's signed-out page carries the
  "return to the application" link to the loopback callback, and following it (bare redirect, then
  the relay's no-fragment request) completes sign-out immediately with tokens cleared.

## Verification

- [x] Runtime-tests head rebuilt for `net10.0-desktop` on macOS, suite re-run: 24/24 (12 original
      + 12 new). The OIDC redirect case fails with the provider fix stashed (second flow still sent
      to the first port) and passes with it.
- [x] Release build of `Uno.Extensions.Authentication.Oidc.WinUI` (and its
      `Uno.Extensions.Authentication.WinUI` dependency) succeeds; the only warnings are the
      pre-existing CS1591 ones the Oidc project already lists as not-as-errors.
- [x] `cspell` and `markdownlint` clean on every touched Markdown file and on the two `main`
      files that were failing.
