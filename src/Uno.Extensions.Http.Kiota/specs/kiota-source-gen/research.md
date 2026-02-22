# Research: Kiota MSBuild Code Generation

**Feature**: kiota-source-gen | **Date**: 2026-02-21

## Research Tasks & Findings

### R1: Kiota.Builder Programmatic API Feasibility (Phase 1)

**Decision**: Use `Microsoft.OpenApi.Kiota.Builder` NuGet package as a library in a wrapper console app.

**Rationale**: `KiotaBuilder` exposes a full programmatic API:
- `new KiotaBuilder(ILogger<KiotaBuilder>, GenerationConfiguration, HttpClient)`
- `builder.GenerateClientAsync(CancellationToken)` — end-to-end generation
- Individual stages also accessible: `CreateUriSpace()`, `CreateSourceModel()`, `ApplyLanguageRefinementAsync()`, `CreateLanguageSourceFilesAsync()`

This is the exact pattern used by NSwag.ApiDescription.Client (MSBuild-embedded generator binary). Production-proven, minimal custom code.

**Alternatives Considered**:
- Invoking `kiota` CLI directly from MSBuild → Rejected: requires `dotnet tool install`, defeats purpose
- Porting Kiota.Builder to netstandard2.0 for direct MSBuild task → Rejected: massive effort, unnecessary for Phase 1

---

### R2: netstandard2.0 Compatibility for Source Generator (Phase 2)

**Decision**: OpenAPI parsing is fully feasible within netstandard2.0. CodeDOM + Writer must be re-implemented.

**Rationale**:

| Dependency | netstandard2.0 | Notes |
|------------|----------------|-------|
| `Microsoft.OpenApi` v3.3.1 | **Yes** | Core object model, dual-targets net8.0 + netstandard2.0 |
| `Microsoft.OpenApi.Readers` v1.6.28 | **Yes** | JSON + YAML reader, targets netstandard2.0 |
| `DotNet.Glob` v3.1.3 | **Yes** | Targets netstandard1.1+ |
| `YamlDotNet` v16.3.0 | **Yes** | Targets netstandard2.0 |
| `Microsoft.OpenApi.Kiota.Builder` | **No** | Targets net8.0/net9.0/net10.0 only |

**What must be re-implemented** (~14,000–16,000 LOC):
1. CodeDOM class hierarchy (~2,000 LOC)
2. CodeDOM construction logic (~4,000 LOC) — URI tree walking, schema resolution, type mapping
3. Type resolution & inheritance trimming (~1,500 LOC) — circular references, allOf/oneOf/anyOf
4. CSharpRefiner (~1,000 LOC) — language-specific adjustments
5. CSharpWriter + sub-writers (~4,000 LOC) — template-like code emission
6. CSharpConventionService + PathSegmenter (~500 LOC) — naming, type mappings
7. Configuration & infrastructure (~1,000 LOC)

**Alternatives Considered**:
- JSON-RPC bridge to out-of-process Kiota.Builder → Rejected: adds complexity, latency, still requires net8.0 process
- Wait for Kiota team to ship netstandard2.0 build → Rejected: explicitly unlikely per GitHub issue #6239

---

### R3: NSwag.ApiDescription.Client Pattern Analysis

**Decision**: Follow NSwag's MSBuild integration pattern for Phase 1.

**Rationale**: NSwag embeds a console app in NuGet `tools/` folder with `.targets` that hook into `CoreCompileDependsOn`. This pattern is:
- Used in production by thousands of projects
- Works across Windows, macOS, Linux
- Compatible with CI/CD (no tool install needed)
- Supports incremental builds via `Inputs`/`Outputs`

