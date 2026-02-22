# Risk Analysis & Mitigation

## Summary

This document catalogs the technical risks, open questions, and mitigation strategies for both phases of the Kiota MSBuild/source generator integration.

---

## 1. Phase 2 Implementation Scope

### Risk: ~15,000 Lines of C# to Re-implement

Phase 2 requires porting significant portions of Kiota.Builder to `netstandard2.0`. The core components:

| Component | Estimated LOC | Complexity |
|-----------|--------------|------------|
| CodeDOM class hierarchy | ~2,000 | Medium — data classes with careful type relationships |
| CodeDOM construction (`KiotaBuilder` logic) | ~4,000 | **High** — URI tree walking, schema resolution, type mapping |
| Type resolution & inheritance trimming | ~1,500 | **High** — circular reference handling, `allOf`/`oneOf`/`anyOf` |
| CSharpRefiner | ~1,000 | Medium — language-specific adjustments |
| CSharpWriter (all sub-writers) | ~4,000 | Medium — template-like code emission |
| CSharpConventionService + PathSegmenter | ~500 | Low — naming conventions and type mappings |
| Configuration & infrastructure | ~1,000 | Low — plumbing |
| **Total** | **~14,000–16,000** | |

**Impact**: High development effort, high surface area for bugs, significant testing burden.

**Mitigation**:
- Phase 1 ships first, providing immediate value while Phase 2 is developed
- Golden-file parity testing against Kiota CLI output catches divergences early
- Incremental delivery: start with the most common patterns (basic CRUD, simple models), add composition types (`oneOf`/`anyOf`/`allOf`) and advanced features iteratively
- Consider generating the CodeDOM + Writer code from Kiota's source via a build-time tool, reducing manual porting

---

## 2. Kiota Version Drift

### Risk: Upstream Kiota Changes Break Compatibility

Kiota is actively developed. The generated code patterns, runtime API surface, and CodeDOM structure evolve across versions.

**Specific concerns**:
- New `CodeMethod.Kind` or `CodeProperty.Kind` values added
- Changes to generated constructor signatures
- New runtime interfaces (like `IComposedTypeWrapper` was added relatively recently)
- Changes to URL template format
- Serialization/deserialization pattern changes

**Impact**: Generated code may be incompatible with newer Kiota runtime packages, or may not match expected patterns.

**Mitigation**:
- **Pin to a specific Kiota version**: Track Kiota releases and explicitly version our generation parity (e.g., "compatible with Kiota v1.21.0 output")
- **Automated parity CI**: Monthly (or on-PR) job that regenerates golden files with latest Kiota CLI and detects drift
- **Runtime package version coupling**: Document which Kiota runtime package versions are compatible with our generator's output
- **Thin abstraction over CodeDOM**: If the upstream CodeDOM changes, only the builder layer needs updating, not the emitters

---

## 3. netstandard2.0 Constraints (Phase 2)

### Risk: Source Generator Runtime Limitations

| Constraint | Impact | Severity |
|-----------|--------|----------|
| No `Span<T>` / performance APIs | Slower parsing for large specs | Low — acceptable for code gen |
| No `System.Text.Json` built-in | Must rely on `Microsoft.OpenApi`'s internal JSON handling | Low — `Microsoft.OpenApi` handles this |
| Synchronous execution only | Can't use `async` APIs from `Microsoft.OpenApi` v3.x | Medium — must use sync overloads or `.GetAwaiter().GetResult()` |
| Limited `System.Collections.Immutable` | Incremental generator caching may need custom equality | Medium — must implement `IEquatable<T>` carefully |
| Assembly loading isolation | Risk of version conflicts with other analyzers | **High** — see Section 4 |

**Mitigation**:
- Use `OpenApiStreamReader` (synchronous, from `Microsoft.OpenApi.Readers` v1.6.x) instead of `OpenApiDocument.LoadAsync()`
- Implement proper `IEquatable<T>` and `GetHashCode()` on all pipeline model types for incremental caching
- Bundle dependencies via ILRepack (see Section 4)

