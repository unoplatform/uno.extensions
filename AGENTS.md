# AI Agents Contribution & Coding Instructions
<!-- cspell:ignore PKCE -->

This document defines strict guardrails for any AI-assisted or automated agent contributions (including Copilot, custom prompt runners, or scripted refactors) working in the **Uno.Extensions** repository. Human contributors must also ensure generated changes comply before merge. It is the single source of truth for repo-wide orientation *and* the rules agents must follow; `CLAUDE.md` at the repo root is a thin pointer that includes this file.

<repository_orientation>

## Repository overview

`Uno.Extensions` is a multi-package library that layers Microsoft.Extensions-style hosting on top of Uno Platform / WinUI to provide Authentication, Configuration, DI, Hosting, Http, Localization, Logging, Navigation, Reactive (MVUX), Serialization, Storage, Toolkit and Validation extensions. ~50 NuGet packages are produced from `src/Uno.Extensions.*`.

UWP support has been dropped. The repo targets the **Uno.Sdk** (see `global.json` — currently `Uno.Sdk` 6.0.x and an internal `Uno.Sdk.Private`). Versioning is driven by **Nerdbank.GitVersioning** (`version.json`); never bump `<Version>` in csproj files — the `255.255.255.255` value in `src/Directory.Build.props` is intentional (NuGet replaces it at pack time from `version.json`).

The deliverable is a **public NuGet API consumed by external apps**. Stability matters more than in an app-only codebase.

## Solution layout

Three solution filters cover the common scenarios:

| File | Purpose |
| --- | --- |
| `Uno.Extensions.sln` | Full solution (only opens cleanly on Windows with all VS workloads). |
| `Uno.Extensions-packageonly.slnf` | What CI builds for NuGet — no WinUI test/runtime-test heads. Use this for fast local builds. |
| `Uno.Extensions-runtimetests.slnf` | Adds the WinUI test/runtime-test heads (`Uno.Extensions.RuntimeTests`, `*UI.Tests`). |
| `Uno.Extensions.Reactive.slnf` | Just the Reactive (MVUX) projects + their tests. Fastest loop when iterating on MVUX. |

Folder layout:

```text
src/                          The 50 published packages, plus generators and test projects.
  Uno.Extensions.<Area>/      Cross-platform "core" of an area (e.g. Reactive, Navigation, Authentication).
  Uno.Extensions.<Area>.UI/   WinUI/Uno-flavored host with .csproj named *.WinUI.csproj.
  Uno.Extensions.<Area>.Tests/        Plain net9.0 unit tests (run by package CI via dotnet test / VSTest).
  Uno.Extensions.<Area>.UI.Tests/     UI tests that require an Uno runtime — run by runtime-test stages, NOT by package CI.
  Uno.Extensions.<Area>.Generators/   Roslyn source generators (referenced via OutputItemType="Analyzer").
  Uno.Extensions.RuntimeTests/        MSTest hosted inside an Uno UI head via Uno.UI.RuntimeTests.Engine.
  Directory.Build.{props,targets}     Cross-targeting plumbing — shapes ALL src projects.
  Directory.Packages.props            Central Package Management (no Version="" in csproj).
  Directory.UnoMetadata.targets       Uno-specific metadata.
  tfms-*.props, Uno.CrossTargeting.props  TFM matrices for non-UI / Maui / WinUI / runtime-tests.
  BannedSymbols.{targets,txt}         Banned-API analyzer entries — add forbidden APIs here, not via #pragma.
samples/Playground/           End-to-end sample app exercising many features (Uno.Sdk-based head).
samples/MauiEmbedding/        Sample showing MAUI controls embedded inside an Uno head.
testing/TestHarness/          UI test harness app + UITest project (Uno.UITest / Selenium driver). Folders under TestHarness/Ext/<Area> mirror src/.
specs/NNN-<topic>/spec.md     Living design docs for in-flight changes (e.g. MVUX SingleValueFeed). Read these before editing the area they describe — they capture intent that isn't in code yet. See "Spec folder naming" below for the numbering convention.
build/                        Sign-Package.ps1, tests.runsettings, Azure Pipelines YAML under build/ci/.
doc/                          DocFX-flavored docs published to platform.uno (TOC files, xref ids).
```

