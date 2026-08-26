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

**Problem:** while adding platform branches to `MsalAuthenticationProvider` (spec 013), `dotnet build -getProperty:DefineConstants` was used to check whether `ANDROID` / `IOS` were defined for the `net9.0-android` / `net9.0-ios` TFMs. Neither appeared, which read as "the `#if ANDROID` branch is dead code". They are in fact defined: the .NET SDK merges `@(ImplicitDefineConstants)` into `DefineConstants` inside a target that runs *before* `CoreCompile` but *after* evaluation, and `-getProperty` reports the evaluation-time value.

**Correct pattern:** to test whether a symbol is live, compile something that depends on it. Either a temporary `#if !SYMBOL` + `#error` probe build, or check the emitted assembly for a type only that branch references (`Foundation.NSBundle` appears only in the iOS assembly). Do not infer symbol state from `-getProperty`.

**Apply to:** any conditional-compilation change on a cross-targeted project. A branch that silently stops compiling does not fail the build — it changes behavior and no test notices, because the other branch compiles fine. The `#error` cross-check this branch first added (`UNO_EXT_MSAL_ANDROID_TFM` / `_IOS_TFM`) is gone again: spec 010 moved platform selection to runtime `OperatingSystem.Is*()` because on Skia mobile heads the TFM no longer implies the OS, which also removed the hazard the guard existed for. Prefer runtime dispatch wherever TFM and OS can diverge; where a compile-time symbol is unavoidable, probe it as above rather than trusting the symbol.

## MSAL's interactive modifiers are unreachable from `PublicClientApplicationBuilder`

**Problem:** `IMsalAuthenticationBuilder.Builder(...)` exposes `PublicClientApplicationBuilder`, which MSAL builds once. Everything that customises a *single* interactive sign-in — `WithPrompt`, `WithLoginHint`, `WithExtraScopeToConsent`, `WithSystemWebViewOptions`, `WithCustomWebUi` — is an extension on `AcquireTokenInteractiveParameterBuilder`, constructed per request inside `MsalAuthenticationProvider.AcquireInteractiveTokenAsync`. Planning assumed `Builder(...)` could reach `WithCustomWebUi` and therefore that unattended interactive tests needed no library change; it cannot.

**Correct pattern:** `InteractiveBuilder(...)` (added alongside `Builder`/`Storage`/`Scopes`) applies a callback to the per-request builder, after `WithUnoHelpers()` so an app can override what the helpers set. Test doubles reach MSAL through the same public surface an app would.

**Apply to:** any wrapper over a builder-per-request API. Check which builder type a modifier hangs off before assuming an existing extension point reaches it.

## MSAL persists its token cache across runtime-test runs — purge, don't assume isolation

**Problem:** on desktop targets `MsalCacheHelper` writes the MSAL token cache to a file in the app data folder that is shared by every test in a run *and* survives across runs. The first draft of `Given_MsalAuthentication` had three tests that appeared to pass while doing nothing meaningful: each reused the account the previous test had signed in, so "login" never prompted, "refresh without login" found an account, and the token-leak assertion read a stub that had issued no tokens.

**Correct pattern:** `CreateHarnessAsync` calls `LogoutAsync` before handing the harness to the test, and asserts the purge itself neither prompted nor requested a token. Driving the product's own logout keeps this correct if the storage location changes.

**Apply to:** any runtime test over a component with platform-backed persistence (token caches, `ApplicationData` settings, keychain/keyring entries). The `[TestMethod]` boundary isolates managed state, not the filesystem. A suite that only ever passes is as suspicious as one that fails.

## Uno.Sdk no longer defines `__WASM__` for consumer projects — source-shipping code that branches on it compiles its non-browser path into Skia-WASM heads

**Problem:** the WebAssembly runtime-test lane exited before running a single test with `PlatformNotSupportedException` from `Console.CancelKeyPress`. `Uno.UI.RuntimeTests.Engine`'s `RuntimeTestEmbeddedRunner` — which ships as **source** and compiles into `Uno.Extensions.RuntimeTests.Core` — guards that registration with `#if !__WASM__`. Uno.Sdk defines `IsBrowserWasm` (see `targets/Uno.IsPlatform.props`) but no `__WASM__` compilation symbol, so the guard never fires and the unsupported call is compiled in. A Skia-WASM head consumes the *plain* `netX.0` `Uno.UI` lib (there is no `browserwasm` lib in `uno.winui`), which is the same substitution spec 010 documents for Skia mobile.

