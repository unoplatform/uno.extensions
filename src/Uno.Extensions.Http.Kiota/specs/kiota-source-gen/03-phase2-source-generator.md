# Phase 2: Pure Roslyn Incremental Source Generator

## Summary

Phase 2 delivers Kiota-compatible C# code generation as a Roslyn `IIncrementalGenerator` that runs inside the compiler and IDE. It reads OpenAPI `.json`/`.yaml` files from `AdditionalFiles`, parses them using `Microsoft.OpenApi` (which targets `netstandard2.0`), builds a Kiota-compatible CodeDOM, and emits C# source files directly into the compilation — providing live code generation during editing, not just at build time.

## Feasibility Analysis

### netstandard2.0 Compatibility

Roslyn source generators **must** target `netstandard2.0`. The critical question is: can we parse OpenAPI documents within that constraint?

| Dependency | netstandard2.0 Support | Notes |
|------------|----------------------|-------|
| `Microsoft.OpenApi` v3.3.1 | **Yes** | Core object model, JSON reader, dual-targets net8.0 + netstandard2.0 |
| `Microsoft.OpenApi.Readers` v1.6.28 | **Yes** | Legacy reader supporting both JSON and YAML, targets netstandard2.0 |
| `DotNet.Glob` v3.1.3 | **Yes** | Targets netstandard1.1+ |
| `YamlDotNet` v16.3.0 | **Yes** | Targets netstandard2.0 |

**Verdict**: OpenAPI parsing is fully feasible within `netstandard2.0`. The `Microsoft.OpenApi` core library and the legacy `Microsoft.OpenApi.Readers` package both provide the necessary parsing capabilities.

### What Cannot Be Reused from Kiota.Builder

`Microsoft.OpenApi.Kiota.Builder` targets `net8.0`/`net9.0`/`net10.0` only. It **cannot** be loaded in a source generator. Therefore, the following Kiota.Builder components must be **re-implemented** for `netstandard2.0`:

1. **CodeDOM classes** — `CodeNamespace`, `CodeClass`, `CodeMethod`, `CodeProperty`, `CodeEnum`, `CodeType`, `CodeIndexer`, `CodeUnionType`, `CodeIntersectionType`
2. **CodeDOM construction logic** — `CreateRequestBuilderClass()`, `CreateModelDeclarations()`, `MapTypeDefinitions()`, `TrimInheritedModels()`
3. **CSharpRefiner** — language-specific refinement
4. **CSharpWriter** and all sub-writers — `CodeClassDeclarationWriter`, `CodeMethodWriter`, `CodePropertyWriter`, `CodeEnumWriter`, etc.
5. **CSharpConventionService** — type mappings, naming conventions
6. **CSharpPathSegmenter** — file naming logic

Estimated scope: ~15,000 lines of C# code to port/re-implement.

## Architecture

```
┌────────────────────────────────────────────────────────────────┐
│  Roslyn IIncrementalGenerator Pipeline                         │
│                                                                │
│  ┌──────────────┐   ┌──────────────┐   ┌──────────────────┐  │
│  │ AdditionalFile│──▶│ OpenAPI Parse │──▶│ CodeDOM Builder  │  │
│  │ Provider      │   │ Layer        │   │                  │  │
│  │ (.json/.yaml) │   │ (M.OpenApi)  │   │ - Request Bldrs  │  │
│  └──────────────┘   └──────────────┘   │ - Models         │  │
│                                         │ - Enums          │  │
│  ┌──────────────┐                       │ - Composed Types │  │
│  │ Analyzer      │                       └────────┬─────────┘  │
│  │ ConfigOptions │─── Configuration ────────────▶ │            │
│  │ Provider      │                                │            │
│  └──────────────┘                                 ▼            │
│                                         ┌──────────────────┐  │
│                                         │ C# Emitter       │  │
│                                         │ - Class writer   │  │
│                                         │ - Method writer  │  │
│                                         │ - Property writer│  │
│                                         │ - Enum writer    │  │
│                                         └────────┬─────────┘  │
│                                                  ▼            │
│                                         ┌──────────────────┐  │
│                                         │ AddSource()      │  │
│                                         │ (into compilation)│  │
│                                         └──────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

## Layer 1: OpenAPI Parser

### Input: `AdditionalFiles`

The generator reads OpenAPI specs from `AdditionalFiles` with specific metadata markers:

```xml
<ItemGroup>
  <AdditionalFiles Include="openapi.json"
    KiotaClientName="PetStoreClient"
    KiotaNamespace="MyApp.PetStore" />