There is **no `Directory.Build.props` at the root** that flows into `src/` automatically — `src/Directory.Build.props` imports the root one explicitly. When adding repo-wide props, add to both or pick the right scope.

## Target frameworks and platform builds

Target frameworks are managed by the props files under `src/`:

- `tfms-non-ui.props` — non-UI core packages (net9.0 + per-platform suffixes).
- `tfms-ui-winui.props` — WinUI-flavored UI heads (`*.WinUI.csproj`).
- `tfms-ui-maui.props` — MAUI-embedded heads.
- `tfms-ui-winui-runtimetests.props` — runtime-test heads.
- `Uno.CrossTargeting.props` — shared cross-targeting infrastructure imported by `src/Directory.Build.props`.

The published TFMs typically include `net9.0`, `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, `net9.0-windows10.0.19041.0`, and `browserwasm`. The Uno SDK version is pinned in `global.json` (`Uno.Sdk` and `Uno.Sdk.Private`).

The top-level `Directory.Build.props` exposes `Build_Android`, `Build_iOS`, `Build_MacCatalyst`, `Build_Windows`, `Build_Desktop`, and `Build_Web` switches; non-Windows hosts default `Build_Windows=false`. Drop a local `DebugPlatforms.props` (template at `DebugPlatforms.props.sample`) to disable platforms you don't have SDKs for — this dramatically shortens local builds. The same file gates the `*.WinUI` cross-targeted heads.

</repository_orientation>

<flow_orchestration>

### 1. Plan Mode Default

- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions)
- If something goes sideways, STOP and re-plan immediately - don't keep pushing
- Use plan mode for verification steps, not just building
- Write detailed specs upfront to reduce ambiguity
- This repo's `specs/` directory is for **living design docs** for in-flight changes (`specs/001-mvux-single-value-feed/spec.md`, `specs/002-mvux-updatefeed-compaction/spec.md`). When you start a non-trivial change in an area, look for an existing spec first — implementation must match the spec, not the other way around.

#### Spec folder naming

Spec folders MUST use the form `specs/NNN-<kebab-case-topic>/spec.md`, where `NNN` is a zero-padded three-digit sequence number assigned in registration order (oldest = `001`). When you create a new spec, pick the next available integer — do not reuse a deleted slot. Do NOT use date prefixes; the numeric prefix is the single source of truth for ordering and makes cross-referencing stable across rebases and authorship.

Rules:
- `NNN` is zero-padded to three digits (`001`, `002`, ... `010`, ... `100`).
- The topic suffix is lowercase kebab-case and should be short but unambiguous.
- One spec per folder, named `spec.md`. Supporting assets (diagrams, snippets) live alongside.
- Once committed, a spec's number is permanent — don't renumber to "fill gaps" or reorder by topic.

### 2. Subagent Strategy

- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- For complex problems, throw more compute at it via subagents
- One tack per subagent for focused execution
- The `architect`, `security`, and `skeptic` reviewers in `.claude/agents/` exist to be dispatched in parallel via the `/review-panel` command on a non-trivial change — use them before commit, not after

### 3. Self-Improvement Loop

- After ANY correction from the user: update `specs/lessons.md` with the pattern (create the file/folder if it does not yet exist)
- Write rules for yourself that prevent the same mistake
- Ruthlessly iterate on these lessons until mistake rate drops
- Review lessons at session start

#### Where corrections are recorded

User corrections, "do this / never do that" rules, workflow guardrails, and tool-usage policies that should bind **every** agent working on this repo MUST be written to a checked-in, shared file:

- Repo-wide rules → `AGENTS.md` (this file).
- Skill-specific rules (e.g. how to use a particular tool/MCP) → the relevant `.claude/skills/<skill>/SKILL.md`.
- Domain lessons / postmortems → `specs/lessons.md`.

🚫 **Never** record cross-agent corrections in personal/auto memory (e.g. `~/.claude/projects/<project>/memory/`, `feedback_*.md`, individual user preference files). Personal memory is per-user and not shared via git, so other agents and contributors will not see it and the mistake will repeat. If a correction is general enough that any future agent should follow it, it belongs in a checked-in file. Reserve personal memory for things that are genuinely individual to one user (their role, their preferences) — not project rules.

When in doubt: if removing the rule would let any other agent on this repo repeat the same mistake, the rule is shared and must be checked in.

### 4. Verification Before Done

- Never mark a task complete without proving it works
- Diff behavior between `main` and your changes when relevant
- Ask yourself: "Would a staff engineer approve this?"
- Run tests, check logs, demonstrate correctness
- You MUST assume that for a given branch, the `main` branch is correct and failures are specific to the current branch. You MUST assume that changes in the current branch are the cause of any new failures.

### 5. Demand Elegance (Balanced)

- For non-trivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution"
- Skip this for simple, obvious fixes - don't over-engineer
- Challenge your own work before presenting it

### 6. Autonomous Bug Fixing

- When given a bug report: just fix it. Don't ask for hand-holding
- Point at logs, errors, failing tests - then resolve them
- Zero context switching required from the user
- Go fix failing CI tests without being told how

## Task Management

1. **Plan First**: Write plan to `specs/<topic>/progress.md` with checkable items
2. **Verify Plan**: Check in before starting implementation
3. **Track Progress**: Mark items complete as you go
4. **Explain Changes**: High-level summary at each step
5. **Document Results**: Add review section to `specs/<topic>/progress.md`
6. **Capture Lessons**: Update `specs/lessons.md` after corrections

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary fixes. Senior developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid introducing bugs.

</flow_orchestration>

<coding_directives>

## 1. Core Engineering Principles

✅ Apply all SOLID principles (SRP, OCP, LSP, ISP, DIP).  
✅ Keep code simple, intention‑revealing; clarity > cleverness.  
✅ Minimize use of the null-forgiving operator (`!`); prefer explicit guards or refactors that satisfy the nullable flow (the repo enables `Nullable=enable` globally — see `src/Directory.Build.props`).  
✅ Separate concerns: contracts (Core) | sources (Sources) | composition (Operators) | binding surface (Presentation) | UI hosts (`*.UI`) | tests.  
✅ Favor composition over inheritance; depend on abstractions where the API surface justifies it.  
✅ Optimize only with evidence (profiling/metrics).

---

## 2. Performance & Allocations

✅ Minimize allocations in hot paths (feed emission, navigation route resolution, configuration binding, HTTP handler invocation). Prefer `StringBuilder` for string assembly and avoid LINQ in tight loops where measurement shows it costs.
✅ Use `readonly` on fields and structs where possible.
✅ Avoid boxing (watch generics, interpolated logging with value types).
✅ Reuse expensive objects (compiled regexes, `JsonSerializerOptions`, `HttpClient` via `IHttpClientFactory`) where the lifecycle allows.
✅ Only introduce `Span<T>` / `Memory<T>` when profiling shows benefit.
✅ When using `System.Text.Json`, prefer source-generated serializers (`[JsonSerializable]`) over reflection-based ones — required for AOT (`Uno.Extensions.Serialization.AotTests` is the backstop) and beneficial for size on WASM.
✅ **Always use typed deserialization** — never parse JSON with `JsonDocument` / `JsonElement` / manual `GetProperty` chains. Define a typed model (only the fields you need) and deserialize into it.

### Memory & WASM Considerations

This repository targets WASM as a first-class platform (sample apps and runtime tests run there). On WASM, `WebAssembly.Memory.grow()` is **irreversible** — every peak allocation permanently inflates `HEAPU8.length`.

✅ **Be allocation-aware in feeds, states, and navigation.** Subscribers, route resolution, and binding can run on every interaction; an extra `ToList()` or repeated string concatenation in a frequently-invoked code path becomes a measurable footprint on WASM.
✅ **Release before allocate** when replacing large object graphs (e.g. swapping a feed pipeline, disposing a region's subtree). Don't hold the previous instance alongside the replacement longer than necessary.
✅ **Watch for leaks via static event subscriptions and long-lived feed subscriptions.** A feed that never completes (no `EndRequest` honored, no terminating signal) holds its observers indefinitely; an attached behavior that subscribes to `Loaded` without unsubscribing on `Unloaded` leaks the entire visual tree branch.

---

## 3. Framework & Platform Usage

✅ Honor `CancellationToken` on any async public API and I/O boundary.
✅ NEVER use `.Result` / `.GetAwaiter().GetResult()` outside a controlled, documented sync bridging point.
✅ ALWAYS prefer non-blocking async flow to remain WASM-safe; if a sync bridge is unavoidable, document why and keep it local.
✅ Prefer the WinUI/Uno-provided primitives (`DependencyProperty`, `VisualStateManager`, `ResourceDictionary` merging, `FrameworkElement.Loaded/Unloaded`) in UI projects over hand-rolled equivalents.
✅ Hosting-as-entry-point: every area exposes its public surface as `IHostBuilder UseFoo(this IHostBuilder, Action<IFooBuilder>? configure = null)`. Service registration goes through `IServiceCollection`; options through `IOptions<FooConfiguration>` bound to `IConfiguration`. Code that reaches into a static service locator, constructs an `IServiceProvider` ad-hoc, or registers services outside the `UseFoo` chain is a layering violation.
✅ Multi-platform aware: code that compiles for net9.0, net9.0-android, net9.0-ios, net9.0-maccatalyst, net9.0-windows10.*, and browser-wasm. When platform behavior diverges, isolate it behind partial classes / conditional compilation symbols (`__WASM__`, `__ANDROID__`, `__IOS__`, `__MACCATALYST__`, `__WINDOWS__`) — don't sprinkle `#if` blocks across method bodies if a platform-specific partial would do the job.

