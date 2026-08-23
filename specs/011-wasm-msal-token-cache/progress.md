# 011 — Progress

Tracking the implementation of `spec.md`. Item numbers match the spec's "Implementation items".

**State as of 2026-08-23: every implementation item (1–7) is done, plus the logout fixes found by
live testing, the findings from two seven-agent review panels (2026-08-20 and 2026-08-23), and
runtime-test lanes for all four platforms.** `spec.md`'s header carries the deviation summary — most
importantly that the default is `LocalStorage`, not `SessionStorage`. This file is the record of
every pass; the sections are dated but not in strict order (the first passes sit under "Done",
later ones were appended at the end). Newest first: the second review panel (last section).

---

## Fifth pass — review-panel high-severity fixes (2026-08-20)

A seven-agent review panel (`/review-panel`, `main..HEAD`) returned `fix-first`. The high-severity
items are fixed here; the medium/low findings are listed at the end of this section as follow-ups.

- **The default is now `LocalStorage`, not `SessionStorage`** — the panel's most consequential
  finding (skeptic HIGH, operability, security). The setting selects the **host-wide** default
  `IKeyValueStorage`, not just the token caches, so defaulting to `sessionStorage` silently
  relocated *every* existing WebAssembly app's key-value data on a package upgrade:
  `localStorage["UnoApplicationDataContainer_Local_*_ADCSSS"]` became unreadable, with no migration
  and no read-through, and the pre-existing cleartext `AuthToken_*` entries were stranded there.
  Defaulting to `LocalStorage` is byte-for-byte what WebAssembly already did, so upgrading is a
  no-op and no migration is needed.

  This is a **deliberate deviation from `spec.md`'s "Default: SessionStorage, not off"** section,
  which argued for matching msal-browser's `DEFAULT_CACHE_OPTIONS`. That argument weighed the
  security delta against shipping the fix switched off; it did not account for the setting also
  governing non-token data. The trade accepted here: a refresh token now survives a tab close by
  default. `SessionStorage` remains one line away and is documented as the tighter choice, and the
  member names still mirror msal-browser so the migration path is unchanged.