</ItemGroup>
```

The generator identifies Kiota-relevant files by checking:
1. File extension is `.json`, `.yaml`, or `.yml`
2. The `KiotaClientName` metadata is present (explicit opt-in)

### Parsing Pipeline

```csharp
// Inside IIncrementalGenerator.Initialize()
var openApiFiles = context.AdditionalTextsProvider
    .Combine(context.AnalyzerConfigOptionsProvider)
    .Where(static (pair) =>
    {
        var (file, options) = pair;
        options.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.KiotaClientName", out var clientName);
        return !string.IsNullOrEmpty(clientName) && IsOpenApiFile(file.Path);
    })
    .Select(static (pair, ct) =>
    {
        var (file, options) = pair;
        var text = file.GetText(ct);
        var config = ReadConfiguration(file, options);
        var document = ParseOpenApiDocument(text);
        return (document, config);
    });
```

### OpenAPI Parsing (netstandard2.0)

Using `Microsoft.OpenApi.Readers` v1.6.28:

```csharp
private static OpenApiDocument ParseOpenApiDocument(SourceText text)
{
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text.ToString()));
    var reader = new OpenApiStreamReader();
    var document = reader.Read(stream, out var diagnostic);
    // Report any diagnostic issues
    return document;
}
```

For the newer `Microsoft.OpenApi` v3.3.1 (also netstandard2.0):

```csharp
private static async Task<OpenApiDocument> ParseOpenApiDocument(SourceText text)
{
    // v3.3.1 has LoadAsync on OpenApiDocument directly
    var result = await OpenApiDocument.LoadAsync(new MemoryStream(Encoding.UTF8.GetBytes(text.ToString())));
    return result.Document;
}
```

> **Note**: Source generators execute synchronously. If using async APIs from `Microsoft.OpenApi`, we must use `.GetAwaiter().GetResult()` or prefer the synchronous reader from v1.6.x.

## Layer 2: CodeDOM Builder

This layer re-implements the core of `KiotaBuilder.CreateSourceModel()` — transforming an `OpenApiDocument` into a language-agnostic CodeDOM tree.

### CodeDOM Types to Implement

```
CodeElement (abstract base)
├── CodeNamespace
├── CodeClass
│   ├── Kind: RequestBuilder | Model | QueryParameters | RequestConfiguration
│   ├── Properties: List<CodeProperty>
│   ├── Methods: List<CodeMethod>
│   └── InnerClasses: List<CodeClass>
├── CodeMethod
│   ├── Kind: Constructor | RequestExecutor | RequestGenerator | Serializer |
│   │         Deserializer | Factory | Getter | Setter | IndexerAccessor | WithUrl
│   ├── ReturnType: CodeTypeBase
│   └── Parameters: List<CodeParameter>
├── CodeProperty
│   ├── Kind: Custom | UrlTemplate | PathParameters | RequestAdapter |
│   │         BackingStore | AdditionalData | QueryParameter | Navigation
│   └── Type: CodeTypeBase
├── CodeEnum
│   └── Options: List<CodeEnumOption>
├── CodeType
│   ├── Name: string
│   ├── TypeDefinition: CodeElement? (resolved reference)
│   └── IsNullable, IsCollection, etc.
├── CodeUnionType (oneOf)
├── CodeIntersectionType (anyOf)
└── CodeIndexer
```

### Request Builder Construction

Port `CreateRequestBuilderClass()` logic:

1. **Walk the URI space tree** from `OpenApiUrlTreeNode`:
   - For each non-parameterized segment → create a `CodeClass` (kind: `RequestBuilder`)
   - For each parameterized segment → create an indexer method

2. **For each HTTP operation** on a path:
   - Create an executor method (e.g., `GetAsync`, `PostAsync`)
   - Create a request information builder (e.g., `ToGetRequestInformation`)
   - Resolve return type from response schemas
   - Build error mappings from `4XX`/`5XX` response schemas

3. **Constructor generation**:
   - `(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter)` → delegates to `BaseRequestBuilder(adapter, urlTemplate, pathParams)`
   - `(string rawUrl, IRequestAdapter requestAdapter)` → raw URL override

4. **Navigation properties** for child segments:
   ```csharp
   public ChildRequestBuilder Child => new ChildRequestBuilder(PathParameters, RequestAdapter);
   ```

### Model Construction

Port `CreateModelDeclarations()` logic:

1. **For each schema** in `Components/Schemas`:
   - Create a `CodeClass` (kind: `Model`) with `IParsable` implementation
   - Map schema properties to `CodeProperty` instances

2. **Type mapping** from OpenAPI types:
   | OpenAPI type | format | C# type |
   |------|--------|---------|
   | `string` | — | `string` |
   | `string` | `date-time` | `DateTimeOffset` |
   | `string` | `date` | `DateOnly` |
   | `string` | `time` | `TimeOnly` |
   | `string` | `duration` | `TimeSpan` |
   | `string` | `uuid` | `Guid` |
   | `string` | `binary` | `Stream` |
   | `string` | `byte` | `byte[]` (base64) |
   | `integer` | `int32` | `int` |
   | `integer` | `int64` | `long` |
   | `number` | `float` | `float` |
   | `number` | `double` | `double` |
   | `number` | `decimal` | `decimal` |
   | `boolean` | — | `bool` |
   | `array` | — | `List<T>` |
   | `object` | — | nested `CodeClass` |

3. **Composition handling**:
   - `allOf` → resolve first `$ref` as base class, merge additional properties
   - `oneOf` → create `CodeUnionType` → generate wrapper class
   - `anyOf` → create `CodeIntersectionType` → generate wrapper class

4. **Discriminator support**:
   - When `discriminator.propertyName` and `discriminator.mapping` exist, generate a `CreateFromDiscriminatorValue` factory that reads the discriminator and instantiates the correct derived type

5. **Serialization members** for every model:
   - `void Serialize(ISerializationWriter writer)`
   - `IDictionary<string, Action<IParseNode>> GetFieldDeserializers()`
   - `static T CreateFromDiscriminatorValue(IParseNode parseNode)`

6. **Additional data** (when `IncludeAdditionalData` is `true`):
   - Add `IDictionary<string, object> AdditionalData { get; set; }` property
   - Implement `IAdditionalDataHolder`

7. **Backing store** (when `UsesBackingStore` is `true`):
   - Wrap properties with `IBackingStore` pattern
   - Implement `IBackedModel`

### Type Resolution

Port `MapTypeDefinitions()`:
- Walk all `CodeType` instances
- Resolve forward references to their `CodeClass` or `CodeEnum` definitions
- Handle circular references (recursive schemas)

## Layer 3: C# Emitter

This layer re-implements Kiota's `CSharpWriter` — converting the CodeDOM into C# source text.

### Emitter Architecture

```csharp
public class CSharpEmitter
{
    public IEnumerable<(string hintName, string source)> Emit(CodeNamespace rootNamespace)
    {
        foreach (var codeClass in rootNamespace.GetAllClasses())
        {
            var sb = new StringBuilder();
            EmitClass(sb, codeClass);
            yield return (GetHintName(codeClass), sb.ToString());
        }

        foreach (var codeEnum in rootNamespace.GetAllEnums())
        {
            var sb = new StringBuilder();
            EmitEnum(sb, codeEnum);
            yield return (GetHintName(codeEnum), sb.ToString());
        }
    }
}
```

### Required Sub-Emitters

| Sub-Emitter | Responsibility |
|-------------|----------------|
| `ClassDeclarationEmitter` | `// <auto-generated/>`, usings, namespace, `[GeneratedCode]`, `partial class`, inheritance |
| `ConstructorEmitter` | Constructor body with `base(...)` call, serializer/deserializer registration |
| `PropertyEmitter` | Properties with nullable conditional compilation guards |
| `ExecutorMethodEmitter` | `GetAsync`, `PostAsync`, etc. — build request info, call `RequestAdapter.SendAsync` |
| `RequestGeneratorEmitter` | `ToGetRequestInformation` — create `RequestInformation`, set method, URL template, headers |
| `SerializerEmitter` | `Serialize(ISerializationWriter)` — `writer.WriteStringValue(...)` per property |
| `DeserializerEmitter` | `GetFieldDeserializers()` — dictionary of field name → parser lambda |
| `FactoryMethodEmitter` | `CreateFromDiscriminatorValue` — discriminator switch logic |
| `IndexerEmitter` | Navigation access via ID parameter |
| `EnumEmitter` | Enum with `[EnumMember(Value = "...")]` attributes |
| `WithUrlMethodEmitter` | `WithUrl(string rawUrl)` — creates new builder with raw URL |