---

## Code Style Guidelines

Formatting and style rules are defined in the repo configuration files below. Treat these files as the source of truth and avoid duplicating their contents here.

✅ EditorConfig formatting and naming rules: [.editorconfig](.editorconfig)
✅ Conventional Commits enforcement: [.github/workflows/conventional-commits.yml](.github/workflows/conventional-commits.yml)
✅ Tabs for `.cs` / `.xaml` / `.xml` / `.targets` / `.props`; 2-space tabs for `.csproj`; spaces for `.yml` / `.md`; final newlines required.
✅ `src/Directory.Build.props` sets `TreatWarningsAsErrors=true`, `Nullable=enable`, `LangVersion=latest`, `GenerateDocumentationFile=true`, and `ImplicitUsings=true`. New code must compile clean under these. `CS1591` (missing XML doc) is suppressed for tests/samples only.
✅ **CPM is on** (`ManagePackageVersionsCentrally=true` + `CentralPackageTransitivePinningEnabled=true`). Add new package versions to `src/Directory.Packages.props`, then reference in csproj without a `Version`.
✅ **Banned symbols** are enforced via `src/BannedSymbols.txt` + `BannedSymbols.targets` — extending the txt file is preferred over scattering pragmas. A `#pragma warning disable RS0030` to silence a banned-API hit is a finding.

