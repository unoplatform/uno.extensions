# Uno Platform 7.0 retarget

Tracking issue: `unoplatform/uno-private#2072`.
Reference implementation for a sibling repo: `unoplatform/Uno.Themes#1722`.

## Goal

Build and ship Uno.Extensions against Uno Platform 7.0: retarget to the 7.0 packages and the
Skia-first target set, fix the breaking-change fallout, then publish a 7.0-compatible preview
followed by a stable release.

## What Uno 7.0 changes for this repo

Uno Platform 7.0 renders with Skia on **every** platform. The consequence that shapes most of
this work: `Uno.WinUI` 7.0 ships library assets for `net10.0`, `net11.0`,
`net10.0-windows10.0.19041.0` and `net11.0-windows10.0.19041.0` — and nothing else. There is no
ios/android/maccatalyst/browserwasm flavour of the UI assembly to bind against any more.

App heads still target the platform they run on. Libraries and heads therefore need different
target-framework lists, which is why `tfms-ui-winui.props` was split (see below).

Mac Catalyst is gone entirely. The 7.0 SDK declares Android, iOS, tvOS, Desktop, Wasm and Windows
platform folders and no longer recognizes `Platforms/MacCatalyst`, so files left there leak into
the default compile glob of every target framework.

## Target versions

| Component | Version | Source of truth |
| --- | --- | --- |
| `Uno.Sdk.Private` | `7.0.0-dev.701` | `global.json`; no public `Uno.Sdk` 7.x exists yet |
| TFM (libraries) | `net10.0`, `net10.0-windows10.0.19041` | `src/tfms-ui-winui.props` |
| TFM (app heads) | + `-ios`, `-android`, `-desktop`, `-browserwasm` | `src/tfms-ui-winui-apps.props` |
| `Microsoft.Extensions.*` | `10.0.12` | MAUI 10 pulls 10.0.0 transitively |
| `Microsoft.Maui.Controls` | `10.0.90` | the SDK's own `packages.json` |
| `SkiaSharp` | `4.151.1` | the SDK's own `packages.json` |
| `Microsoft.WindowsAppSDK` | `2.4.0` | `Uno.WinUI` 7.0 nuspec floor |
| `Uno.Core.Extensions.Logging` | `5.0.0-dev.28` | required by `Uno.UI.Adapter.*` 7.0 |
| .NET SDK (CI) | `10.0.102` | matches the Uno 7 sibling repos |

Read versions from `D:\Packages\NuGet\uno.sdk.private\<version>\targets\netstandard2.0\packages.json`
rather than copying them between repos — the sibling repos move on their own timelines.

## Status

### Done — the package surface is green

`Uno.Extensions-packageonly.slnf` builds with **0 errors** and produces all 38 packages, and the
whole unit-test layer passes (1,613 tests, including 1,433 Reactive tests).

Fallout fixed:

- **.NET 10 `System.Linq.AsyncEnumerable`** collides with the `System.Linq.Async` package: 104
  CS0121 + 11 cascading CS0149 in `Uno.Extensions.Reactive`. The package is dropped, the four
  Ix.NET-only APIs still in use are re-homed into the internal `AsyncEnumerableExtensions`, and
  `MoveNextAsync(ct)` comes back as `AsyncEnumeratorExtensions`.
- **BC53** — `Uno.UI.Toolkit` is renamed to `Uno.UI.Extras`, but the types move to their natural
  namespaces rather than traveling as a block. `StorageFileHelper` is now `Uno.Storage.StorageFileHelper`.
  Note that `using:Uno.UI.Extras` **compiles and resolves nothing** — the namespace exists, the
  types are not in it.
- NU1510 on `System.Collections.Immutable` / `System.Text.Json` /
  `System.Threading.Tasks.Extensions`, which are framework-provided on net10.0.

### Done — the app heads and UI tests build