### Output Patterns

Every emitted type follows Kiota's conventions:

```csharp
// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace {namespace}
{
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "{version}")]
    public partial class {ClassName} : {BaseClass}
    {
        // ... members
    }
}
```

Nullable properties use conditional compilation:

```csharp
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public string? PropertyName { get; set; }
#nullable restore
#else
        public string PropertyName { get; set; }
#endif
```

All type references use `global::` prefix:

```csharp
public global::MyApp.Client.Models.User User { get; set; }
```

## Layer 4: Configuration

### MSBuild Properties (via `CompilerVisibleProperty`)

Following the pattern from `Uno.Extensions.Core.Generators/buildTransitive/Uno.Extensions.Core.props`:

```xml
<!-- buildTransitive/Uno.Extensions.Http.Kiota.SourceGenerator.props -->
<Project>
  <ItemGroup>
    <CompilerVisibleProperty Include="KiotaGenerator_Enabled" />
    <CompilerVisibleProperty Include="KiotaGenerator_DefaultUsesBackingStore" />
    <CompilerVisibleProperty Include="KiotaGenerator_DefaultIncludeAdditionalData" />
    <CompilerVisibleProperty Include="KiotaGenerator_DefaultExcludeBackwardCompatible" />
    <CompilerVisibleProperty Include="KiotaGenerator_DefaultTypeAccessModifier" />
  </ItemGroup>

  <!-- Make AdditionalFiles metadata visible to the generator -->
  <ItemGroup>
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="KiotaClientName" />
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="KiotaNamespace" />
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="KiotaUsesBackingStore" />
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="KiotaIncludeAdditionalData" />
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="KiotaExcludeBackwardCompatible" />
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="KiotaTypeAccessModifier" />
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="KiotaIncludePatterns" />
    <CompilerVisibleItemMetadata Include="AdditionalFiles" MetadataName="KiotaExcludePatterns" />
  </ItemGroup>
</Project>
```