**Correct pattern:** define the symbol yourself only where you own every branch it turns on, and supply what those branches need. `Uno.Extensions.RuntimeTests.Core` defines `__WASM__` for its browserwasm TFM and ships a ~10-line `WebAssemblyRuntimeShim` (`[JSImport]` over `globalThis.eval`) for the engine's `Uno.Foundation.WebAssemblyRuntime.InvokeJS` call sites, which a Skia-WASM head cannot otherwise reference (`CS0234`). The WebAssembly lane (`build/ci/stage-runtime-tests-wasm.yml`) is enabled and runs the full `$(RuntimeTestsFilter)` — the whole `Authentication.MSAL.UI.Tests` namespace, no longer just `Given_BrowserTokenCacheStorage` — which became possible once `MsalAuthenticationProvider.Build` applied the app's `Builder(...)` callback *after* `WithUnoHelpers()`, so the stub `HttpClient` factory survives in the browser (expected; the CI run after that change is pending). For product code, runtime checks remain the portable form — `PlatformHelper.IsWebAssembly` / `OperatingSystem.IsBrowser()` — exactly as spec 010 concluded for `ANDROID`/`IOS`.

**Apply to:** any `#if __WASM__` in this repo or in a source-shipping package we consume. Symbol-based platform branching is only reliable for symbols the SDK actually emits, and the Skia unification removed most of them. Cross-check with the `-getProperty:DefineConstants` lesson above: neither `-getProperty` nor "it compiled" proves a branch is live — only running it on the target does.

## `new Window()` in a UI/runtime test is a desktop-only construct

**Problem:** `Given_MsalAuthentication`'s harness created its own `Window` to pass to `AddMsal` and `Dispatcher`. All 10 tests passed on the desktop head and all 10 failed on the iOS simulator with `InvalidOperationException: Creating secondary windows on this platform is not allowed`. Android has the same restriction. The failure is invisible until a mobile lane actually runs, which is why it survived until the device stages were wired up.

**Correct pattern:** take `UnitTestsUIContentHelper.CurrentTestWindow` — the host's own window, present on every head. Only bracket with `SaveOriginalContent`/`RestoreOriginalContent` if the test assigns `Content`; a test that just needs a `Window` reference does not. Recorded as a repo-wide rule in `AGENTS.md` § "Windows in UI / runtime tests"; the pre-existing `[RunsInSecondaryApp]` bullet was the same trap seen from one platform only.

**Apply to:** every remaining `new Window()` under `src/**/*.UI.Tests/` (`Given_ChainedGetDataAsync`, `Given_FrameContentRehook`, `Given_NavigatorStartup`, `Given_NestedNavReloadRecovery`, `Given_RouteNotifier`, `Given_TabNavigation`). They are green today only because `RuntimeTestsFilter` scopes the device lanes to the `Authentication.MSAL.UI.Tests` namespace; widening that filter fails them all.

## MSAL on iOS cannot build a `PublicClientApplication` without a keychain entitlement — and the entitlement drags in provisioning

**Problem:** every `Given_MsalAuthentication` case failed on the iOS simulator lane with `MsalClientException: cannot_access_publisher_keychain` thrown from `PublicClientApplicationBuilder.Build()`. `iOSPlatformProxy.CreateTokenCacheAccessor` returns `iOSTokenCacheAccessor` unconditionally — `WithCacheOptions` cannot opt out — and its constructor calls `GetTeamId()`, which writes a probe item to the keychain and reads back its access group. The runtime-test head shipped an empty `Entitlements.plist`, so the probe failed and MSAL threw before any test logic ran. Adding the entitlement then broke the *build* instead: a non-empty entitlements file makes the iOS SDK demand a provisioning profile (`Could not find any available provisioning profiles`), which a hosted agent with no Apple team cannot supply.

