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
platform folders and no longer recognises `Platforms/MacCatalyst`, so files left there leak into
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
whole unit-test layer passes (1,608 tests, including 1,431 Reactive tests).

Fallout fixed:

- **.NET 10 `System.Linq.AsyncEnumerable`** collides with the `System.Linq.Async` package: 104
  CS0121 + 11 cascading CS0149 in `Uno.Extensions.Reactive`. The package is dropped, the four
  Ix.NET-only APIs still in use are re-homed into the internal `AsyncEnumerableExtensions`, and
  `MoveNextAsync(ct)` comes back as `AsyncEnumeratorExtensions`.
- **BC53** — `Uno.UI.Toolkit` is renamed to `Uno.UI.Extras`, but the types move to their natural
  namespaces rather than travelling as a block. `StorageFileHelper` is now `Uno.Storage.StorageFileHelper`.
  Note that `using:Uno.UI.Extras` **compiles and resolves nothing** — the namespace exists, the
  types are not in it.
- NU1510 on `System.Collections.Immutable` / `System.Text.Json` /
  `System.Threading.Tasks.Extensions`, which are framework-provided on net10.0.

### Blocked upstream — app heads and UI tests

Every app head and UI-test surface fails to build, all through the same root cause: packages built
against Uno 6 still reference the `Uno` assembly, which 7.0 no longer ships. It surfaces as
`CS0012: The type 'CoreDispatcher' is defined in an assembly that is not referenced ... 'Uno'`
out of the `BindableTypeProviders` generator.

| Surface | Blocked by | Upstream |
| --- | --- | --- |
| Playground, TestHarness | `Uno.Toolkit.WinUI` 8.4.2, `Uno.Material`/`Uno.Themes.WinUI` 6.1.1 | `uno.toolkit.ui#1635`, `Uno.Themes#1722` |
| RuntimeTests head | `Uno.Toolkit.WinUI`, via `Uno.Extensions.Navigation.Toolkit` | `uno.toolkit.ui#1635` |
| The three `*.Markup` packages | `Uno.WinUI.Markup` — builds, but UNOB0020 says it will fail at runtime | `uno.csharpmarkup#906` |

None of these can be worked around here: all three are locked to a pre-7.0 `Uno.WinUI` at the
nuspec dependency-group level. The `*.Markup` packages are the subtle case — they build green, so
only the UNOB0020 warning distinguishes "works" from "will throw on first use".

### Known, not yet addressed

- **BC14** — `UserControl`/`Page` reparent onto `Control`, so
  `ApplicationBuilderExtensions.NavigateAsync<TShell>()`'s `as ContentControl` cast silently fails
  for any Shell that does not implement `IContentControlProvider`, giving a permanent blank screen.
  The maintained template and this repo's own samples implement it, so they are unaffected. Needs a
  runtime-verified fix plus a regression test.
- **BC50** — `PrimaryLanguageOverride` is restart-to-apply, which is exactly what
  `LocalizationService.SetCurrentCultureAsync` depends on for live language switching. Produces no
  compile signal; needs a runtime test to confirm.
- `Uno.UI.RuntimeTests.Engine` 2.0.0-dev.79 breaks MSIX packaging on the Windows leg of the
  RuntimeTests head (APPX0002, an absolute path concatenated into a relative one). CI does not build
  that leg.
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