---

## 4. Source Generator Dependency Hell

### Risk: Assembly Version Conflicts

Roslyn loads all analyzers into a shared `AssemblyLoadContext` (or `AppDomain` in older VS versions). If two analyzers ship different versions of the same assembly, unpredictable behavior occurs.

**Specific conflict vectors**:
- `Microsoft.OpenApi.dll` — another analyzer or tool might load a different version
- `System.Text.Json` — very commonly bundled by various tools
- `YamlDotNet` — used by `Microsoft.OpenApi.Readers` for YAML support
- `DotNet.Glob` — low risk, but possible

**Impact**: Runtime `TypeLoadException`, `MissingMethodException`, or silent behavior differences.

**Mitigation strategies** (in order of preference):

1. **ILRepack with internalization** (Recommended):
   - Merge all dependencies into the generator assembly
   - Rename all dependency namespaces to internal ones (e.g., `Microsoft.OpenApi` → `Uno.Internal.OpenApi`)
   - Eliminates all version conflict risk
   - Adds ~5-10 MB to the analyzer assembly (acceptable)

2. **Separate `AssemblyLoadContext`** (Backup):
   - Load dependencies in an isolated context
   - Only works on .NET hosts (not in older VS versions on .NET Framework)
   - More complex, less reliable

3. **Ship dependencies as separate DLLs** (Simplest):
   - Place all dependency DLLs alongside the generator in `analyzers/dotnet/cs/`
   - Relies on Roslyn's assembly resolution
   - Works in most cases but doesn't prevent version conflicts
   - This is the approach the existing Uno generators use via `ToolOfPackage` + `ReferenceCopyLocalPaths`

**Recommendation**: Start with option 3 (simplest, matches existing repo patterns). If conflicts arise in practice, upgrade to option 1 (ILRepack).

---

## 5. OpenAPI Spec Edge Cases

### Risk: Complex or Non-Standard Specs Produce Incorrect Code

OpenAPI specifications vary wildly in complexity and adherence to the spec. Edge cases that Kiota handles but are easy to miss:

| Edge Case | Difficulty | Notes |
|-----------|-----------|-------|
| Circular `$ref` references | **High** | Models that reference themselves or form cycles |
| `allOf` with multiple `$ref` + inline properties | **High** | Must correctly identify base class vs. mixin |
| `oneOf` without discriminator | Medium | Must generate a try-each-type factory |
| `anyOf` with mixed primitive + object types | Medium | Mixed composed types |
| Deeply nested schemas (no `$ref`) | Medium | Inline schema definitions at arbitrary depth |
| `additionalProperties: true` | Low | Must generate `AdditionalData` dictionary |
| `additionalProperties` with typed schema | Medium | Typed dictionary values |
| Enum values with special characters | Low | Dashes, dots, spaces in enum member names |
| `x-ms-enum` and other extensions | Low | Vendor extensions affecting generation |
| Path segments with special characters | Medium | Dots, dashes in URL segments |
| Multiple response content types | Medium | Must pick the right response schema |
| `204 No Content` responses | Low | Executor returns `void` / `Task` |
| `binary` / `Stream` response bodies | Medium | Different executor method signature |
| Multipart form data request bodies | Medium | Different request body handling |
| Header parameters | Low | Less commonly used |
| Cookie parameters | Low | Rarely used but valid |

**Mitigation**:
- Build a comprehensive test suite matching Kiota's own test coverage
- Start with the most common patterns and expand iteratively
- Use real-world OpenAPI specs (GitHub, Microsoft Graph, Petstore) as integration tests
- Document known limitations explicitly

---

## 6. Large OpenAPI Specifications

### Risk: Performance Degradation with Large Specs

Some OpenAPI specifications are extremely large:
- Microsoft Graph: ~100,000 lines, ~2,500 endpoints
- Azure Resource Manager: 50,000+ lines per service
- Large enterprise APIs: 10,000+ lines

