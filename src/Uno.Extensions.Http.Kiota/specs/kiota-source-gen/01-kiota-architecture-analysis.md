# Kiota Architecture Analysis

This document provides a detailed analysis of how the [Kiota](https://github.com/microsoft/kiota) code generator works internally, serving as the reference for both Phase 1 (MSBuild Task) and Phase 2 (Source Generator) implementation.

## High-Level Pipeline

```
┌───────────────────────┐
│  OpenAPI Spec          │  JSON or YAML, local file or URL
│  (.json / .yaml)       │
└──────────┬────────────┘
           ▼
┌───────────────────────┐
│  1. Load & Parse       │  Microsoft.OpenApi → OpenApiDocument + OpenApiDiagnostic
│     (OpenAPI Reader)   │
└──────────┬────────────┘
           ▼
┌───────────────────────┐
│  2. Filter Paths       │  Include/exclude glob patterns via DotNet.Glob
└──────────┬────────────┘
           ▼
┌───────────────────────┐
│  3. Create URI Space   │  OpenApiUrlTreeNode — hierarchical tree of API paths
└──────────┬────────────┘
           ▼
┌───────────────────────┐
│  4. Build CodeDOM      │  Language-agnostic intermediate representation
│     (Source Model)     │  CodeNamespace → CodeClass → CodeMethod/CodeProperty
└──────────┬────────────┘
           ▼
┌───────────────────────┐
│  5. Language Refinement│  CSharpRefiner — adjust types, add usings, fix names
└──────────┬────────────┘
           ▼
┌───────────────────────┐
│  6. Write C# Files     │  CSharpWriter → one .cs file per class
└───────────────────────┘
```

## Stage 1: Loading & Parsing the OpenAPI Document

### Entry Point

`KiotaBuilder` is the main orchestrator. Constructed with:
```csharp
new KiotaBuilder(ILogger<KiotaBuilder> logger, GenerationConfiguration config, HttpClient httpClient)
```

### Loading

- `LoadStreamAsync()` downloads or reads the OpenAPI description from a URL or local file path (`GenerationConfiguration.OpenAPIFilePath`).
- Supports both JSON and YAML formats.
- Implements caching via `DocumentCachingProvider` (hashes content for change detection).
- For URL sources, uses the provided `HttpClient`; for local files, reads from disk.

### Parsing

- `CreateOpenApiDocumentWithResultAsync()` delegates to `OpenApiDocumentDownloadService.GetDocumentWithResultFromStreamAsync()`.
- Uses `Microsoft.OpenApi.Reader` namespace (from the `Microsoft.OpenApi` NuGet package v3.3.1).
- Produces:
  - `OpenApiDocument` — the fully resolved object model containing `Paths`, `Components/Schemas`, `Servers`, etc.
  - `OpenApiDiagnostic` — validation warnings and errors.
- Supports OpenAPI v2 (Swagger), v3.0, and v3.1 specifications.

### Key Library: `Microsoft.OpenApi`

| Package | Version | TFMs | Purpose |
|---------|---------|------|---------|
| `Microsoft.OpenApi` | 3.3.1 | net8.0, **netstandard2.0** | Core object model and serializers |
| `Microsoft.OpenApi.YamlReader` | 3.3.1 | net8.0 | YAML reading support |
| `Microsoft.OpenApi.Readers` | 1.6.28 | **netstandard2.0** | Legacy reader (JSON+YAML), netstandard2.0 compatible |

> **Critical for Phase 2**: `Microsoft.OpenApi` v3.3.1 targets `netstandard2.0`, meaning it can be loaded in a Roslyn source generator. The legacy `Microsoft.OpenApi.Readers` v1.6.x also targets `netstandard2.0` and supports both YAML and JSON.

## Stage 2: Path Filtering

After parsing, `FilterPathsByPatterns()` applies `GenerationConfiguration.IncludePatterns` and `ExcludePatterns`:

- Uses [DotNet.Glob](https://www.nuget.org/packages/DotNet.Glob/) (v3.1.3) for glob pattern matching.
- Filters both URL paths and HTTP methods.
- This allows consumers to generate clients for a subset of the API (e.g., only `/users/**` endpoints).

## Stage 3: URI Space Tree

`CreateUriSpace()` builds an `OpenApiUrlTreeNode` tree from the filtered `OpenApiDocument`:

```
Root
├── users
│   ├── {id}          → parameterized segment (indexer)
│   │   ├── messages
│   │   └── profile
│   └── me
├── posts
│   └── {postId}
└── auth
    └── login
```

Each node stores:
- The path segment name
- The `OpenApiPathItem` (containing operations for GET, POST, PUT, DELETE, etc.)
- Child nodes (sub-paths)
- Whether the segment is parameterized (e.g., `{id}`)

## Stage 4: CodeDOM Construction

`KiotaBuilder.CreateSourceModel()` builds the **language-agnostic CodeDOM** — the intermediate representation from which all language-specific code is generated.

### CodeDOM Class Hierarchy

| Type | Purpose |
|------|---------|
| `CodeNamespace` | Namespace hierarchy; root → client namespace → models namespace |
| `CodeClass` | Classes — request builders, models, query parameter classes, request configs |
| `CodeMethod` | Methods — constructors, request executors, request generators, serializers, deserializers, factory methods |
| `CodeProperty` | Properties — data properties, navigation properties, URL template, path parameters, request adapter, backing store, additional data |
| `CodeEnum` | Enumeration types |
| `CodeEnumOption` | Individual enum members |
| `CodeType` / `CodeTypeBase` | Type references — primitive, external (from NuGet), or internal (from another CodeClass/CodeEnum) |
| `CodeIndexer` | Collection indexers (`users["{id}"]`) |
| `CodeUnionType` | Composed types from `oneOf` schemas |
| `CodeIntersectionType` | Composed types from `anyOf` schemas |
| `CodeUsing` | Import/using statements |
| `CodeFile` | File-level container (used for TypeScript/Go modules) |
| `CodeInterface` | Interface declarations |

### Request Builder Generation

`CreateRequestBuilderClass()` recursively traverses the URI space tree and creates:

1. **A `CodeClass` (kind: `RequestBuilder`)** for each URL segment:
   - Extends `BaseRequestBuilder`
   - Has a URL template string (e.g., `"{+baseurl}/users/{user%2Did}"`)

2. **Navigation properties** for child segments:
   ```csharp
   public UsersRequestBuilder Users => new UsersRequestBuilder(PathParameters, RequestAdapter);
   ```

3. **Indexers** for parameterized segments:
   ```csharp
   public UserItemRequestBuilder this[string id] => new UserItemRequestBuilder(...)
   ```

4. **Executor methods** for each HTTP operation:
   ```csharp
   public async Task<User?> GetAsync(Action<RequestConfiguration<GetQueryParameters>>? config = default, CancellationToken ct = default)
   {
       var requestInfo = ToGetRequestInformation(config);
       return await RequestAdapter.SendAsync<User>(requestInfo, User.CreateFromDiscriminatorValue, errorMapping, ct);
   }
   ```

5. **Request generator methods** (information builders):
   ```csharp
   public RequestInformation ToGetRequestInformation(Action<RequestConfiguration<GetQueryParameters>>? config = default)
   {
       var requestInfo = new RequestInformation(Method.GET, UrlTemplate, PathParameters);
       requestInfo.Configure(config);
       requestInfo.Headers.TryAdd("Accept", "application/json");
       return requestInfo;
   }
   ```

6. **Error mappings** from HTTP status codes to error model types:
   ```csharp
   var errorMapping = new Dictionary<string, ParsableFactory<IParsable>> {
       { "4XX", ODataError.CreateFromDiscriminatorValue },
       { "5XX", ODataError.CreateFromDiscriminatorValue },
   };
   ```

7. **Constructors** accepting `(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter)` and `(string rawUrl, IRequestAdapter requestAdapter)`.

8. **`WithUrl` method** for raw URL overrides.

### Model Generation

`CreateModelDeclarations()` creates model classes from `OpenApiSchema` objects in `Components/Schemas`:

#### Simple Models (flat properties)
- Each schema property → `CodeProperty` on the `CodeClass`
- Types mapped: `string`, `integer` (→ `int`/`long`), `number` (→ `double`/`float`/`decimal`), `boolean`, `array`, `object`
- Format specifiers: `date-time` → `DateTimeOffset`, `date` → `DateOnly`, `time` → `TimeOnly`, `duration` → `TimeSpan`, `uuid` → `Guid`, `binary` → `byte[]`/`Stream`

#### Composition Patterns
- **`allOf`** → C# class inheritance. First ref is base class, additional properties are added.
- **`oneOf`** → `CodeUnionType` → generates a wrapper class implementing `IComposedTypeWrapper`.
- **`anyOf`** → `CodeIntersectionType` → similar wrapper class.

#### Discriminator Support
- When a schema has `discriminator.propertyName` and `discriminator.mapping`, Kiota generates a `CreateFromDiscriminatorValue` factory method that reads the discriminator property from the parse node and instantiates the correct derived type.

#### Serialization Members
Every model class gets:
- `Serialize(ISerializationWriter writer)` — writes all properties
- `GetFieldDeserializers()` → `IDictionary<string, Action<IParseNode>>` — maps JSON keys to property setters
- `CreateFromDiscriminatorValue(IParseNode parseNode)` — static factory method

#### Additional Data & Backing Store
- When `IncludeAdditionalData` is `true` (default): adds `IDictionary<string, object> AdditionalData` property and `IAdditionalDataHolder` interface.
- When `UsesBackingStore` is `true`: wraps all properties with a `IBackingStore`-backed pattern for dirty tracking.

### Type Definition Resolution

`MapTypeDefinitions()` resolves all `CodeType` references to their actual `CodeClass` or `CodeEnum` definitions. This handles cross-file references and ensures all type links are resolved.

### Model Trimming

`TrimInheritedModels()` removes model classes that aren't referenced by any request builder, reducing the generated surface area.

## Stage 5: Language Refinement

`ApplyLanguageRefinementAsync()` invokes `CSharpRefiner.RefineAsync()` which applies C#-specific transformations to the CodeDOM:

1. **Add using statements** — resolves and adds required `using` directives for each file
2. **Reserved keyword handling** — escapes C# reserved words used as property/class names (using `CSharpReservedNamesProvider`, `CSharpReservedClassNamesProvider`, `CSharpExceptionsReservedNamesProvider`, `CSharpReservedTypesProvider`)
3. **Type name adjustments** — maps CodeDOM types to C# types (e.g., `integer`→`int`, `boolean`→`bool`)
4. **Collection types** — wraps array types as `List<T>`
5. **Nullability** — adds conditional `#nullable enable/restore` blocks with `#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER` guards
6. **Composed type wrappers** — generates wrapper classes for `oneOf`/`anyOf` types
7. **Naming conventions** — PascalCase for public members, camelCase for parameters
8. **Backward compatibility** — generates deprecated query parameter config classes when `ExcludeBackwardCompatible` is `false`

## Stage 6: C# Code Writer

### Writer Architecture

`CSharpWriter` registers specialized sub-writers for each CodeDOM element:

| Writer | Handles |
|--------|---------|
| `CodeClassDeclarationWriter` | Class declarations, inheritance, interface implementations |
| `CodeMethodWriter` | Method bodies — request execution, serialization, deserialization, constructors, factory methods |
| `CodePropertyWriter` | Property declarations with nullable guards |
| `CodeEnumWriter` | Enum declarations with `[EnumMember]` attributes |
| `CodeIndexerWriter` | Indexer accessor methods |
| `CodeBlockEndWriter` | Closing braces |
| `CodeTypeWriter` | Type references (`global::` prefixed) |

### Conventions (`CSharpConventionService`)

- Type mappings: maps abstract types to C# concrete types
- Access modifiers: `public`, `internal` based on `GenerationConfiguration.TypeAccessModifier`
- `global::` prefixing on all type references for unambiguous resolution
- `partial class` for all generated classes
- `[GeneratedCode("Kiota", "{version}")]` attribute on all types

### File Structure (`CSharpPathSegmenter`)

- One `.cs` file per `CodeClass` or `CodeEnum`
- File path mirrors namespace hierarchy:
  ```
  Models/User.cs
  Models/AuthResponse.cs
  Users/UsersRequestBuilder.cs
  Users/Item/UserItemRequestBuilder.cs
  ```

## GenerationConfiguration — C#-Relevant Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `OpenAPIFilePath` | `string` | `"openapi.yaml"` | Path or URL to the OpenAPI file |
| `OutputPath` | `string` | `"./output"` | Directory for generated files |
| `ClientClassName` | `string` | `"ApiClient"` | Name of the root client class |
| `ClientNamespaceName` | `string` | `"ApiSdk"` | Root namespace for generated code |
| `Language` | `GenerationLanguage` | `CSharp` | Target language |
| `UsesBackingStore` | `bool` | `false` | Enable `IBackingStore` pattern |
| `IncludeAdditionalData` | `bool` | `true` | Add `AdditionalData` dictionary |
| `ExcludeBackwardCompatible` | `bool` | `false` | Skip deprecated compat code |
| `TypeAccessModifier` | `string` | `"Public"` | Access modifier for types (`Public`/`Internal`) |
| `Serializers` | `List<string>` | Json, Text, Form, Multipart factories | Serialization writer factories to register |
| `Deserializers` | `List<string>` | Json, Text, Form factories | Parse node factories to register |
| `StructuredMimeTypes` | `List<string>` | `application/json`, etc. | MIME types with preference weights |
| `IncludePatterns` | `List<string>` | `[]` | Glob patterns for paths to include |
| `ExcludePatterns` | `List<string>` | `[]` | Glob patterns for paths to exclude |
| `CleanOutput` | `bool` | `false` | Delete output dir before generating |
| `DisabledValidationRules` | `List<string>` | `[]` | Validation rules to skip |
| `MaxDegreeOfParallelism` | `int` | `-1` | Parallelism for CodeDOM construction |
| `DisableSSLValidation` | `bool` | `false` | Skip SSL certificate checks |

## Kiota.Builder Dependencies

### Build-time Dependencies (for Phase 1)

| Package | Version | TFMs | Purpose |
|---------|---------|------|---------|
| `Microsoft.OpenApi` | 3.3.1 | net8.0, netstandard2.0 | OpenAPI parsing, object model |
| `Microsoft.OpenApi.YamlReader` | 3.3.1 | net8.0 | YAML format support |
| `Microsoft.OpenApi.ApiManifest` | 3.0.0-preview.1 | net8.0 | API manifest support |
| `Microsoft.Kiota.Bundle` | 1.21.3 | net8.0 | Runtime abstractions bundle |
| `DotNet.Glob` | 3.1.3 | netstandard1.1+ | Glob pattern matching |
| `YamlDotNet` | 16.3.0 | net8.0, netstandard2.0 | YAML processing |
| `AsyncKeyedLock` | 8.0.2 | net8.0 | Concurrent keyed locking |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.3 | net8.0 | Logging |

### Runtime Dependencies (already in Uno.Extensions.Http.Kiota)

All at version **1.21.0** as declared in `src/Directory.Packages.props`:

| Package | Purpose |
|---------|---------|
| `Microsoft.Kiota.Abstractions` | Core interfaces: `IRequestAdapter`, `BaseRequestBuilder`, `IParsable`, `ISerializationWriter`, `IParseNode` |
| `Microsoft.Kiota.Http.HttpClientLibrary` | `HttpClientRequestAdapter` implementation |
| `Microsoft.Kiota.Serialization.Json` | JSON serialization/deserialization via `System.Text.Json` |
| `Microsoft.Kiota.Serialization.Text` | Plain text serialization |
| `Microsoft.Kiota.Serialization.Form` | URL-encoded form serialization |
| `Microsoft.Kiota.Serialization.Multipart` | Multipart form data serialization |

## Programmatic API

`KiotaBuilder` can be used as a library (not just CLI). The full generation is:

```csharp
var config = new GenerationConfiguration
{
    OpenAPIFilePath = "path/to/openapi.json",
    OutputPath = "./generated",
    Language = GenerationLanguage.CSharp,
    ClientClassName = "MyApiClient",
    ClientNamespaceName = "MyApp.Client",
};

var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<KiotaBuilder>();
var httpClient = new HttpClient();
var builder = new KiotaBuilder(logger, config, httpClient);
var result = await builder.GenerateClientAsync(cancellationToken);
// result contains the list of generated file paths
```

Individual pipeline stages are also accessible:
- `builder.CreateUriSpace()` — build the URL tree
- `builder.CreateSourceModel()` — build the CodeDOM
- `builder.ApplyLanguageRefinementAsync()` — refine for target language
- `builder.CreateLanguageSourceFilesAsync()` — write files

This programmatic API is the foundation for Phase 1.
