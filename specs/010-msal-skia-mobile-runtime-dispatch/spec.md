# 010 — MSAL: Skia-mobile heads load the stub lib; move platform selection to runtime dispatch

**Status: implemented 2026-08-19 (items 1–7, 9, 10); item 8 (live testbed validation) still
pending — it needs the macOS iteration loop.** One deviation from the plan sketch: the runtime iOS
check is `OperatingSystem.IsIOS() && !OperatingSystem.IsMacCatalyst()`, because `IsIOS()` is
documented to also return true on Mac Catalyst — the bare `IsIOS()` in the item-3 sketch would
have routed Skia Catalyst heads to the iOS msauth scheme, contradicting this spec's own caveat
that they take the Desktop path. Written 2026-08-19 after live
debugging on the iOS simulator against the `Uno.Samples` testbed (branch `dev/sb/msa-ext`,
`UI/Authentication.MsalExtensionsDemo` — see its `HANDOFF-MACOS.md` for the pack/purge/rebuild
iteration loop). Read `specs/012-msal-auth-fixes/progress.md` first: this spec fixes a regression
that 009's "Define gates unified" review item introduced, and reuses 009's test infrastructure.

## Symptom

On the demo app's `net10.0-ios` head (simulator, Skia renderer), **every** auth call fails —
startup `RefreshAsync`, `LoginAsync`, `LogoutAsync` — with:

```text
Uno.Extensions.Authentication.AuthenticationService: Error: AuthenticationProvider - No providers specified for the application
```

`AddMsal` registered nothing. Android Skia heads have the same failure (same mechanism, untested
live). Desktop, WASM, and WinAppSDK heads are unaffected.

## Root cause (verified end-to-end)