- **One strict config reader** (architect HIGH, quality HIGH, skeptic, contract, operability — 5/7,
  the panel's most-agreed finding). `KeyValueStorageConfiguration.BrowserCacheLocation` was bound as
  an options property that *no production code read*; the real reader was a lenient `Enum.TryParse`
  with a silent fallback. So the documented "invalid values are rejected" guarantee was an accident
  of whether anything happened to resolve `IOptions<KeyValueStorageConfiguration>` — and it did not
  hold at all for numerics: `"3"` parsed to an undefined enum value and fell through to the default.
  The bound property is gone; `ResolveBrowserCacheLocation` is the single reader, rejects anything
  not `Enum.IsDefined`, and names the key plus its legal values.

  It validates on **every** platform even though only the browser acts on the value: a typo in a
  shared `appsettings.json` has to fail on the desktop run a developer actually does, and that is
  also the only way the guarantee is testable while the WebAssembly lane is down.

- **`spa`/24h prerequisite is now announced at runtime** (security HIGH). Nothing enforced the
  registration type that spec 011's whole security position rests on, and `IsEncrypted` — sitting
  right there on the store — was never consulted. `SetupStorageCore` now logs a Warning naming the
  unprotected store, the `spa` requirement, and the `MemoryStorage` escape hatch. It cannot detect
  the registration type, and says so.

- **A failed silent refresh no longer reports success** (security HIGH; carried over from
  `specs/009` as "latent"). It is not latent: security's reframing is that with a 24h non-sliding
  refresh token this is the *ordinary daily* WebAssembly path — signed-in UI, no `Authorization`
  header, 401 on every call. `TokensOrNull` returns `null` rather than an access token of
  `string.Empty`, so `AuthenticationService` clears the cache and reports not-authenticated.
  Applies to login and refresh.

- **Cache callbacks rethrow cancellation and attach the exception** (operability HIGH). The
  catch-all reported a cancelled operation as "browser storage unavailable" and logged `ex.Message`
  with no stack (AGENTS.md §8 ordering, §7). Rider on the same defect class:
  `SessionStorageKeyValueStorage` passed `ex.Message` into `LogWarningMessage`'s
  `[CallerMemberName]` parameter, dropping the reason and corrupting the caller tag — the sibling
  `ApplicationDataKeyValueStorage:155` still has that bug.

### Verification

`Given_BrowserTokenCacheStorage` 8/8 (two new: unknown-value and numeric-value both throw naming
the setting), `Given_MsalAuthentication` 14/14, MSAL unit tests, Release `packageonly`, and the full
desktop suite against the pre-branch baseline.

## Sixth pass — review-panel medium findings (2026-08-20)

- **Cancelled sign-out no longer leaves token material.** `InternalLogoutAsync` checked
  cancellation *between* account removals and before the blob delete, so an abandoned logout left
  surviving accounts signed in, their refresh tokens in the serialized cache, the access token
  cached, and `IsAuthenticated` still true — a user who asked to log out, was told it was cancelled,
  and is still authenticated. Sign-out has no half-done state worth stopping at: the loop no longer
  honours cancellation (it is all local cache mutation, nothing slow to interrupt), and the blob
  delete moved into a `finally` with `CancellationToken.None` — an already-cancelled token would
  otherwise make the delete itself the no-op the `finally` exists to prevent. Also `.ToArray()`
  before the loop: `RemoveAsync` mutates the enumerable being walked.
  Red/green: `When_LogoutCancelled_Then_SerializedMsalCacheStillRemoved` (verified failing with the
  `finally` temporarily reverted).

- **`IsEncrypted => true` is now true in practice on unpackaged Windows.**
  `EncryptedApplicationDataKeyValueStorage.GetObjectValue` returned the DPAPI `byte[]`, but the
  unpackaged path persists through `ISettings`, which is string-only, and the base class stores
  `value?.ToString()` — so protected values were written as the literal `"System.Byte[]"` and read
  back as `default`, while their keys survived and kept `HasTokenAsync` true with nothing to
  recover. The same fail-open shape as the empty-token bug, on the *default Windows store*.
  `GetObjectValue` now returns base64; `GetTypedValue` accepts `byte[]` as well so existing
  **packaged** caches are not orphaned, and treats an undecodable string as absent rather than
  throwing on every read.

  Pre-existing, but item 6's `IsEncrypted` flip made the claim actively wrong there.

  **Scoped to the broken path after an attempt to verify it failed** (see below): the packaged path
  still persists the raw `byte[]`, unchanged — it works, `ApplicationData` settings hold it
  natively, it is what every existing install contains, and it is the default store on Windows. Only
  the unpackaged path switched to base64, and that path provably stored `"System.Byte[]"` garbage
  before, so there is no working data there to regress. `GetTypedValue` accepts both regardless of
  which path is active, so a value written before this change still reads back.
  `ApplicationDataKeyValueStorage.UseApplicationData` became `protected` so a derived store can tell
  which backend its `GetObjectValue` return value will cross.

### Attempted and abandoned: running the WinAppSDK runtime-test lane

To get real execution coverage for the DPAPI change, the runtime-test head was built for
`net9.0-windows10.0.19041` (it does target that TFM, and on Windows the default store is the
encrypted one, so `Given_BrowserTokenCacheStorage.When_Value_Written_Then_Round_Trips_Through_Default_Storage`
would have exercised exactly the round-trip that changed). It does not build:

- `Uno.Extensions.Navigation.UI.Tests` fails that TFM with `InitializeComponent` /
  `_contentGrid` not found across its XAML pages — the WinUI XAML code generation does not run there.
- Not caused by this branch: that project references only Navigation.UI, Navigation.Toolkit,
  Hosting.UI and RuntimeTests.Core — no Authentication or Storage code — and **every** CI lane
  passes `Build_Windows=false` (`stage-build-runtimetests-*.yml`, `stage-runtime-tests-desktop.yml`),
  so that TFM has never been built by anyone and nobody has noticed.
- Substituting the product Navigation projects for the test project does not work either: the head's
  own `App.xaml.cs:1` opens with `using Uno.Extensions.Navigation.UI.Tests;`, so it hard-depends on
  the project that cannot compile.

Worth its own issue: fixing XAML code generation for the WinAppSDK TFM would unlock a runtime-test lane for
the one platform whose *default* store is the encrypted one — currently the least-covered store in
the repo despite being the most security-relevant.

- **Concurrent resolves can no longer double-build a provider.**
  `ProviderFactory.AuthenticationProvider` was a bare `configuredProvider ??= ConfigureProvider(...)`.
  Two concurrent resolves could both build; since `ConfigureProvider` returns a record *copy*, the
  losing instance stayed subscribed to `ITokenCache.Cleared` for the host's lifetime with no
  reference left to unsubscribe it — leaking that provider and its `IPublicClientApplication`, and
  running the clear handler twice. Now a `Lazy` with `ExecutionAndPublication`. This is shared
  `Uno.Extensions.Authentication` code, so it fixes the Oidc and Custom providers too. The code
  comment in `MsalAuthenticationProvider.Build` that asserted this "cannot double-subscribe" was
  simply wrong and now says what actually guarantees it.

- **The `MsalCache_` prefix is pinned where package CI runs.**
  `Given_MsalTokenCacheStore.When_KeyPrefix_Then_It_Matches_The_Persisted_Contract` asserts the
  literal and that it does not collide with `TokenCache`'s `AuthToken_` prefix. The other literal
  guards live in runtime-test lanes that are filter-scoped and two of which are disabled, so
  renaming the prefix — which orphans every user's serialized cache — would not have been caught.

### Decided: `LoginAsync` without a dispatcher stays throwing, and is now documented

The sibling of the logout defect — `AuthenticationServiceExtensions.LoginAsync(credentials,
provider, ct)` passes `dispatcher: default`, so `MsalAuthenticationProvider.InternalLoginAsync`
throws `ArgumentNullException` and that public overload can never work with MSAL.

Analyzed and **deliberately left as-is for now** (documented instead, 2026-08-20):

- The guard is genuinely too strict, not merely correct-but-unfriendly. The dispatcher is used
  *only* by the interactive leg: `AcquireTokenAsync` (`:541`) calls `AcquireSilentTokenAsync`, which
  takes no dispatcher, and escalates to `AcquireInteractiveTokenAsync` only when there is no usable
  token. The guard sits at the top of `InternalLoginAsync`, so it rejects the call before trying
  silent — failing a sign-in that would have shown no UI at all.
- Spec 011 sharpens it: WebAssembly sign-in now survives a reload, so "silent login succeeds" went
  from impossible there to routine. The guard blocks the scenario this spec enabled.
- The fix is *not* the logout fix. Logout never touched the dispatcher, so that guard was deleted;
  login does need one for `AcquireTokenInteractive`, so the guard has to move to
  `AcquireInteractiveTokenAsync` rather than disappear — with a message that says interactive
  sign-in is what requires it.
- Why not now: it changes public behavior of a published package (`LoginAsync(ct)` starts
  succeeding where it always threw) and the honest red/green test is "silent login succeeds with no
  dispatcher", which needs a cached account and a working silent path — the same harness territory
  where the shared on-disk MSAL cache already defeated one attempt (see the empty-token finding in
  `specs/009`). Worth doing properly, not as a drive-by.

Documented meanwhile: `<remarks>` on the extension overload naming the exception and pointing at the
`IDispatcher` overload, and an IMPORTANT note in `doc/Learn/Authentication/HowTo-MsalAuthentication.md`
§8 contrasting it with sign-out, which needs no dispatcher.

### Still open after this pass

~~`AuthenticationService._providers` is a plain `Dictionary` mutated from `BuildProviders()` without
synchronization~~ — **fixed in the seventh pass:** it is now a `Lazy<IDictionary<…>>` built once with
`ExecutionAndPublication` and never mutated, so concurrent `AuthenticationProvider(...)` calls cannot
corrupt it and the `providers.First()` fallback cannot observe a half-written dictionary.
`Providers` deliberately still reports empty until something else has built them — it checks
`IsValueCreated` rather than forcing the build, because building constructs real clients (MSAL builds
an `IPublicClientApplication`) and `TestHarness` view models bind to that property during page
construction. Making it build on read would be a separate, deliberate behavior change.

`_SSKVS` is still unpinned — it lives in `Storage.UI`, which has no test project (see the
namespace-parity item below). The `ITokenCache.Cleared` handler remains fire-and-forget, so a stale
continuation can still delete a blob a *subsequent* login wrote; logout's own clear is awaited, so
this is the narrow case only.

### Panel follow-ups still open after this pass

The first panel's medium findings on cancelled logout, the `Build()`/`Cleared` double-subscribe,
`IsEncrypted` on unpackaged Windows, the `MsalCache_` pin and the `IEnumerable<IAccount>`
enumerated-while-mutated loop are fixed above in this pass. Left open here:

Medium: `_SSKVS` is not pinned by any package-CI test; redundant blob deserialize on every MSAL
access plus a base64→JSON double-encode (~5-6x transient peak, irreversible on WASM); the logout
loop re-serializes per account and deletes the key twice; the `LoginAsync(credentials, provider,
ct)` overload throwing with MSAL (analyzed and documented above rather than fixed); linked-source
test project papering over a missing MSAL core/UI split; no `Storage.UI.Tests` project for
storage-owned behavior.

Low/info: cross-app token pickup on a shared origin; sign-out does not clear the IdP session cookie
(now documented); unreachable `DefaultKeySuffix`; `UNO_EXT_MSAL_NOSTORAGE` names the opposite of
what it does (*renamed `UNO_EXT_MSAL_BROWSER` 2026-08-23*); clear that is never awaited can delete a
fresh blob; CI lane hygiene (iOS liveness check, unpinned `playwright install`).

---

## Next agent: start here

The code is complete, and the WebAssembly path has now been exercised by hand. What remains are
coverage gaps, not unfinished work:

1. ~~**Manual sign-in pass on the WASM head**~~ — **done 2026-08-21.** Confirmed working on
   `Uno.Samples` → `Authentication.MsalExtensionsDemo` (branch `dev/sb/msa-ext`) with
   `KeyValueStorageConfiguration:BrowserCacheLocation` set to `SessionStorage` and a real ClientId.
   At the time this was the only evidence for the browser path: the WebAssembly runtime-test lane
   could not run until the ninth pass, and until 2026-08-23 ran only `Given_BrowserTokenCacheStorage`
   (see the second review panel section — it is now expected to run the full auth namespace). The
   demo renders through SkiaRenderer as a single `<canvas>`, so synthetic pointer events never
   reach the sign-in button — it had to be a human click. **Re-run it by hand after any change to
   `SessionStorageKeyValueStorage`, the `SetBeforeAccessAsync`/`SetAfterAccessAsync` callbacks, or
   the store selection** until the widened lane is confirmed green.
2. **The multi-account logout path has no automated guard** — see "What is still not covered" in
   the third-pass section.
3. ~~The empty-token refresh finding (`specs/009-msal-auth-fixes/progress.md`) is still open and
   still needs a deterministic repro before it is fixed.~~ **Fixed 2026-08-23** — see the second
   review panel section and `specs/009-msal-auth-fixes/progress.md`.

The sections below are history in reverse order; the warnings they open with were true when
written and are superseded by the passes above them.

### Corrections to the second-pass note, for whoever reads it in the history

The earlier plan for the DI problem was wrong in a way worth recording:

> **Plan:** add `TryAddSingleton<IKeyValueStorage>(sp => sp.GetRequiredDefaultInstance<IKeyValueStorage>())`
> inside `InternalAddMsal`.

That is a **no-op** whenever `UseStorage` ran first. `AddNamedSingleton` calls
`services.TryAddTransient<TService>(sp => sp.GetRequiredService<TImplementation>())` for each
provider (`Core/DependencyInjection/ServiceCollectionExtensions.cs:38`), and `AddKeyedStorage`
registers `InMemoryKeyValueStorage` first — so plain `IKeyValueStorage` is **already** registered,
resolving to in-memory storage, and any later `TryAdd` loses. MSAL would have silently persisted to
nothing, order-dependently. The landed fix is `MsalKeyValueStorage`, a one-line wrapper record whose
type nobody else registers, built from `GetRequiredDefaultInstance` in a factory.

Left alone deliberately: plain `IKeyValueStorage` resolving to in-memory is a trap for any consumer
who injects the interface, but changing it is a public behavior change for a shipped package and is
not 011's business. Worth its own issue.

### The spec's open question, now closed

> check whether the MSAL configuration section is bound early enough

**It is, in every ordering.** `HostBuilder.Build()` runs *every* `ConfigureAppConfiguration`
delegate (`BuildAppConfiguration`) before the *first* `ConfigureServices` delegate
(`CreateServiceProvider`), and host configuration is folded into app configuration ahead of both. So
`ctx.Configuration` inside `UseStorage`'s `ConfigureServices` already carries the `Msal` section even
though `UnoHost.CreateDefaultBuilder` calls `UseStorage()` before the app has called `AddMsal`. No
storage-level setting, and no ordering contract on apps, was needed.
`Given_BrowserTokenCacheStorage` exercises exactly that ordering: the test adds the configuration
*after* `CreateDefaultBuilder` has already registered storage.

---

## Done

- [x] **Item 1 — `BrowserCacheLocation` + `MsalConfiguration.BrowserCacheLocation`.**
  New `src/Uno.Extensions.Authentication.MSAL/BrowserCacheLocation.cs`, defaulting to
  `SessionStorage`. Landed `internal` rather than the spec's `public` — rationale recorded in
  `spec.md` under the public-surface section.
- [x] **Item 2 (storage half) — `MsalTokenCacheStore`.**
  New `src/Uno.Extensions.Authentication.MSAL/MsalTokenCacheStore.cs`: base64 round-trip of the
  `SerializeMsalV3` blob through `IKeyValueStorage`, keyed `MsalCache_{ClientId}`. Deliberately free
  of `#if`, of Uno types and of `Microsoft.Identity.Client` types so the test project can compile it
  as linked source — that is the seam that makes the browser cache testable at all, since MSAL's
  `TokenCacheNotificationArgs` cannot be constructed by a test.
- [x] **Item 2b — the MSAL half.** `SetupStorageCore`'s browser branch (`UNO_EXT_MSAL_BROWSER`,
  then still named `UNO_EXT_MSAL_NOSTORAGE`; since 2026-08-23 split out as `SetupBrowserStorage()`)
  registers `SetBeforeAccessAsync` / `SetAfterAccessAsync` over `MsalTokenCacheStore`
  (`MsalAuthenticationProvider.cs`). Both callbacks catch and log rather than propagate: browser
  storage can be unavailable outright (private mode, sandboxed iframe, storage disabled by policy),
  and the fallback — an empty cache — is exactly the pre-011 behavior, so a broken store must not be
  the reason a sign-in fails.

  Note the `MemoryStorage` early-return the spec called for turned out to be unnecessary: the
  provider never reads `BrowserCacheLocation`. It writes through whatever the default
  `IKeyValueStorage` is, so `MemoryStorage` → `InMemoryKeyValueStorage` gives in-memory-only for
  free, with one decision point instead of two that can disagree.
