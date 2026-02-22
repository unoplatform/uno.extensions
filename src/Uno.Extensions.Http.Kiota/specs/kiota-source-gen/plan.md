# Implementation Plan: Kiota MSBuild Code Generation

**Branch**: `kiota-source-gen` | **Date**: 2026-02-21 | **Spec**: [README.md](README.md)
**Input**: Feature specifications from `specs/kiota-source-gen/`

## Summary

Integrate Kiota OpenAPI code generation into the `dotnet build` pipeline for Uno.Extensions.Http.Kiota, eliminating the manual `dotnet tool install` + `kiota generate` workflow. Delivered in two phases: Phase 1 ships an MSBuild Task wrapping the existing `Kiota.Builder` library; Phase 2 adds a pure Roslyn `IIncrementalGenerator` for live IDE-time generation. Both phases produce C# output identical to the Kiota CLI, compatible with the existing `AddKiotaClient<T>()` DI surface.

## Technical Context

**Language/Version**: C# 12 / .NET 9.0 (runtime), netstandard2.0 (source generator & MSBuild task)
**Primary Dependencies**:
- `Microsoft.OpenApi.Kiota.Builder` 1.30.0 (Phase 1 CLI wrapper)
- `Microsoft.OpenApi` 3.3.1 + `Microsoft.OpenApi.Readers` 1.6.28 (Phase 2 source generator, netstandard2.0)
- `DotNet.Glob` 3.1.3 (path filtering, netstandard2.0)
- `System.CommandLine` 2.0.0-beta4 (Phase 1 CLI arg parsing)
- `Microsoft.Build.Framework` / `Microsoft.Build.Utilities.Core` 17.12.6 (Phase 1 MSBuild task)
- `Uno.Roslyn` 1.3.0-dev.12 (Phase 2 source generator)
- Existing Kiota runtime packages at 1.21.0 (`Microsoft.Kiota.Abstractions`, serialization libraries)
**Storage**: N/A (code generation, no persistence)
**Testing**: MSTest (matches existing repo convention), `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit` (Phase 2 generator tests)
**Target Platform**: All Uno Platform targets — Windows, iOS, Android, macOS, Linux, WebAssembly (build-time tool is host-only)
**Project Type**: Library + build tools within the Uno.Extensions monorepo
**Performance Goals**: Incremental builds skip generation when spec unchanged; IDE generation (Phase 2) completes in <2s for typical specs (<5K lines)
**Constraints**: Phase 2 source generator must target `netstandard2.0`; no `Span<T>`, no `async/await` in generator Execute; all dependencies must be bundled
**Scale/Scope**: Support OpenAPI specs up to Microsoft Graph size (~100K lines, ~2,500 endpoints) with appropriate path filtering

## Constitution Check

*GATE: Project must integrate cleanly into the Uno.Extensions monorepo.*

| Gate | Status | Notes |
|------|--------|-------|
| Follows `ToolOfPackage` pattern | **PASS** | Phase 2 generator uses `<ToolOfPackage>Uno.Extensions.Http.Kiota</ToolOfPackage>` per `Uno.Extensions.Core.Generators` precedent |
| No breaking changes to existing API | **PASS** | `AddKiotaClient<T>()` and `AddKiotaClientWithEndpoint<T, TEndpoint>()` remain unchanged |
| Central package management | **PASS** | All new packages added to `src/Directory.Packages.props` |
| Cross-platform build support | **PASS** | Phase 1: multi-RID or framework-dependent CLI; Phase 2: pure netstandard2.0 |
| Existing test conventions | **PASS** | MSTest + golden-file parity testing |

## Project Structure

### Documentation (this feature)

```text
src/Uno.Extensions.Http.Kiota/specs/kiota-source-gen/
├── README.md                              # Overview & problem statement
├── 01-kiota-architecture-analysis.md      # How Kiota works internally
├── 02-phase1-msbuild-task.md              # Phase 1 MSBuild Task spec
├── 03-phase2-source-generator.md          # Phase 2 Source Generator spec
├── 04-generated-code-structure.md         # Expected C# output patterns
├── 05-project-structure.md                # Repository layout changes
├── 06-risk-analysis.md                    # Risk assessment
├── plan.md                                # This file
├── research.md                            # Research findings
├── data-model.md                          # Data model (CodeDOM types)
├── quickstart.md                          # Quick start / validation guide
├── tasks.md                               # Actionable task list
└── checklists/
    └── implementation-readiness.md        # Quality checklist
```

### Source Code (repository root)

