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
