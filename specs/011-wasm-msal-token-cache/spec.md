# 011 — WebAssembly: persist the MSAL token cache, governed by an MSAL.js-shaped policy

**Status: implemented 2026-08-20 — all items (1-7), then hardened after a seven-agent review panel.**

> [!IMPORTANT]
> **The default is `LocalStorage`, not `SessionStorage`** — see "Default: `SessionStorage`, not off"
> below, which is superseded. That section weighed the security delta of persisting at all; it did
> not account for this setting selecting the *host-wide* default `IKeyValueStorage`, so defaulting
> to `sessionStorage` silently relocated every existing WebAssembly app data on upgrade.
> `progress.md`'s fifth-pass section has the full reasoning.

`progress.md` carries the verification trail (red/green runs, browser evidence, baseline-diffed
suites) and the full history; the deviations from this spec's sketches are summarized just below.
Originally written after tracing why
`doc/Learn/Authentication/HowTo-MsalAuthentication.md:20` says WebAssembly gets
"In-memory only (tokens don't survive a page reload)", and evaluating
`Microsoft.Authentication.WebAssembly.Msal` as an alternative. Read
`specs/012-msal-auth-fixes/progress.md` and `specs/010-msal-skia-mobile-runtime-dispatch/spec.md`
first — this spec touches the same provider and reuses their test infrastructure.

## Implementation deviations (recorded 2026-08-20, after all items landed)

- **The setting is owned by storage, not MSAL.** Configured as
  `KeyValueStorageConfiguration:BrowserCacheLocation`; the enum lives in `Uno.Extensions.Storage`
  (still `internal`). The `MsalConfiguration.BrowserCacheLocation` property sketched below was
  implemented first and then removed: nothing ever read it (the provider writes through whatever
  the default store is), a `Msal:`-keyed setting made apps with renamed providers configure two
  sections, and an MSAL-named section would have governed the token-cache location of non-MSAL
  providers. The member names still mirror msal-browser's, so the migration-path rationale holds.
- **Item 2's adapter is not a type.** The `SetBeforeAccessAsync`/`SetAfterAccessAsync` registration
  lives inline in `SetupStorageCore`'s browser branch (`UNO_EXT_MSAL_BROWSER`, renamed from
  `UNO_EXT_MSAL_NOSTORAGE` on 2026-08-23; now `SetupBrowserStorage()`) over `MsalTokenCacheStore`;
  a `WasmMsalTokenCacheAdapter` class had nothing else to hold. The `MemoryStorage` early-return is
  unnecessary: the provider never reads the setting — `MemoryStorage` simply makes
  `InMemoryKeyValueStorage` the default store, giving in-memory-only with one decision point
  instead of two that can disagree.