```text
src/
├── Directory.Packages.props                              (MODIFIED — add new package versions)
│
├── Uno.Extensions.Http.Kiota/                            (EXISTING — MODIFIED)
│   ├── Uno.Extensions.Http.Kiota.csproj                  (MODIFIED — ToolOfPackage ref)
│   └── ServiceCollectionExtensions.cs                    (UNCHANGED)
│
├── Uno.Extensions.Http.Kiota.Generator.Cli/              (NEW — Phase 1)
│   ├── Uno.Extensions.Http.Kiota.Generator.Cli.csproj
│   ├── Program.cs
│   ├── KiotaGeneratorCommand.cs
│   └── GeneratorOptions.cs
│
├── Uno.Extensions.Http.Kiota.Generator.Tasks/            (NEW — Phase 1)
│   ├── Uno.Extensions.Http.Kiota.Generator.Tasks.csproj
│   ├── KiotaGenerateTask.cs
│   └── buildTransitive/
│       ├── Uno.Extensions.Http.Kiota.Generator.props
│       └── Uno.Extensions.Http.Kiota.Generator.targets
│
├── Uno.Extensions.Http.Kiota.SourceGenerator/            (NEW — Phase 2)
│   ├── Uno.Extensions.Http.Kiota.SourceGenerator.csproj
│   ├── KiotaSourceGenerator.cs
│   ├── Configuration/
│   │   ├── KiotaGeneratorConfig.cs
│   │   └── ConfigurationReader.cs
│   ├── Parsing/
│   │   └── OpenApiDocumentParser.cs
│   ├── CodeDom/
│   │   ├── CodeElement.cs
│   │   ├── CodeNamespace.cs
│   │   ├── CodeClass.cs
│   │   ├── CodeMethod.cs
│   │   ├── CodeProperty.cs
│   │   ├── CodeEnum.cs
│   │   ├── CodeType.cs
│   │   ├── CodeUnionType.cs
│   │   ├── CodeIntersectionType.cs
│   │   ├── CodeIndexer.cs
│   │   ├── CodeParameter.cs
│   │   └── KiotaCodeDomBuilder.cs
│   ├── Refinement/
│   │   ├── CSharpRefiner.cs
│   │   └── CSharpConventionService.cs
│   ├── Emitter/
│   │   ├── CSharpEmitter.cs
│   │   ├── ClassDeclarationEmitter.cs
│   │   ├── ConstructorEmitter.cs
│   │   ├── PropertyEmitter.cs
│   │   ├── MethodEmitter.cs
│   │   ├── SerializerEmitter.cs
│   │   ├── DeserializerEmitter.cs
│   │   ├── FactoryMethodEmitter.cs
│   │   └── EnumEmitter.cs
│   └── buildTransitive/
│       └── Uno.Extensions.Http.Kiota.SourceGenerator.props
│
└── Uno.Extensions.Http.Kiota.Generator.Tests/            (NEW)
    ├── Uno.Extensions.Http.Kiota.Generator.Tests.csproj
    ├── Phase1/
    │   ├── CliIntegrationTests.cs
    │   └── MsBuildTaskTests.cs
    ├── Phase2/
    │   ├── OpenApiParserTests.cs
    │   ├── CodeDomBuilderTests.cs
    │   ├── CSharpEmitterTests.cs
    │   └── GeneratorIntegrationTests.cs
    ├── Parity/
    │   ├── ParityTestBase.cs
    │   ├── PetstoreParityTests.cs
    │   ├── InheritanceParityTests.cs
    │   └── ComposedTypeParityTests.cs
    ├── TestData/
    │   ├── petstore.json
    │   ├── inheritance.json
    │   ├── composed-types.json
    │   ├── enums.json
    │   └── error-responses.json
    └── GoldenFiles/
        └── petstore/
            ├── PetStoreClient.cs
            └── Models/
                ├── Pet.cs
                └── Error.cs
```

**Structure Decision**: Follows Uno.Extensions monorepo conventions — generators in separate projects under `src/`, test projects adjacent, `ToolOfPackage` pattern for bundling generators into host NuGet packages.

## Complexity Tracking

| Complexity | Justification | Simpler Alternative Rejected Because |
|------------|---------------|--------------------------------------|
| Two-phase delivery (MSBuild + SourceGen) | Phase 1 provides immediate value; Phase 2 delivers ideal DX | Single-phase SourceGen would delay delivery by 12+ weeks with high risk |
| ~15K LOC CodeDOM re-implementation (Phase 2) | Required because `Kiota.Builder` targets net8.0+, incompatible with netstandard2.0 source generators | Using `Kiota.Builder` directly is impossible in Roslyn generators |
| Multi-RID or framework-dependent CLI (Phase 1) | Must work on all CI/build environments | Single-RID would exclude macOS/Linux users |
| ILRepack dependency bundling (Phase 2) | Prevents assembly version conflicts in shared Roslyn `AssemblyLoadContext` | Separate DLLs risk `TypeLoadException` from other analyzers |