1. **Uno.Sdk substitutes the plain-TFM lib on Skia mobile heads.** The `ReplaceUnoRuntime` target
   (shipped in `uno.winui`'s `buildTransitive/uno.winui.runtime-replace.targets`) runs
   `RuntimeAssetsSelectorTask.HandleSkiaMobileForNonRuntimeEnabledPackages` (uno repo,
   `src/SourceGenerators/Uno.UI.Tasks/RuntimeAssetsSelector/RuntimeAssetsSelectorTask.cs`). On a
   Skia iOS/Android head it takes every NuGet assembly that (a) resolved from a
   `lib/netX.0-{ios,android,maccatalyst,tvos}*` folder, (b) **references `Uno.UI`**, and (c) has a
   plain `lib/netX.0` sibling, and replaces it with the plain build — because on Skia heads
   `Uno.UI` itself is the plain/skia flavor. There is **no opt-out**; `Uno.UI.MSAL` survives only
   via a hardcoded exemption (`IsWinRTAssembly`, uno#20601). Binlog proof on the demo build:
   `Replacing uno.extensions.authentication.msal.winui .../lib/net9.0/Uno.Extensions.Authentication.MSAL.WinUI.dll`.
   NuGet restore is *not* the problem — `project.assets.json` correctly selects
   `lib/net9.0-ios18.0`; the swap happens later, inside the build.

2. **On this branch the plain lib is a deliberate no-op stub.** The `UNO_EXT_MSAL` allow-list
   (csproj and `build/Package.targets`) keys on `GetTargetPlatformIdentifier($(TargetFramework))`,
   which is empty for plain `netX.0` → symbol undefined → `HostBuilderExtensions.InternalAddMsal`
   compiles to `return builder;` (2 bytes of IL: `ldarg.0; ret`). The functional build is ~113
   bytes. So the app bundles an `AddMsal` that silently registers nothing.

3. **This is a regression relative to `main` / released packages.** On main, `UNO_EXT_MSAL` is
   defined for everything except `_IsNetStd`/`_IsMacOS`/`_IsCatalyst`, so the released plain lib
   is functional and Skia mobile heads got a working (if desktop-flavored) provider. 009's
   allow-list hardening ("fail-safe for unknown future platforms") didn't account for the Uno.Sdk
   substitution making the plain lib the *primary* runtime artifact on Skia mobile. Related note:
   009 recorded "contract's 'desktop heads resolve the stub lib' was refuted" — correct for
   desktop; the actual stub consumer nobody checked was Skia mobile.

### Fast re-verification probes

- **Which build is in the app bundle:** dump the IL body size of
  `HostBuilderExtensions.InternalAddMsal` in the `.app`'s
  `Uno.Extensions.Authentication.MSAL.WinUI.dll` — 2 bytes = stub, ~113 = functional. Small
  `System.Reflection.Metadata` file-based app (`dotnet run probe.cs -- <dll>`):
  `PEReader` → `GetMetadataReader()` → find TypeDef `HostBuilderExtensions` → method
  `InternalAddMsal` → `pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes().Length`.
- **Whether the swap fired:** build the head with `/bl` and search the binlog for
  `Replacing uno.extensions.authentication.msal.winui`.
- **Important:** after the fix the swap **still fires** — that's by design and out of our control.
  Success is the swapped-in plain lib being functional (113-byte probe + provider registered at
  runtime), *not* the swap disappearing.

## Why runtime dispatch is safe (each binding verified on the demo's iOS head)

The swap only affects assemblies referencing `Uno.UI`. Everything the provider needs at runtime is
still platform-real:

- **`Microsoft.Identity.Client`** resolves `lib/net8.0-ios18.0` and is *not* swapped (no `Uno.UI`
  reference) → native iOS token persistence and interactive UI are present. The plain extensions
  assembly compiles against MSAL's neutral surface and binds against the loaded iOS build at
  runtime (same identity, superset surface).
- **`Uno.UI.MSAL`** is exempted ("follows the WinRT layer") → the *native* `lib/net10.0-ios26.0`
  build is loaded at runtime, and `Uno.WinUI.MSAL` ships plain `net9.0`/`net10.0` libs to compile
  against → `WithUnoHelpers()` performs the real iOS parent-window wiring even when called from
  the plain extensions assembly.
- **The WinRT layer (`Uno.dll`)** also follows the WinRT layer → native at runtime →
  `Windows.ApplicationModel.Package.Current.Id.FamilyName` returns `CFBundleIdentifier`
  (uno repo, `src/Uno.UWP/ApplicationModel/PackageId.Apple.cs`). This replaces the
  ios-TFM-only `Foundation.NSBundle` call.
- **Storage is already runtime-dispatched** (009 plan item 1): `SetupStorageCore` early-returns
  "MSAL persists natively" via `OperatingSystem.IsAndroid()/IsIOS()/IsMacCatalyst()` — correct on
  Skia iOS precisely because the loaded MSAL *is* the iOS build. `OperatingSystem.Is*()` reports
  the OS, not the TFM, so it is true regardless of which lib variant was loaded.

## Plan

All changes in `src/Uno.Extensions.Authentication.MSAL/` unless noted.

- [x] 1. **csproj allow-list: add the plain TFM** (empty platform identifier; `net9.0` is the only
  TFM in `tfms-ui-winui.props` with one — there is no netstandard). Only `maccatalyst` remains
  a stub. Update the comment to record *why* plain must be functional:

  ```xml
  <!-- The plain netX.0 lib must be functional: on Skia iOS/Android heads, Uno.Sdk's
       ReplaceUnoRuntime substitutes it for the platform lib for any package referencing
       Uno.UI (RuntimeAssetsSelectorTask.HandleSkiaMobileForNonRuntimeEnabledPackages),
       so it is the build that actually runs there. Platform behavior inside it is
       selected at runtime via OperatingSystem.Is*(). Keep this allow-list in sync with
       build/Package.targets. -->
  <PropertyGroup Condition="'$(_UnoExtMsalTargetPlatform)'=='' or '$(_UnoExtMsalTargetPlatform)'=='android' or '$(_UnoExtMsalTargetPlatform)'=='ios' or '$(_UnoExtMsalTargetPlatform)'=='windows' or '$(_UnoExtMsalTargetPlatform)'=='desktop' or '$(_UnoExtMsalTargetPlatform)'=='browserwasm'">
      <DefineConstants>$(DefineConstants);UNO_EXT_MSAL</DefineConstants>
  </PropertyGroup>
  ```

- [x] 2. **Mirror the `''` addition in `build/Package.targets`** (consuming-project allow-list),
  same comment. Keeps plain-TFM shared class libraries that call `AddMsal` consistent.

- [x] 3. **`MsalAuthenticationProvider.CurrentRedirectPlatform`: compile-time chain → runtime
  checks.** Only `WINDOWS` stays compile-time — deliberately, because
  `OperatingSystem.IsWindows()` is also true on Skia-desktop-on-Windows, where
  Desktop/localhost (not the WAM broker) is correct:

  ```csharp
  private static MsalRedirectPlatform CurrentRedirectPlatform
  {
      get
      {
          if (PlatformHelper.IsWebAssembly)
          {
              return MsalRedirectPlatform.WebAssembly;
          }

  #if WINDOWS
          // WinAppSDK head: the WAM broker owns the redirect URI. Compile-time on purpose —
          // IsWindows() is also true on Skia desktop, where Desktop/localhost is correct.
          return MsalRedirectPlatform.BrokerManaged;
  #else
          // Runtime, not compile-time: on Skia iOS/Android heads Uno.Sdk substitutes the
          // plain netX.0 build of this assembly, so the TFM no longer implies the OS.
          if (OperatingSystem.IsAndroid())
          {
              return MsalRedirectPlatform.Android;
          }
          if (OperatingSystem.IsIOS())
          {
              return MsalRedirectPlatform.IOS;
          }
          return MsalRedirectPlatform.Desktop;
  #endif
      }
  }
  ```

- [x] 4. **`ApplyPlatformRedirectUri`: drop the `#if IOS` / `NSBundle` branch** in favor of the
  WinRT-layer API, so the ios TFM and the plain TFM run *identical* code (native and Skia
  heads exercise the same path — fewer divergence bugs):

  ```csharp
  // Package's Apple implementation returns CFBundleIdentifier. It lives in the WinRT
  // layer (Uno.dll), which on Skia mobile heads is always the *native* build ("follows
  // the WinRT layer" in RuntimeAssetsSelectorTask), so this works from the plain netX.0
  // build of this assembly too — unlike Foundation.NSBundle, which only compiles on the
  // ios TFM.
  var bundleId = OperatingSystem.IsIOS()
      ? global::Windows.ApplicationModel.Package.Current.Id.FamilyName
      : null;
  ```

  `FamilyName` falls back to `string.Empty` if the plist key is missing;
  `GetPlatformRedirectUri` already treats empty as "nothing to derive" →
  `WithDefaultRedirectUri()` — acceptable degradation, no new handling needed.

- [x] 5. **Delete `UNO_EXT_MSAL_ANDROID_TFM` / `UNO_EXT_MSAL_IOS_TFM` and their `#error` guards**
  (csproj + the provider's file header). They guarded exactly one hazard — a compile-time
  platform branch silently dying and falling through to the desktop path — which runtime
  dispatch eliminates. The replacement safety net is behavioral (plan item 7).

- [x] 6. **Make the remaining stub loud.** In `HostBuilderExtensions.InternalAddMsal`'s
  `#if !UNO_EXT_MSAL` branch (post-change: Mac Catalyst only), replace the silent
  `return builder;` with `throw new PlatformNotSupportedException("MSAL authentication is
  not supported on this target platform.")`. The silent no-op is what turned this bug into a
  cross-repo debugging session — `AuthenticationService`'s "No providers specified" surfaces
  far from the cause. **Behavior change** for Catalyst apps that call `AddMsal` today
  (silent → throw): declare in PR notes; if the panel objects, the fallback position is an
  `ILogger`-visible error via a registered startup diagnostic, but prefer the throw.

- [x] 7. **Tests.**
  - Unit layer (`Uno.Extensions.Authentication.MSAL.Tests`,
    `Uno.Extensions.Authentication.MSAL.UI.Tests`): unaffected by design —
    `MsalRedirectDefaults.Apply` takes the platform as a parameter, and the linked-source
    harness defines `UNO_EXT_MSAL`. Run them; expect green (27 unit at 009 handoff, plus
    `Given_MsalAuthentication`).
  - Add runtime tests (the ios/android runtime-test stages built in 009) asserting on-device:
    (a) resolving `IAuthenticationService` and calling `RefreshAsync` does **not** log/throw
    "No providers specified" — i.e. the provider registered; (b) the redirect decision on
    iOS/Android is `PlatformDerived` (surface `MsalRedirectDecision` to the test via logs or
    an internal hook — `InternalsVisibleTo` already exists for the test projects).
  - Note: the *packaged-artifact* behavior (the swap) can't be covered by unit tests at all —
    only the live testbed run (item 8) proves it.

- [ ] 8. **Live validation on the testbed** — full agent run book in
  [`macos-validation.md`](macos-validation.md) (this folder), written for hand-off to an
  agent on the macOS machine. (`Uno.Samples` @ `dev/sb/msa-ext`,
  `UI/Authentication.MsalExtensionsDemo`, iteration loop in its `HANDOFF-MACOS.md`): repack
  `Uno.Extensions.Authentication.MSAL.WinUI` at `255.255.255.255-local`, purge
  `~/.nuget/packages/uno.extensions.authentication.msal.winui/255.255.255.255-local`,
  **delete `bin/obj` `net10.0-ios` trees** (incremental build will not refresh the swapped
  dll), rebuild `-f net10.0-ios`, run the IL probe on the `.app` dll (expect ~113), deploy.
  Expected at startup: Information log
  `Using RedirectUri 'msauth.{bundleId}://auth'; sign-in requires a matching redirect URI...`
  and no "No providers specified". Logout on MainPage navigates to LoginPage (back stack
  cleared — already implemented demo-side in `MainModel.Logout`). Repeat the smoke test on
  the android head if an emulator is at hand.

- [x] 9. **Docs** (`doc/Learn/Authentication/HowTo-MsalAuthentication.md` — note: this file has
  uncommitted edits from an earlier session, per 009/HANDOFF): add that Skia iOS/Android
  heads run the package's plain-TFM build, so app-side `#if ANDROID`/`#if IOS` blocks in
  `Builder(...)` callbacks behave by TFM (they still compile per-head in the *app*, which
  keeps its platform TFM — but library authors must not assume TFM==OS). Fold in the still
  pending 009 checklist item 5 content (`InteractiveTimeout`, WASM SPA registration) if not
  already done.

- [x] 10. **Cross-bookkeeping**: update `specs/012-msal-auth-fixes/progress.md` PR notes — the
  "consumer MSBuild-surface change" bullet is now partially reverted (plain-TFM define
  returns, by design); reference this spec.

## Verification (2026-08-19, implementation session)

- All 7 TFMs of `Uno.Extensions.Authentication.MSAL.WinUI` build in Release with zero
  project-local warnings.
- **IL probe** (per "Fast re-verification probes"): plain `net9.0` `InternalAddMsal` = **113
  bytes** (functional — was the 2-byte stub), ios/android/desktop = 113, maccatalyst = 11 (the
  new throw). Probe script preserved conceptually in the probes section above.
- Unit layer: `Uno.Extensions.Authentication.MSAL.Tests` 27/27 green, unchanged as designed.
- Runtime layer: desktop head (`net9.0-desktop`, `UNO_RUNTIME_TESTS_RUN_TESTS` filter
  `Given_MsalAuthentication`) — **10/10 passed**, including the two new tests
  (`When_AddMsal_Then_ProviderRegistered`, `When_Built_Then_RedirectDecisionMatchesRuntimePlatform`);
  the android/ios/wasm lanes run the same suite in CI.
- Item 8 (live Uno.Samples testbed on the iOS simulator) remains open — macOS-only loop.

## Caveats / PR notes (declare in the PR description)

- **Skia Catalyst/tvOS heads** are also subject to the swap and now receive a *functional* plain
  lib (previously: functional on released packages, stub on this branch). At runtime they take
  the Desktop redirect path; Catalyst storage hits the native-persistence early return via
  `IsMacCatalyst()`. Untested territory, but strictly better than a silent no-op and matches
  released behavior. The *native* maccatalyst TFM stub now throws (plan item 6).
- **Windows apps consuming mac-built local packages** (no `Build_Windows` → no windows10 lib)
  resolve the plain lib → Desktop path, no WAM broker. Pre-existing local-dev caveat
  (HANDOFF-MACOS.md), now degraded-but-functional instead of silently broken.
- **`#error` guard removal** is intentional: the guarded failure mode (dead compile-time platform
  branch) no longer exists once platform selection is runtime-based.
- **Storage.WinUI is swapped too — confirmed 2026-08-23 by assembly-reference inspection, not
  inferred.** `Uno.Extensions.Storage.UI.dll` for `net9.0-android` and `net9.0-ios` references
  `Uno.UI` and the package ships a `lib/net9.0` build, so `HandleSkiaMobileForNonRuntimeEnabledPackages`
  replaces it on Skia Android/iOS heads; the default `IKeyValueStorage` there is
  `ApplicationDataKeyValueStorage` (plaintext, app-sandboxed). KeyStore/KeyChain cannot exist in
  the `net9.0` TFM, so this is documented (`doc/Learn/Storage/StorageOverview.md` table + note,
  `doc/Learn/Authentication/HowTo-MsalAuthentication.md`) rather than fixed in code. Item 8 (live
  testbed validation) is still the only end-to-end proof of the rest of the dispatch and remains
  open.
- An upstream alternative was considered and rejected for now: adding this package to the uno
  repo's `IsWinRTAssembly`-style exemption so Skia heads keep the platform lib. That couples the
  repos, needs an uno release, and the runtime-dispatch design removes the need — the plain lib
  becomes the single always-correct artifact.

## References

- uno repo (sibling checkout): `src/SourceGenerators/Uno.UI.Tasks/RuntimeAssetsSelector/RuntimeAssetsSelectorTask.cs`
  (`HandleSkiaMobileForNonRuntimeEnabledPackages`, `IsWinRTAssembly`),
  `src/Uno.UWP/ApplicationModel/PackageId.Apple.cs`; uno#20601, uno#24055.
- `uno.winui` package: `buildTransitive/uno.winui.runtime-replace.targets` (`ReplaceUnoRuntime`).
- Testbed: `Uno.Samples` @ `dev/sb/msa-ext`, `UI/Authentication.MsalExtensionsDemo/HANDOFF-MACOS.md`.
- Prior work: `specs/012-msal-auth-fixes/progress.md` (esp. "Define gates unified" review item and
  the storage rework this spec builds on).