- [x] **Item 3 — `SessionStorageKeyValueStorage`** in `src/Uno.Extensions.Storage.UI/KeyValueStorage/`.
  Derives from `BaseKeyValueStorageWithCaching` like its siblings, over four `[JSImport]` bindings
  on `globalThis.sessionStorage` (`getItem`/`setItem`/`removeItem`/`key`). Two decisions worth
  knowing:
  - `sessionStorage.length` is a *property*, which `[JSImport]` cannot bind, so enumeration walks
    `key(index)` until it returns null — what the DOM spec guarantees past the last entry.
  - Keys carry a `_SSKVS` suffix and clear-all removes only suffixed keys, unlike
    `ApplicationDataKeyValueStorage`'s container-wide clear. sessionStorage belongs to the whole
    origin, so `ClearAllAsync` must not reach another script's state.
- [x] **Item 4 — the default instance follows the setting.** `AddKeyedStorage` now takes the
  `IConfiguration` and, **at runtime** (`OperatingSystem.IsBrowser()`, not a compile-time symbol),
  maps `Msal:BrowserCacheLocation` (fourth pass: moved to
  `KeyValueStorageConfiguration:BrowserCacheLocation`) to a provider name: `SessionStorage` → the new provider,
  `LocalStorage` → `ApplicationData`, `MemoryStorage` → `InMemory`, anything else → `SessionStorage`.
  The runtime check is what keeps Skia desktop — which shares the same `#else` branch — on
  `ApplicationData`; see the spec's Scope guard.
- [x] **Item 7 — docs.** `doc/Learn/Authentication/HowTo-MsalAuthentication.md`: the WebAssembly
  platform-support row, a new `#### WebAssembly token cache` subsection with the three values and
  the "both caches or the setting is cosmetic" rationale, and a new `## Prerequisites` section
  stating the `spa` registration requirement with its 24-hour non-sliding consequence (the redirect
  NOTE in section 4 now links to it instead of mentioning SPA in passing). Done in this pass rather
  than last because the old text became actively false the moment the trio landed.

### Tests added

| Test | Project |
| --- | --- |
| `Given_BrowserCacheLocation` — 2 cases: the enum member names Storage.UI matches strings against, and the `SessionStorage` default *(deleted in the fourth pass — the string contract it guarded no longer exists)* | `Authentication.MSAL.Tests` |
| `Given_BrowserTokenCacheStorage` — 6 cases: default / `LocalStorage` / `MemoryStorage` / other casing / unknown value, plus a set-get-enumerate-clear round trip through the resolved store | `Authentication.MSAL.UI.Tests` |
| `Given_MsalAuthentication.When_Login_Then_MsalCacheOnlyPersistedThroughStorageOnWebAssembly` | `Authentication.MSAL.UI.Tests` |

`Given_BrowserCacheLocation` exists because Storage.UI cannot reference the internal enum across the
assembly boundary, so it matches the member *names* as strings. Renaming a member — or adding a
fourth — would otherwise silently reroute the token cache; the test fails instead.

The UI tests deliberately run on **every** head, not just WebAssembly. Off-browser they assert the
setting is *ignored*, which is the regression guard for the Scope guard: Skia desktop shares the
`#else` branch, and following the setting there would turn DPAPI / Keychain / libsecret into
cleartext.

### Verification performed

- `dotnet test src/Uno.Extensions.Authentication.MSAL.Tests` → **40/40 passed** (2 new, 38 existing).
- `msbuild Uno.Extensions-packageonly.slnf -restore -m -p:Configuration=Release` → **0 errors**, and
  no warning from any changed file. (On Windows this must be `msbuild`, not `dotnet build`: Uno.Sdk
  fails WinUI class libraries with XAML under `dotnet build` with UNOB0008.)
- Skia desktop runtime tests: `Given_BrowserTokenCacheStorage` **6/6 passed**,
  `Given_MsalAuthentication` **11/11 passed** — the latter confirms the new required constructor
  parameter on the provider resolves from DI on a real head.
- Full Skia desktop suite (`!_HotReload`), run twice — once on this branch, once with the whole
  change `git stash`ed — to satisfy AGENTS.md's "assume the branch caused it" rule: **16 failures,
  byte-identical sets**. All pre-existing (Navigation `GetDataAsync` chains, Selection/ComboBox,
  `When_ResetLogger_*`), which is the failing set `.azure-pipelines.yml:96` refers to as "those 15"
  when explaining why `stage-build-runtimetests-skia.yml` is disabled — the real count is 16.
