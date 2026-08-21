# Lessons

Domain lessons / postmortems for Uno.Extensions. See `AGENTS.md` §3 for when to add here.

## `src/Directory.Packages.props` versions are the published packages' dependency ranges — never bump them to fix an app-head restore

**Problem:** Bumping `Uno.Sdk.Private` made the SDK inject an implicit `Microsoft.Extensions.Logging.Console` reference (version from the SDK's `targets/*/packages.json`) into the `Uno.Extensions.RuntimeTests` app head that was newer than the `Microsoft.Extensions.*` pins in `src/Directory.Packages.props`. With `CentralPackageTransitivePinningEnabled=true` this produced NU1109 downgrade errors in the head. The obvious fix — raising the central pins — is wrong: `src/Directory.Packages.props` defines the dependency versions that get baked into every published `Uno.Extensions.*` NuGet, so a global bump raises the minimum `Microsoft.Extensions.*` versions for all external consumers just to satisfy a test head.

**Correct pattern:** Scope the override to the app head that needs it, via `<PackageVersion Update="..." Version="..." />` items in that head's csproj (works because `Directory.Packages.props` is imported during the props phase, before the project body). Only the packages the newer transitive closure actually forces up need overriding — for `Logging.Console` that closure is `Logging`, `Logging.Abstractions`, `Configuration.Abstractions`, and `DependencyInjection` (required ≥ by `Logging` itself); unpinned transitive packages float up on their own. `samples/` and `testing/` have their *own* `Directory.Packages.props`, which is why bumping `MicrosoftLoggingVersion` there (e.g. 00cbdcbbf) is safe — those files never feed the packed nuspecs.

**Apply to:** any restore/version conflict in an app head (`Uno.Extensions.RuntimeTests`, `*.UI.Tests`, Playground, TestHarness, MauiEmbedding) after an `Uno.Sdk.Private` / `UnoVersion` bump. Fix in the head (or the samples/testing-scoped props file), not in `src/Directory.Packages.props`. Central pins under `src/` change only when we deliberately want to raise the dependency floor of the shipped packages.

## A navigator that returns `null` from `Show()` as a *contract* must not be treated as a failed view resolution (spec 004)

**Problem:** `SelectorNavigator<TControl>` (base of `TabBarNavigator` and `NavigationViewNavigator`) returns `null` from `Show()` **by design** — it selects the matching item and delegates page rendering to the sibling content region (`// Don't return path, as we need for path to be passed down to children`). `ControlNavigator<TControl>.ExecuteRequestAsync` treats a `null` `Show()` result as a failed view resolution unless one special case matches (`CurrentView is FrameView`, spec 003 Fix 4). A selector's `CurrentView` is the selected `TabBarItem` / `NavigationViewItem` — never a `FrameView` — so every selector navigation fell through to the failure path: it logged a misleading `"Show() returned null. No matching view was found or created."` warning **and** called `RememberPendingFailedRequest`. Spec 003's hot-reload self-heal then re-issued that phantom pending request on every later HR delta (`RetryPendingFailedRequestsFromRoot`), thrashing the active tab during a burst of deltas — the user-visible "menu pages silently fail to resolve after Hot Reload" symptom.

**Correct pattern:** Distinguish "Show returned null because it delegated the view to another region (success)" from "Show returned null because the view could not be resolved (failure)". Added `ControlNavigator.IsNullShowResultExpected` (virtual, default `false`; `SelectorNavigator` overrides to `true`). When set, `ExecuteRequestAsync` clears any pending slot and returns `Route.Empty` without warning and without recording an HR retry. The route flow is unchanged — the failure path already returned `Route.Empty`, so `CoreNavigateAsync.Trim` keeps the request route intact for the sibling/child regions. The `FrameView` wrapper (spec 003) and the `SelectorNavigator` are two shapes of the same "intentionally null" contract; a third shape should reuse this hook.

**Apply to:** any caller that keys a retry/queue/error path off a navigator's `null`/empty result. If `null` is a legitimate "handled, delegated downstream" signal for some navigator, the caller must recognize it explicitly — conflating it with "could not resolve" poisons everything downstream of the failure signal (here, the HR retry walk). Guarded by `Given_TabBar_HotReload.When_TabNavigated_Then_NoPendingFailedRequestRecorded`.

## Hot-reload fixture files must be committed in their pre-HR baseline state

**Problem:** `HotReloadHelper.UpdateSourceFile(...)` physically rewrites fixture files on disk (`HotReloadTabBarTarget.cs`, `HotReloadRouteNotifierTarget.cs`, `MvuxHotReloadFeedToStateModel.cs`, the `HotReloadTabBar*Page.xaml` pages, ...) during a test run and reverts them on dispose. A run that is aborted mid-test (debugger stop, crash, Ctrl+C) leaves the post-HR mutation on disk. Committing that mutated state breaks every test using the fixture twice over: the pre-HR baseline assertion reads the post-HR value (CI failure: expected "original", got "updated" in `Given_TabBar_HotReload`), and `UpdateSourceFile` can no longer find its search string. This happened in b9c85f822; fixed by restoring the three files from `main` (939b9c344).

**Apply to:** before committing on any branch that ran HR tests locally, `git diff` the `HotReload*Target*.cs` / `MvuxHotReload*Model.cs` / `HotReload*Page.xaml` fixture files — any diff that flips a baseline literal ("original"→"updated", "handled-"→"modified-", `IFeed`→`IState`) is leftover test mutation, not an intentional change. Restore from `main` instead of committing. The baseline literal of each fixture is whatever its tests assert *before* calling `UpdateSourceFile`.

## `-getProperty:DefineConstants` does not list the SDK's implicit symbols (`ANDROID`, `IOS`, ...)

**Problem:** while adding platform branches to `MsalAuthenticationProvider` (spec 009), `dotnet build -getProperty:DefineConstants` was used to check whether `ANDROID` / `IOS` were defined for the `net9.0-android` / `net9.0-ios` TFMs. Neither appeared, which read as "the `#if ANDROID` branch is dead code". They are in fact defined: the .NET SDK merges `@(ImplicitDefineConstants)` into `DefineConstants` inside a target that runs *before* `CoreCompile` but *after* evaluation, and `-getProperty` reports the evaluation-time value.

**Correct pattern:** to test whether a symbol is live, compile something that depends on it. Either a temporary `#if !SYMBOL` + `#error` probe build, or check the emitted assembly for a type only that branch references (`Foundation.NSBundle` appears only in the iOS assembly). Do not infer symbol state from `-getProperty`.

**Apply to:** any conditional-compilation change on a cross-targeted project. A branch that silently stops compiling does not fail the build — it changes behavior and no test notices, because the other branch compiles fine. `Uno.Extensions.Authentication.MSAL.WinUI.csproj` now emits `UNO_EXT_MSAL_ANDROID_TFM` / `_IOS_TFM` from `_UnoExtMsalTargetPlatform` and `MsalAuthenticationProvider.cs` cross-checks them with `#error`, so the two can never drift apart again. Copy that pattern rather than trusting the symbol.

## MSAL's interactive modifiers are unreachable from `PublicClientApplicationBuilder`

**Problem:** `IMsalAuthenticationBuilder.Builder(...)` exposes `PublicClientApplicationBuilder`, which MSAL builds once. Everything that customises a *single* interactive sign-in — `WithPrompt`, `WithLoginHint`, `WithExtraScopeToConsent`, `WithSystemWebViewOptions`, `WithCustomWebUi` — is an extension on `AcquireTokenInteractiveParameterBuilder`, constructed per request inside `MsalAuthenticationProvider.AcquireInteractiveTokenAsync`. Planning assumed `Builder(...)` could reach `WithCustomWebUi` and therefore that unattended interactive tests needed no library change; it cannot.

**Correct pattern:** `InteractiveBuilder(...)` (added alongside `Builder`/`Storage`/`Scopes`) applies a callback to the per-request builder, after `WithUnoHelpers()` so an app can override what the helpers set. Test doubles reach MSAL through the same public surface an app would.

**Apply to:** any wrapper over a builder-per-request API. Check which builder type a modifier hangs off before assuming an existing extension point reaches it.

## MSAL persists its token cache across runtime-test runs — purge, don't assume isolation

**Problem:** on desktop targets `MsalCacheHelper` writes the MSAL token cache to a file in the app data folder that is shared by every test in a run *and* survives across runs. The first draft of `Given_MsalAuthentication` had three tests that appeared to pass while doing nothing meaningful: each reused the account the previous test had signed in, so "login" never prompted, "refresh without login" found an account, and the token-leak assertion read a stub that had issued no tokens.

**Correct pattern:** `CreateHarnessAsync` calls `LogoutAsync` before handing the harness to the test, and asserts the purge itself neither prompted nor requested a token. Driving the product's own logout keeps this correct if the storage location changes.

**Apply to:** any runtime test over a component with platform-backed persistence (token caches, `ApplicationData` settings, keychain/keyring entries). The `[TestMethod]` boundary isolates managed state, not the filesystem. A suite that only ever passes is as suspicious as one that fails.

## `__WASM__` is no longer defined for consumer projects — code (ours or a package's) that branches on it is dead on Skia-WASM heads

**Problem:** the WebAssembly runtime-test lane exited before running a single test with `PlatformNotSupportedException` from `Console.CancelKeyPress`. `Uno.UI.RuntimeTests.Engine`'s `RuntimeTestEmbeddedRunner` — which ships as **source** and compiles into `Uno.Extensions.RuntimeTests.Core` — guards that registration with `#if !__WASM__`. Uno.Sdk defines `IsBrowserWasm` (see `targets/Uno.IsPlatform.props`) but no `__WASM__` compilation symbol, so the guard never fires and the unsupported call is compiled in. A Skia-WASM head consumes the *plain* `netX.0` `Uno.UI` lib (there is no `browserwasm` lib in `uno.winui`), which is the same substitution spec 010 documents for Skia mobile.

**Correct pattern:** don't add `__WASM__` back. Defining it locally fails to compile: the engine's other `#if __WASM__` branches call `Uno.Foundation.WebAssemblyRuntime.InvokeJS`, and `Uno.Foundation.Runtime.WebAssembly` is not referenced by a Skia-WASM head (`CS0234`, verified). Runtime checks are the portable form — `PlatformHelper.IsWebAssembly` / `OperatingSystem.IsBrowser()` — exactly as spec 010 concluded for `ANDROID`/`IOS`. The WASM device lane in `build/ci/.azure-pipelines.yml` is commented out until the engine converts its guards.

**Apply to:** any `#if __WASM__` in this repo or in a source-shipping package we consume. Symbol-based platform branching is only reliable for symbols the SDK actually emits, and the Skia unification removed most of them. Cross-check with the `-getProperty:DefineConstants` lesson above: neither `-getProperty` nor "it compiled" proves a branch is live — only running it on the target does.

## `new Window()` in a UI/runtime test is a desktop-only construct

**Problem:** `Given_MsalAuthentication`'s harness created its own `Window` to pass to `AddMsal` and `Dispatcher`. All 10 tests passed on the desktop head and all 10 failed on the iOS simulator with `InvalidOperationException: Creating secondary windows on this platform is not allowed`. Android has the same restriction. The failure is invisible until a mobile lane actually runs, which is why it survived until the device stages were wired up.

**Correct pattern:** take `UnitTestsUIContentHelper.CurrentTestWindow` — the host's own window, present on every head. Only bracket with `SaveOriginalContent`/`RestoreOriginalContent` if the test assigns `Content`; a test that just needs a `Window` reference does not. Recorded as a repo-wide rule in `AGENTS.md` § "Windows in UI / runtime tests"; the pre-existing `[RunsInSecondaryApp]` bullet was the same trap seen from one platform only.

**Apply to:** every remaining `new Window()` under `src/**/*.UI.Tests/` (`Given_ChainedGetDataAsync`, `Given_FrameContentRehook`, `Given_NavigatorStartup`, `Given_NestedNavReloadRecovery`, `Given_RouteNotifier`, `Given_TabNavigation`). They are green today only because `RuntimeTestsFilter` scopes the device lanes to `Given_MsalAuthentication`; widening that filter fails them all.

## MSAL on iOS cannot build a `PublicClientApplication` without a keychain entitlement — and the entitlement drags in provisioning

**Problem:** every `Given_MsalAuthentication` case failed on the iOS simulator lane with `MsalClientException: cannot_access_publisher_keychain` thrown from `PublicClientApplicationBuilder.Build()`. `iOSPlatformProxy.CreateTokenCacheAccessor` returns `iOSTokenCacheAccessor` unconditionally — `WithCacheOptions` cannot opt out — and its constructor calls `GetTeamId()`, which writes a probe item to the keychain and reads back its access group. The runtime-test head shipped an empty `Entitlements.plist`, so the probe failed and MSAL threw before any test logic ran. Adding the entitlement then broke the *build* instead: a non-empty entitlements file makes the iOS SDK demand a provisioning profile (`Could not find any available provisioning profiles`), which a hosted agent with no Apple team cannot supply.

**Correct pattern:** two changes, and neither alone is enough. (1) Entitle exactly the group MSAL will compute — it uses `<first dot component of the access group>.com.microsoft.adalcache`, so `unoexttests.com.microsoft.adalcache` makes `GetTeamId()` return `unoexttests` and the derived group match what is entitled. A shipping app writes `$(AppIdentifierPrefix)com.microsoft.adalcache` and gets the prefix from its profile; a synthetic prefix works here because the simulator does not validate the group against one. (2) Pass `-p:CodesignRequireProvisioningProfile=false` **from the CI build command**, not the csproj, so it applies only to the simulator-only CI build and a device build still requires a profile. Verified end to end: the entitlement appears in the bundle's `archived-expanded-entitlements.xcent` and the lane went 0/10 → 10/10 in build 228835.

**Apply to:** any iOS runtime-test head exercising a library that touches the keychain (MSAL, `ITokenCache`, anything using `SecKeyChain`). Check the built `.app`'s `archived-expanded-entitlements.xcent` rather than the source plist — that is the artifact that proves the entitlement survived signing.

## A CI tool that exits 0 has not necessarily done anything — never discard its stdout

**Problem:** the Android emulator lane failed twice for two different reasons that shared one cause. `avdmanager create avd ... >/dev/null` exited 0, then the emulator died in two seconds with `Unknown AVD name [uno-extensions-rt-avd]`: the two tools each fall back through their own list of default AVD locations (`$ANDROID_AVD_HOME`, `$ANDROID_SDK_HOME/avd`, `$HOME/.android/avd`) and disagreed about which one to use. Because `create`'s stdout was redirected to `/dev/null`, whatever it said about where it wrote went with it. Worse, the *first* symptom was not this at all: `adb wait-for-device` has no timeout, so the dead emulator hung the job for the full 120 minutes, and a cancelled Azure DevOps job skips even `condition: always()` steps — so the emulator log that named the cause was never published.

**Correct pattern:** pin the contested location (`export ANDROID_AVD_HOME=...`, honoured by both tools) instead of relying on defaults agreeing; keep the tool's output; and assert the postcondition (`emulator -list-avds | grep -qx "${AVD_NAME}"`) rather than trusting the exit code. Separately, every unbounded wait in a CI script needs a timeout *and* a liveness check on the process it is waiting for, with the relevant log printed inline — an artifact upload is not a diagnostic channel for a job that gets cancelled.

**Apply to:** `build/test-scripts/*.sh` generally. The device scripts are full of waits on external processes (emulator boot, simulator boot, result-file polling); each one should fail loudly and quickly with its log rather than burning the job budget. A two-hour timeout with no output costs a whole CI round and tells you nothing.

## `OutputType` is rewritten to `Library` for Android app heads - a library-only rule then hit an app

**Problem:** the Android runtime-test app installed, launched, and died in `Application.OnCreate` with `open_from_bundles: failed to load bundled assembly Uno.Extensions.Reactive.dll` then `[System.Reflection.TargetInvocationException]` through `Java.Interop.TypeManager.CreateProxy`. None of the 23 ProjectReference outputs were in the APK; the ~330 NuGet-supplied assemblies per ABI all were.

**Root cause:** `src/Directory.Build.targets` carried

```xml
<ItemDefinitionGroup>
  <ProjectReference>
    <Private Condition="'$(OutputType)' == 'library' and '$(NugetOverrideVersion)'==''">false</Private>
  </ProjectReference>
</ItemDefinitionGroup>
```

which exists so our ~50 libraries don't each duplicate the closure into their own `bin`. **.NET Android rewrites an app head's `OutputType` to `Library`** - an Android app has no managed entry point - so the APK head matched a rule written for libraries. Measured on the same head: `net9.0-android` reports `OutputType=Library` + `AndroidApplication=true`, `net9.0-desktop` reports `OutputType=Exe`. Every ProjectReference therefore got `Private=false` on Android *only*, which emptied `ReferenceCopyLocalPaths` (2 `.uprimarker` files, 0 assemblies), which left the closure out of `ResolvedFileToPublish` and out of Android's `ResolvedUserAssemblies`, and NuGet-restored assemblies were unaffected because they arrive as `RuntimeCopyLocalItems` instead. Fix: add `and '$(AndroidApplication)' != 'true'`, the discriminator this file already uses elsewhere. Copy-local went 0 -> 23 immediately.

**Second, independent bug this uncovered:** with the closure packaged, the app booted and `MobileRuntimeTestsAutostart` ran, then failed with `UnauthorizedAccessException: Access to the path '/storage/emulated/0/Android/data/<pkg>' is denied`. `Directory.CreateDirectory` walks parents, and an app cannot create the `Android/data/<package>` level - only the platform API can. `MainActivity.OnCreate` now calls `GetExternalFilesDir(null)` when the result-file variable is present, which creates the chain with the right ownership. Reproduced on API 34 *and* 36, so it was never a newer-scoped-storage quirk.

**How it was found:** by diffing against `studio.live`, whose Android runtime-test lane works. Its head has 18 ProjectReferences and 19 copy-local assemblies at `net10.0-android`; ours had 23 references and 0. Its references carry no `Private` metadata at all, ours carried `Private='false'` - and studio.live has no such `ItemDefinitionGroup`. Its app-side `MobileRuntimeTestsAutostart` is otherwise line-for-line equivalent to ours, including the same `Directory.CreateDirectory` call, which is why the second bug only shows up on a fresh emulator where nothing has created the directory yet.

**Ruled out along the way, each by experiment - don't re-test:** `dotnet publish -o <dir>` (same result without it), `EmbedAssembliesIntoApk` (it fixed a *separate* Fast Deployment abort, `No assemblies found in .../.__override__/x86_64`, and must stay set), `BaseOutputPath=bin\$(MSBuildProjectName)` (overriding it to the SDK default changed nothing, despite the suggestive head-named folders it leaves inside every reference), Debug vs Release, single- vs multi-TFM graphs (`UnoTargetFrameworkOverride`), the emulator, the AVD, and APK signing.

**Verified:** local API 34 AVD, matching CI's `system-images;android-34;...;x86_64` - 23 runtime tests, 23 passed, 0 failed, validator exit 0.

**Apply to:** any repo-wide `ItemDefinitionGroup` or property condition keyed on `OutputType`. `OutputType` is not stable across target platforms - Android rewrites it, and a rule that reads "libraries only" silently becomes "and also every Android app". Prefer a platform-intent property (`AndroidApplication`, `IsUnoHead`) over inferring app-ness from `OutputType`.

## `TryAdd*` cannot claim an interface `AddNamedSingleton` already seeded — inject a dedicated type instead

**Problem:** planning for spec 011 assumed `services.TryAddSingleton<IKeyValueStorage>(sp => sp.GetRequiredDefaultInstance<IKeyValueStorage>())` inside `AddMsal` would make the platform-default store constructor-injectable. It is a silent no-op whenever `UseStorage` ran first: `AddNamedSingleton` calls `TryAddTransient<TService>(sp => sp.GetRequiredService<TImplementation>())` for every provider it registers (`Core/DependencyInjection/ServiceCollectionExtensions.cs:38`), and `AddKeyedStorage` registers `InMemoryKeyValueStorage` first — so plain `IKeyValueStorage` is already claimed and resolves to the *in-memory* store. The MSAL cache would have "persisted" to memory, order-dependently, with no error anywhere.

**Correct pattern:** define a one-line wrapper record nobody else registers (`MsalKeyValueStorage(IKeyValueStorage Storage)`) and register it with a factory that calls `GetRequiredDefaultInstance<IKeyValueStorage>()` — the same shape `UseAuthentication` uses to build `TokenCache`. The named-instance system is the only correct resolution path for "the platform default"; the bare interface gets you whichever `TryAdd` ran first.

**Apply to:** any constructor that needs the *default* named instance of a service `AddKeyedStorage`-style registration owns. Also note the standing consumer trap this leaves: an app that injects plain `IKeyValueStorage` silently gets `InMemoryKeyValueStorage`. Changing that is a public behavior change and needs its own issue — do not "fix" it as a side effect.

## `IAuthenticationService`'s convenience overloads pass a null `IDispatcher` — never demand one on a path that shows no UI

**Problem:** `MsalAuthenticationProvider.InternalLogoutAsync` opened with a null-guard throwing `ArgumentNullException` for `dispatcher` — a parameter the method never uses (`RemoveAsync` only mutates MSAL's own cache). The documented `IAuthenticationService.LogoutAsync(CancellationToken)` extension (`AuthenticationServiceExtensions.cs:43`) always passes `dispatcher: default`, so *every* logout through it threw; an app whose command swallows exceptions sees "clicking logout does nothing", and because the token cache never cleared, `HasTokenAsync` stayed true and `RefreshAsync` kept reporting success forever. On `main` since `4bd7bde31`; found only by live app testing — every in-repo test passed a dispatcher.

**Correct pattern:** require the dispatcher only where UI is actually shown (interactive login). Logout and refresh paths must accept null — `OidcAuthenticationProvider` already ignores the parameter. Guarded by `Given_MsalAuthentication.When_LogoutWithoutDispatcher_Then_SignedOut`, which calls the convenience overload the way an app does.

**Apply to:** every `IAuthenticationProvider` implementation and any new `Internal*` override: read what `AuthenticationServiceExtensions` actually passes before adding a parameter guard. A guard on an unused parameter is not defensive — it converts a valid public call pattern into a guaranteed throw.

## Same-version local-package loops: purge the NuGet cache, then verify the bytes that actually run

**Problem:** validating this branch through `Uno.Samples/UI/Authentication.MsalExtensionsDemo` (which pins every `Uno.Extensions.*` package to `255.255.255.255-local` from `../uno.extensions/artifacts`): the version never changes, so NuGet's cache serves the stale copy after every repack, and even after purging, a fix was reported "still broken" while an older build output was being served. Separately, a *library* fix can look broken because of an *app* bug — the second "logout still does nothing" report was the sample's own `MainModel` not navigating after a fully successful sign-out, plus a draft that passed the `CancellationToken` as the `sender` argument of `NavigateViewModelAsync` (compiles — `sender` is `object`).

**Correct pattern:** the loop is repack → `rm -rf ~/.nuget/packages/<id>/255.255.255.255-local` → rebuild the head → **verify the deployed artifact**, e.g. fetch `/_framework/<assembly>.wasm` from the running host and check it for a marker string of the change, before asking anyone to validate. And separate library behavior from app behavior by asserting on observable state (storage keys such as `AuthToken_*` / `MsalCache_*`), not on what the UI appears to do.

**Apply to:** all local-feed validation against the demo apps (the full loop is in `Uno.Samples/UI/Authentication.MsalExtensionsDemo/HANDOFF-MACOS.md`). "The build succeeded" proves nothing about what the browser downloaded, and "the button did nothing" names a symptom, not a layer.

## Cancellation is not always a courtesy — a half-done sign-out is worse than a slow one

**Problem:** `MsalAuthenticationProvider.InternalLogoutAsync` checked `ThrowIfCancellationRequested()` between account removals and before deleting the serialized MSAL cache. Honouring cancellation there produced the worst reachable state: some accounts removed, the rest still signed in with their refresh tokens in the serialized cache, the access token still cached, `IsAuthenticated` still true — and the caller told the operation was cancelled. A user who asked to log out is left authenticated, believing they are not.

**Correct pattern:** ask what a partial completion *means* before plumbing a token through a loop. Sign-out is all local cache mutation, so there is nothing slow to interrupt and no reason to offer an exit; the removal loop ignores cancellation and the cleanup moved into a `finally` so a mid-loop failure still takes the credential material with it. That `finally` passes `CancellationToken.None` deliberately — an already-cancelled token would make the delete itself the no-op the `finally` exists to prevent. Guarded by `Given_MsalAuthentication.When_LogoutCancelled_Then_SerializedMsalCacheStillRemoved`.

**Apply to:** any teardown/revoke/clear path (logout, cache eviction, credential rotation, temp-file cleanup). AGENTS.md §10's "honour cancellation quickly" is about *work the caller is waiting for*, not about abandoning a cleanup halfway. If the operation removes something dangerous, cancellation between steps is a leak, not responsiveness.

## `ISettings` is string-only — a non-string value silently persists as its type name

**Problem:** `EncryptedApplicationDataKeyValueStorage.GetObjectValue` returned the DPAPI-protected `byte[]`. On **packaged** Windows that flows into `ApplicationData.Current.LocalSettings`, which stores `byte[]` fine. On **unpackaged** Windows `ApplicationDataKeyValueStorage.SetSetting` takes the other branch — `UnpackagedSettings.Set(name, value?.ToString())` — so the blob was written as the literal string `"System.Byte[]"`. Reads then failed the `is byte[]` test and returned `default`, but the *key* persisted, so `TokenCache.HasTokenAsync` (which counts keys, not values) reported the user authenticated with nothing recoverable. The encrypted store — the **default on Windows** — silently persisted nothing, and spec 011 item 6 had just flipped its `IsEncrypted` to `true`, advertising it as the safe choice.

**Correct pattern:** values crossing `ISettings` must already be strings. `GetObjectValue` returns base64; `GetTypedValue` accepts `byte[]` too so existing packaged caches are not orphaned, and treats an undecodable string as absent rather than throwing on every read. When two storage backends sit behind one type, check that every value shape round-trips through *both* — `value?.ToString()` compiles for anything and fails silently for everything that isn't already a string.

**Apply to:** `ApplicationDataKeyValueStorage` and anything deriving from it, plus any future `ISettings` consumer. Note this is only reachable on a WinAppSDK head (`#if WINDOWS`) with `PlatformHelper.IsAppPackaged == false`, which no runtime-test lane covers — the bug survived because the branch that has it never runs in CI. A round-trip assertion per platform is worth more than the type checking out.

## A `??=` cache of an expensive object is a race, and record copies make it unrecoverable

**Problem:** `ProviderFactory.AuthenticationProvider` was `configuredProvider ??= ConfigureProvider(Provider, Settings)`. Two concurrent resolves can both observe null and both invoke the callback — which builds an `IPublicClientApplication` and subscribes to `ITokenCache.Cleared`. Because `ConfigureProvider` does `provider with { … }`, each invocation returns a **different record instance**, so the losing one stays subscribed for the host's lifetime with no reference anywhere to `-=` it: a leaked provider, a leaked MSAL client, and the clear handler running twice. A code comment asserted the opposite ("Build runs exactly once per provider, so this cannot double-subscribe") and was believed.

**Correct pattern:** `Lazy<T>` with `LazyThreadSafetyMode.ExecutionAndPublication` for any single-instance cache whose factory has side effects. Do not hand-roll `??=` when the value owns a subscription, a native handle, or an HTTP client. And when a comment claims an invariant, follow the reference and check it — this one named `ProviderFactory` without reading it.

**Apply to:** every `??=`/`if (x is null)` memoisation in shared code (`ProviderFactory` covered Msal, Oidc and Custom at once). Side-effect-free memoisation of a value type can race harmlessly; anything that registers, connects or allocates unmanaged state cannot.

## A test filter naming a class silently drops its siblings — scope device lanes by namespace

**Problem:** `.azure-pipelines.yml` set `RuntimeTestsFilter: 'Given_MsalAuthentication'` to scope the four device runtime-test stages away from the 15 pre-existing failures in the wider suite. `Uno.UI.RuntimeTests.Engine`'s filter is a plain substring match on the fully-qualified test name, so that string matched exactly one class. The 8 `Given_BrowserTokenCacheStorage` cases added in the same branch — the entire guard on which browser store the token cache lands in — matched nothing and never ran on any lane. Both device lanes were green and the check list read "Runtime Tests - Desktop (Skia): pass", so the gap was invisible; it surfaced only because a human read the check names and asked which platforms actually run the auth tests.

**Correct pattern:** scope a substring filter by **namespace**, not class — `'Authentication.MSAL.UI.Tests'` picks up all 23 cases and keeps picking up classes added later. When a filter is narrowed to dodge unrelated failures, assert the resulting *count*: run the filter locally and confirm it matches the tests you think it does, because a filter that matches too little fails open, not closed.

**Apply to:** `RuntimeTestsFilter` and any `dotnet test --filter` / engine filter in CI. The general shape — a green lane that silently ran a subset — also applies to VSTest `**/*.Tests.dll` globs and `[TestCategory]` selectors. `TreatNoTestsAsError` in `build/tests.runsettings` catches zero matches; nothing catches "half".

## A cross-platform test must assert the documented contract, not the platform it was written on

**Problem:** `Given_BrowserTokenCacheStorage.When_Value_Written_Then_Round_Trips_Through_Default_Storage` ended with `(await storage.GetAsync<string>(key, ct)).Should().BeNull()` after clearing the key. That passes on Skia desktop and fails on iOS with `KeyNotFoundException`, because the two stores behind one interface disagree: `IKeyValueStorage.GetAsync` is documented as *"If that value does not exist, throws a `KeyNotFoundException`"*, `KeyChainKeyValueStorage` honours it, and `ApplicationDataKeyValueStorage` returns `default` instead. The assertion was written and verified on desktop, so it encoded the implementation that violates the contract — and it only surfaced when the CI filter was widened to actually run the class on a second platform.

**Correct pattern:** when a test runs on every head, assert the part of the contract every implementation agrees on, and read the interface's XML docs before asserting a behavior you observed. Here `GetKeysAsync().Should().NotContain(key)` is the platform-stable "it is gone" check, and it is also what `TokenCache.HasTokenAsync` actually relies on. Where implementations genuinely disagree, that is a product finding to record, not a detail for a test to quietly pick a winner for.

**Apply to:** any `*.UI.Tests` / RuntimeTests case, since those run on desktop, iOS, Android and WebAssembly from one source file. Also treat "documented to throw, one implementation returns default" as its own bug: the divergence in `ApplicationDataKeyValueStorage` is deliberately left alone here because making the default Windows/desktop/browser store start throwing is a public-surface behavior change that needs its own PR, not a drive-by fix inside a storage-selection change.

## `OutputType` is not the only property a platform rewrites - check what an SDK bump drags with it

**Problem:** bumping `Uno.Sdk` 6.0.67 -> 6.8.0-dev.21 to pick up an `Uno.WinUI` fix also moved
`Uno.Toolkit.WinUI` 7.0.2 -> 9.2.0-dev.18, because the Sdk pins that too. Toolkit had dropped its Mac
Catalyst assembly at 8.5.0-dev.29, so `utu:NativeFramePresenter` - used inside an `<ios:ControlTemplate>`,
and Uno's `ios` conditional XAML namespace also matches catalyst - no longer existed for that TFM. One
`UXAML0001` plus five cascading generator errors, none of them in code anyone had touched.

**Correct pattern:** an SDK version is a bundle, not a single package. Before a bump, list what the
Sdk pins (`targets/netstandard2.0/packages.json` in the Uno.Sdk package: `Core`, `Extensions`,
`UnoToolkit`, `MsalClient`, ...) and diff the groups, not just the one you came for. Verify the claim
you are relying on against the *shipped binary* rather than a branch name or release note - here, a
UTF-16 search for the allowlist string in `Uno.UI.Tasks.dll` across cached versions established the
exact floor, and disproved a "leading suspect" that had already been written down twice.

**Apply to:** any `global.json` SDK bump in this repo. Also note `dotnet build` cannot build the
XAML-bearing WinUI libraries here at all - it fails UNOB0008 on the current SDK too - so a bump has to
be validated with `msbuild`, or the first error tells you nothing about the bump.