**Correct pattern:** two changes, and neither alone is enough. (1) Entitle exactly the group MSAL will compute — it uses `<first dot component of the access group>.com.microsoft.adalcache`, so `unoexttests.com.microsoft.adalcache` makes `GetTeamId()` return `unoexttests` and the derived group match what is entitled. A shipping app writes `$(AppIdentifierPrefix)com.microsoft.adalcache` and gets the prefix from its profile; a synthetic prefix works here because the simulator does not validate the group against one. (2) Pass `-p:CodesignRequireProvisioningProfile=false` **from the CI build command**, not the csproj, so it applies only to the simulator-only CI build and a device build still requires a profile. Verified end to end: the entitlement appears in the bundle's `archived-expanded-entitlements.xcent` and the lane went 0/10 → 10/10 in build 228835.

**Apply to:** any iOS runtime-test head exercising a library that touches the keychain (MSAL, `ITokenCache`, anything using `SecKeyChain`). Check the built `.app`'s `archived-expanded-entitlements.xcent` rather than the source plist — that is the artifact that proves the entitlement survived signing.

## A CI tool that exits 0 has not necessarily done anything — never discard its stdout

**Problem:** the Android emulator lane failed twice for two different reasons that shared one cause. `avdmanager create avd ... >/dev/null` exited 0, then the emulator died in two seconds with `Unknown AVD name [uno-extensions-rt-avd]`: the two tools each fall back through their own list of default AVD locations (`$ANDROID_AVD_HOME`, `$ANDROID_SDK_HOME/avd`, `$HOME/.android/avd`) and disagreed about which one to use. Because `create`'s stdout was redirected to `/dev/null`, whatever it said about where it wrote went with it. Worse, the *first* symptom was not this at all: `adb wait-for-device` has no timeout, so the dead emulator hung the job for the full 120 minutes, and a cancelled Azure DevOps job skips even `condition: always()` steps — so the emulator log that named the cause was never published.

**Correct pattern:** pin the contested location (`export ANDROID_AVD_HOME=...`, honoured by both tools) instead of relying on defaults agreeing; keep the tool's output; and assert the postcondition (`emulator -list-avds | grep -qx "${AVD_NAME}"`) rather than trusting the exit code. Separately, every unbounded wait in a CI script needs a timeout *and* a liveness check on the process it is waiting for, with the relevant log printed inline — an artifact upload is not a diagnostic channel for a job that gets cancelled.

**Apply to:** `build/test-scripts/*.sh` generally. The device scripts are full of waits on external processes (emulator boot, simulator boot, result-file polling); each one should fail loudly and quickly with its log rather than burning the job budget. A two-hour timeout with no output costs a whole CI round and tells you nothing.

## A code-quality bot comment on a PR is addressed with code or a resolved thread — not a reply and an open thread

**Problem:** `github-code-quality[bot]` flagged three generic catch clauses in `MobileRuntimeTestsAutostart` on the stack's bottom PR. The first response was an in-thread "intentional, no change" reply that left the threads open, and the reviewer had to ask again. CodeQL's `cs/catch-of-all-exceptions` exempts exactly two shapes — a `when` filter, or a bare `throw;` inside the block — so "specific catches first, generic last" (`AGENTS.md` §8) does not silence it, and a reply does not close it.

**Correct pattern:** first look for the version of the code that does not need the catch at all: observe a fire-and-forget task through a continuation instead of a catch-all; fold duplicated dispatcher marshalling into one helper so the unavoidable catch exists once. Where a generic catch *is* the contract — forwarding everything to a `TaskCompletionSource` — say so in a code comment at the site, reply once with the reason, and resolve the thread.

**Apply to:** every bot review on this repo. A thread left open reads as unaddressed to the next reviewer, whatever the reply underneath it says.

## Retrying the `Build Tests` stage cannot succeed — push a new head instead

**Problem:** a flaky screenshot test in `UI Tests - WebAssembly` turned #3160's build red, and the stage was retried (twice). Every retry failed *harder*: the three `Build Runtime Tests - {Android,iOS,WebAssembly}` jobs re-ran, rebuilt fine, and then died in `PublishPipelineArtifact` with `Artifact android-runtime-tests already exists for build 229940`. Pipeline artifacts are immutable per build, the head builds publish under fixed names because the device test stages download them by those names, and a stage retry re-executes the publish step. The retry therefore reports a compile-looking failure on jobs that had already succeeded in attempt 1.