## 4. Build & Validation

Common commands (run from repo root):

```powershell
# Restore + build the package surface (fast)
dotnet build Uno.Extensions-packageonly.slnf -c Release

# Pack NuGets into a folder (matches what stage-build-packages.yml does)
dotnet build Uno.Extensions-packageonly.slnf -c Release /r /p:PackageOutputPath=$PWD/artifacts /p:PackageVersion=255.255.255.255-local

# Run the unit-test layer (matches CI's VSTest filter)
dotnet test Uno.Extensions-packageonly.slnf -c Release --filter "FullyQualifiedName!~UI.Tests"
# or per-project:
dotnet test src/Uno.Extensions.Reactive.Tests/Uno.Extensions.Reactive.Tests.csproj
dotnet test src/Uno.Extensions.Core.Tests/Uno.Extensions.Core.Tests.csproj
dotnet test src/Uno.Extensions.Serialization.Tests/Uno.Extensions.Serialization.Tests.csproj

# Run a single test (MSTest is the framework everywhere)
dotnet test src/Uno.Extensions.Reactive.Tests/Uno.Extensions.Reactive.Tests.csproj --filter "FullyQualifiedName~Given_Feed.When_..."
```

CI test selector (`build/ci/stage-build-packages.yml`) targets `**/*.Tests.dll` + `**/*.AotTests.dll` and **excludes** `**/*UI.Tests.dll` — those run in dedicated runtime-test stages because they require a real Uno UI host. `build/tests.runsettings` pins net9.0 / x86 and `TreatNoTestsAsError=true` — keep new test projects discoverable or that filter will fail the build.