### Reading Configuration in the Generator

```csharp
private static KiotaGeneratorConfig ReadConfiguration(
    AdditionalText file,
    AnalyzerConfigOptionsProvider optionsProvider)
{
    var fileOptions = optionsProvider.GetOptions(file);

    fileOptions.TryGetValue("build_metadata.AdditionalFiles.KiotaClientName", out var clientName);
    fileOptions.TryGetValue("build_metadata.AdditionalFiles.KiotaNamespace", out var ns);
    fileOptions.TryGetValue("build_metadata.AdditionalFiles.KiotaUsesBackingStore", out var backingStore);
    // ... etc.

    var globalOptions = optionsProvider.GlobalOptions;
    globalOptions.TryGetValue("build_property.KiotaGenerator_DefaultUsesBackingStore", out var defaultBackingStore);
    // ... etc.

    return new KiotaGeneratorConfig
    {
        ClientClassName = clientName ?? "ApiClient",
        ClientNamespaceName = ns ?? "ApiSdk",
        UsesBackingStore = bool.TryParse(backingStore ?? defaultBackingStore, out var bs) && bs,
        // ... etc.
    };
}
```

## User Experience

### Basic Usage

```xml
<ItemGroup>
  <AdditionalFiles Include="openapi.json"
    KiotaClientName="PetStoreClient"
    KiotaNamespace="MyApp.PetStore" />
</ItemGroup>
```

