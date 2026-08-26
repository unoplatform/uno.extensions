# 015 — MSAL token-cache persistence check

## Problem

`MsalAuthenticationProvider.SetupStorageCore` called `MsalCacheHelper.VerifyPersistence()` on every
storage setup. The check writes, reads and deletes a probe entry in the platform's secure store, so
it costs three round-trips per app start.

On macOS it is worse than the count suggests. MSAL's validation accessor builds its probe entry as

```csharp
// MacKeychainAccessor.CreateForPersistenceValidation(), Microsoft.Identity.Client.Extensions.Msal
new MacKeychainAccessor(_cacheFilePath + ".test", _service + Guid.NewGuid(), _account, _logger)
```

— a **different keychain service name on every run**. macOS gates keychain access with a per-item
ACL, so a probe entry that never has the same name twice is a first-ever access every time. The user
is prompted, answers "Always Allow", the entry is deleted at the end of the check, and the next
launch asks again. There is no way for the grant to stick.

Compounding it during development: a Skia desktop head is ad-hoc signed
(`codesign -d -r-` reports `designated => cdhash H"…"`), so even the grant on the *real* cache entry
is bound to the hash of that exact binary and is invalidated by the next rebuild.

## Why the call could not simply be deleted

`VerifyPersistence()` was the only thing reporting a broken store, because `MsalCacheHelper` handles
the two directions differently:

- **Read** — `Storage.ReadData()` rethrows, so a failure surfaces out of `GetAccountsAsync()`.
- **Write** — `AfterAccessNotification` catches and logs `"Could not write the token cache.
  Ignoring. See previous error message."`

Dropping the check would therefore have converted a startup error into silent loss of the refresh
token: the app looks signed in, and the user discovers otherwise after a restart.

## Design

Two facts about the MSAL extensions accessors make a cheaper equivalent possible:

1. Every `ICacheAccessor.Write` ends in `FileIOWithRetries.TouchFile(_cacheFilePath)`, and `Clear`
   begins with `DeleteCacheFile`. The cache file tracks whether a write landed — on macOS too, where
   the payload itself lives in the keychain.
2. `TokenCache.OnAfterAccessAsync` invokes the synchronous `TokenCacheCallback` and *then* the
   asynchronous one. `RegisterCache` uses only the synchronous slots, so the provider can attach
   `SetAfterAccessAsync` without displacing it.

Hence:

- **`MsalConfiguration.VerifyCachePersistence`** (`Auto` default, `Always`, `Never`). Note that
  signing out does not re-arm `Auto`: `AfterAccessNotification` only calls `Storage.Clear()` when
  serialization *throws*, so an emptied cache is rewritten and the file stays put.
  `MsalStorageDefaults.ShouldVerifyPersistence(mode, cacheAlreadyPersisted)` holds the decision as a
  pure function; the provider supplies `cacheAlreadyPersisted` from `File.Exists(cacheFilePath)`.
- **`ArmWriteVerification` / `OnAfterCacheAccessAsync`** re-add the loud failure at no secure-store
  cost: after the first state-changing write, the provider checks that the cache file exists. If it
  does not, the write was rejected and swallowed — log an error and null `_setupStorageTask` so the
  next `SetupStorage` rebuilds. That rebuild sees no cache file, so the probe runs even under `Auto`
  and either recovers or takes the `AllowUnprotectedTokenCacheFallback` path.
- The unprotected-file fallback still verifies unconditionally: it only runs because the protected
  store already failed, and an ordinary file carries none of the cost that justifies skipping.

Steady-state macOS launches now make **zero** extra secure-store round-trips.

## Risks

- `File.Exists` as the persistence signal depends on `TouchFile`-on-write, which is an MSAL
  implementation detail rather than a contract. If a future version drops it, `Auto` degrades to
  "never probe" and the write watchdog fires on every write — noisy, but not silent.
- Two heads sharing a cache can race: one signs out (deleting the file) between the other's write
  and its check, producing one spurious error and one redundant setup.
- Under `Never`, a genuinely broken store surfaces as an exception out of `GetAccountsAsync()` in
  `InternalRefreshAsync`, which the provider does not currently wrap. Wrapping that read is a
  natural follow-up.

## Tests

`Given_MsalStorageDefaults` covers the decision table (`Auto` both ways, `Always`, `Never`) and pins
`Auto` as both the configuration default and `default(MsalCachePersistenceCheck)`.
