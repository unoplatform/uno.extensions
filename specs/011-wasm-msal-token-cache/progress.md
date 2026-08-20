# 011 — Progress

Tracking the implementation of `spec.md`. Item numbers match the spec's "Implementation items".

**State as of 2026-08-20:** the pure storage seam is implemented and tested. The provider is **not**
wired up, so behavior on WebAssembly is byte-for-byte unchanged from `main`. Read
"Next agent: start here" before writing any code.

---

## Next agent: start here

**Do items 3 + 4 + the MSAL half of item 2 as one change.** They are separable in the spec but not
in reality: that trio is the smallest increment after which `SessionStorage` actually means
`sessionStorage` end to end.

Why they can't land separately — `SetDefaultInstance<IKeyValueStorage>` currently hardcodes
`ApplicationDataKeyValueStorage` for WASM, which is `localStorage`. So:

- Wiring the provider (2b) without items 3+4 makes the **default** setting, `SessionStorage`,
  silently persist to `localStorage`. A setting whose default name contradicts its behavior is
  worse than no setting at all, and worse than today's honest "in-memory only".
- Item 3 without item 4 builds a backend nothing selects.
- Item 4 without item 3 has nothing to select.

That sequencing is the reason the provider was left unwired rather than "finished". Please don't
land a partial slice of it.

### The four things I'd have hit next, already dug out

1. **DI: `IKeyValueStorage` is not resolvable as a plain service.** The provider is constructed by
   `services.TryAddTransient<TAuthenticationProvider>()`
   (`src/Uno.Extensions.Authentication/HostBuilderExtensions.cs:95`), so *every* constructor
   parameter must resolve from DI — but `IKeyValueStorage` is registered **only** as a named
   instance via `SetDefaultInstance` (`Storage.UI/ServiceCollectionExtensions.cs:68`); there is no
   plain `AddSingleton<IKeyValueStorage>`. `TokenCache` sidesteps this by being registered with an
   explicit factory that calls `sp.GetRequiredDefaultInstance<IKeyValueStorage>()`
   (`Authentication/HostBuilderExtensions.cs:143-146`).
   **Plan:** add `TryAddSingleton<IKeyValueStorage>(sp => sp.GetRequiredDefaultInstance<IKeyValueStorage>())`
   inside `InternalAddMsal`. Additive, `Try*` so an app's own registration still wins, and it adds
   no new prerequisite — `UseStorage` is already required for auth to work at all.

2. **Item 4's open question is still open.** Does the `Msal` configuration section get bound early
   enough for `AddKeyedStorage` to read `BrowserCacheLocation` at registration time? `AddMsal` calls
   `UseConfiguration(... .Section<MsalConfiguration>(name))`, but `AddKeyedStorage` runs from
   `UseStorage`, and ordering between them is app-controlled. If it doesn't hold, fall back to a
   storage-level setting that `AddMsal` writes, rather than forcing an ordering contract on apps.

3. **Item 5 is a confirmed bug, so it needs red/fix/green** (AGENTS.md), not a fold-in.
   `TokenCache.SaveAsync` clears only keys matching its own `AuthToken_` predicate
   (`TokenCache.cs:135`), so an `MsalCache_*` entry survives logout. `_pca.RemoveAsync` only
   self-heals it for the **first** account (`MsalAuthenticationProvider.cs:204-215`), and not at all
   if `_pca` was never built. Write the failing test first.

4. **Do not widen the blast radius past browser-wasm.** On Skia desktop (`net9.0-desktop`) none of
   `__ANDROID__` / `__IOS__` / `WINDOWS` are defined, so `SetDefaultInstance` falls to the same
   `#else` branch → plaintext `ApplicationData`. A change to that branch that isn't browser-gated
   turns DPAPI / Keychain / libsecret into cleartext on the three platforms that currently do this
   correctly. See the spec's "Scope guard" section.

### Facts already verified, so you don't have to re-check

- `Uno.Extensions.Storage.UI` **does** produce `net9.0-browserwasm` — `tfms-ui-winui.props:12` adds
  it under `Build_Web`. A local `DebugPlatforms.props` disabling `Build_Web` will hide it; there is
  no such file in the repo right now.