✅ Zero warnings in Release is mandatory (`TreatWarningsAsErrors=true`).
✅ Suppress a warning only with justification in PR + targeted scope (`#pragma` with comment).
✅ Do not disable deterministic builds (`DotNet.ReproducibleBuilds` + `Microsoft.SourceLink.GitHub` are added automatically to non-test, non-sample projects when `SourceLinkEnabled != false`).
✅ Do not expand global `<NoWarn>` in `src/Directory.Build.props` without prior approval.

## 5. Testing Requirements

The repo has three distinct test surfaces — each with a different runner:

- **Unit tests** in `src/Uno.Extensions.<Area>.Tests/` — plain net9.0 MSTest. Run with `dotnet test`. Included by package CI's VSTest filter.
- **WinUI tests** in `src/Uno.Extensions.<Area>.UI.Tests/` — MSTest that requires an Uno UI host. **Excluded** by package CI (`!**/*UI.Tests.dll`); built and run by the runtime-test stages.
- **Runtime tests** in `src/Uno.Extensions.RuntimeTests/` — **MSTest hosted inside an Uno UI head** via `Uno.UI.RuntimeTests.Engine`. There is no `dotnet test` entry point for these; they ride along with a sample-app head selected by the runtime-test stages (`stage-build-runtimetests-skia.yml`, `stage-build-runtimetests-skia-hotreload.yml`).

Test-placement gotcha: a UI-host-requiring test placed in a `*.Tests` (not `*.UI.Tests`) project will be **picked up** locally by `dotnet test` (and likely fail or flake) but **excluded** by package CI — producing platform-dependent results. Match the project type to the host requirement.

### Hot-reload tests

Files in `src/Uno.Extensions.Navigation.UI.Tests/` named `Given_HotReload.cs` (and the supporting `HotReload*.cs` targets) use `[RunsInSecondaryApp]` and `HotReloadHelper.UpdateSourceFile(...)`. Constraints — see `src/Uno.Extensions.Navigation.UI.Tests/HotReload.Spec.md` for the living spec:

- **Secondary app window:** `new Window()` inside `[RunsInSecondaryApp]` produces an un-composited window whose `Loaded`/`Activate` events never fire. Always host content in `UnitTestsUIContentHelper.CurrentTestWindow` and reset via `SaveOriginalContent`/`RestoreOriginalContent`.
- **Initial route must be explicit:** descending from `""` to an `IsDefault` leaf requires `Region.Children` to be populated. Pass the target page name in `initialRoute:` rather than relying on default routing.
- **Repo-relative HR paths:** `HotReloadHelper.UpdateSourceFile(...)` uses paths relative to the test project. A rename or move that breaks that relative layout silently breaks HR tests.

### General testing rules

✅ Every new public behavior must include tests in the appropriate project (unit, UI, or runtime).
✅ Test class naming follows `Given_<Subject>` with `When_<Scenario>` methods (BDD-style); FluentAssertions (`.Should().BeXxx()`) for assertions.
✅ AAA pattern (Arrange / Act / Assert).
✅ Lack of coverage for new logic blocks merge.
✅ **Every bug fix MUST follow red/fix/green**: first add a test that reproduces the bug and fails, then apply the fix, then confirm it passes. The failing test must be committed alongside the fix so the regression is permanently guarded.
🚫 **Never use `Assert.Inconclusive`** (or equivalent). A test either asserts behavior and passes, or fails. Skipping via inconclusive hides regressions. If a scenario cannot run on a platform, gate it with an explicit platform attribute / `#if` instead.
🚫 **Do not deactivate, skip, `[Ignore]`, or delete an existing test** unless the underlying feature is being entirely removed from the product — not merely because a file was refactored, renamed, or a class was restructured. Refactors must keep the behavioral coverage intact; if a test no longer compiles after a rename, update it, don't remove it.

### Minimum Test Additions Per PR

