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

## A test filter naming a class silently drops its siblings — scope device lanes by namespace

**Problem:** `.azure-pipelines.yml` set `RuntimeTestsFilter: 'Given_MsalAuthentication'` to scope the four device runtime-test stages away from the 15 pre-existing failures in the wider suite. `Uno.UI.RuntimeTests.Engine`'s filter is a plain substring match on the fully-qualified test name, so that string matched exactly one class. The 8 `Given_BrowserTokenCacheStorage` cases added in the same branch — the entire guard on which browser store the token cache lands in — matched nothing and never ran on any lane. Both device lanes were green and the check list read "Runtime Tests - Desktop (Skia): pass", so the gap was invisible; it surfaced only because a human read the check names and asked which platforms actually run the auth tests.

**Correct pattern:** scope a substring filter by **namespace**, not class — `'Authentication.MSAL.UI.Tests'` picks up all 23 cases and keeps picking up classes added later. When a filter is narrowed to dodge unrelated failures, assert the resulting *count*: run the filter locally and confirm it matches the tests you think it does, because a filter that matches too little fails open, not closed.

**Apply to:** `RuntimeTestsFilter` and any `dotnet test --filter` / engine filter in CI. The general shape — a green lane that silently ran a subset — also applies to VSTest `**/*.Tests.dll` globs and `[TestCategory]` selectors. `TreatNoTestsAsError` in `build/tests.runsettings` catches zero matches; nothing catches "half".