**Correct pattern:** for a flake in this pipeline, do not retry the stage. Push a new head (an empty rebase onto `main` is enough) so a fresh build id gets fresh artifact slots. If a retry has already happened, read the failed task's name before diagnosing: `PublishPipelineArtifact` + "already exists" is the retry, not the code.

**Apply to:** any stage that publishes named pipeline artifacts consumed downstream (`stage-build-runtimetests-mobile.yml`, the runtime-test lanes). Making the publish step idempotent would need attempt-suffixed names on both the publish and the download side.

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

## A package's TFM folder name is not evidence of what its assembly references

**Problem:** bumping `Uno.Sdk` 6.0.67 -> 6.8.0-dev.21 for an `Uno.WinUI` fix also moved
`Uno.Toolkit.WinUI` 7.0.2 -> 9.2.0-dev.18, because the Sdk pins that too. `samples/Directory.Packages.props`
already pinned the toolkit to 8.4.2 with a comment saying newer builds are compiled against
Microsoft.iOS 26 while the .NET 9 iOS workload here references 18.2 (CS1705). I checked that Toolkit
9.2.0-dev.18 ships a folder named `lib/net9.0-ios18.0`, concluded the pin was stale, removed it - and
wrote that conclusion into the props file as justification. CI then failed the Packages job with
exactly the CS1705 the comment predicted: the folder name is the TFM the package *declares*, while the
assembly inside references `Microsoft.iOS 26.0.0.0`.

**Correct pattern:** to test "is this pin still needed", read the assembly's references, not the lib
folder name. And treat an existing pin's comment as a claim to *disprove with the same evidence it
cites* - here the cited evidence was a compiler error on an iOS build, which cannot be reproduced on a
Windows host at all, so the honest move was to keep the pin and let CI speak. Carrying the same pin
into `src/Directory.Packages.props` fixed it, and because Toolkit 8.4.2 still ships a maccatalyst
flavor containing `NativeFramePresenter`, it also made a breaking TFM removal unnecessary - the wrong
fix I had already applied and documented.

**Apply to:** any `global.json` SDK bump here. An SDK version is a bundle: list what it pins
(`targets/netstandard2.0/packages.json` in the Uno.Sdk package - `Core`, `Extensions`, `Toolkit`,
`Themes`, `WinAppSdkBuildTools`, `MsalClient`) and diff the groups, not just the one you came for. Also
note the local package build cannot compile `net9.0-ios` on a Windows host, so a clean local build says
nothing about the iOS TFM - and `dotnet build` cannot build the XAML-bearing WinUI libraries at all
(UNOB0008, true on the old SDK too), so validate a bump with `msbuild` or the first error misleads.
*(Superseded in part, 2026-08-23: the pin was itself masking a stale CI SDK — see the next lesson.
The evidence rule stands; the remedy it reached did not.)*

## A pin that makes CI green can be hiding a toolchain skew — fix the SDK, not the package

**Problem:** the CS1705 above (`Uno.Toolkit.WinUI` built against `Microsoft.iOS 26` vs. a workload
referencing 18.2) was not a Toolkit problem at all: CI was building with the .NET **9.0.200** SDK,
whose iOS workload is Microsoft.iOS 18.2, against an `Uno.Sdk` whose Toolkit is built for
Microsoft.iOS 26. Pinning `UnoToolkitVersion` 8.4.2 (and, to keep its dependencies consistent,
`UnoHotDesignVersion` 1.19.175, `UnoThemesVersion` 6.1.1 and `System.Text.Json` 8.0.5) made the
build pass by freezing three packages at the last versions the *stale* toolchain could compile —
a workaround recorded as a fix, in two `Directory.Packages.props` files and the eleventh-pass notes.