| Change Type | Required Tests |
| ------------- | ---------------- |
| New `UseXxx` builder / extension API | Happy-path registration + at least one option-binding case (unit test in `*.Tests`) |
| New / changed feed or state operator | Subscription, completion, refresh, cancellation cases (`Uno.Extensions.Reactive.Tests`) |
| New navigator / region behavior | UI test in `Uno.Extensions.Navigation.UI.Tests` covering attach, navigate, back-stack, detach |
| New auth provider / handler | Login, refresh, logout, cancellation cases (token-leak guard required) |
| Source-generator change | Unit test against a fixture project + a clean rebuild check (Roslyn caches generator outputs) |
| Bug fix | Repro test + non-regression guard |
| Hot-reload-sensitive change (Navigation) | Add or update a `Given_HotReload.cs` case |

### String equivalence assertions (multiline)

- Use raw strings (`"""..."""`) for expected and actual samples.
- Avoid manual newline normalization (`Replace("\r\n", "\n")`); rely on the test framework's options where available.

✅ Always run runtime tests as part of verification of UI / navigator / hot-reload changes — they are not optional manual steps.
✅ Maintain or improve passing test count.
✅ Never delete tests without equivalent protection.

---

## 6. API Conventions

The deliverable here is a public NuGet API consumed by external Uno apps. Stability matters more than in an app-only codebase.

✅ Public types and members must have XML doc comments suitable for IntelliSense (`GenerateDocumentationFile=true` is on).
✅ Hosting-as-entry-point: each area exposes `IHostBuilder UseFoo(this IHostBuilder, Action<IFooBuilder>? configure = null)` (or close variants). Service registration via `IServiceCollection`, options via `IOptions<FooConfiguration>` bound from `IConfiguration`. Don't expose internal services directly on the public surface.
✅ Reactive / MVUX layering: `Core/` defines contracts (`IFeed`, `IListFeed`, `IState`, `IListState`); `Sources/` produces messages; `Operators/` composes; `Presentation/` is the binding-friendly surface generated by `Uno.Extensions.Reactive.Generator`. New feed types belong in `Sources/`.
✅ Navigation layering: contracts in `Uno.Extensions.Navigation`; one navigator per host control type in `Uno.Extensions.Navigation.UI/Navigators/` (derived from `ControlNavigator` or `DialogNavigator`). Logic that branches on host control type *outside* a navigator is a leaky abstraction.
✅ Source generators (`Uno.Extensions.{Core,Navigation,Reactive}.Generator{,s}`) target **netstandard2.0** because Roslyn's analyzer host requires it. Raising the TFM is a build break for consumers — don't.
✅ Prefer additive change. A breaking change to a public API requires explicit justification in the PR description and is likely to be rejected (see PR template — "Contains **NO** breaking changes" is a checklist item).
✅ Persisted enums: explicit underlying type; mark `[Flags]` only when actually flag-shaped.

---

## 7. Logging & Diagnostics

✅ Structured logging where applicable (`logger.LogInformation("Processed {Id}", id)`). Use the `Microsoft.Extensions.Logging` abstractions — the repo's `Uno.Extensions.Logging{,.Serilog}` packages plug into them.
✅ No PII / secrets / device identifiers in logs. **Tokens, refresh tokens, ID tokens, cookies, and Authorization headers must never appear in `Uno.Extensions.*` log output** — verify `ToString()` overrides on auth/HTTP types do not interpolate secrets.
✅ Correct level semantics (Trace/Debug/Info/Warning/Error/Critical).
✅ If values are computed only for logging (for example `ToList()`, `string.Join(...)`, projections, or expensive formatting), wrap that computation in `if (logger.IsEnabled(LogLevel.X))` for the corresponding level so disabled logs do not pay the allocation/computation cost.

---

## 8. Error Handling

✅ Never swallow exceptions silently — wrap with context or let propagate. Surface errors through `IFeed<T>` error states or async results, not by silently dropping.
✅ Order `catch` blocks from most specific to most general: catch predicted exceptions first (`OperationCanceledException`, `InvalidOperationException`, etc.), then use a generic `catch (Exception ex)` as the final fallback. Never use a bare `catch` or generic-only handler when specific exceptions are foreseeable.
✅ For navigators and bindings, prefer graceful degradation (e.g. fall back to a sensible default state) over throwing from layout / template-application paths — exceptions in those paths can take down the visual tree on consumers.

---

## 9. Constants & Magic Strings

