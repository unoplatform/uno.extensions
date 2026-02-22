# Tasks: Kiota MSBuild Code Generation

**Input**: Design documents from `specs/kiota-source-gen/`
**Prerequisites**: plan.md (required), research.md, data-model.md, 01–06 spec documents

**Organization**: Tasks are organized by user story / phase to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Paths use the Uno.Extensions monorepo structure rooted at `src/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, solution setup, and package management

- [ ] T001 Update `src/Directory.Packages.props` — add package versions: `Microsoft.OpenApi.Kiota.Builder` 1.30.0, `Microsoft.OpenApi` 3.3.1, `Microsoft.OpenApi.Readers` 1.6.28, `DotNet.Glob` 3.1.3, `Microsoft.Build.Framework` 17.12.6, `Microsoft.Build.Utilities.Core` 17.12.6, `System.CommandLine` 2.0.0-beta4.22272.1
- [ ] T002 [P] Add new project entries to `Uno.Extensions.sln` under a `Kiota` solution folder: Generator.Cli, Generator.Tasks, SourceGenerator, Generator.Tests
- [ ] T003 [P] Acquire test OpenAPI spec files (petstore, inheritance, composed-types, enums, error-responses) and place in `src/Uno.Extensions.Http.Kiota.Generator.Tests/TestData/`
- [ ] T004 [P] Generate golden files by running Kiota CLI against each test spec and place in `src/Uno.Extensions.Http.Kiota.Generator.Tests/GoldenFiles/`

**Checkpoint**: Repository setup complete — all new projects build (even if empty), test data available.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before any user story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Uno.Extensions.Http.Kiota.Generator.Tests.csproj` — test project with MSTest, references to Generator.Tasks and SourceGenerator projects, TestData/GoldenFiles as content
- [ ] T006 Create base test infrastructure `src/Uno.Extensions.Http.Kiota.Generator.Tests/Parity/ParityTestBase.cs` — golden file comparison helper (load golden file, normalize whitespace/version strings, compare to generated output)

**Checkpoint**: Foundation ready — user story implementation can now begin.

---

## Phase 3: User Story 1 — MSBuild Task CLI Wrapper (Priority: P1) 🎯 MVP

**Goal**: `dotnet build` generates Kiota client code from an OpenAPI spec with zero manual steps, no tool installation required.

**Independent Test**: Run `kiota-gen --openapi petstore.json --output ./out --class-name PetStoreClient --namespace Test.PetStore` and verify generated files match golden output.

### Implementation for User Story 1