**Correct pattern:** a CS1705 in a package build names a mismatch between the package and the
*toolchain*, so the first question is which SDK and workloads the failing job actually used. The
fix is in `build/ci/templates/dotnet-install*.yml` (`DotNetVersion: '10.0.x'`, `UnoCheck_Version:
'1.34.1'`, plus the .NET 9 runtime for the net9.0 test heads) and `.azure-pipelines.yml` (Xcode
26.2 / iOS 26.2 simulator); the pins are removed. One property legitimately remains, in the root
`Directory.Build.props`: `UnoToolkitVersion` 9.2.0-dev.18. That is not a workaround — the app heads
(Playground, TestHarness, `Uno.Extensions.RuntimeTests`) build with `Uno.Sdk.Private`, whose
Toolkit group lags the public `Uno.Sdk`'s, so without one floor-sync property a head referencing
`Navigation.Toolkit` fails NU1605 against the libraries' higher floor. Verified locally: all four
heads restore with zero NU1xxx and `Navigation.Toolkit` builds for `net9.0-maccatalyst` and
`net9.0-ios` (catalyst via `_UnoExtensionsDropIosXamlOnCatalyst`, since Toolkit ships no catalyst
assembly from 8.5). iOS/Android lanes not yet re-run.

**Apply to:** any `src/Directory.Packages.props` / `samples/Directory.Packages.props` pin whose
comment says "newer builds don't compile on CI". Check the CI SDK version first; a pin is only
right when the *package* is wrong. And when `Uno.Sdk` and `Uno.Sdk.Private` disagree on a group
(Toolkit, Themes, Core), sync the floor in one place rather than per head.

## Any `*.WinUI` package whose platform assembly references `Uno.UI` and ships a plain `netX.0` lib is swapped on Skia-mobile heads — check the references, not the TFM list

**Problem:** spec 010 established the swap for `Uno.Extensions.Authentication.MSAL.WinUI`; what
was still a hypothesis was that `Uno.Extensions.Storage.UI` gets the same treatment, which would
put the Android KeyStore / iOS KeyChain stores out of reach on Skia heads. Reading the assembly
metadata settled it: `Uno.Extensions.Storage.UI.dll` for `net9.0-android` and `net9.0-ios`
references `Uno.UI`, and the package ships a `lib/net9.0` build, so
`RuntimeAssetsSelectorTask.HandleSkiaMobileForNonRuntimeEnabledPackages` replaces it. The default
`IKeyValueStorage` on Skia Android/iOS is therefore `ApplicationDataKeyValueStorage` — plaintext,
app-sandboxed — and it cannot be otherwise: KeyStore/KeyChain do not exist in the `net9.0` TFM.

**Correct pattern:** before assuming a platform store (or any platform-specific code path) exists
on a Skia mobile head, open the platform assembly with `System.Reflection.Metadata` and list its
assembly references; `Uno.UI` present + a plain-TFM sibling in the package = the plain build is
what runs. Where the plain build cannot do the platform thing, document it at the public surface
(`doc/Learn/Storage/StorageOverview.md` table + note, `HowTo-MsalAuthentication.md`) rather than
papering over it in code. Spec 010 item 8 (live device validation) is still the only proof that
the rest of the dispatch holds end to end.

**Apply to:** every `Uno.Extensions.*.UI` package: `Hosting.UI`, `Navigation.UI`, `Storage.UI`,
`Authentication.*.UI`. Each one's plain `netX.0` build is the *primary* runtime artifact on Skia
Android/iOS, so platform behavior belongs behind `OperatingSystem.Is*()` in that build, and the
plain build must never be a stub.

## A fake IdP must mint refresh tokens unique per instance — MSAL throttles by token hash, process-wide

**Problem:** after adding `Given_MsalAuthentication.When_RefreshTokenRejected_Then_SignedOutAndLoggedOutRaised`
(StubEntra answers the refresh with `400 invalid_grant`), the *next* test's silent acquisition
failed too, with no request reaching the stub. MSAL.NET's `UiRequiredProvider` throttles silent
requests for 120 s after an `invalid_grant`, process-wide, keyed on client id, authority, scopes
and the SHA-256 of the refresh token. `StubEntra` minted `stub-refresh-token-{counter}` in every
instance, so a fresh harness presented a refresh token byte-identical to the one just rejected and
was throttled without a network call. The failure moved between tests depending on order.

**Correct pattern:** refresh tokens from a test IdP carry a per-instance GUID (`_instanceId`), so
no two harnesses ever share one. More generally: a token-cache library has state that outlives
the `[TestMethod]` boundary *and* the MSAL cache purge — throttling caches are keyed on request
content, not on the cache you cleared. When a test starts failing only after a sibling ran, look
for client-side negative caching before looking at the fake server.

