# Kiota MSBuild Code Generation — Overview

## Problem Statement

Today, generating a typed API client with [Kiota](https://github.com/microsoft/kiota) requires installing the Kiota CLI (`dotnet tool install --global Microsoft.OpenApi.Kiota`) and running a manual generation step before building. This creates several pain points:

1. **Breaks "clone and build" workflows.** A new developer cannot simply `git clone` a repository and run `dotnet build`. They must first install the Kiota tool and run a separate generation command.
2. **Cannot run in restricted environments.** CI/CD pipelines, air-gapped build servers, Docker-based builds, and environments where `dotnet tool` commands are unavailable or restricted cannot regenerate client code.
3. **Version drift across developers.** A globally installed tool can differ between team members, producing different generated code depending on which Kiota version is installed locally. Kiota's `kiota-lock.json` mitigates this partially but doesn't eliminate it.
4. **No IDE-time feedback.** Generated code exists as pre-committed `.cs` files. If the OpenAPI spec changes, the developer must remember to re-run generation — there is no automatic regeneration on build or during IDE editing.
5. **Incompatible with the Uno Platform model.** Uno.Extensions packages are NuGet-delivered and must work across all target platforms (iOS, Android, WebAssembly, macOS, Windows, Linux) without requiring platform-specific tool installation.

## Goals

- **Seamless MSBuild integration**: OpenAPI `.json`/`.yaml` → C# code generation as part of `dotnet build` with zero manual steps.
- **No `dotnet tool` dependency**: everything ships inside NuGet packages.
- **Full Kiota output parity**: generated code must be identical (or functionally equivalent) to what the Kiota CLI produces — including `allOf`/`oneOf`/`anyOf` composition, discriminator factories, error response mappings, backing store support, and serialization/deserialization.
- **Compatible with existing DI surface**: generated client classes work with the existing `AddKiotaClient<T>()` and `AddKiotaClientWithEndpoint<T, TEndpoint>()` registration methods in `Uno.Extensions.Http.Kiota.ServiceCollectionExtensions` without modification.
- **Incremental build support**: only regenerate when input files change.
- **Cross-platform**: works on Windows, macOS, and Linux build environments.

## Two-Phase Approach

### Phase 1 — MSBuild Task with Embedded Kiota.Builder (spec: `02-phase1-msbuild-task.md`)

Ship a NuGet package containing:
- A self-contained .NET console application wrapping `Microsoft.OpenApi.Kiota.Builder`
- MSBuild `.props`/`.targets` files that hook into the build pipeline
- Multi-RID binaries (`win-x64`, `linux-x64`, `osx-arm64`, `osx-x64`)

This is the **fastest path to delivery**. It follows the proven pattern used by [NSwag.ApiDescription.Client](https://github.com/RicoSuter/NSwag/tree/master/src/NSwag.ApiDescription.Client) — embed the generator binary in the NuGet package and invoke it via MSBuild targets during build.

**Pros**: Full Kiota feature support out of the box (wraps the real `KiotaBuilder`), minimal new code.
**Cons**: No IDE-time generation (runs only during build), larger NuGet package footprint (multi-RID binaries), requires separate process invocation.

### Phase 2 — Pure Roslyn Incremental Source Generator (spec: `03-phase2-source-generator.md`)

Ship a `netstandard2.0` Roslyn `IIncrementalGenerator` that:
- Reads OpenAPI `.json`/`.yaml` files from `AdditionalFiles`
- Parses them using `Microsoft.OpenApi` (which supports `netstandard2.0`)
- Builds a Kiota-compatible CodeDOM
- Emits C# source identical to Kiota CLI output

This is the **ideal developer experience** — live generation in the IDE, instant feedback when specs change, no external process. However, it requires porting Kiota's CodeDOM construction and C# writer (~15K LOC) to operate within `netstandard2.0` constraints.

**Pros**: IDE-time generation, no external tooling, incremental by nature, smaller package.
**Cons**: Substantial implementation effort, must maintain parity with Kiota updates, `netstandard2.0` API limitations.

## Prior Art & Community Context

### Kiota Team Position

The Kiota team has explicitly decided **not** to build a first-party MSBuild task or source generator:

- **[Issue #3005](https://github.com/microsoft/kiota/issues/3005)** — `Microsoft.OpenApi.Kiota.ApiDescription.Client` was deprecated due to "extremely low usage" and Visual Studio integration gaps.
- **[Issue #3015](https://github.com/microsoft/kiota/issues/3015)** — Discussion of tool vs. package distribution. The Kiota team acknowledged the value but cited `netstandard2.0` limitations for source generators as a blocker.
- **[Issue #3815](https://github.com/microsoft/kiota/issues/3815)** — Feature request for source generator client proxy generation. The team was open to community contribution but noted `netstandard2.0` constraints and the possibility of using JSON-RPC as a workaround. No implementation materialized.
- **[Issue #6239](https://github.com/microsoft/kiota/issues/6239)** — Most recent request (March 2025) for project-based Roslyn generators. The Kiota team pointed to [community projects](https://github.com/HavenDV/Kiota) and the `Microsoft.OpenApi.Kiota.Builder` library as building blocks but confirmed this is "unlikely" to be a first-party option.

### Community Attempts

- **[HavenDV/Kiota](https://github.com/HavenDV/Kiota)** — A community source generator attempt. It was **archived on May 5, 2025** and marked "Currently not working (waiting for release of netstandard version of Kiota.Builder)." The approach was to use `AdditionalFiles` and `<PropertyGroup>` for configuration — a pattern we adopt in Phase 2.
- **NSwag.ApiDescription.Client** — The NSwag project's MSBuild integration, embedding a console app in a NuGet package with `.targets` files. This is a mature, production-proven pattern we adopt for Phase 1.

### Key Insight: `Microsoft.OpenApi` Supports `netstandard2.0`

While `Kiota.Builder` itself targets `net8.0`/`net9.0`/`net10.0` (incompatible with Roslyn source generators), the underlying OpenAPI parsing library — `Microsoft.OpenApi` v3.3.1 — **does target `netstandard2.0`**. This means a source generator can parse OpenAPI documents natively. The `Microsoft.OpenApi.Readers` v1.6.28 package (for YAML support) also targets `netstandard2.0`. This is what makes Phase 2 feasible.

## Document Index

| Spec | Description |
|------|-------------|
| [01-kiota-architecture-analysis.md](01-kiota-architecture-analysis.md) | How Kiota generates code today — parsing, CodeDOM, C# writer, configuration |
| [02-phase1-msbuild-task.md](02-phase1-msbuild-task.md) | Phase 1: MSBuild Task wrapping Kiota.Builder binaries |
| [03-phase2-source-generator.md](03-phase2-source-generator.md) | Phase 2: Pure Roslyn incremental source generator |
| [04-generated-code-structure.md](04-generated-code-structure.md) | Detailed structure of the generated C# code |
| [05-project-structure.md](05-project-structure.md) | How new projects fit into the Uno.Extensions repository |
| [06-risk-analysis.md](06-risk-analysis.md) | Risk assessment and mitigation strategies |