- [ ] T007 [US1] Create `src/Uno.Extensions.Http.Kiota.Generator.Cli/Uno.Extensions.Http.Kiota.Generator.Cli.csproj` — console app targeting net8.0;net9.0, references `Microsoft.OpenApi.Kiota.Builder` and `System.CommandLine`, PublishTrimmed, PublishSingleFile, AssemblyName=kiota-gen
- [ ] T008 [US1] Create `src/Uno.Extensions.Http.Kiota.Generator.Cli/GeneratorOptions.cs` — POCO mapping all CLI arguments to GenerationConfiguration properties (per data-model.md GeneratorOptions)
- [ ] T009 [US1] Create `src/Uno.Extensions.Http.Kiota.Generator.Cli/KiotaGeneratorCommand.cs` — System.CommandLine root command definition mapping all options to GeneratorOptions, then configuring GenerationConfiguration and invoking KiotaBuilder.GenerateClientAsync()
- [ ] T010 [US1] Create `src/Uno.Extensions.Http.Kiota.Generator.Cli/Program.cs` — entry point wiring KiotaGeneratorCommand, creating MSBuild-formatted logger (error KIOTA001/warning KIOTA002 format), returning exit code 0/1
- [ ] T011 [US1] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Phase1/CliIntegrationTests.cs` — test that CLI generates petstore output matching golden files, test error exit code on invalid spec, test each CLI argument maps correctly

**Checkpoint**: CLI wrapper generates Kiota clients standalone. Can be tested independently.

---

## Phase 4: User Story 2 — MSBuild Props/Targets Integration (Priority: P1)

**Goal**: Users add `<KiotaOpenApiReference Include="openapi.json" ... />` to their .csproj and `dotnet build` automatically generates client code.

**Independent Test**: Create a test .csproj with `<KiotaOpenApiReference>`, run `dotnet build`, verify generated code compiles and client class exists.

### Implementation for User Story 2

- [ ] T012 [US2] Create `src/Uno.Extensions.Http.Kiota.Generator.Tasks/Uno.Extensions.Http.Kiota.Generator.Tasks.csproj` — netstandard2.0, IsPackable, DevelopmentDependency, references Microsoft.Build.Framework/Utilities.Core, packs buildTransitive/ and CLI tool binaries
- [ ] T013 [US2] Create `src/Uno.Extensions.Http.Kiota.Generator.Tasks/buildTransitive/Uno.Extensions.Http.Kiota.Generator.props` — define `<KiotaOpenApiReference>` item group with `<ItemDefinitionGroup>` defaults (ClientClassName=ApiClient, Namespace=$(RootNamespace).Client, UsesBackingStore=false etc.), property defaults (KiotaGeneratorEnabled=true, KiotaOutputPath=$(IntermediateOutputPath)KiotaGenerated\)
- [ ] T014 [US2] Create `src/Uno.Extensions.Http.Kiota.Generator.Tasks/buildTransitive/Uno.Extensions.Http.Kiota.Generator.targets` — resolve `_KiotaGeneratorExe` per OS via MSBuild IsOSPlatform conditions, hook `_KiotaGenerate` target into `CoreCompileDependsOn`, implement Exec command with all item metadata arguments, Inputs/Outputs for incremental build, `_KiotaIncludeGenerated` to add generated .cs to Compile items, `_KiotaClean` target
- [ ] T015 [US2] Create `src/Uno.Extensions.Http.Kiota.Generator.Tasks/KiotaGenerateTask.cs` — optional MSBuild ITask implementation wrapping CLI invocation (alternative to Exec-based targets for better error reporting)
- [ ] T016 [US2] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Phase1/MsBuildTaskTests.cs` — test end-to-end build with KiotaOpenApiReference against petstore spec, verify incremental (second build skips), verify multiple references, verify error handling

**Checkpoint**: Phase 1 complete. `dotnet build` generates Kiota clients from OpenAPI specs with no tool install. Ready to ship.

---

## Phase 5: User Story 3 — Source Generator Project Setup & OpenAPI Parsing (Priority: P2)

**Goal**: Create the source generator project skeleton and implement OpenAPI document parsing within netstandard2.0 constraints.

**Independent Test**: Unit test that parses petstore.json into OpenApiDocument and extracts paths/schemas correctly.

### Implementation for User Story 3