Generated code appears in the IDE immediately — IntelliSense, Go to Definition, and Find References all work. Code regenerates automatically when the OpenAPI file is saved.

### Multiple APIs

```xml
<ItemGroup>
  <AdditionalFiles Include="apis/petstore.json"
    KiotaClientName="PetStoreClient"
    KiotaNamespace="MyApp.PetStore" />

  <AdditionalFiles Include="apis/weather.yaml"
    KiotaClientName="WeatherClient"
    KiotaNamespace="MyApp.Weather" />
</ItemGroup>
```

### Advanced Configuration

```xml
<ItemGroup>
  <AdditionalFiles Include="openapi.json"
    KiotaClientName="GraphClient"
    KiotaNamespace="MyApp.Graph"
    KiotaUsesBackingStore="true"
    KiotaIncludeAdditionalData="true"
    KiotaExcludeBackwardCompatible="true"
    KiotaTypeAccessModifier="Internal"
    KiotaIncludePatterns="/users/**;/groups/**"
    KiotaExcludePatterns="/admin/**" />
</ItemGroup>
```

## Incremental Generator Pipeline

The `IIncrementalGenerator` pipeline must be carefully designed for correctness and performance:

```csharp
[Generator]
public class KiotaSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Filter AdditionalFiles to those with KiotaClientName metadata
        var kiotaFiles = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Where(HasKiotaMetadata)
            .Select(ParseAndBuildConfig);

        // 2. For each file: parse OpenAPI → build CodeDOM → emit C#
        context.RegisterSourceOutput(kiotaFiles, (spc, model) =>
        {
            var (document, config) = model;
            if (document == null) return;

            // Build CodeDOM
            var codeModel = new KiotaCodeDomBuilder(config).Build(document);

            // Refine for C#
            new CSharpRefiner(config).Refine(codeModel);

            // Emit C# source
            var emitter = new CSharpEmitter(config);
            foreach (var (hintName, source) in emitter.Emit(codeModel))
            {
                spc.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
            }
        });
    }
}
```

### Incremental Correctness

The Roslyn incremental pipeline automatically caches intermediate results:
- If the OpenAPI file hasn't changed (`SourceText` equality), the pipeline short-circuits
- If config hasn't changed (`AnalyzerConfigOptions` equality), intermediate results are reused
- Only when inputs actually change does the fullparse → build → emit pipeline execute

This is critical for IDE performance — large OpenAPI specs can take hundreds of milliseconds to parse and generate, but this cost is only paid when the spec file is modified.

## Dependency Bundling

All dependencies must be bundled into the generator's analyzer package since source generators can't rely on NuGet package resolution at analysis time.

### Strategy: ILRepack into Single Assembly

```xml
<!-- Uno.Extensions.Http.Kiota.SourceGenerator.csproj -->
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <IsPackable>false</IsPackable>
  <ToolOfPackage>Uno.Extensions.Http.Kiota</ToolOfPackage>
</PropertyGroup>

<ItemGroup>
  <!-- All dependencies with PrivateAssets="All" -->
  <PackageReference Include="Microsoft.OpenApi" PrivateAssets="All" />
  <PackageReference Include="Microsoft.OpenApi.Readers" PrivateAssets="All" />
  <PackageReference Include="DotNet.Glob" PrivateAssets="All" />
  <PackageReference Include="Uno.Roslyn" PrivateAssets="All" />
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="All" />
</ItemGroup>

<!-- ILRepack all dependencies into the generator assembly -->
<Target Name="ILRepack" AfterTargets="Build">
  <!-- Merge Microsoft.OpenApi, DotNet.Glob, etc. into the generator DLL -->
</Target>
```