The upstream block is cleared: `Uno.Toolkit.WinUI` `11.0.0-dev.90` and `Uno.Material`/`Uno.Themes.WinUI`
`9.0.0-dev.16` are published against `Uno.WinUI` 7.0.0-dev.701, which is exactly what `global.json`
pins. Both stay pinned explicitly — `Uno.Sdk.Private` 7.0.0-dev.701 still defaults its Toolkit group
to 9.1.0-dev.2 and Themes to 7.1.0-dev.1, both on the Uno 6 line.

Verified locally: `Uno.Extensions-runtimetests.slnf` (desktop), `Playground.sln` (desktop + Windows),
`TestHarness.sln` (desktop + Windows), and the Android head of every app all build with 0 errors. The
wasm head compiles clean; only the native-asset link step needs the `wasm-tools` workload, which CI
installs. iOS is not buildable on a Windows host.

Fallout fixed:

- **Hot Design and MCP support are off repo-wide** (`UnoDisableHotDesign` / `UnoDisableMCPSupport` in
  the root `Directory.Build.props`). Neither has an Uno 7 build: `Uno.UI.HotDesign` 1.23.0-dev.174 is
  net9.0 against Uno 6.8 and drags `Uno.Toolkit.WinUI` 9.1.3 with it, and `Uno.UI.App.Mcp` 2.0.0-dev.4
  is net9.0 against SkiaSharp 3.119 — its `GlobalStaticResources` initializer threw
  `FileNotFoundException` for `SkiaSharp.Views.Windows` before `OnLaunched` ran, killing the
  runtime-test host at startup. Disabling Hot Design does **not** cost XAML hot reload: the dev-server
  processor that applies it is Uno's own, and the hot-reload suite still passes without it.
- **The runtime-test engine moves to 2.0.0-dev.81.** Uno 7 moved `PointerPointProperties` from
  `Windows.UI.Input` to `Microsoft.UI.Input`, and dev.79's `InputInjectorHelper` hard-casts to the
  Windows type. That cast runs in `CleanupPointers` after every test, so the desktop lane reported 136
  failures over 105 cases. dev.81 reads the button states reflectively and is otherwise identical;
  dev.85 and later require MSTest 4.x, which this repo is not on.
- **Android heads override `CreateHost()`.** `Microsoft.UI.Xaml.NativeApplication` is abstract in
  Uno 7 and `CreateHost()` is the Android equivalent of `Main` (Android has no managed entry point).
  `ConfigureUniversalImageLoader` goes with it — `Com.Nostra13.Universalimageloader` is the non-Skia
  image pipeline and is no longer referenced.
- **TestHarness loses its platform key-value stores.** `Uno.Extensions.Storage.UI` builds for net10.0
  and net10.0-windows only, so `KeyStoreKeyValueStorage` / `KeyChainKeyValueStorage` exist in no
  assembly a head can see. `TestingKeyValueStorage` now derives from `ApplicationDataKeyValueStorage`
  everywhere, which is what the Skia mobile heads already resolved at runtime.
- **AndroidX pins rise to the floors MAUI 10.0.90 requires** (Browser 1.8.0.11, Navigation 2.9.2.1,
  SwipeRefreshLayout 1.1.0.29, Material 1.12.0.5); below them the MauiEmbedding Android head failed
  NU1605 on seven packages.

### Fixed along the way

- CI pins the .NET SDK to `10.0.101`, the band `uno.check` 1.34.1 provisions `wasm-tools` for; on
  `10.0.102` every `net10.0-ios` project failed `NETSDK1147` on Windows agents.