- [ ] T017 [US3] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Uno.Extensions.Http.Kiota.SourceGenerator.csproj` — netstandard2.0, ToolOfPackage=Uno.Extensions.Http.Kiota, EnforceExtendedAnalyzerRules, references Uno.Roslyn, Microsoft.CodeAnalysis.CSharp, Microsoft.OpenApi, Microsoft.OpenApi.Readers, DotNet.Glob (all PrivateAssets=All)
- [ ] T018 [US3] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/buildTransitive/Uno.Extensions.Http.Kiota.SourceGenerator.props` — CompilerVisibleProperty declarations (KiotaGenerator_Enabled, defaults), CompilerVisibleItemMetadata for AdditionalFiles (KiotaClientName, KiotaNamespace, etc.)
- [ ] T019 [US3] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Configuration/KiotaGeneratorConfig.cs` — immutable config record implementing IEquatable<T> with all fields from data-model.md
- [ ] T020 [US3] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Configuration/ConfigurationReader.cs` — reads AnalyzerConfigOptionsProvider to build KiotaGeneratorConfig from file metadata + global properties
- [ ] T021 [US3] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Parsing/OpenApiDocumentParser.cs` — synchronous OpenAPI parsing using OpenApiStreamReader (v1.6.28), supports JSON and YAML, returns OpenApiDocument + diagnostics
- [ ] T022 [US3] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/KiotaSourceGenerator.cs` — initial IIncrementalGenerator skeleton: filter AdditionalFiles with KiotaClientName metadata, parse OpenAPI docs, placeholder for CodeDOM + emit
- [ ] T023 [P] [US3] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Phase2/OpenApiParserTests.cs` — test JSON parsing, YAML parsing, invalid spec error handling, path filtering with DotNet.Glob

**Checkpoint**: Source generator project exists, OpenAPI parsing works within netstandard2.0.

---

## Phase 6: User Story 4 — CodeDOM Implementation (Priority: P2)

**Goal**: Implement the language-agnostic CodeDOM types and builder that transforms OpenApiDocument into the intermediate representation.

**Independent Test**: Unit test that builds CodeDOM from petstore OpenApiDocument and verifies correct class count, method count, property types.

### Implementation for User Story 4

- [ ] T024 [P] [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/CodeElement.cs` — abstract base class for all CodeDOM types
- [ ] T025 [P] [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/CodeNamespace.cs` — namespace container with Classes, Enums, child Namespaces lists
- [ ] T026 [P] [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/CodeClass.cs` — class definition with Kind (RequestBuilder/Model/QueryParameters/RequestConfiguration), properties, methods, inner classes, base class
- [ ] T027 [P] [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/CodeMethod.cs` — method definition with Kind (Constructor/RequestExecutor/RequestGenerator/Serializer/Deserializer/Factory/WithUrl), return type, parameters, async flag
- [ ] T028 [P] [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/CodeProperty.cs` — property with Kind (Custom/UrlTemplate/PathParameters/RequestAdapter/Navigation/BackingStore/AdditionalData/QueryParameter), type, serialized name
- [ ] T029 [P] [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/CodeEnum.cs` — enum with options list, IsFlags flag
- [ ] T030 [P] [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/CodeType.cs` — type reference with name, nullability, collection kind, resolved TypeDefinition reference. Also `CodeTypeBase`, `CodeUnionType`, `CodeIntersectionType`
- [ ] T031 [P] [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/CodeIndexer.cs` and `CodeParameter.cs` — indexer and method parameter types
- [ ] T032 [US4] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/CodeDom/KiotaCodeDomBuilder.cs` — main builder: CreateRequestBuilders (walk OpenApiUrlTreeNode tree, create RequestBuilder classes with navigation/indexers/executors/generators), CreateModelDeclarations (schema→CodeClass, type mapping per data-model.md, composition handling: allOf→inheritance, oneOf→CodeUnionType, anyOf→CodeIntersectionType, discriminator factories), MapTypeDefinitions (resolve forward references), TrimInheritedModels
- [ ] T033 [US4] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Phase2/CodeDomBuilderTests.cs` — test petstore: correct client class, request builders for each path, model classes for schemas, property types map correctly; test inheritance spec: allOf produces base class; test composed types: oneOf/anyOf produce wrapper classes

**Checkpoint**: CodeDOM builder transforms OpenAPI docs into correct intermediate representation.

---

## Phase 7: User Story 5 — C# Refinement & Emitter (Priority: P2)

**Goal**: Implement C# language refinement and source code emission from CodeDOM, producing output matching Kiota CLI.

**Independent Test**: End-to-end test: parse petstore.json → build CodeDOM → refine → emit C# → compare with golden files.

### Implementation for User Story 5

- [ ] T034 [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Refinement/CSharpConventionService.cs` — type name mapping (CodeDOM→C# types), PascalCase/camelCase naming, reserved word escaping, global:: prefixing, nullable conditional compilation patterns
- [ ] T035 [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Refinement/CSharpRefiner.cs` — apply C#-specific transformations: add usings, escape reserved words, adjust types, wrap collections as List<T>, add nullable guards, generate composed type wrappers, backward compat classes
- [ ] T036 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/CSharpEmitter.cs` — main emitter orchestrating sub-emitters, iterates CodeDOM tree producing (hintName, source) tuples for each type
- [ ] T037 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/ClassDeclarationEmitter.cs` — emit `// <auto-generated/>`, `#pragma warning disable`, usings, namespace, `[GeneratedCode]` attribute, `partial class` declaration with inheritance and interface list
- [ ] T038 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/ConstructorEmitter.cs` — emit constructors with base() calls, serializer/deserializer registration for root client, path parameter dictionary setup
- [ ] T039 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/PropertyEmitter.cs` — emit properties with `#if NETSTANDARD2_1_OR_GREATER` nullable guards, backing store pattern when enabled, `global::` type references
- [ ] T040 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/MethodEmitter.cs` — emit executor methods (GetAsync/PostAsync), request info builders (ToGetRequestInformation), WithUrl method, indexer accessor
- [ ] T041 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/SerializerEmitter.cs` — emit `Serialize(ISerializationWriter)` body with correct WriteXxxValue calls per type dispatch table (data-model.md Section 3)
- [ ] T042 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/DeserializerEmitter.cs` — emit `GetFieldDeserializers()` returning Dictionary<string, Action<IParseNode>> with correct GetXxxValue calls per type dispatch table
- [ ] T043 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/FactoryMethodEmitter.cs` — emit `CreateFromDiscriminatorValue` static factory, discriminator switch for inheritance hierarchies
- [ ] T044 [P] [US5] Create `src/Uno.Extensions.Http.Kiota.SourceGenerator/Emitter/EnumEmitter.cs` — emit enum declarations with `[EnumMember]` attributes, optional `[Flags]` for flags enums
- [ ] T045 [US5] Wire complete pipeline in `src/Uno.Extensions.Http.Kiota.SourceGenerator/KiotaSourceGenerator.cs` — connect parse → CodeDOM build → refine → emit → AddSource in RegisterSourceOutput
- [ ] T046 [US5] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Phase2/CSharpEmitterTests.cs` — test individual emitters produce correct patterns (class declaration, property nullable guards, executor method shape, serialization dispatch)
- [ ] T047 [US5] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Phase2/GeneratorIntegrationTests.cs` — end-to-end source generator test: AdditionalFile with petstore.json + KiotaClientName metadata → verify generated source compiles

**Checkpoint**: Source generator produces C# code from OpenAPI specs. IDE integration working.

---

## Phase 8: User Story 6 — Parity Testing & Advanced Features (Priority: P2)

**Goal**: Ensure generated output matches Kiota CLI output for all supported spec patterns. Handle edge cases.

**Independent Test**: Golden-file parity tests pass for all test specs.

### Implementation for User Story 6

- [ ] T048 [US6] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Parity/PetstoreParityTests.cs` — compare generator output to Kiota CLI golden files for petstore.json, assert diffs are empty or contain only acceptable differences (version strings)
- [ ] T049 [US6] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Parity/InheritanceParityTests.cs` — parity test for allOf inheritance spec with discriminators
- [x] T050 [US6] Create `src/Uno.Extensions.Http.Kiota.Generator.Tests/Parity/ComposedTypeParityTests.cs` — parity test for oneOf/anyOf composed type specs
- [ ] T051 [US6] Fix deviations found by parity tests — iteratively update CodeDomBuilder, Refiner, and Emitter until parity achieved
- [ ] T052 [US6] Add error handling in source generator — wrap all logic in try/catch, report failures as diagnostics (KIOTA001–KIOTA040 per 06-risk-analysis.md), partial generation on individual type failure, fallback comment on complete failure
- [ ] T053 [US6] Add large spec performance testing — test against Microsoft Graph-sized spec (or equivalent), verify incremental caching prevents re-parse when spec unchanged, emit KIOTA020 warning for specs over recommended size

**Checkpoint**: All parity tests pass. Source generator handles edge cases gracefully.

---

## Phase 9: User Story 7 — NuGet Packaging & CI (Priority: P2)

**Goal**: Package Phase 1 (Generator NuGet) and Phase 2 (SourceGenerator bundled in host package) for distribution.

**Independent Test**: Install NuGet packages in a fresh project, verify both Phase 1 and Phase 2 work.

### Implementation for User Story 7

- [ ] T054 [US7] Configure NuGet packaging for `Uno.Extensions.Http.Kiota.Generator.Tasks` — pack buildTransitive/, CLI tool binaries (net8.0/net9.0), set DevelopmentDependency=true, SuppressDependenciesWhenPacking=true
- [ ] T055 [US7] Verify `ToolOfPackage` bundles source generator into `Uno.Extensions.Http.Kiota.nupkg` under `analyzers/dotnet/cs/` with all dependency DLLs
- [ ] T056 [US7] Update CI scripts in `build/ci/` — add build steps for new projects, test step for Generator.Tests, package step for Phase 1 NuGet, parity validation step
- [ ] T057 [P] [US7] Create end-to-end NuGet validation test — install packages in a fresh project, add KiotaOpenApiReference + AdditionalFiles, verify both paths produce working clients
- [ ] T058 [P] [US7] Update `Uno.Extensions-packageonly.slnf` to include new generator projects

**Checkpoint**: NuGet packages build and distribute correctly. CI pipeline validates everything.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and improvements across all user stories

- [ ] T059 [P] Create user documentation — usage guide for Phase 1 (`<KiotaOpenApiReference>`) and Phase 2 (`<AdditionalFiles KiotaClientName="...">`) in `doc/`
- [ ] T060 [P] Add XML doc comments to all public APIs in Generator.Cli, Generator.Tasks, and SourceGenerator projects
- [ ] T061 Code cleanup and formatting — ensure all new code passes StyleCop (per `stylecop.json` in repo root)
- [ ] T062 [P] Add migration documentation — guide for migrating from manual Kiota CLI to Phase 1, and from Phase 1 to Phase 2
- [ ] T063 Run quickstart.md validation — execute all validation steps from quickstart.md and confirm pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can then proceed in priority order (P1 → P2)
  - Or partially in parallel once Phase 4 (MSBuild) is done
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (CLI Wrapper, P1)**: Can start after Foundational (Phase 2) — No dependencies on other stories
- **US2 (MSBuild Integration, P1)**: Depends on US1 (needs CLI binary to invoke)
- **US3 (SourceGen Setup, P2)**: Can start after Foundational — No dependencies on US1/US2
- **US4 (CodeDOM, P2)**: Depends on US3 (needs project + parser)
- **US5 (Emitter, P2)**: Depends on US4 (needs CodeDOM types)
- **US6 (Parity, P2)**: Depends on US5 (needs working generator)
- **US7 (Packaging, P2)**: Depends on US2 + US5 (needs both phases working)

### Within Each User Story

- Models/types before services/logic
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- All tasks marked [P] within a phase can run in parallel
- US1 (CLI) and US3 (SourceGen Setup) can run in parallel since they're independent
- All emitter sub-tasks (T036–T044) can run in parallel
- All parity tests (T048–T050) can run in parallel
- All CodeDOM type files (T024–T031) can run in parallel

---

## Parallel Example: Phase 7 (C# Emitter)

```bash
# Launch all emitter sub-files in parallel:
Task: "Create CSharpEmitter.cs in src/.../Emitter/"
Task: "Create ClassDeclarationEmitter.cs in src/.../Emitter/"
Task: "Create ConstructorEmitter.cs in src/.../Emitter/"
Task: "Create PropertyEmitter.cs in src/.../Emitter/"
Task: "Create MethodEmitter.cs in src/.../Emitter/"
Task: "Create SerializerEmitter.cs in src/.../Emitter/"
Task: "Create DeserializerEmitter.cs in src/.../Emitter/"
Task: "Create FactoryMethodEmitter.cs in src/.../Emitter/"
Task: "Create EnumEmitter.cs in src/.../Emitter/"

# Then wire together (sequential):
Task: "Wire pipeline in KiotaSourceGenerator.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US1 — CLI Wrapper
4. Complete Phase 4: US2 — MSBuild Integration
5. **STOP and VALIDATE**: Test MSBuild integration with petstore spec
6. **Ship Phase 1 MVP**

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add US1 + US2 → Test independently → **Ship Phase 1** (MVP!)
3. Add US3 → Source generator project bootstrapped
4. Add US4 → CodeDOM works
5. Add US5 → Source generator emits code → Test independently
6. Add US6 → Parity validated
7. Add US7 → Packaged → **Ship Phase 2**
8. Polish → Documentation, cleanup

### Timeline Estimate

| Phase | Duration | Cumulative |
|-------|----------|------------|
| Setup + Foundational | 3 days | Week 1 |
| US1: CLI Wrapper | 3 days | Week 1 |
| US2: MSBuild Integration | 4 days | Week 2 |
| **Phase 1 Ship** | — | **Week 2** |
| US3: SourceGen Setup + Parser | 4 days | Week 3 |
| US4: CodeDOM Implementation | 8 days | Weeks 4–5 |
| US5: C# Emitter | 10 days | Weeks 6–7 |
| US6: Parity + Edge Cases | 5 days | Week 8 |
| US7: Packaging + CI | 3 days | Week 9 |
| **Phase 2 Ship** | — | **Week 9** |
| Polish | 3 days | Week 10 |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Phase 1 (US1+US2) is the MVP and ships first — Phase 2 development can proceed independently
- Golden-file parity testing is essential for Phase 2 correctness — establish golden files early (T004)