✅ Centralize non-trivial strings (configuration keys, route names, scheme identifiers, qualifier prefixes) and numeric literals.
✅ Comment rationale for timeouts, retry counts, threshold values.
✅ Avoid scattering duplicate values across XAML and code-behind / configuration and code.

---

## 10. Async & Concurrency

✅ All I/O-bound operations async.
✅ Honor `CancellationToken` quickly — plumb it to the next `await`, don't accept-and-ignore.
✅ Avoid shared mutable state; where needed protect with locks/concurrent collections.
✅ Use `ConfigureAwait(false)` only in library layers that genuinely don't need to resume on the UI thread (the non-UI core packages mostly do).
🚫 **NEVER use `async void`** except for event handlers required by the framework (e.g. XAML event handlers, `OnLaunched`).
✅ Every `async void` method **MUST** wrap its entire body in `try/catch` — unhandled exceptions in `async void` crash the runtime (especially critical on WASM where the runtime runs in a web worker).
✅ Prefer returning `Task`/`ValueTask` from async methods so callers can observe exceptions.
✅ Fire-and-forget patterns (`_ = SomeAsync()`) **MUST** have a `try/catch` inside the called method.

---

## 11. UI / XAML

✅ Minimize code-behind in UI projects; prefer attached properties, behaviors, or template parts for reusable logic.
✅ Use bindings for state propagation; avoid hand-wired property change handlers when a binding suffices. Reactive's `Bindable*` types (generated by `Uno.Extensions.Reactive.Generator`) are the canonical binding surface for MVUX.
✅ Avoid manual dispatcher usage unless necessary.
✅ WinUI supports implicit `bool` → `Visibility` bindings; do not add redundant BoolToVisibility converters.
✅ Source generators are referenced as `<ProjectReference Include="..." OutputItemType="Analyzer" ReferenceOutputAssembly="false" />`. After modifying any generator project, **clean-rebuild** consumers — Roslyn caches generator outputs and stale caches mask regressions.

---

## 12. Security & Reliability

✅ No secrets in code.
✅ Validate input at public API entry points where it influences resource lookups, file paths, reflection, or network endpoints.
✅ When deserializing user-supplied content (JSON, configuration, refit/kiota responses) treat it as untrusted and prefer typed, source-generated deserializers.
✅ **Authentication area is the highest-leverage attack surface.** Token storage (`ITokenCache`), refresh-token flows, OIDC discovery, PKCE, redirect-URI validation, MSAL broker integration, cookie handlers — every one of these is RCE-or-account-takeover-adjacent if mishandled. A change in this area without a matching test is a finding.
✅ **HTTP pipeline:** new `DelegatingHandler`s must not forward Authorization headers to wrong hosts (cross-host token leak), must not disable TLS validation, and must propagate `CancellationToken`. Refit/Kiota client lifetimes go through `IHttpClientFactory`, not `new HttpClient()`.

---

## 13. Documentation

The PR template (`.github/pull_request_template.md`) requires updating documentation as needed. Repo docs live under `doc/` and are DocFX-flavored (TOC files, `xref:` ids, e.g. `xref:Uno.Extensions.Overview`):

- **General docs**: `doc/ExtensionsOverview.md` and area entry points.
- **Authentication / Configuration / DependencyInjection / Hosting / Localization / Logging**: `doc/Learn/<Area>/`.
- **Markup**: `doc/Learn/Markup/` — covers the C# markup surface for both the Reactive and Navigation packages.
- **KeyEquality**: `doc/Learn/KeyEquality/` (MVUX record-equality rules).

✅ Prefer updating an existing page over adding a new one; cross-link where appropriate.
✅ When adding or changing a public API, update the relevant `doc/Learn/<Area>/` page and TOC.

</coding_directives>

<review_directives>

## 1. Pull Request (Agent) Checklist

Beyond the items in `.github/pull_request_template.md`:

- [ ] Related specs and `progress.md` updated with context and plan (if a plan was used).
- [ ] Release build of `Uno.Extensions-packageonly.slnf`: zero warnings/errors.
- [ ] Tests added for new/changed logic (list names) and placed in the correct project type (`*.Tests` for unit, `*.UI.Tests` for UI-host-requiring, `Uno.Extensions.RuntimeTests` for in-app runtime tests).
- [ ] Runtime tests have been run via the appropriate sample-app head and MUST pass for UI / navigator / HR-sensitive changes.
- [ ] Hot-reload tests added/updated where the change touches a navigator, region, or HR-sensitive surface (per `HotReload.Spec.md`).
- [ ] CPM respected: new package versions added to `src/Directory.Packages.props`, no `Version=""` in csproj.
- [ ] No unjustified additions to `<NoWarn>` (`src/Directory.Build.props`) or `BannedSymbols` suppressions.
- [ ] SOLID + separation of concerns respected; layering preserved (Core not depending on UI; UI not inverted; generators stay netstandard2.0).
- [ ] Public API changes documented with XML docs and reflected in `doc/Learn/<Area>/`.
- [ ] Structured logging; no tokens / PII / Authorization headers in log output.
- [ ] Error handling consistent.
- [ ] No magic strings (constants added where needed).
- [ ] Performance considerations documented if hot path changed.
- [ ] PR description matches actual change scope, includes the required issue link, and confirms "no breaking changes" (or justifies them explicitly).

---

## 2. Agent Prompting Guidance

Provide explicit constraints to reduce refactor churn:

1. Specify the target layer (e.g. `Uno.Extensions.<Area>` core vs `<Area>.UI`; feed source vs operator vs presentation; navigator vs region).
2. Define the contract (public types, `UseXxx` builder shape, `IFeed`/`IState` semantics, navigator events — inputs, outputs, errors).
3. Request tests inline with implementation, in the correct project type.
4. State performance expectations (no extra allocations in feed emission / route resolution / handler invocation; cancellation honored).
5. Indicate error strategy (graceful fallback vs. exceptions; `IFeed<T>` error state vs. throw).

Example:
> Add a `UseFoo` extension to `IHostBuilder` in a new `Uno.Extensions.Foo` package. Register `IFooService` in DI, bind `FooConfiguration` from `IConfiguration`, expose an `IHostBuilder Foo(...)` chain. Add unit tests in `Uno.Extensions.Foo.Tests` covering registration, options binding, and a cancellation case. Add a `doc/Learn/Foo/` entry with TOC. Source generator is not required.

---

## 3. Definition of Done

1. Release build of `Uno.Extensions-packageonly.slnf` warning-free.
2. Tests added & passing in the correct project type (relevant runtime tests run for UI changes).
3. Principles & conventions adhered to.
4. No unjustified performance regressions or added allocations on hot paths.
5. PR template checklist completed; no breaking changes (or justified).

---

## 4. Exceptions Process

If a guideline cannot be met:

- Constraint
- Impact
- Mitigation / follow-up issue reference

Unexplained deviations block merge.

---

## 5. Quick Reference Table

| Area | Rule |
| ------ | ------ |
| Build | Release: zero warnings (TreatWarningsAsErrors) |
| Tests | New behavior + edge case in correct project (`*.Tests` / `*.UI.Tests` / `RuntimeTests`) |
| SOLID | All five applied |
| Layering | Core (net9.0) ⊄ UI (WinUI); UI ⊄ Markup; Generators stay netstandard2.0 |
| Allocations | Minimize hot paths; WASM-aware; feeds must be completable |
| Logging | Structured; no PII / tokens / cookies |
| Errors | Surface via `IFeed<T>` errors or async results; specific catches |
| API | XML docs on public surface; `UseXxx(IHostBuilder)` convention; additive change preferred |
| CPM | All package versions in `src/Directory.Packages.props`; no `Version=` in csproj |
| Constants | Centralize and document |
| Validation | At public entry points; auth area is highest-leverage |
| Async | Honor cancellation; NEVER produce blocking code |
| Generators | netstandard2.0; clean-rebuild after generator edits |

---

## 6. Source Control

- Commit messages: clear, imperative, reference issues.
- MUST follow [Conventional Commits](https://www.conventionalcommits.org/) — enforced by `.github/workflows/conventional-commits.yml`. Bullet points, no big walls of text.

</review_directives>

---

## Final Note

Agents must act deterministically and transparently. This document is the authoritative guardrail — adhere strictly to sustain maintainability, reliability, and trust.

---

(End of AGENTS Instructions)
