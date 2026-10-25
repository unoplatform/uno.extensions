# Packages still reference `Uno.UI.Lottie` on Uno.Sdk 7.0.0-dev.89 (#3262)

## Findings

Recommended template apps on Uno.Sdk 7.0.0-dev.89 (Material, MVUX, desktop) never show a window. Reproduced in Debug and Release:

```text
System.IO.FileNotFoundException: Could not load file or assembly 'Uno.UI.Lottie, Version=255.255.255.255, ...'
   at Uno.Extensions.Navigation.Toolkit.WinUI.GlobalStaticResources.Initialize()
   at QaApp.GlobalStaticResources.Initialize()
   at QaApp.GlobalStaticResources..cctor()
```

No resource in this repo uses Lottie. The Uno XAML generator makes a library's `GlobalStaticResources.Initialize()` call `Initialize`, `RegisterDefaultStyles` and `RegisterResourceDictionariesBySource` on the `GlobalStaticResources` of **every referenced assembly that has XAML**, transitive ones included. Our packages were built against Toolkit 11.0.0-dev.140, which depended on `Uno.WinUI.Lottie`, so `Uno.UI.Lottie` was in that set and the calls are compiled into:

- `Uno.Extensions.Navigation.Toolkit.UI.dll` (8.0.0-dev.101, .118, .123),
- `Uno.Extensions.Reactive.UI.dll` (8.0.0-dev.118).

Since unoplatform/uno#25117 Lottie is part of `Uno.WinUI`, and Toolkit 11.0.0-dev.155 / Themes 9.0.0-dev.56 (pinned by Uno.Sdk 7.0.0-dev.89) no longer depend on `Uno.WinUI.Lottie`. Nothing ships `Uno.UI.Lottie.dll`, and the JIT fails on the first library initializer that names it. The app calls them alphabetically, so `Navigation.Toolkit` fails first and `Reactive.UI` would fail next.

- The Fluent template works only because its `UnoFeatures` include `Lottie`, which still pulls `Uno.WinUI.Lottie` 7.0.0-dev.1574.
- In Debug, `Uno.UI.HotDesign*` 2.0.0-dev.31 and `Uno.UI.App.Mcp.Client` 2.0.0-dev.16 make the same calls. They are not built here; a Release build references only our two packages.

Toolkit .155 and Themes .56 require `Uno.WinUI` >= 7.0.0-dev.2285, above our `Uno.Sdk.Private` 7.0.0-dev.2162, so the SDK moves with them. `Uno.Sdk.Private` 7.0.0-dev.2428 is the `Uno.WinUI` that Uno.Sdk 7.0.0-dev.89 uses; its `packages.json` differs from 2162 only by adding `Uno.WinUI.Graphics3DWebGpu`, and it still defaults Toolkit/Themes to .140/.41, so the explicit pins stay.

## Plan

- [x] `global.json`: `Uno.Sdk.Private` 7.0.0-dev.2428.
- [x] `src/Directory.Packages.props` and `samples/Directory.Packages.props`: Toolkit 11.0.0-dev.155, Themes 9.0.0-dev.56.
- [x] Release build of `Uno.Extensions-packageonly.slnf`, 0 errors.
- [x] No packed assembly references `Uno.UI.Lottie`; nuspecs depend on Toolkit .155 / Uno.WinUI 2428.
- [x] The #3262 repro app (Uno.Sdk 7.0.0-dev.89, Material, Release) starts with the rebuilt packages.

## Out of scope

- HotDesign and App MCP rebuilds (Debug builds of the template stay broken until they ship).
- The generator emitting hard calls into transitive references, which makes every Uno library break when a dependency drops a package (unoplatform/uno).
- A regression guard against our build-time Toolkit drifting from the version the SDK pins.

## Review

- `Uno.Extensions-packageonly.slnf`, Release, VS MSBuild: 0 errors, 38 packages. The only warnings are CS1591, all from the 14 projects that list it in `WarningsNotAsErrors`, none in generated code.
- Unit-test layer (`dotnet test` on the Release build, `FullyQualifiedName!~UI.Tests`): all pass, Reactive 1492 passed / 19 skipped as on `main`.
- Playground desktop head (samples pins), VS MSBuild with only `Build_Desktop` on: builds with no NuGet or compile warnings, resolves Toolkit 11.0.0-dev.155 and Themes 9.0.0-dev.56.
- All 93 assemblies under `lib/` of the 38 packages: none references `Uno.UI.Lottie` (the same scan finds it in the published 8.0.0-dev.118 `Navigation.Toolkit` and `Reactive.UI`). `Navigation.Toolkit.WinUI` and `Reactive.WinUI` nuspecs depend on Toolkit 11.0.0-dev.155 and Uno.WinUI 7.0.0-dev.2428.
- The #3262 repro app (`dotnet new unoapp -preset recommended -presentation mvux -theme material`, Uno.Sdk 7.0.0-dev.89) with `UnoExtensionsVersion` pointed at the local packages:
  - Release: restores and builds with 0 warnings; no assembly in the output has an unresolved reference; window in about 2 s on 3 of 3 runs, main page shown ("Enter your name:", "Go to Second Page"). With 8.0.0-dev.118 the same app shows no window after 30 s.
  - Debug: still no window. It now fails in `Uno.UI.App.Mcp.Client.GlobalStaticResources.Initialize()`; App MCP 2.0.0-dev.16 and HotDesign 2.0.0-dev.31 still reference `Uno.UI.Lottie` and are not built here.