- iOS lanes select Xcode 26.1.1: .NET for iOS 26.1 refuses Xcode 16.4.
- The RuntimeTests head pins `Xamarin.AndroidX.Lifecycle.Process` 2.10.0.2 (NU1608 against the
  `Lifecycle.Runtime` version Uno 7's AndroidX pins require).
- `ModalFlyout` no longer carries `ios:`/`android:` templates using Toolkit's `NativeFramePresenter`,
  which Toolkit 11 removes.

### Runtime-test state

**Run the runtime tests on Linux, not on a Windows host.** CI runs every runtime-test lane on
`ubuntu-24.04` under xvfb, and on main all of them are green. A Windows host running the Win32 Skia
backend is a different story: 12 desktop tests fail there on the Uno 6 merge base (`945312137`) just
as they do on this branch — `Given_ChainedGetDataAsync` (7 of 13), the three `*_ComboBox` cases and
`When_PreselectedItem_SelectedItems_ListView` in `Given_BindableCollection_Selection`,
`Given_NavigatorStartup.When_DefaultRouteConfigured_Then_NavigationSucceeds`, and
`Given_RouteNotifier.When_NavigateBack_Then_RouteChanged_Has_Route`. Identical on both sides, so they
are host-specific, not retarget fallout — but it does mean a local Windows run is not a usable signal
for this lane.

Two things did change:

- `Given_Region.When_ResetLogger_WithDifferentInstance_Then_CurrentLoggerKept` is fixed here.
  `NullLoggerFactory.CreateLogger(name)` returns the shared `NullLogger.Instance` whatever name it is
  given, so the test's "running" and "stopped" host loggers were the same object.
- **Uno 7 segfaults on the 9th window** on a Win32 host whose GPU cannot create a Vulkan device.
  `Given_ChainedGetDataAsync` opens one `new Window()` per test and never closes it, so the process
  dies mid-class with no results file at all — a hard job failure rather than a test failure. Whether
  the X11 host on a headless agent takes the same path is unverified. Recorded for upstream.
- **XAML hot reload applies nothing on a Windows host.** All 16 `*ViaXamlHR*` cases fail while every
  C#-only hot-reload case passes. The delta is compiled and sent, `ClientHotReloadProcessor` logs
  `Deltas applied.`, then `HotReloadAgent.GetMetadataUpdateTypes` finds none of the updated types in
  the running app and `UpdateApplication` is called with an empty `Type[]` — "Invalid metadata update,
  ignore it." — so the visual tree is never re-applied. This is Uno core, before any Hot Design code
  could participate, which contradicts the old pin comment claiming Hot Design's dev-server processor
  is what applies XAML hot reload. Same Windows-host caveat as above: confirm against the CI lane.

### Known, not yet addressed

- **BC14** — `UserControl`/`Page` reparent onto `Control`, so
  `ApplicationBuilderExtensions.NavigateAsync<TShell>()`'s `as ContentControl` cast silently fails
  for any Shell that does not implement `IContentControlProvider`, giving a permanent blank screen.
  The maintained template and this repo's own samples implement it, so they are unaffected. Needs a
  runtime-verified fix plus a regression test.
- **BC50** — `PrimaryLanguageOverride` is restart-to-apply, which is exactly what
  `LocalizationService.SetCurrentCultureAsync` depends on for live language switching. Produces no
  compile signal; needs a runtime test to confirm.
- `Uno.UI.RuntimeTests.Engine` breaks MSIX packaging on the Windows leg of the RuntimeTests head
  (APPX0002, an absolute path concatenated into a relative one). CI does not build that leg.
- `Uno.Extensions.Reactive` still ships a `Uno.Toolkit` 7.0.7 dependency with no visible consumer in
  the package. It resolves upward next to Toolkit 11, but it is a stale floor to carry into a 7.0
  release.
- Version policy for the 7.0 line is unsettled. `version.json` still reads `7.4-dev.{height}`;
  sibling repos took a major bump (Themes to 9.0, Toolkit to 11.0). This is a release decision.

## Local build notes

`dotnet build` cannot build a WinUI-flavoured project that has XAML pages (UNOB0008 — the SDK
refuses it by design). Use MSBuild, which is what CI's `MSBuild@1` task does:

```powershell
& "$(vswhere -latest -property installationPath)\MSBuild\Current\Bin\MSBuild.exe" `
    Uno.Extensions-packageonly.slnf -r -m -v:m -p:Configuration=Release
```

`dotnet test` is still the right runner for the unit-test layer, which has no XAML.