**Key differences from NSwag**:
| Aspect | NSwag | Uno Kiota (Phase 1) |
|--------|-------|---------------------|
| Generator binary | .NET framework-specific | Framework-dependent with `RollForward=Major` |
| Item group | `<OpenApiReference>` | `<KiotaOpenApiReference>` |
| Output | Single-file per API | Multi-file per API (Kiota's request builder pattern) |
| Runtime deps | Newtonsoft.Json | Microsoft.Kiota.Abstractions |

---

### R4: Existing Uno.Extensions Source Generator Patterns

**Decision**: Follow the `Uno.Extensions.Core.Generators` project structure exactly.

**Rationale**: The existing generator pattern in the repo:
- Targets `netstandard2.0`
- Uses `<ToolOfPackage>Uno.Extensions.Core</ToolOfPackage>` to route output into the host package's `analyzers/dotnet/cs/` folder
- `IsPackable=false` (packed via ToolOfPackage mechanism in `Uno.CrossTargeting.props`)
- All dependencies are `PrivateAssets="All"`
- Uses `Uno.Roslyn` helper library
- Includes `buildTransitive/*.props` for `CompilerVisibleProperty` declarations

The Kiota source generator will mirror this exactly with `<ToolOfPackage>Uno.Extensions.Http.Kiota</ToolOfPackage>`.

---

### R5: Assembly Loading and Dependency Bundling (Phase 2)

**Decision**: Start with separate DLLs in analyzers folder (matches existing `ToolOfPackage` pattern). Upgrade to ILRepack if conflicts arise.

**Rationale**:
- **Option 1 (Chosen)**: Ship dependency DLLs alongside generator in `analyzers/dotnet/cs/`. Simplest, matches existing repo conventions. The `ToolOfPackage` + `ReferenceCopyLocalPaths` mechanism handles this automatically.
- **Option 2 (Fallback)**: ILRepack all dependencies with namespace internalization. Eliminates all version conflict risk but adds build complexity.
- **Option 3 (Rejected)**: Separate `AssemblyLoadContext`. Only works on .NET hosts, not in older VS versions on .NET Framework.

**Risk**: `Microsoft.OpenApi.dll` version conflict with other analyzers. Probability: low-medium. Mitigated by upgrading to ILRepack if observed.

---

### R6: Kiota Runtime Package Version Compatibility

**Decision**: Pin Phase 1 generator to `Kiota.Builder` 1.30.0; generated code targets runtime packages 1.21.0 (existing in `Directory.Packages.props`).

**Rationale**: 
- Kiota.Builder 1.30.0 generates code compatible with Kiota runtime 1.21.0 (backward compatible within major version)
- The `GenerationConfiguration.ExcludeBackwardCompatible` flag controls whether deprecated patterns are emitted
- Generated code patterns (request builders, IParsable models) are stable across 1.x versions
- Phase 2 re-implementation will target the 1.21.0 runtime API surface explicitly

**Risk**: Minor discrepancies between 1.30.0 generator patterns and 1.21.0 runtime expectations. Mitigated by golden-file testing.

---

### R7: Kiota Team Position on First-Party Source Generator

**Decision**: Community-driven implementation is the only viable path.

**Findings**:
- [Issue #3005](https://github.com/microsoft/kiota/issues/3005): `Microsoft.OpenApi.Kiota.ApiDescription.Client` deprecated due to low usage
- [Issue #3015](https://github.com/microsoft/kiota/issues/3015): netstandard2.0 limitations cited as blocker for first-party source generator
- [Issue #3815](https://github.com/microsoft/kiota/issues/3815): Open to community contribution but no implementation materialized
- [Issue #6239](https://github.com/microsoft/kiota/issues/6239): Most recent request (March 2025) — team confirmed "unlikely" to be first-party
- [HavenDV/Kiota](https://github.com/HavenDV/Kiota): Community attempt archived May 2025, "not working, waiting for netstandard Kiota.Builder"

**Conclusion**: Kiota team will not ship this. Our Phase 1 approach (wrap Kiota.Builder) avoids the netstandard2.0 constraint. Phase 2 addresses it through re-implementation.

---

### R8: OpenAPI Spec Format Support

**Decision**: Support both JSON and YAML for Phase 1 and Phase 2.

**Rationale**:
- Phase 1: `Kiota.Builder` natively supports both formats
- Phase 2: `Microsoft.OpenApi.Readers` v1.6.28 (netstandard2.0) supports both JSON and YAML
- `YamlDotNet` v16.3.0 targets netstandard2.0, so YAML support is feasible in source generators
- Many popular APIs distribute specs in YAML (OpenAPI generators commonly accept both)

---

### R9: OpenAPI Version Support

**Decision**: Support OpenAPI 3.0 and 3.1. Swagger 2.0 supported in Phase 1 (via Kiota.Builder), best-effort in Phase 2.

**Rationale**:
- `Microsoft.OpenApi` library handles v2→v3 conversion automatically
- Phase 1 inherits full Kiota CLI support (Swagger 2.0, OpenAPI 3.0, 3.1)
- Phase 2 benefits from `Microsoft.OpenApi.Readers` conversion layer
- Swagger 2.0 usage is declining; most new APIs use 3.0+

---

### R10: Generated Code AOT Compatibility

**Decision**: Defer AOT compatibility. Current Kiota output uses `Activator.CreateInstance()` and reflection-based patterns.

**Rationale**:
- The existing `AddKiotaClient<T>()` uses `Activator.CreateInstance(typeof(TClient), requestAdapter)` — inherently non-AOT
- Kiota's `CreateFromDiscriminatorValue` factory pattern is already AOT-friendly (no reflection)
- Full AOT support would require changes to `ServiceCollectionExtensions.cs` (out of scope)
- Can be addressed in a future iteration without breaking changes
