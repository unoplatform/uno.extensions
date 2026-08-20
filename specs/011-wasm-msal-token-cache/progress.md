# 011 — Progress

Tracking the implementation of `spec.md`. Item numbers match the spec's "Implementation items".

**State as of 2026-08-20 (fourth pass): every implementation item (1–7) is done, plus two logout
fixes and a configuration relocation found by live testing.** `spec.md`'s header now carries the
deviation summary. This file is the chronological record — read newest-first: fourth pass
(config move to `KeyValueStorageConfiguration`), third pass (items 5+6, logout fixes), second pass
(items 3+4+2b, "the trio"), first pass (items 1+2, the storage seam, committed as `5cd3bb484`).

---

## Next agent: start here

The code is complete. What remains is confirmation and one coverage gap:

1. **Manual sign-in pass on the WASM head** (`Uno.Samples` → `Authentication.MsalExtensionsDemo`,
   branch `dev/sb/msa-ext`): sign in → `MsalCache_{ClientId}` appears in `sessionStorage`; reload →
   session restored without a prompt; logout → the entry and every `AuthToken_*` entry disappear;
   tab close → session gone. Needs the real ClientId (redacted in git) and a human click — the demo
   renders through SkiaRenderer, one `<canvas>`, and synthetic pointer events don't reach it.
2. **The multi-account logout path has no automated guard** — see "What is still not covered" in
   the third-pass section.
3. The empty-token refresh finding (`specs/009-msal-auth-fixes/progress.md`) is still open and
   still needs a deterministic repro before it is fixed.

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
- [x] **Item 2b — the MSAL half.** `SetupStorageCore`'s `UNO_EXT_MSAL_NOSTORAGE` branch now
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

And the lifecycle behaviour the feature exists for, same tab vs. new browsing session:

1. first load → `value already stored: <none>`, key written
2. **reload → `value already stored: written by ProbeTokenCacheStorage`** — survives a page reload
3. new browser context → `<none>` again — does not outlive the session

So the four `[JSImport]` bindings (`getItem` / `setItem` / `removeItem` / `key`) all work, the
`key(index)`-until-null enumeration works, the `_SSKVS` suffix is applied and stripped, and item 4's
selection is honored from a real embedded `appsettings.development.json` — a different configuration
source than the in-memory one the unit/UI tests use.

**Still unverified: the MSAL blob itself** (`MsalCache_{ClientId}`, item 2b). That needs a completed
sign-in, which needs the real ClientId (deliberately redacted from git) and a human click — the
sample renders through SkiaRenderer, one `<canvas>`, and synthetic pointer events do not reach it.

### Why CI can't do the above

`uno-runtimetests-wasm` against the published head reaches
`"Application configured to start runtime-tests (Config=Given_BrowserTokenCacheStorage)"` and then
dies in the engine before the first test:

```
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
  than a silent behaviour change. `Given_BrowserCacheLocation` — which existed only to assert the
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
  behaviour testable on the only runtime-test lane that actually runs (see below).

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

  Specced as a separate PR; kept as its own commit so it can still be split out.

### Tests added, and why they run everywhere

| Test | Guards |
| --- | --- |
| `When_Logout_Then_SerializedMsalCacheRemoved` | logout deletes the `MsalCache_{ClientId}` entry |
| `When_TokenCacheCleared_Then_SerializedMsalCacheRemoved` | the `ITokenCache.Cleared` path does too |

Both seed the entry through the host's default `IKeyValueStorage` rather than by signing in on a
browser, which is what lets them assert the security-relevant behaviour — *refresh-token material
does not outlive a sign-out* — on the Skia desktop lane. Verified red first (both reported
`found True`, i.e. the entry survived) with the provider change stashed, then green.

**What is still not covered:** the multi-account half of the root-cause fix. A red test needs two
accounts in MSAL's cache, and the provider's own login path cannot produce them — `AcquireTokenAsync`
tries silent first, which succeeds for the existing account, so a second interactive sign-in never
happens. Forcing silent to fail lands in the same non-determinism as the empty-token finding in
`specs/009-msal-auth-fixes/progress.md`. The loop is a strictly smaller behaviour than
`FirstOrDefault()` (it removes a superset), so it cannot regress the single-account case the suite
does cover.

## Remaining

Every implementation item in `spec.md` is now done. What is left is confirmation, not code:

- [ ] **Sign-in pass on the WASM head.** Storage selection and the reload/session lifecycle are
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