**Apply to:** `StubEntra` and any future fake IdP or token endpoint used across tests; also any
test that deliberately drives an `invalid_grant`/`interaction_required` response — follow it by
checking the next silent call still reaches the server.
## Runtime-test suites share process-global registries (2026-08-21, spec 013)

All `*.UI.Tests` suites run in one process inside the runtime-test head. A product-side
`ApiExtensibility.Register` (first-wins) triggered while ONE suite builds its host - e.g.
`AddOidc`/`AddWeb` registering the desktop `WebAuthenticationBroker` - is visible to every suite
that runs later: the Web suite's stub broker lost the race to the Oidc suite's host building and
all Web tests drove a real loopback listener. Two rules:

- A test seam that depends on winning a first-wins registration must be installed at **assembly
  load** (`[ModuleInitializer]`, with a justified CA2255 suppression - the banned-in-libraries rule
  exists for consumer-facing libraries, and a test assembly pre-empting product registration is the
  legitimate use), not in a harness or class initializer.
- Per-suite filter runs are not sufficient verification: always finish with a **combined run using
  the exact CI filter**, because cross-suite interference only shows up there.

## Silent no-ops hide DI failures; filtered build output hides failures (2026-08-21, spec 013 samples)

- A view whose click handlers null-check the view model (`if (_viewModel is { } vm)`) turns a
  navigation-time DI failure into "the button does nothing" with zero diagnostics. When the VM
  can't activate (here: `IHttpClientFactory` unregistered - `UseHttp` only adds the factory when
  clients are registered), the page still renders. When forking sample scaffolding, prefer failing
  loudly (throw or log) over silently ignoring a missing DataContext.
- Piping builds through `tail -1`/`grep "Time Elapsed"` reports success for a FAILED build - three
  debugging iterations ran against a stale binary before this was noticed. Gate on `"N Error(s)"`
  or the exit code, never on elapsed-time output. (Repeat offender this session: `grep|head` exit
  codes; see the runtime-test filter entry above.)

## `git cherry` cannot see a commit main absorbed under a different message (2026-08-26, spec 013)

**Problem:** `dev/sb/auth-providers-fixes` was 36 commits behind `main` with 90 of its own, and about
forty of those had already reached `main` through the split PRs carved out of it - reworded and
squashed on the way in. `git cherry main <branch>` marked all 90 as `+` (not upstream), because it
compares patch-ids and a reword changes nothing about the patch but a squash changes everything. Read
literally, that says "the branch has 90 commits to replay". `git rebase main` then conflicted on
commit 1 of 90 across six files, replaying an MSAL fix `main` had already shipped in refined form -
and the failure mode of pushing through is not a mess, it is silently reverting `main`'s newer version
of every overlapping file.

**Correct pattern:** before rebasing a long-lived branch whose work has been split upstream, find the
boundary rather than trusting `git cherry`. `git log --reverse --format='%h %ad %s'` over
`main..branch` and match subjects against `merge-base..main` by hand: here the branch's own history
was chronological, so commits 1-69 were the split-out work and 70+ were the genuinely new commits. A
21-commit cherry-pick of that tail onto `main` produced a clean linear branch with six conflicts, all
of them real (a CI filter to union, a wasm filter where `main` was newer, a refactor to graft onto, a
duplicate `.sln` entry to drop).

**Apply to:** any branch that has had PRs split out of it. Also worth knowing:
- The overlap is *not* symmetric. Take `main`'s side where it refined the same code (the
  `MsalSecureStore` refactor), the branch's side where it is new (the persistence check), and the
  union where both added independently (CI filters, `lessons.md`).
- Two projects with the same name in a `.sln` is a hard MSBuild stop (MSB5004), not a warning, and
  `dotnet build`'s output does not obviously say which solution or GUIDs are involved - `grep` the
  name in the `.sln` and compare against `main`'s GUID.
- Spec numbers collide when a spec is renumbered during a split. `main`'s copy keeps its number; the
  branch's renumber has to carry the "spec NNN" references in source comments with it. Blanket
  replacement is not safe: an unrelated spec here referenced a *planned* "spec 013" that means
  something else entirely.
