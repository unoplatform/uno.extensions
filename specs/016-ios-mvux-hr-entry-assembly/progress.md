# iOS: MVUX hot reload and the entry assembly (#3240)

Issue: https://github.com/unoplatform/uno.extensions/issues/3240

## Findings

### The 5 MVUX failures: root cause confirmed

On the iOS simulator (Mono interpreter, Debug), `Assembly.GetEntryAssembly()` is `null` while module initializers run and only becomes non-null once `Main` starts. Temporary `Console.WriteLine` diagnostics (os_log):

```
14:50:59.635 ConfigureHotReload module=Uno.Extensions.RuntimeTests isEnabled=True cachedEntry=<null> liveEntry=<null> isApp=True tid=1
14:50:59.640 Main liveEntry=Uno.Extensions.RuntimeTests
             ConfigureHotReload module=Uno.Extensions.Reactive.WinUI.Tests isEnabled=True cachedEntry=<null> liveEntry=Uno.Extensions.RuntimeTests isApp=False
             StartIfRequested liveEntry=Uno.Extensions.RuntimeTests effectiveHR=Enabled
```

`ModuleFeedConfiguration._entryAssembly` is a `static readonly` captured when the type initializes, which is inside the app's `[ModuleInitializer]`, so it is `null` on iOS. Before #3235 the app module never matched and `EffectiveHotReload` stayed `Disabled`. That is the same symptom as Android (unoplatform/uno#25417), for a different reason: Android has no entry assembly at all, while iOS has one that isn't populated yet.

#3235's first-caller rule already covers iOS: the entry assembly's module initializer runs before `Main`, so it runs before any library's initializer and wins.

Side note: the issue's suggested logging recipe can't show `Module '<name>' has enabled...`, because that message is logged from the module initializer, before `Main` can call `InitializeLogging()`.

### The hang: not reproduced locally

| Run | Code | Simulator | Result |
|---|---|---|---|
| Reactive `Given_HotReload` | `dev/sb/mobile-hr-lanes` (has #3235) | iOS 18.5 | 14/14, including the 5 |
| CI-shaped filter, 5 included | same | iOS 18.5 | 41/41 |
| CI-shaped filter, 5 included (repeat) | same | iOS 18.5 | 41/41 |
| Draft #3239's own filter | `6acbf9ca1` (exact hung CI commit) | iOS 18.5 | 42/42, all 3 launches handshake |

CI's hung builds 239619 and 239628 ran draft #3239, whose base lacks `3443f8683` (lane stabilization). That commit doesn't touch the handshake, so it isn't a root cause. The exact hung commit completes locally, so the hang is specific to the CI environment (iOS 26.1, hosted agent); its cause is unknown. Build 239767 (`main` with #3235, 5 excluded) is green on CI, and re-enabling the 5 tests is the next CI data point.

### iOS 27 simulator: unrelated launch crash

On iOS 27.0, the app is killed at launch in `_UIApplicationEvaluateRuntimeIssueForNoSceneLifecycleAdoption` (EXC_BREAKPOINT): UIKit rejects apps that haven't adopted `UIScene`. CI (iOS 26.1) doesn't hit this, but it will once CI moves to Xcode 27. Known issue, outside this change.

## Plan

- [x] `ModuleFeedConfiguration.cs`: comment says iOS also has a `null` entry assembly during module initialization (no logic change).
- [x] `Given_ModuleFeedConfiguration.cs`: test comment mentions iOS.
- [x] `PlatformHelper.cs` remarks: note that iOS/Mono returns `null` from `GetEntryAssembly()` during module initialization.
- [x] `build/ci/stage-runtime-tests-mobile-hotreload.yml`: remove the five `When_*` exclusions from `IosTestFilters` (keep #3242 and #3245).
- [x] RuntimeTests head: call `App.InitializeLogging()` from `Main.iOS.cs` and the Android `Application`, so the next CI failure leaves logs.
- [ ] CI: iOS hot-reload lane green with the 5 included.

## Review

- `Given_ModuleFeedConfiguration`: 2/2 passing (the first-caller rule from #3235 is unchanged and already covered).
- RuntimeTests head builds for `net10.0-ios` and `net10.0-android`.
- Final local run of the updated `IosTestFilters` (iOS 18.5 simulator): 41/41, including the 5 MVUX tests.
- Pending: the iOS hot-reload lane on CI with the 5 tests included.