**Impact on Phase 1 (MSBuild Task)**: Acceptable — runs once per build, output is cached. Even Graph generates in ~30 seconds with Kiota CLI.

**Impact on Phase 2 (Source Generator)**: More concerning — the generator runs in the IDE process. A 30-second generation cycle would freeze intellisense.

**Mitigation**:
- **Incremental pipeline caching**: Roslyn's `IIncrementalGenerator` automatically caches results when inputs haven't changed. The full pipeline only runs when the OpenAPI file is modified.
- **Include/Exclude patterns**: Allow users to generate only a subset of endpoints (`KiotaIncludePatterns`, `KiotaExcludePatterns`), dramatically reducing output for large specs.
- **Lazy generation**: Consider splitting the pipeline so parsing is cached separately from emission.
- **Size limit warning**: Emit a diagnostic warning for specs over a certain size, recommending Phase 1 (MSBuild task) instead.
- **Benchmark early**: Test with Microsoft Graph spec during development to establish performance baselines.

---

## 7. IDE Experience Reliability

### Risk: Source Generator Crashes or Produces Errors

Source generators that throw unhandled exceptions or produce invalid code create a poor developer experience:
- Missing IntelliSense
- Red squiggles everywhere
- IDE hangs or crashes
- Difficult to diagnose (generator errors are buried in diagnostics)

**Mitigation**:
- **Wrap all generator logic in try/catch**: Report failures as diagnostics, never throw
- **Emit descriptive diagnostic IDs**: `KIOTA001` through `KIOTA099` with clear messages
- **Partial generation**: If a single endpoint or model fails, skip it and generate everything else
- **Fallback source**: On complete failure, emit a comment explaining the error
- **Extensive logging**: Support verbose diagnostic output when `KiotaGenerator_Verbose=true`

### Diagnostic Catalog (Proposed)

| ID | Severity | Description |
|----|----------|-------------|
| `KIOTA001` | Error | Failed to parse OpenAPI document |
| `KIOTA002` | Error | Invalid OpenAPI version (must be 3.0 or 3.1) |
| `KIOTA003` | Error | Missing required `KiotaClientName` metadata |
| `KIOTA010` | Warning | Unresolved `$ref` reference, skipping |
| `KIOTA011` | Warning | Unsupported schema composition, generating fallback |
| `KIOTA012` | Warning | Circular reference detected, using forward declaration |
| `KIOTA020` | Warning | OpenAPI spec exceeds recommended size, consider using MSBuild task |
| `KIOTA030` | Info | Code generation completed: N types generated |
| `KIOTA040` | Warning | Unknown vendor extension ignored |

---

## 8. Phase 1 → Phase 2 Migration

### Risk: Breaking Changes Between Phases

If users adopt Phase 1 and we later want them to migrate to Phase 2, the transition must be smooth.

**Potential breaking changes**:
- Different MSBuild property names (`KiotaOpenApiReference` vs `AdditionalFiles` with metadata)
- Different generated code directory structure (on-disk files vs in-memory source)
- Different default configuration values
- Subtle code generation differences between Kiota.Builder and our re-implementation

**Mitigation**:
- **Both phases coexist**: Never force migration; Phase 1 remains supported
- **Configuration compatibility**: Accept the same semantic properties in both phases, even if the MSBuild plumbing differs
- **Parity testing**: Ensure Phase 2 output matches Phase 1 output (which matches Kiota CLI output)
- **Migration documentation**: Provide a step-by-step guide for `KiotaOpenApiReference` → `AdditionalFiles` migration
- **Detect conflicts**: If both are configured for the same spec, emit a warning

---

## 9. Licensing & Legal

### Risk: Kiota Source Code Licensing