Alternative: use `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` and ensure all dependency DLLs are included in the analyzer NuGet folder alongside the generator. The repo's existing generators use the `ToolOfPackage` pattern with `ReferenceCopyLocalPaths` copying to handle this.

### Assembly Loading Concerns

Roslyn loads analyzers in a shared `AssemblyLoadContext`. Potential conflicts:
- Different versions of `Microsoft.OpenApi` loaded by other analyzers
- Version conflicts with analyzers that also bundle `System.Text.Json`

**Mitigation**: ILRepack/ILMerge all dependencies with namespace internalization, ensuring no type conflicts with other loaded assemblies.

## Testing Strategy

### Unit Tests

1. **OpenAPI Parse Tests**: Verify that various OpenAPI specs parse correctly through `Microsoft.OpenApi`
2. **CodeDOM Builder Tests**: Verify that the CodeDOM accurately represents the API structure
3. **C# Emitter Tests**: Verify that emitted C# source matches Kiota CLI output

### Parity Tests

Golden-file testing against Kiota CLI output:

1. Take a set of reference OpenAPI specs (petstore, complex specs with allOf/oneOf/anyOf):
   - Simple CRUD API (petstore)
   - API with inheritance (`allOf`)
   - API with discriminated unions (`oneOf`)
   - API with intersection types (`anyOf`)
   - API with enums
   - API with error responses
   - API with query parameters
   - API with path parameters and indexers

2. Generate with the Kiota CLI → save as golden files
3. Generate with the source generator → compare output
4. Diff must be empty (or contain only acceptable differences like version strings)

### Integration Tests

1. **Build an actual project** with the source generator referencing a test OpenAPI spec
2. **Verify compilation** succeeds
3. **Verify the generated client** can be registered with `AddKiotaClient<T>()`
4. **Verify basic API calls** work against a mock server

## netstandard2.0 Challenges and Mitigations

| Challenge | Impact | Mitigation |
|-----------|--------|------------|
| No `Span<T>` / `Memory<T>` | Can't use span-based parsing optimizations | Use `string`/`byte[]` based APIs; acceptable performance for this use case |
| No `System.Text.Json` built-in | Must rely on `Microsoft.OpenApi`'s own JSON parsing | `Microsoft.OpenApi.Readers` handles JSON parsing internally |
| No `async`/`await` in generator Execute | Can't use async `Microsoft.OpenApi` APIs | Use synchronous reader (`OpenApiStreamReader.Read()`) or `.GetAwaiter().GetResult()` |
| No `IAsyncEnumerable<T>` | Can't stream results | Build full results in memory; acceptable for code generation |
| No `init` accessors | Can't use modern property patterns | Use `{ get; set; }` for all internal CodeDOM types |
| No default interface methods | Can't use DIM for shared behavior | Use abstract base classes or helper methods |
| No `System.HashCode` | Can't use modern hash code combiners | Implement manual hash code computation |
| No `StringSyntaxAttribute` | Minor — no syntax highlighting hints | Skip; not needed for generation |
| Limited string interpolation | No interpolated string handlers | Use `StringBuilder` and `string.Format` |

## Migration Path from Phase 1

Phase 1 (MSBuild task) and Phase 2 (source generator) can coexist:

1. **Initial release**: Ship Phase 1 only (`<KiotaOpenApiReference>`)
2. **Phase 2 development**: Build source generator in parallel
3. **Dual availability**: Ship both; users choose:
   - `<KiotaOpenApiReference>` → Phase 1 (MSBuild task)
   - `<AdditionalFiles KiotaClientName="...">` → Phase 2 (source generator)
4. **Eventual default**: Once Phase 2 achieves parity and stability, make it the recommended default
5. **Phase 1 deprecation**: Keep Phase 1 available for edge cases (YAML support if Phase 2 can't load YAML reader, extremely large specs, etc.)
