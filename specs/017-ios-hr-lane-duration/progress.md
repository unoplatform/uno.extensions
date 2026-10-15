# iOS hot-reload lane duration (#3238)

## Findings

The iOS hot-reload job took 26-44 min on #3238 (Android: ~16 min). The test bodies are only ~7 min of that. From builds 239608, 239648, 239758, 239790 and 239810:

| Phase | Typical | Note |
|---|---|---|
| `simctl install` | 2.5-5 min | Blocked on the fresh simulator's CoreSimulator data migration, which continues after "Booted". |
| DevServer workspace load, launch 1 | 5-6 min | 10.5 min when its recovery `dotnet restore` hit the 120s timeout (239810, 239790 attempt 1), past the app's 10-min wait. |
| DevServer workspace load, launch 2 | 2-4 min | The DevServer creates a new workspace for every connection. |
| Tests, both launches | ~7 min | |

### Root cause of the slow workspace load: a leading `;` in `TargetFrameworks`

`tfms-ui-winui.props`, `tfms-ui-winui-apps.props` and `tfms-ui-maui.props` build the list as `$(TargetFrameworks);tfm`, so every UI project evaluates to `;net10.0-ios;net10.0`. MSBuild skips the empty entry. Roslyn's `MSBuildWorkspace`, which the DevServer uses, does not: `ProjectBuildManager` splits with `Split(';')` and builds every entry, so each UI project is also loaded with `TargetFramework=""`, i.e. its cross-targeting outer build (`Project does not contain 'Compile' target`). For the head this gives a flavor with no framework references, and the DevServer reacts by running `dotnet restore` and reloading the whole workspace (Uno spec 049). The flavor is still broken after the restore, so every launch on every platform paid for two full workspace loads plus a restore.

Local measurement, iOS 18.5 simulator, 2 launches of 1 test each:

| | Before | After |
|---|---|---|
| Workspace ready, launch 1 | 30.0s | 18.7s |
| Workspace ready, launch 2 | 43.6s | 20.0s |
| `no 'Compile' target` workspace failures | 80 | 0 |
| Recovery restore + reload | 2 | 0 |
| Loaded flavors | `<unresolved>`, net10.0-ios, net10.0 | net10.0-ios, net10.0 |

## Plan

- [x] Trim the leading `;` in the three TFM props (no change to the TFM set or order).
- [x] `ios-simulator-runtime-tests.sh`: `SIMULATOR_BOOT_ONLY=true` selects and boots the simulator, then exits.
- [x] Hot-reload lane: boot the simulator right after Xcode selection, so its data migration overlaps the build.
- [x] Correct the comments that said only the first launch loads the workspace, and that install doesn't wait.
- [ ] CI: iOS and Android hot-reload lane durations, once these commits run on #3238 or `main` (a PR into `dev/sb/mobile-hr-lanes` gets no Azure run).

## Out of scope

- On macOS, `Set Cache for dotnet install` (227-390s on a hit) costs about as much as the miss path's workload install (303-456s). Shared template `templates/dotnet-install.yml`.
- The DevServer can't reuse a workspace across app launches, and the lane needs two launches (deltas accumulated in one Mono process end in `BadImageFormatException`).

## Review

- TFM evaluation: `net10.0-ios;net10.0` (iOS-only `DebugPlatforms.props`), `net10.0-ios;net10.0-android;net10.0-desktop;net10.0-browserwasm;net10.0` (head, all platforms), `net10.0-ios` with `UnoTargetFrameworkOverride`.
- `dotnet build Uno.Extensions-packageonly.slnf -c Release`: 0 errors. The CS1591 warnings are already on the base branch (2240 on a full rebuild without this change).
- Local iOS: boot-only mode leaves the simulator booted and the following run reuses it; 2/2 on the small filter.
- Local iOS, the lane's full `IosTestFilters` after a boot-only start: 34/34 passed, workspace ready 18.8s and 17.3s after each launch, 0 `no 'Compile' target` failures, no recovery restore.