- `Uno.Extensions.Storage` and `Uno.Extensions.Core` are plain `net9.0` (`tfms-non-ui.props`) with
  no Uno.UI dependency, so they reference cleanly from a plain test host — that's how
  `MsalTokenCacheStore` is unit-tested at all.
- `IKeyValueStorage.GetAsync<T>` returns `default` for a missing key; it does **not** throw
  `KeyNotFoundException` despite what its XML doc says (`BaseKeyValueStorageWithCaching:68-79`).
- `ApplicationDataKeyValueStorage` → `ApplicationData.Current.LocalSettings`, `IsEncrypted => false`.
- MSAL's `LogLevel` collides with `Microsoft.Extensions.Logging.LogLevel` in this project; both
  `MsalAuthenticationProvider` and `MsalTokenCacheStore` alias it.

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
- [x] **Tests — 11 new cases** (`Given_MsalTokenCacheStore`): round-trip; absent entry; empty and
  null blob clear the entry rather than storing a placeholder; corrupt base64 is discarded instead
  of reaching `DeserializeMsalV3`; clear is idempotent and write-free; per-client-id key separation;
  well-formed key with no client id; last-write-wins; and the `AuthToken_` collision guard.
  Supporting `FakeKeyValueStorage` is hand-rolled per AGENTS.md's preference over mocks.
- [x] **Test project wiring.** Linked the two new sources into
  `Uno.Extensions.Authentication.MSAL.Tests` and added a `ProjectReference` to
  `Uno.Extensions.Storage`.

### Verification performed

- `dotnet test src/Uno.Extensions.Authentication.MSAL.Tests -c Release` → **38/38 passed**, 0 failed
  (11 new, 27 pre-existing).
- `dotnet build src/Uno.Extensions.Authentication.MSAL/…WinUI.csproj -c Release` → **succeeded**
  across all six locally-enabled TFMs (`net9.0`, `-android`, `-browserwasm`, `-desktop`, `-ios`,
  `-maccatalyst`), with **no warning or error originating from the new files**. The CS1591 warnings
  in that output are pre-existing, in `Uno.Extensions.Authentication/HttpUtility.cs`.
- Not yet verified: anything on a real WASM head. Nothing to verify there yet — the provider is
  unwired, so runtime behavior is unchanged.

## Remaining

- [ ] **Items 3 + 4 + 2b together** — see "Next agent: start here".
  - [ ] Item 3: `SessionStorageKeyValueStorage` in `Uno.Extensions.Storage.UI`, browser-wasm only,
        over `[JSImport]` (`globalThis.sessionStorage`), deriving from
        `BaseKeyValueStorageWithCaching` like its siblings.
  - [ ] Item 4: honor `BrowserCacheLocation` in `SetDefaultInstance<IKeyValueStorage>` so
        `ITokenCache` and the MSAL cache share one policy — otherwise the access token outlives both
        the tab and the refresh token, and the weakest link sets the posture.
  - [ ] Item 2b: register `SetBeforeAccessAsync` / `SetAfterAccessAsync` in `SetupStorageCore`'s
        `UNO_EXT_MSAL_NOSTORAGE` branch, with `MemoryStorage` keeping today's early return.
- [ ] **Item 5 — clear the blob on logout.** Red/fix/green; independent of the trio above.
- [ ] **Item 6 — `IsEncrypted` reports the wrong value.** Separate PR, as specced. Independent.
- [ ] **Item 7 — docs.** Do last, once behavior is real: the WebAssembly row (`:20`), the token-cache
      section (`:347-360`), and promoting the `spa` registration requirement from an aside (`:191`)
      to a stated prerequisite with its 24-hour non-sliding refresh-token consequence.
- [ ] End-to-end on a real WASM head: reload survives sign-in; closing the tab does not; and with
      `LocalStorage`, closing the tab does.

## Not part of this work

`doc/Learn/Authentication/HowTo-MsalAuthentication.md` has an unrelated uncommitted edit in the
working tree (removal of a NOTE about uno#20601 and the Skia-mobile assembly substitution) that
predates this task. It was deliberately left out of the 011 commit.