Kiota is licensed under MIT. However:
- **Phase 1**: Uses `Microsoft.OpenApi.Kiota.Builder` as a NuGet package — no licensing concerns
- **Phase 2**: Re-implements Kiota logic — must ensure:
  - We are not copying source code directly
  - Our implementation is a clean-room re-implementation based on observed behavior and public documentation
  - We properly attribute the Kiota project where appropriate
  - MIT license is compatible with Uno.Extensions' license

**Mitigation**:
- Do not copy-paste Kiota source code
- Implement based on the specification (OpenAPI spec) and observed behavior (golden file tests)
- Reference Kiota's architecture as inspiration, not as copied implementation
- Include MIT license attribution in the NuGet package

---

## 10. Open Questions

| # | Question | Impact | Status |
|---|----------|--------|--------|
| 1 | Should Phase 1 use `Microsoft.OpenApi.Kiota.Builder` v1.30.0 or match the runtime packages at v1.21.0? | Version mismatch could cause subtle differences | **Open** |
| 2 | Should the source generator support OpenAPI 2.0 (Swagger) specs? | Scope increase — Kiota CLI supports them | **Open** |
| 3 | Should we support YAML in Phase 2 or only JSON? | YAML requires `Microsoft.OpenApi.Readers` + `YamlDotNet` in the generator | **Open** — both are netstandard2.0 compatible |
| 4 | How should the generator handle specs fetched from URLs? | Phase 1 can download at build time; Phase 2 would need `AdditionalFiles` pointing to a local copy | **Open** |
| 5 | Should we integrate with `kiota-lock.json` for migration from Kiota CLI? | Useful for adoption, but adds complexity | **Open** |
| 6 | What is the minimum OpenAPI spec version we support? | OpenAPI 3.0.0+ vs 3.1.0+ | **Open** — Kiota supports 2.0, 3.0, 3.1 |
| 7 | Should generated code be AOT-compatible? | Current Kiota output uses reflection (Activator.CreateInstance) | **Open** |
| 8 | Do we need Windows/macOS/Linux cross-platform support for Phase 1 CLI? | NuGet needs runtime-specific publish or RID-agnostic framework-dependent | **Open** — framework-dependent with `RollForward=Major` covers this |

---

## 11. Risk Priority Matrix

| Risk | Probability | Impact | Priority |
|------|-------------|--------|----------|
| Phase 2 scope underestimated | High | High | **Critical** |
| Kiota version drift | Medium | High | **High** |
| Assembly version conflicts (P2) | Medium | High | **High** |
| Large spec performance (P2) | Medium | Medium | **Medium** |
| OpenAPI edge cases | High | Medium | **Medium** |
| IDE reliability (P2) | Low | High | **Medium** |
| Phase 1 → Phase 2 migration | Low | Medium | **Low** |
| Licensing concerns | Low | High | **Low** |
| netstandard2.0 limitations | Low | Low | **Low** |

---

## 12. Recommended Development Order

Based on the risk analysis, the recommended implementation sequence is:

### Phase 1 (Weeks 1–4)
1. `Uno.Extensions.Http.Kiota.Generator.Cli` — Console app wrapping Kiota.Builder
2. `Uno.Extensions.Http.Kiota.Generator.Tasks` — MSBuild task + props/targets
3. Integration tests with petstore spec
4. NuGet packaging and CI integration
5. **Ship Phase 1**

### Phase 2 Incremental (Weeks 5–16)
1. **Foundation** (Weeks 5–6): Project setup, OpenAPI parser, basic CodeDOM types
2. **Basic generation** (Weeks 7–9): Simple CRUD request builders + flat models
3. **Serialization** (Weeks 10–11): Full IParsable implementation, type dispatch tables
4. **Advanced models** (Weeks 12–13): Inheritance (allOf), compositions (oneOf/anyOf), enums
5. **Parity & polish** (Weeks 14–15): Golden-file parity tests, error handling, diagnostics
6. **Performance & edge cases** (Week 16): Large spec testing, IDE experience validation
7. **Ship Phase 2**