- Full multi-TFM builds of `Uno.Extensions.Storage.UI` and `Uno.Extensions.Authentication.MSAL`
  succeed, which is what proves the `[JSImport]` partial methods compile on the non-browser TFMs too
  (same arrangement as `Uno.Extensions.Hosting.UI`'s `Imports`).
- WebAssembly runtime-test head builds (`-f net9.0-browserwasm`, the command
  `stage-build-runtimetests-mobile.yml:85` runs).

### Verified in a real browser, outside CI

The WebAssembly runtime-test lane cannot run (see below), so the browser half was verified against
the **`Authentication.MsalExtensionsDemo` sample** (`Uno.Samples`, branch `dev/sb/msa-ext`) built on
locally-packed `255.255.255.255-local` artifacts, driven with headless chromium via Playwright.

Nothing in that app's startup touches the token cache on a fresh profile — with no cached provider,
`AuthenticationService.RefreshAsync` returns without reading a token — so a
`ProbeTokenCacheStorage` diagnostic was added to its `App.xaml.cs` (`InitialNavigate`) that
resolves the default `IKeyValueStorage`, round-trips one key and logs the result. Results:

| `Msal:BrowserCacheLocation` | Resolved store | Browser storage after startup |
| --- | --- | --- |
| absent / `SessionStorage` | `SessionStorageKeyValueStorage` | `sessionStorage["Probe011_SSKVS"]` present |
| `LocalStorage` | `ApplicationDataKeyValueStorage` | `localStorage["UnoApplicationDataContainer_Local_Probe011_ADCSSS"]` present, `sessionStorage` empty |
| `MemoryStorage` | `InMemoryKeyValueStorage` | both empty — nothing reaches browser storage |

And the lifecycle behavior the feature exists for, same tab vs. new browsing session:

1. first load → `value already stored: <none>`, key written
2. **reload → `value already stored: written by ProbeTokenCacheStorage`** — survives a page reload
3. new browser context → `<none>` again — does not outlive the session

So the four `[JSImport]` bindings (`getItem` / `setItem` / `removeItem` / `key`) all work, the
`key(index)`-until-null enumeration works, the `_SSKVS` suffix is applied and stripped, and item 4's
selection is honored from a real embedded `appsettings.development.json` — a different configuration
source than the in-memory one the unit/UI tests use.

~~**Still unverified: the MSAL blob itself**~~ — **verified by hand 2026-08-21** (see "Next agent:
start here"). The blob needed a completed sign-in, which needed the real ClientId (deliberately
redacted from git) and a human click, because the sample renders through SkiaRenderer as one
`<canvas>` and synthetic pointer events do not reach it. Confirmed working with
`BrowserCacheLocation` set to `SessionStorage`.

### Why CI can't do the above

*(Superseded: the ninth pass enabled the lane, and the second review panel widened it to the full
auth namespace.)*

`uno-runtimetests-wasm` against the published head reaches
`"Application configured to start runtime-tests (Config=Given_BrowserTokenCacheStorage)"` and then
dies in the engine before the first test:

```text
System.PlatformNotSupportedException: Operation is not supported on this platform.
   at System.Runtime.InteropServices.PosixSignalRegistration.Register(...)
   at System.Console.add_CancelKeyPress(...)
   at Uno.UI.RuntimeTests.Engine.RuntimeTestEmbeddedRunner.RunTestsAndExit(...)
```

That is exactly the blocker `.azure-pipelines.yml:106-114` documents when disabling
`stage-runtime-tests-wasm.yml` (engine 2.0.0-dev.79 compiles the `Console.CancelKeyPress`
registration in because Uno.Sdk no longer defines `__WASM__` for consumer projects). It is not
fixable from this repo and is unrelated to this change.

So in CI the new `Given_BrowserTokenCacheStorage` cases assert only the *off-browser* half — which is
the Scope-guard regression guard, and worth having. **Do not read the green CI badge as coverage of
this feature**; the browser evidence is the sample-app run above, and it has to be repeated by hand
after any change to `SessionStorageKeyValueStorage` until the engine takes a runtime check.

### Configuration wart this surfaced — since fixed

**Resolved 2026-08-20 (third pass): the setting moved to `KeyValueStorageConfiguration`.**
`BrowserCacheLocation` is now `KeyValueStorageConfiguration:BrowserCacheLocation`, bound by the
section `UseStorage` already registers, and `MsalConfiguration.BrowserCacheLocation` is gone. The
enum moved with it, to `Uno.Extensions.Storage` (still `internal`; the existing `InternalsVisibleTo`
covers `Storage.UI`).

Two things forced the decision once the demo made it concrete:

1. **The old key was a lie about ownership.** The setting selects the host's single default
   `IKeyValueStorage`, which the shared `ITokenCache` uses — so an app on `AddOidc` or
   `AddCustom` had its token-cache location dictated by a section named after MSAL, and an app that
   renamed its provider (`AddMsal(..., name: "MsalAuthentication")` — what the sample does) had to
   write the setting twice, once under its own name and once under `Msal`.
2. **Nothing ever read `MsalConfiguration.BrowserCacheLocation`.** Confirmed by grep: the provider
   writes through whatever the default storage is, so the property existed purely as a typed schema
   anchor for a key another assembly parsed as a string.

Consequences worth knowing:

- **The cross-assembly string contract is gone.** Storage.UI now parses its own enum with
  `Enum.TryParse<BrowserCacheLocation>(..., ignoreCase: true)`, so a rename is a compile error rather
  than a silent behavior change. `Given_BrowserCacheLocation` — which existed only to assert the
  enum's member names still matched the literals Storage.UI compared against — was deleted, since the
  coupling it guarded no longer exists. Coverage did not shrink: `Given_BrowserTokenCacheStorage`
  asserts the same mapping end to end on a real host, typed.
- **An invalid value now fails loudly.** Because the value is a bound enum, configuration binding
  throws (`FormatException: … is not a valid value for BrowserCacheLocation`) instead of falling back
  to the default. That is a deliberate improvement for a setting that decides where refresh tokens
  live — a typo silently selecting `sessionStorage` when the app asked for `memoryStorage` would be
  invisible. `When_Configured_With_Unknown_Value_Then_Fails_Loudly` replaces the old
  `…_Then_Falls_Back_To_Default`.
- The enum's member names still mirror msal-browser's (`SessionStorage` / `LocalStorage` /
  `MemoryStorage`), so item 1's vocabulary-carry-over rationale survives — only the section moved.

### Original write-up of the wart

The demo registers its provider as `AddMsal(window, name: "MsalAuthentication")`, so every MSAL
setting lives under `MsalAuthentication:` while the cache location has to be written under `Msal:` —
the well-known key Storage reads, since there is one default `IKeyValueStorage` per host and no way
to know which section names the app used. It works, and it is documented, but two adjacent sections
where one would do is exactly the kind of thing a consumer reports as a bug. Options if it is worth
revisiting: move the setting to the storage section `UseStorage` already binds
(`KeyValueStorageConfiguration:BrowserCacheLocation`, with the enum moving to
`Uno.Extensions.Storage`), or add an `IHostBuilder` overload that names the section. Not changed
here — it would undo item 1's landed naming decision, whose msal-browser carry-over rationale is in
`spec.md`.

*(Kept for the reasoning. The first option is what shipped — see above.)*

## Done — items 5 and 6 (2026-08-20, third pass)

- [x] **Item 5 — logout clears the blob.** Two changes in `MsalAuthenticationProvider`:
  - `InternalLogoutAsync` removes **every** account, not `accounts.FirstOrDefault()`. That was the
    root cause: with more than one account, removing one left the rest signed in *and* their refresh
    tokens in the serialized cache, so silent sign-in resurrected whichever account survived.
  - The serialized cache is then deleted explicitly via `MsalTokenCacheStore.ClearAsync`, plus the
    same call from an `ITokenCache.Cleared` handler as belt-and-braces. Removing the accounts
    normally makes MSAL serialize an empty cache, which the after-access callback turns into a
    delete — but only if those callbacks were registered (`SetupStorage` can fail) and only if MSAL
    considered the cache changed. Deleting explicitly is what guarantees no refresh-token material
    outlives the sign-out.

  **Deliberately not `#if`-gated to browser-wasm.** Off the browser nothing ever writes that key, so
  the clear is a no-op there rather than a special case — one less conditional, and it makes the
  behavior testable on the only runtime-test lane that actually runs (see below).

  The `Cleared` handler is fire-and-forget off a synchronous event; `ClearTokenCacheStoreAsync` owns
  the try/catch so nothing escapes (AGENTS.md §10), and it is never unsubscribed because the
  provider and the token cache are both host-lifetime singletons.

- [x] **Item 6 — `IsEncrypted` corrected.** `EncryptedApplicationDataKeyValueStorage` (DPAPI via
  `DataProtectionProvider`) and `PasswordVaultKeyValueStorage` (Credential Locker) both reported
  `false`. Now `true`, with the rationale in each XML remark: the property is on the public
  `IKeyValueStorage` surface and is exactly the flag a consumer would branch on to decide whether a
  store is safe for tokens, so under-reporting steers callers *away* from the stores that protect
  them. Note `PasswordVaultKeyValueStorage` is behind `#if !WINUI && (...)` and so is not compiled
  in this repo at all today — corrected for consistency rather than effect.

  Scoped as a separate PR; kept as its own commit so it can still be split out.

### Tests added, and why they run everywhere

| Test | Guards |
| --- | --- |
| `When_Logout_Then_SerializedMsalCacheRemoved` | logout deletes the `MsalCache_{ClientId}` entry |
| `When_TokenCacheCleared_Then_SerializedMsalCacheRemoved` | the `ITokenCache.Cleared` path does too |

Both seed the entry through the host's default `IKeyValueStorage` rather than by signing in on a
browser, which is what lets them assert the security-relevant behavior — *refresh-token material
does not outlive a sign-out* — on the Skia desktop lane. Verified red first (both reported
`found True`, i.e. the entry survived) with the provider change stashed, then green.

**What is still not covered:** the multi-account half of the root-cause fix. A red test needs two
accounts in MSAL's cache, and the provider's own login path cannot produce them — `AcquireTokenAsync`
tries silent first, which succeeds for the existing account, so a second interactive sign-in never
happens. Forcing silent to fail lands in the same non-determinism as the empty-token finding in
`specs/009-msal-auth-fixes/progress.md`. The loop is a strictly smaller behavior than
`FirstOrDefault()` (it removes a superset), so it cannot regress the single-account case the suite
does cover.

## Remaining

Every implementation item in `spec.md` is now done. What is left is confirmation, not code:

- [x] **Sign-in pass on the WASM head.** Confirmed manually by the user on 2026-08-20 with
      `BrowserCacheLocation: SessionStorage`. The tab-close leg was not separately reported.
      Original scope: Storage selection and the reload/session lifecycle are
      confirmed (see "Verified in a real browser"); what is left is the MSAL blob: sign in, confirm a
      `MsalCache_{ClientId}` entry appears in `sessionStorage`, reload and confirm the session is
      restored *without* a prompt, log out and confirm the entry is gone, then close the tab and
      confirm the session does not come back. Needs the real ClientId and a human click on the
      SkiaRenderer canvas.
- [ ] The multi-account logout path has no automated guard — see "What is still not covered" above.

## Not part of this work

`doc/Learn/Authentication/HowTo-MsalAuthentication.md` had an unrelated uncommitted edit in the
working tree (removal of a NOTE about uno#20601 and the Skia-mobile assembly substitution) that
predates this task; it was committed separately as `4a8c2ad0d` before this pass.

## Eighth pass — the device lanes were only running half the auth suite (2026-08-21)

Prompted by a question about CI coverage, not by a failure. The check list on PR #3139 shows five
runtime-test entries, and only two of them run anything:

| Check | What it does |
| --- | --- |
| `Runtime Tests - Devices Runtime Tests - Desktop (Skia)` | **Runs** the filtered suite |
| `Runtime Tests - Devices Runtime Tests - iOS Simulator` | **Runs** the filtered suite |
| `Build Tests Build Runtime Tests - Android` | Builds the head as an artifact; the run stage is commented out (head does not package its `ProjectReference` closure — `failed to load bundled assembly Uno.Extensions.Reactive.dll`) |
| `Build Tests Build Runtime Tests - WebAssembly` | Builds the head; run stage commented out (`RuntimeTestEmbeddedRunner` calls `Console.CancelKeyPress`, which throws because Uno.Sdk no longer defines `__WASM__`) |
| `Build Tests Hot-Reload Tests - Skia Desktop (Debug)` | Separate HR suite, unrelated to auth |

The bug: `RuntimeTestsFilter` was `'Given_MsalAuthentication'`. The engine filter is a substring
match on the fully-qualified test name, so it matched that one class and **nothing else** — the 8
`Given_BrowserTokenCacheStorage` cases added by this spec never executed on any lane, on any
platform, at any point in this branch. Both lanes were green throughout.

Fixed by scoping the filter to the namespace instead: `'Authentication.MSAL.UI.Tests'`. Verified
locally against the desktop head before committing — three candidate filters, each run to
completion:

| Filter | Tests matched |
| --- | --- |
| `Given_MsalAuthentication` (was) | 15 |
| `MSAL.UI.Tests` | 23 |
| `Authentication.MSAL.UI.Tests` (now) | 23, 0 failed |

23 = 15 `Given_MsalAuthentication` + 8 `Given_BrowserTokenCacheStorage`. A namespace-scoped filter
also picks up classes added later, which a class-named one does not.

Lesson recorded in `specs/lessons.md` ("A test filter naming a class silently drops its siblings").

## Ninth pass — the WebAssembly lane runs, and iOS found a contract bug (2026-08-21)

Three outcomes from asking why only two platforms were running the auth tests.

### The browser lane is enabled, and the spec's own code is finally covered by CI

The blocker was never this repo's: `Uno.UI.RuntimeTests.Engine` ships as source, and its embedded
runner guards `Console.CancelKeyPress` - a `PlatformNotSupportedException` in the browser - with
`#if !__WASM__`. Uno.Sdk stopped defining `__WASM__` for consumer projects, so the registration
compiled in and killed the app before the first test (build 228808). Still true in the newest engine,
`2.0.0-dev.81`, so there was nothing to wait for.

What the earlier note got wrong: it concluded that defining `__WASM__` "does not work either" because
the symbol's other branches call `Uno.Foundation.WebAssemblyRuntime.InvokeJS`, which the browser head
cannot reference (CS0234, and `src/Directory.Build.props`' `__RemoveUnoRuntimeWasm` target drops that
package for non-head projects on purpose). That is a reason to *supply* the type, not to give up: the
engine's sources compile into `Uno.Extensions.RuntimeTests.Core`, so an internal one in the same
assembly satisfies them. `WebAssemblyRuntimeShim.cs` is ~10 lines over a `[JSImport]` binding to
`globalThis.eval`, which is what Uno's own `InvokeJS` did, and the three call sites pass
self-contained IIFEs. The symbol and `AllowUnsafeBlocks` (SYSLIB1074) are scoped to the browserwasm
TFM of that one project.

Also worth recording, since it decided the approach: those `#if __WASM__` branches are only a
*preference* for reading configuration from the URL query string. The engine's own comment says the
environment-variable path "works on all platforms - on WASM, the test runner injects these into
uno-config.js", so nothing about the lane depends on them.

Verified locally, not on hope - the full auth namespace, in headless chromium via
`build/test-scripts/wasm-runtime-tests.sh`:

| Run | Result |
| --- | --- |
| Before | 0 tests; `PlatformNotSupportedException` before the first case |
| Full namespace (`Authentication.MSAL.UI.Tests`) | 23 run, 13 pass, 10 fail |
| Lane as configured (`Given_BrowserTokenCacheStorage`) | **8 run, 8 pass**, validator exit 0 |

So the lane is scoped to `Given_BrowserTokenCacheStorage` rather than the pipeline-wide
`$(RuntimeTestsFilter)`. All 10 failures are one harness limitation, not a product defect: MSAL's
`.WithHttpClientFactory(StubEntra.HttpClientFactory)` is not honoured in the browser, so `StubWebUi`'s
fabricated authorization code escapes to the real `login.microsoftonline.com` and returns
`AADSTS9002313 "Invalid request. Request is malformed or invalid"`. The stub itself never produces
that string - it reached Entra. The product path is fine; a manual sign-in on the WASM head works.
Widening the lane needs the stub to apply in the browser first, which is its own piece of work.

*Done 2026-08-23:* the factory was being replaced because `Build` applied the app's `Builder(...)`
callback *before* `WithUnoHelpers()`; it now runs after, the lane runs `$(RuntimeTestsFilter)`
(the full namespace), and the CI run is pending — see the second review panel section.

The 8 that do run are the ones that matter most here: they are the only automated coverage anywhere
of what this spec added - browser store selection, the strict-reader rejection of a bad value, and a
round trip through the selected store - and until now they had never executed in a browser at all.

### iOS turned up a real inconsistency in `IKeyValueStorage`

Widening `RuntimeTestsFilter` (eighth pass) ran `Given_BrowserTokenCacheStorage` on iOS for the first
time and one case failed: `When_Value_Written_Then_Round_Trips_Through_Default_Storage` asserted
`GetAsync` returns null after the key is cleared, and Keychain threw `KeyNotFoundException`.

Keychain is right. `IKeyValueStorage.GetAsync` is documented as *"If that value does not exist, throws
a `KeyNotFoundException`"*; `KeyChainKeyValueStorage` honours it and `ApplicationDataKeyValueStorage`
returns `default` instead - so the default store on Windows, Skia desktop **and** the browser violates
its own contract, and the assertion had encoded that violation because it was written on desktop.

The test now asserts only what both stores agree on (`GetKeysAsync` no longer contains the key), which
is also what `TokenCache.HasTokenAsync` actually relies on. **The divergence itself is left unfixed on
purpose**: making the default store on three platforms start throwing is a public-surface behavior
change that deserves its own PR and its own risk assessment, not a drive-by edit inside a
storage-selection change. It is worth an issue.

### Android is still disabled, but no longer for the reason the comment claimed

Re-investigated from scratch; the old note ("bin/net9.0-android contains exactly one assembly") was
measuring the wrong thing - Android does not copy to `bin` at all, and the APK does carry 329 entries
per ABI. The real chain, all for the same head at net9.0-android/Debug:

- `_ResolvedProjectReferencePaths` lists all 23 references, `Uno.Extensions.Reactive.dll` included -
  so resolution is fine;
- `ReferenceCopyLocalPaths` holds **no assemblies at all**, just two `.uprimarker` files, where
  net9.0-desktop holds the full set;
- so `ResolvedFileToPublish` (240 dlls) and Android's `ResolvedUserAssemblies` (161) contain only the
  head's own assembly out of everything we build. Every managed assembly that does reach the APK comes
  from the NuGet restore graph - which project references are not part of, leaving them no route in.

Ruled out by experiment: `-o` (rebuilt without it, closure still absent - the earlier note was right
to dismiss it), `EmbedAssembliesIntoApk`, the emulator, and the ABI split. Leading suspect is
`src/Directory.Build.props`' `BaseOutputPath=bin\$(MSBuildProjectName)`: during an Android build every
referenced project also emits into a second, *head-named* folder
(`<ref>/bin/Uno.Extensions.RuntimeTests/Debug/net9.0-android/`), which means the head's output path is
reaching its references as a global property. Unproven, and not a simple path mismatch - the expected
folder is populated too. Re-enable the lane once `ReferenceCopyLocalPaths` carries the closure.

## Tenth pass — the Android lane, fixed by diffing against studio.live (2026-08-21)

The ninth pass left Android disabled with the loss traced to `ReferenceCopyLocalPaths` being empty
of assemblies, and `BaseOutputPath` named as the leading suspect. **That suspect was wrong** -
overriding it to the SDK default changed nothing, and so did Debug vs Release and single- vs
multi-TFM graphs. What found it was comparing against `studio.live`, whose Android runtime-test lane
works: 18 project references, 19 copy-local assemblies at `net10.0-android`, and no `Private`
metadata on any of them. Ours carried `Private='false'`.

The source is `src/Directory.Build.targets`:

```xml
<Private Condition="'$(OutputType)' == 'library' and '$(NugetOverrideVersion)'==''">false</Private>
```

which exists so our ~50 libraries don't each duplicate the closure into their own `bin`. **.NET
Android rewrites an app head's `OutputType` to `Library`**, because an Android app has no managed
entry point - measured on the one head, `net9.0-android` reports `OutputType=Library` and
`AndroidApplication=true` while `net9.0-desktop` reports `Exe`. So the APK head matched a
library-only rule, every project reference lost copy-local on Android alone, and NuGet-restored
assemblies were unaffected only because they arrive as `RuntimeCopyLocalItems` instead. studio.live
has no such `ItemDefinitionGroup`, which is the whole difference.

Adding `and '$(AndroidApplication)' != 'true'` took copy-local from 0 to 23.

That exposed a second, independent bug: with the closure packaged the app booted and
`MobileRuntimeTestsAutostart` ran, then failed with `UnauthorizedAccessException` on
`/storage/emulated/0/Android/data/<pkg>`. `Directory.CreateDirectory` walks parents and an app cannot
create the `Android/data/<package>` level - only the platform API can. `MainActivity.OnCreate` now
calls `GetExternalFilesDir(null)` when the result-file variable is set. studio.live's autostart has
the same `Directory.CreateDirectory` call, so it only shows up where nothing has created that
directory yet.

Verified end to end on a local API 34 AVD, matching CI's `system-images;android-34;...;x86_64`:

| Stage | Result |
| --- | --- |
| Before | 0 tests; `open_from_bundles: failed to load bundled assembly Uno.Extensions.Reactive.dll` |
| APK contents after the fix | 24 `Uno.Extensions.*` assemblies, `Uno.Extensions.Reactive.dll` included |
| CI's exact `dotnet publish -o` | 48 entries (24 assemblies x 2 ABIs) |
| Emulator run, filter `Authentication.MSAL.UI.Tests` | **23 run, 23 passed, 0 failed**, validator exit 0 |

Android runs the full auth suite, including the 15 `Given_MsalAuthentication` cases that cannot run
on WebAssembly - the MSAL stub HTTP factory works there. All four device lanes are now enabled:
desktop, WebAssembly (8 storage tests), Android (23) and iOS (23).

Worth noting for whoever owns the sample heads: the same `Private=false` rule applied to every
Android app head in this repo, so `samples/Playground` and `testing/TestHarness` were packaging
APKs without their project closure too. Neither is built for Android in CI, so nothing caught it.

## Eleventh pass — Uno.Sdk bumped so the packages require the Uno that has the fix (2026-08-21)

Interactive MSAL sign-in on Skia Android/iOS/WebAssembly needs
[unoplatform/uno#24055](https://github.com/unoplatform/uno/pull/24055), which fixes
`RuntimeAssetsSelectorTask` replacing `Uno.UI.MSAL.dll` with its no-op Skia flavor and thereby making
`WithUnoHelpers()` do nothing. That is an app-build-time task shipped in **`Uno.WinUI`**, and these
packages already depend on `Uno.WinUI` - so raising `$(UnoVersion)` is enough to express the
requirement, and `global.json` is the only place it has to change:

| | before | after |
| --- | --- | --- |
| `Uno.Sdk` | 6.0.67 (UnoVersion 6.0.465) | **6.8.0-dev.21** (UnoVersion 6.8.0-dev.46) |
| `Uno.Sdk.Private` | 6.7.0-dev.938 | **6.8.0-dev.51** |

Both move together; leaving the private one behind puts the heads on a lower `Uno.WinUI` than the
libraries they reference. Confirmed against the shipped binaries that Uno.WinUI 6.8.0-dev.46 contains
the fix (the `uno.ui.msal` allowlist entry in `Uno.UI.Tasks`), and confirmed on the packed nuspec that
the floor now reads `Uno.WinUI 6.8.0-dev.46`. Consumers below it get an NU1605 package-downgrade
error instead of a silent no-op - the docs prerequisite was corrected accordingly, since it had said
no package reference expressed the requirement.

Note `dotnet build` cannot build this repo's XAML-bearing WinUI libraries at all (UNOB0008, true on
6.0.67 as well) - use `msbuild`. Two real breaks came out of the bump.

### Uno.Toolkit pinned to 8.4.2 - and the wrong fix that got there first

*(Superseded 2026-08-23: the pin — and the Hot Design, Themes and `System.Text.Json` pins below —
are removed; the CS1705 was CI building with the .NET 9.0.200 SDK. See the second review panel
section and the "pin hiding a toolchain skew" lesson in `specs/lessons.md`.)*

`Controls/ModalFlyout.xaml` uses `utu:NativeFramePresenter` inside an `<ios:ControlTemplate>`, and
Uno's `ios` conditional XAML namespace also matches Mac Catalyst. Uno.Sdk 6.8.0-dev.21 supplies
Uno.Toolkit 9.2.0-dev.18, which ships no maccatalyst assembly, so catalyst fell back to the plain
`net9.0` Toolkit flavor and the type was gone: `UXAML0001` plus five cascading generator errors.

**The first fix was to drop the `net9.0-maccatalyst` TFM from that package, and it was wrong.** CI then
failed the Packages job with the real problem:

```console
error CS1705: Assembly 'Uno.Toolkit.WinUI' uses 'Microsoft.iOS, Version=26.0.0.0' which has a higher
version than referenced assembly 'Microsoft.iOS', Version=18.2.0.0
```

Toolkit 9.2.0-dev.18's **iOS** assembly is built against Microsoft.iOS 26, while the .NET 9 iOS
workload used here references 18.2 - exactly what the existing `UnoToolkitVersion 8.4.2` pin in
`samples/Directory.Packages.props` was already guarding against. I had read that pin's comment,
checked that Toolkit 9.2.0-dev.18 ships a folder named `net9.0-ios18.0`, and concluded the rationale
was stale. **The folder name is the TFM the package declares, not what the assembly references.** That
wrong conclusion was briefly written into the samples props file as justification for removing the pin.

The correct fix is to carry the same pin in `src/Directory.Packages.props`: Toolkit 8.4.2 is the latest
stable whose iOS flavor references 18.2, **and it still ships a maccatalyst flavor containing
`NativeFramePresenter`** - so pinning it removes the CS1705, removes the UXAML0001, and makes the TFM
drop unnecessary. The published surface is unchanged and there is no breaking change. It also means the
Navigation.Toolkit package declares a floor on stable 8.4.2 rather than on a prerelease 9.2.0-dev build.

`Uno.WinUI` itself is not the problem: 6.8.0-dev.46 still ships `net9.0-ios18.0` and
`net9.0-maccatalyst18.0` alongside its net10 flavors, which is why only the Toolkit needed pinning.
Remove both pins when this repo moves to net10 / iOS 26.

### Hot Design is pinned, never disabled - its processor is what applies XAML hot reload

`Uno.UI.HotDesign` is referenced implicitly by the Sdk, and the 1.20.0-dev.750 that 6.8.0-dev.21
defaults to demands `Uno.Toolkit.WinUI >= 9.0.0-dev.9` and `System.Text.Json >= 9.0.0`, the first
colliding with the toolkit pin above. **The first attempt disabled the package** in the runtime-test,
Playground and TestHarness heads, on the reasoning that a design-time tool has no role in an automated
test run. CI disagreed: the hot-reload lane went from 64/64 to **40/64**, and every one of the 24
failures was a XamlHR case timing out with "XAML HR did not replace the ... instance within 30s".

Diffing the dev-server output between the green and red runs shows why:

```console
pre-bump  (64/64):  Processor assembly location: .nuget/uno.ui.hotdesign/1.19.1...
post-bump (40/64):  no hotdesign processor at all
```

**`Uno.UI.HotDesign` contributes a dev-server processor, and that processor is what applies XAML hot
reload.** Removing the package removes XAML HR. Note the wrong turn in between: the package's own
`Uno.WinUI.DevServer` dependency is 6.7.0-dev.688, *lower* than the 6.8.0-dev.51 the head references,
which was used to argue the removal could not be at fault. Dependency versions were the wrong signal -
the package's contribution is a server-side plugin.

The fix keeps the feature and pins the version: `UnoHotDesignVersion 1.19.175`, the one in use before
the bump, whose dependencies are exactly what this repo already pins - Toolkit 8.4.2, Themes 6.1.1,
Uno.WinUI 6.6.166, and no System.Text.Json floor at all. So the toolkit pin, the 8.0.5
System.Text.Json pin and Hot Design all coexist.

Also worth recording, because it looked like the culprit and was not: the hot-reload lane's generated
`DebugPlatforms.props` workaround is still doing its job. Its failure mode -
`MSBuildWorkspace [Failure] ... Project does not contain 'Compile' target` - appears **152 times in
both** the 64/64 and the 40/64 runs, so it is pre-existing noise rather than a regression.

The mobile device lanes still pass `-p:UnoDisableHotDesign=true -p:UnoDisableMCPSupport=true`, and that
is left alone deliberately: those flags were added for a diagnosed startup crash
(`Uno.UI.App.Mcp.Client` -> `FileNotFoundException: SkiaSharp.Views.Windows`), Hot Design also ships
`SkiaSharp.Views.WinUI`, and no XamlHR test runs on those lanes. Re-enabling it there would risk
reintroducing that crash on three green lanes for no coverage gain.

`Microsoft.Windows.SDK.BuildTools` moved to 10.0.28000.2526 in `samples/Directory.Packages.props`
because `Uno.Extensions.Hosting.WinUI` now requires it, and the TestHarness UITest SkiaSharp pin moved
from a duplicate `PackageVersion` to `VersionOverride` (NU1506).

### IL2026 in Reactive.UI on WebAssembly

`Utils/ButtonExtensions.cs` registers an attached DP of type `Exception`. On browserwasm - the only
TFM where the trim analyzer runs - `RegisterAttached`'s `propertyType` is treated as reflected-over,
which reaches `Exception.TargetSite`, whose getter is `[RequiresUnreferencedCode]` as of .NET 9.
Suppressed at the field with a justification: the property only stores and returns the instance a
command surfaced, so the metadata the trimmer may drop is never read.

### Verification

- `msbuild Uno.Extensions-packageonly.slnf -c Release`: **0 errors** (7 before the two fixes, in 2
  project/TFM combos, nothing masked behind them).
- `dotnet test Uno.Extensions-packageonly.slnf -c Release --no-build --filter "FullyQualifiedName!~UI.Tests"`:
  **1562 passed, 0 failed, 19 skipped** across 6 projects.
- Packed nuspec floors: `Uno.WinUI` / `Uno.WinUI.MSAL` 6.8.0-dev.46, `Microsoft.Identity.Client` 4.87.0.

### Verified in CI (build 229616)

The bump lands green. Every runtime-test surface now runs on Uno.Sdk 6.8.0-dev.21:

| Suite | Result |
| --- | --- |
| Hot-Reload Runtime Tests | 64/64 (was 40/64 while Hot Design was disabled) |
| Desktop Runtime Tests | 23/23 |
| Android Emulator Runtime Tests | 23/23 |
| iOS Simulator Runtime Tests | 23/23 |
| WebAssembly Runtime Tests | 8/8 |
| Unit tests | 1562/1581, 19 skipped |

24 checks pass. The one red check is `Build and Deploy Job` -> `Publish to Azure Static WebApps`,
which has failed on every run of this PR (the Static Web App is at its staging-environment limit) and
is unrelated to anything here. `WebAssembly Test Run` at 64/86 is the pre-existing UI-test baseline -
identical in the pre-bump green run 229315, so not a regression from the bump.

Note what this changes about the earlier passes: the Mac Catalyst TFM removal is gone, so **this branch
carries no breaking change**, and the published floors move only where intended - `Uno.WinUI` and
`Uno.WinUI.MSAL` to 6.8.0-dev.46 (the build containing unoplatform/uno#24055) and
`Microsoft.Identity.Client` to 4.87.0. `Uno.Toolkit.WinUI` floors on stable 8.4.2 rather than a
prerelease, and `System.Text.Json` stays at 8.0.5. *(The last sentence no longer holds — see the
second review panel section: Toolkit follows the Sdk again and `System.Text.Json` is 9.0.14.)*

## Second review panel (2026-08-23)

A second `/review-panel` run over the branch. The `quality`, `architect` and `skeptic` reviewers
also flagged that several spec/lesson entries had fallen behind the code; those are reconciled in
the same pass (`specs/lessons.md`, this file, `spec.md`, `specs/009`, `specs/010`). Fixed:

- **`UNO_EXT_MSAL_NOSTORAGE` → `UNO_EXT_MSAL_BROWSER`** (csproj, `MsalAuthenticationProvider.cs`,
  `MsalStorageDefaults.cs`) — the define had named the opposite of what it does since item 2b.
  `SetupStorageCore` is split into `SetupBrowserStorage()` (`#if UNO_EXT_MSAL_BROWSER`) and
  `SetupDesktopStorage(ct)` (`#else`).
- **The WebAssembly lane runs the full auth namespace.** `MsalAuthenticationProvider.Build` now
  applies the app's `Builder(...)` callback *after* `WithUnoHelpers()`, so the stub `HttpClient`
  factory is no longer replaced in the browser (the ninth-pass limitation);
  `stage-runtime-tests-wasm.yml` runs `$(RuntimeTestsFilter)` instead of
  `Given_BrowserTokenCacheStorage`. `__WASM__` stays defined for `Uno.Extensions.RuntimeTests.Core`.
  Expected 23/23; **the CI run is pending.**
- **Empty-token / silent-refresh fix** (the security HIGH carried from `specs/009`, finally with a
  red test): `InternalRefreshAsync` returns `null` only on `MsalUiRequiredException` (the session
  cannot be renewed); any other failure keeps the current tokens (`Tokens.GetAsync`) and logs a
  Warning. `AuthenticationService.RefreshAsync` calls `_tokens.ClearAsync` when the provider returns
  no tokens, so `ITokenCache.Cleared` and `IAuthenticationService.LoggedOut` fire and the MSAL blob
  is purged. New `src/Uno.Extensions.Authentication.Tests/` — `Given_AuthenticationService`
  (`When_RefreshReturnsNoTokens_Then_SignedOutAndLoggedOutRaised`,
  `When_RefreshReturnsEmptyTokens_Then_SignedOut`,
  `When_RefreshReturnsTokens_Then_SavedAndStillAuthenticated`,
  `When_NotAuthenticated_Then_RefreshDoesNotCallProvider`,
  `When_CalledConcurrently_Then_ProvidersBuiltOnce`,
  `When_NothingResolvedYet_Then_ProvidersIsEmptyWithoutBuilding`) and `Given_ProviderFactory`
  (`When_ResolvedConcurrently_Then_ConfiguredOnceAndSameInstance`) — 7/7 green, 2 verified red
  without the fix. Plus `Given_MsalAuthentication.When_RefreshTokenRejected_Then_SignedOutAndLoggedOutRaised`
  (`StubEntra.RejectRefreshTokens` → 400 `invalid_grant`) and
  `When_TokenEndpointUnavailable_Then_StillAuthenticated` (`StubEntra.FailTokenRequests` → 503),
  both red before / green after on the desktop head; MSAL desktop suite 25/25.
- **StubEntra refresh tokens are unique per instance.** MSAL.NET throttles silent requests
  process-wide for 120 s after an `invalid_grant`, keyed on clientId + authority + scopes + SHA-256
  of the refresh token; identical `stub-refresh-token-{counter}` values across instances let one
  test's rejected RT throttle the next test's silent call. Tokens now carry a per-instance GUID.
  Lesson recorded in `specs/lessons.md`.
- **Interactive timeout is desktop-only by default.** `DefaultInteractiveTimeout` (5 min) applies
  only when `CurrentRedirectPlatform == MsalRedirectPlatform.Desktop`; an explicit
  `Msal:InteractiveTimeout` applies everywhere. The dispatcher call passes the cancellation token
  through (`dispatcher.ExecuteAsync(..., cancellationToken)`).
- **`ISettings` is registered by Storage.UI itself** (`AddKeyedStorage`,
  `TryAddSingleton<ISettings, Settings>()`), so `UseStorage` works on any host; new ProjectReference
  `Storage.WinUI` → `Core.WinUI` plus an `InternalsVisibleTo("Uno.Extensions.Storage.UI")` in
  Core.WinUI. The interim registration in `UnoHost.CreateDefaultBuilder` is removed;
  `UseThemeSwitching` keeps its own `TryAdd`.
- **Storage on Skia-mobile heads is confirmed, not hypothesized.** `Uno.Extensions.Storage.UI.dll`
  for `net9.0-android`/`net9.0-ios` references `Uno.UI` and the package ships `lib/net9.0`, so
  `RuntimeAssetsSelectorTask` swaps it on Skia Android/iOS; the default `IKeyValueStorage` there is
  `ApplicationDataKeyValueStorage` (plaintext, app-sandboxed). KeyStore/KeyChain cannot exist in
  the `net9.0` TFM, so this is documented (`doc/Learn/Storage/StorageOverview.md`,
  `doc/Learn/Authentication/HowTo-MsalAuthentication.md`), not fixed in code.
- **`EncryptedApplicationDataKeyValueStorage.GetObjectValue` always returns the DPAPI `byte[]`**;
  the base `ApplicationDataKeyValueStorage.SetSetting` base64-encodes `byte[]` on the string-only
  `ISettings` path. `UseApplicationData` is private again (the sixth pass had made it protected).
- **CI toolchain instead of pins.** `build/ci/templates/dotnet-install*.yml` default
  `DotNetVersion: '10.0.x'` and `UnoCheck_Version: '1.34.1'` (`dotnet-install.yml` also installs the
  .NET 9 runtime for the net9.0 test heads); `.azure-pipelines.yml` selects Xcode 26.2 / iOS 26.2
  simulator. The eleventh pass's `UnoToolkitVersion` 8.4.2 and `UnoHotDesignVersion` 1.19.175 pins
  (src + samples) and `UnoThemesVersion` 6.1.1 (samples) are **removed** — they worked around CI
  building with the .NET 9.0.200 SDK (Microsoft.iOS 18.2) against an Sdk Toolkit built for
  Microsoft.iOS 26. `System.Text.Json` 8.0.5 → 9.0.14 (src + TestHarness). One property remains, in
  the root `Directory.Build.props`: `UnoToolkitVersion` 9.2.0-dev.18 — a floor sync, not a
  workaround: the heads build with `Uno.Sdk.Private` (Toolkit group 9.1.0-dev.2 even at
  6.8.0-dev.85) while the libraries' public `Uno.Sdk` 6.8.0-dev.21 carries 9.2.0-dev.18, and
  without it heads referencing `Navigation.Toolkit` fail NU1605.
  `Uno.Extensions.Navigation.Toolkit.WinUI.csproj` keeps `net9.0-maccatalyst` via
  `_UnoExtensionsDropIosXamlOnCatalyst`, which removes the `ios` XAML prefix for the catalyst TFM
  (Toolkit ships no catalyst assembly since 8.5; `NativeFramePresenter` is only in the `<ios:>`
  template). Verified locally: Navigation.Toolkit builds for `net9.0-maccatalyst` + `net9.0-ios`
  with Toolkit 9.2.0-dev.18; all four heads restore with zero NU1xxx. **iOS/Android CI lanes not yet
  re-run.**
- **Docs:** `doc/Learn/UpdatingExtensions.md` "Upgrading to Extensions 7.4" — `Uno.WinUI` floor
  `6.8.0-dev.46` (no stable 6.8.0 on nuget.org yet); Mac Catalyst `AddMsal` throws (+ guard
  sample); WASM redirect precedence change; WASM token cache persisted by default (key
  `MsalCache_{ClientId}`, opt-out `MemoryStorage`, residue note); `Builder(...)` runs last;
  interactive timeout desktop-only default; logout removes all accounts; unrenewable refresh →
  `LoggedOut`; MSAL exceptions rethrown untouched; removed vendored
  `Microsoft.Identity.Client.Extensions.Msal.Wasm.Storage`; `IsEncrypted` truthfulness; `ISettings`
  registered by `UseStorage`.
- **TestHarness:** the hard-coded `"uno-extensions://auth"` `RedirectUri` is removed from
  `testing/TestHarness/TestHarness/Ext/Authentication/Msal/appsettings.msal.json` and
  `appsettings.multi.json`, and the matching `.WithRedirectUri("uno-extensions://auth")` calls from
  `MsalAuthenticationHostInit.cs` and `MsalAuthenticationMultiHostInit.cs` (commit a26d9a002).
- **Test placement:** `Given_BrowserTokenCacheStorage` stays in
  `Uno.Extensions.Authentication.MSAL.UI.Tests` — no `Uno.Extensions.Storage.UI.Tests` project
  exists. Recorded exception to the namespace-parity rule, not an oversight.

### Still open after this pass

- Spec 010 item 8 — live testbed validation on a device (macOS loop).
- iOS / Android / WebAssembly CI lanes have not been re-run since the toolchain change and the
  wasm filter widening; the numbers above are local.
- Low-severity panel findings, not addressed (the logout-exception path - `LogoutAsync` not clearing
  `ITokenCache` when the provider throws - was fixed afterwards by "fix(auth): clear the token cache when a provider's logout throws"): `IsEncrypted` warning noise on the InMemory store;
  `Lazy` caches a throwing `Build`; `_setupStorageTask` check-then-set race;
  `LogWarning(ex, ex.Message)` template misuse; CI script hardening items (iOS liveness check,
  unpinned `playwright install`).