- **Item 4 is gated at runtime, not compile time.** `OperatingSystem.IsBrowser()` inside the
  `SetDefaultInstance` `#else` branch — Skia desktop shares that branch and must keep
  `ApplicationData` (this spec's Scope guard). The configured value is *validated* on every
  platform, however, so a typo fails the desktop run a developer actually does; only the store
  *selection* is browser-only. One strict reader owns both (`ResolveBrowserCacheLocation`), rejecting
  anything `Enum.IsDefined` does not accept — including numerics, which a lenient `TryParse` let
  through to a silent default.
- **Item 5 also removes every account.** `RemoveAsync(firstAccount)` left other accounts' refresh
  tokens live; logout now loops all accounts, then deletes the blob explicitly and again from
  `ITokenCache.Cleared`.
- **The enum is `internal`** — rationale under "Public surface" below.

## Symptom

On a WebAssembly head, a page reload signs the user out. The doc row is accurate, but the reason
given for it ("MSAL can't persist in a browser") is not true, and the current behavior is worse
than "nothing is persisted".

## Root cause

`MsalAuthenticationProvider` only ever obtains persistence one way — `MsalCacheHelper.RegisterCache`
(`MsalAuthenticationProvider.cs:326` and `:354`). `MsalCacheHelper` comes from
`Microsoft.Identity.Client.Extensions.Msal` and knows exactly three backends: DPAPI file (Windows),
Keychain (macOS), libsecret/keyring (Linux). It throws on browser-wasm, so the whole code path is
compiled out via `UNO_EXT_MSAL_BROWSER`
(`Uno.Extensions.Authentication.MSAL.WinUI.csproj:76`) and `SetupStorageCore` returns `true` after
logging "not supported" (`:259-267`).

**This is not an MSAL.NET limitation.** MSAL.NET's cache-serialization API is platform-agnostic and
is what `MsalCacheHelper` itself is built on:

```csharp
_pca.UserTokenCache.SetBeforeAccessAsync(async args => args.TokenCache.DeserializeMsalV3(await Load()));
_pca.UserTokenCache.SetAfterAccessAsync(async args =>
{
    if (args.HasStateChanged) { await Save(args.TokenCache.SerializeMsalV3()); }
});
```

Point that at any byte store and browser persistence works.

### The current state is incoherent, not merely absent

The Uno-side `ITokenCache` (`TokenCache.cs`) writes through `IKeyValueStorage`, which on WASM
resolves to `ApplicationDataKeyValueStorage` (`Storage.UI/ServiceCollectionExtensions.cs`,
`SetDefaultInstance` `#else` branch) → `ApplicationData.Current.LocalSettings` → browser
`localStorage`, with `IsEncrypted => false`.

So today on WASM:

| Cache | Contents | Persistence |
| --- | --- | --- |
| Uno `ITokenCache` | access token (~1 h) | **localStorage, cleartext, survives tab close** |
| MSAL `UserTokenCache` | refresh + ID token, account | memory only |

After a reload `IsAuthenticated` returns `true` (the access token is still in localStorage), then
`InternalRefreshAsync` (`:221-238`) calls `GetAccountsAsync()`, finds nothing because MSAL's cache
was memory-only, returns `default`, and `AuthenticationService.RefreshAsync` saves empty tokens and
reports `false`. The user is signed out anyway — after we already paid the cost of a persistent
plaintext access token. Worst of both.

## Decision

Adopt MSAL.js's cache policy model, verbatim, and apply it to **both** caches on WASM.

Verified from the `@azure/msal-browser` v4.30.0 bundle shipped inside
`Microsoft.Authentication.WebAssembly.Msal` 9.0.19 (`staticwebassets/AuthenticationService.js`),
`DEFAULT_CACHE_OPTIONS`:

```js
a = { cacheLocation: os, cacheRetentionDays: 5, temporaryCacheLocation: os,
      storeAuthStateInCookie: !1, secureCookies: !1,
      cacheMigrationEnabled: !(!r || r.cacheLocation !== ns), claimsBasedCachingEnabled: !1 }
// os = "sessionStorage"   ns = "localStorage"   is = "memoryStorage"
```

`sessionStorage` is the default for both `cacheLocation` and `temporaryCacheLocation`;
`memoryStorage` appears only in the non-browser fallback path.

### Public surface

**Deviation from this sketch, applied during implementation: the enum is `internal`, not `public`.**
`MsalConfiguration` is itself `internal` and is configured through JSON, and its closest sibling
setting (`AllowUnprotectedTokenCacheFallback`) is documented as a JSON-only knob. Configuration
binding resolves internal enums by name, so nothing in the user-facing contract needs the type to
be public — a public enum reachable from no public API would be surface we have to keep forever for
no consumer benefit. Revisit only if a public builder overload ever takes the value.

```csharp
namespace Uno.Extensions.Authentication.MSAL;

/// <summary>Where the token cache is kept on WebAssembly. Mirrors msal-browser's
/// <c>BrowserCacheLocation</c>; ignored on every other platform.</summary>
internal enum BrowserCacheLocation
{
    /// <summary>Survives a reload, cleared when the tab closes. The default, matching MSAL.js.</summary>
    SessionStorage,
    /// <summary>Survives a tab close. Wider exposure window — opt in deliberately.</summary>
    LocalStorage,
    /// <summary>Never written to browser storage. The pre-011 behavior.</summary>
    MemoryStorage,
}
```

added to `MsalConfiguration` as
`public BrowserCacheLocation BrowserCacheLocation { get; init; } = BrowserCacheLocation.SessionStorage;`
(the property is `public` on an `internal` type, matching the rest of `MsalConfiguration`).

The enum name and member names deliberately match msal-browser's own. That is not cosmetic: when
the WASM provider is later reimplemented over msal-browser directly (see "Migration path"), this
value becomes a straight pass-through to `cacheLocation`, and the configuration surface, docs, and
user mental model all survive the change unchanged.

### Default: `SessionStorage`, not "off"

Considered and rejected: defaulting to `MemoryStorage` so the feature ships opt-in. That preserves
today's posture, but it ships the fix switched off, leaves the docs row unchanged, and buys a
security delta that is small next to the fact that a 24-hour SPA refresh token is in the browser at
all. It would also give Uno WASM apps worse behavior than every other MSAL SPA for no articulable
gain. Microsoft chose `sessionStorage` for exactly this scenario; match it.

### Both caches, or the setting is cosmetic

If MSAL's cache moves to `sessionStorage` while `ITokenCache` stays on `ApplicationData` →
`localStorage`, the access token outlives both the tab and the refresh token, and the weakest link
sets the posture. `BrowserCacheLocation` must govern the Uno `ITokenCache` on WASM too. This is the
part of the work with the most actual value and the most new code — see item 3.

## Scope guard: browser-only and strictly additive

**Do not implement this as "replace `MsalCacheHelper` with `IKeyValueStorage`."** On Skia desktop
(`net9.0-desktop`) none of `__ANDROID__` / `__IOS__` / `WINDOWS` are defined, so `SetDefaultInstance`
falls to the `#else` branch → `ApplicationDataKeyValueStorage` → plaintext file. A blanket swap
would convert DPAPI / Keychain / libsecret into cleartext on the three platforms that currently do
this correctly. Every change here sits behind the existing browser-wasm condition; the
`MsalCacheHelper` path on Windows/macOS/Linux and the native path on Android/iOS are untouched.

Open item for implementation: the provider already prefers a runtime `PlatformHelper.IsWebAssembly`
check over a compile-time symbol in `CurrentRedirectPlatform` (`:129-133`), with the comment that
"WebAssembly shares the browserwasm TFM with the generic Skia stack". Confirm whether the
browserwasm TFM can be loaded by a non-browser host before relying on `UNO_EXT_MSAL_BROWSER`
alone; if so, gate the storage selection at runtime the same way.

## Security position, stated plainly

`SessionStorage` and `LocalStorage` both mean **cleartext, readable by any script on the origin.**
The MSAL v3 blob contains the refresh token, not just an access token.

What bounds the risk is lifetime, not encryption. Per Microsoft's documentation
(`learn.microsoft.com/entra/identity-platform/refresh-tokens` and
`.../reference-third-party-cookies-spas`):

- Refresh tokens issued to a redirect URI registered as **`spa`**: **24 hours, non-sliding,
  non-adjustable.**
- All other scenarios (mobile/desktop public client): **90 days**, sliding.
- Microsoft's framing: *"Cross-site scripting (XSS) attacks or compromised JS packages can steal the
  refresh token and use it remotely until it expires or is revoked. […] In order to minimize the risk
  of stolen refresh tokens, SPAs are issued tokens valid for 24 hours only."*

Our docs already point WASM users at a SPA registration
(`HowTo-MsalAuthentication.md:191`), and MSAL.NET's token request from the browser goes over
`HttpClient` → `fetch`, so CORS effectively forces `spa` anyway. **The 24-hour non-sliding refresh
token is what makes this shippable.** A 90-day sliding token in cleartext browser storage would not
be. Item 7 makes that prerequisite explicit rather than incidental.

### Deliberately not doing: encrypting the blob ourselves

An earlier sketch proposed wrapping the blob with a non-extractable AES-GCM `CryptoKey` in
IndexedDB. Dropped, after checking what msal-browser actually does:

```js
crypto.subtle.generateKey({name:"AES-GCM",length:256}, !0, [...])   // extractable: true
  → exportKey(...) → importKey(..., !1, ["deriveKey"])              // HKDF, non-extractable
```

The base key is generated **extractable** and exported. MSAL.js's cache encryption is
obfuscation-at-rest, not an XSS defense, and Microsoft does not claim otherwise. In a browser there
is no secret the page can use that an XSS cannot. Building something more elaborate than what MSAL
does would imply a guarantee we cannot back. Match their posture; spend the effort on scope and
lifetime.

## Implementation items

1. **`BrowserCacheLocation` enum + `MsalConfiguration.BrowserCacheLocation`.** Public, XML-documented,
   defaulting to `SessionStorage`. Note `MsalConfiguration` is currently `internal`; the enum is
   public because it appears in the configuration JSON contract and in docs.

2. **`WasmMsalTokenCacheAdapter`** in `Uno.Extensions.Authentication.MSAL`, compiled only for
   browser-wasm. Registers `SetBeforeAccessAsync` / `SetAfterAccessAsync` on
   `_pca.UserTokenCache` in `SetupStorageCore`'s `UNO_EXT_MSAL_BROWSER` branch, backed by
   `IKeyValueStorage`. `MemoryStorage` keeps today's early-return.

   **Key-collision hazard — must not be missed.** `TokenCache` uses the prefix `AuthToken_` and
   `GetAsync` returns *every* key matching `TokenPrefixPredicate`, mapped into the token dictionary
   that is then handed to the HTTP handlers as if each entry were a token
   (`TokenCache.cs:63-77`). The MSAL blob key **must not** start with `AuthToken_`. Use a distinct
   prefix (e.g. `MsalCache_{ClientId}`) and add a test asserting `TokenCache.GetAsync` never
   surfaces it.

3. **`SessionStorageKeyValueStorage`** in `Uno.Extensions.Storage.UI`, browser-wasm only. There is
   no WinRT surface for `sessionStorage` (`ApplicationData.Current.LocalSettings` is `localStorage`),
   so this is new code over `[JSImport]` from `System.Runtime.InteropServices.JavaScript`
   (`globalThis.sessionStorage` `getItem`/`setItem`/`removeItem`/`key`/`length`), deriving from
   `BaseKeyValueStorageWithCaching` like its siblings. (`Uno.Extensions.Storage.UI` imports
   `tfms-ui-winui.props`, which adds `net9.0-browserwasm` under `Build_Web` — verified, so the TFM
   is available; a local `DebugPlatforms.props` that disables `Build_Web` will hide it.)

4. **Honor the setting for `ITokenCache` on WASM.** `SetDefaultInstance<IKeyValueStorage>` currently
   hardcodes `ApplicationDataKeyValueStorage.Name` for the `#else` branch. On browser-wasm the
   selection must follow `BrowserCacheLocation`: `SessionStorage` → the new provider,
   `LocalStorage` → `ApplicationDataKeyValueStorage`, `MemoryStorage` → `InMemoryKeyValueStorage`.
   `SetDefaultInstance<TService>(string)` resolves a *name*, so this can stay a registration-time
   decision if the value is read from `IConfiguration` during `AddKeyedStorage`; check whether the
   MSAL configuration section is bound early enough, and if not, introduce a storage-level setting
   that `AddMsal` writes.

5. **Logout must clear the blob.** `InternalLogoutAsync` calls `_pca.RemoveAsync(firstAccount)`,
   which mutates the cache and so triggers `SetAfterAccessAsync` with `HasStateChanged` — but only
   for the *first* account (`:204-215`), and not at all if `_pca` was never built. Delete the blob
   explicitly on logout, and subscribe to `ITokenCache.Cleared` as a belt-and-braces. Test both.

6. **`IsEncrypted` reports the wrong value** — carve out as a **separate PR**, not part of this one.
   `EncryptedApplicationDataKeyValueStorage` DPAPI-protects via `DataProtectionProvider` (`:44-53`)
   but returns `IsEncrypted => false` (`:24`); `PasswordVaultKeyValueStorage` returns `false` too
   (`:21`). Nothing in the repo reads the property today (grep is empty), so it is latent — but it
   is on the public `IKeyValueStorage` interface and is exactly the flag a consumer would branch on
   to decide whether a store is safe for tokens.

7. **Docs** — `doc/Learn/Authentication/HowTo-MsalAuthentication.md`:
   - Replace the WebAssembly row in the platform-support table (`:20`).
   - Extend the token-cache section (`:347-360`), which currently states WASM is memory-only and
     that no configuration is honored there.
   - State the `spa` registration requirement as a **prerequisite**, not the aside it currently is
     at `:191`, including the 24-hour non-sliding refresh-token consequence and the top-level-frame
     re-auth it implies.

## Tests

Per `AGENTS.md`, an auth change without tests is a finding, and this is red/fix/green work.

| Case | Project |
| --- | --- |
| `SerializeMsalV3` round-trips through a fake `IKeyValueStorage` | `Authentication.MSAL.Tests` |
| `MemoryStorage` writes nothing to the store | `Authentication.MSAL.Tests` |
| MSAL blob key is not surfaced by `TokenCache.GetAsync` (item 2 hazard) | `Authentication.Tests` |
| Logout removes the blob, incl. the zero-account and never-built-`_pca` paths | `Authentication.MSAL.Tests` |
| Non-WASM platforms still take the `MsalCacheHelper` path — regression guard for the scope guard | `Authentication.MSAL.Tests` |
| `SessionStorageKeyValueStorage` get/set/remove/enumerate | `Uno.Extensions.RuntimeTests` (needs a browser) |
| Reload survives sign-in; tab close does not | manual, WASM testbed |

`MsalStorageDefaults.cs` is compiled as linked source into `Authentication.MSAL.Tests` because the
WinUI assembly can't load in a plain test host (see its file header). Any new type that tests need
must observe the same constraint — keep it free of Uno dependencies, or test it via a seam.

## Migration path — this is a stopgap

The strategically correct browser implementation is a `WasmMsalAuthenticationProvider` over
`@azure/msal-browser` via `[JSImport]`, which gets the 24-hour SPA semantics, Microsoft-maintained
cache hardening, and top-level-frame re-auth handling as a matter of course, instead of us
hand-rolling browser token storage in a library whose refresh-token lifetime we don't control.

Referencing `Microsoft.Authentication.WebAssembly.Msal` itself is **not** viable: it depends on
`Microsoft.AspNetCore.Components.WebAssembly.Authentication` →
`Microsoft.AspNetCore.Components.Authorization` + `.Web`, and its `AddMsalAuthentication` registers a
`RemoteAuthenticationService<>` needing `IJSRuntime` and `NavigationManager` from DI, with redirect
completion normally driven by the `RemoteAuthenticatorView` Blazor component routed at
`/authentication/{action}`. Its entire .NET public surface is three options records plus one
extension method; the substance is the bundled JS.

That JS is not Blazor-coupled — the bundle ends with `window.AuthenticationService = ah`, exposing
`init(settings, loggerOptions)`, `getUser()`, `getAccessToken(request)`, `signIn(state)`,
`completeSignIn(url)`, `signOut(state)`, `completeSignOut(url)`. Blazor's .NET side is just
`IJSRuntime.InvokeAsync("AuthenticationService.init", …)`. Uno can call the same entry points via
`[JSImport]` with no Blazor assemblies, and `Uno.Wasm.Bootstrap.targets` already processes
`StaticWebAsset` items. But if we are calling msal-browser anyway, depend on it directly rather than
on Microsoft's wrapper, whose `AuthenticationService` bakes in Blazor conventions (sessionStorage
state keyed `Microsoft.Authentication.WebAssembly.Msal.AuthorizeService.*`,
`RemoteAuthenticationStatus` result shapes, its own redirect state machine).

Costs to weigh when that work is scheduled: the WASM head stops using MSAL.NET entirely — a second
provider implementation, a separate test surface, and behavior diverging from the other targets.
The `BrowserCacheLocation` surface defined here is designed to carry over unchanged.
