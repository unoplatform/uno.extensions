---
uid: Uno.Extensions.Http.HowToKiotaSourceGenerator
---
# Kiota Source Generator Reference

> **UnoFeatures:** `HttpKiota` (add to `<UnoFeatures>` in your `.csproj`)

The Kiota source generator is a **Roslyn incremental source generator** that produces strongly-typed C# API client code directly inside the compiler process. It reads OpenAPI specification files declared as `AdditionalFiles` items, builds a code model, and emits C# source — all without requiring a separate CLI tool or build step.

This page is a deep-dive reference for developers who want to understand how the generator works, how to configure it in detail, and how to troubleshoot issues. For a quick-start guide, see [Generate Kiota clients at build time](xref:Uno.Extensions.Http.HowToKiotaBuildGeneration).

---

## How it works

The generator executes a five-stage pipeline each time the OpenAPI spec file or configuration changes:

```
AdditionalFiles
     │
     ▼
┌─────────────────────┐
│ 1. Filter            │  Only files with KiotaClientName metadata
│    (.json/.yaml/.yml)│  and a supported extension are selected.
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│ 2. Parse             │  OpenApiStreamReader (Microsoft.OpenApi)
│    OpenAPI document  │  parses the spec into an OpenApiDocument.
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│ 3. Build CodeDOM     │  KiotaCodeDomBuilder translates the
│    (code model)      │  OpenApiDocument into a CodeNamespace tree.
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│ 4. Refine for C#     │  CSharpRefiner applies C#-specific naming
│                      │  conventions and language adjustments.
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│ 5. Emit C# source   │  CSharpEmitter writes .g.cs files for each
│    files             │  type and registers them with Roslyn.
└─────────────────────┘
```

The pipeline is **incremental**: Roslyn caches intermediate results and skips re-generation when neither the file content nor the MSBuild configuration changes between compilations. This makes rebuilds fast for unchanged specs.

---

## Getting started

### 1. Add a spec as an AdditionalFiles item

```xml
<ItemGroup>
  <AdditionalFiles Include="openapi.json"
    KiotaClientName="PetStoreClient"
    KiotaNamespace="MyApp.PetStore" />
</ItemGroup>
```

The presence of `KiotaClientName` metadata is the opt-in signal — files without it are ignored.

### 2. Build or open in IDE

- **IntelliSense**: The generated `PetStoreClient` type appears under **Dependencies > Analyzers > Uno.Extensions.Http.Kiota.SourceGenerator** within seconds.
- **Build**: `dotnet build` compiles the generated source normally alongside the rest of your code.

### 3. Register and use the client

```csharp
hostBuilder.UseHttp((context, services) =>
    services.AddKiotaClient<PetStoreClient>(
        context,
        options: new EndpointOptions { Url = "https://petstore.example.com" }
    )
);
```

---

## Per-file metadata reference

Each `<AdditionalFiles>` item with `KiotaClientName` metadata supports the following configuration:

| Metadata | Type | Default | Description |
|----------|------|---------|-------------|
| `KiotaClientName` | string | *(required)* | Name of the root client class. Acts as the opt-in marker. |
| `KiotaNamespace` | string | `ApiSdk` | Root namespace for all generated types. |
| `KiotaUsesBackingStore` | bool | `false` | Generated models implement `IBackedModel` and use `IBackingStore` for property storage. Enables change tracking scenarios. |
| `KiotaIncludeAdditionalData` | bool | `true` | Generated models implement `IAdditionalDataHolder` and expose an `AdditionalData` dictionary for extra JSON properties. |
| `KiotaExcludeBackwardCompatible` | bool | `false` | When `true`, deprecated backward-compatible overloads are not generated, producing a smaller output. |
| `KiotaTypeAccessModifier` | string | `Public` | `Public` or `Internal`. Controls the access modifier on all generated types. |
| `KiotaIncludePatterns` | string | *(empty — all paths)* | Semicolon-separated glob patterns restricting which API paths to include (e.g. `**/pets/**;**/stores/**`). |
| `KiotaExcludePatterns` | string | *(empty)* | Semicolon-separated glob patterns restricting which API paths to exclude. Applied after include patterns. |

### Example with all metadata

```xml
<AdditionalFiles Include="openapi.yaml"
  KiotaClientName="MyApiClient"
  KiotaNamespace="MyApp.Api"
  KiotaUsesBackingStore="true"
  KiotaIncludeAdditionalData="true"
  KiotaExcludeBackwardCompatible="true"
  KiotaTypeAccessModifier="Internal"
  KiotaIncludePatterns="**/users/**;**/orders/**"
  KiotaExcludePatterns="**/admin/**" />
```

---

## Global properties

These MSBuild properties apply to **all** `AdditionalFiles` items processed by the source generator. Set them in a `<PropertyGroup>` in your `.csproj` or `Directory.Build.props`:

| Property | Default | Description |
|----------|---------|-------------|
| `KiotaGenerator_Enabled` | `true` | Set to `false` to disable the source generator entirely. Useful for CI builds that use the MSBuild task instead. |
| `KiotaGenerator_DefaultUsesBackingStore` | `false` | Default value for `KiotaUsesBackingStore` when not set per-file. |
| `KiotaGenerator_DefaultIncludeAdditionalData` | `true` | Default value for `KiotaIncludeAdditionalData` when not set per-file. |
| `KiotaGenerator_DefaultExcludeBackwardCompatible` | `false` | Default value for `KiotaExcludeBackwardCompatible` when not set per-file. |
| `KiotaGenerator_DefaultTypeAccessModifier` | `Public` | Default value for `KiotaTypeAccessModifier` when not set per-file. |

### Configuration precedence

Per-file metadata always takes precedence over global defaults:

1. Per-file `KiotaUsesBackingStore` metadata on the `<AdditionalFiles>` item
2. Global `KiotaGenerator_DefaultUsesBackingStore` property
3. Built-in default (`false`)

---

## Multiple clients

You can generate multiple clients from different specs in the same project by adding multiple `<AdditionalFiles>` items:

```xml
<ItemGroup>
  <AdditionalFiles Include="specs/petstore.json"
    KiotaClientName="PetStoreClient"
    KiotaNamespace="MyApp.PetStore" />

  <AdditionalFiles Include="specs/weather.yaml"
    KiotaClientName="WeatherClient"
    KiotaNamespace="MyApp.Weather" />
</ItemGroup>
```

Each spec produces an independent set of generated types in its own namespace.

---

## Supported OpenAPI formats

The source generator supports the following input formats:

| Format | File extensions | OpenAPI versions |
|--------|-----------------|------------------|
| JSON | `.json` | 2.0 (Swagger), 3.0, 3.1 |
| YAML | `.yaml`, `.yml` | 2.0 (Swagger), 3.0, 3.1 |

The format is auto-detected from the file extension. Both local files and files from NuGet package content are supported as long as they are declared via `<AdditionalFiles>`.

---

## Path filtering

Use glob patterns to restrict which API endpoints are included in the generated client. This is useful when your spec contains many endpoints but you only need a subset.

```xml
<AdditionalFiles Include="large-api.json"
  KiotaClientName="SubsetClient"
  KiotaNamespace="MyApp.Subset"
  KiotaIncludePatterns="**/pets/**;**/stores/**"
  KiotaExcludePatterns="**/admin/**" />
```

- When `KiotaIncludePatterns` is empty, **all** paths are included.
- `KiotaExcludePatterns` is applied **after** include patterns.
- Multiple patterns are separated by semicolons (`;`).
- Patterns use the same glob syntax as the Kiota CLI (`--include-path` / `--exclude-path`).

---

## Performance considerations

The source generator runs in the IDE hot path (same process as IntelliSense). For most API specs, generation is fast and transparent. However, for very large specs, be aware of the following:

| Spec size | Expected behavior |
|-----------|-------------------|
| Small (< 1,000 lines) | Instant generation; no noticeable IDE impact. |
| Medium (1,000–5,000 lines) | Generation completes in under a second. |
| Large (> 5,000 lines) | May cause brief IDE pauses during initial load. |
| Very large (> 100,000 characters) | Consider the MSBuild task instead. A `KIOTA020` warning is emitted when the spec exceeds 5,000,000 characters. |

> [!TIP]
> For very large specs, use the [MSBuild task approach](xref:Uno.Extensions.Http.HowToKiotaBuildGeneration#option-a--msbuild-task-kiotaopenapiref) for CI builds and keep the source generator for smaller internal APIs where IDE IntelliSense is most valuable.

### Incremental caching

The Roslyn incremental pipeline caches the parsed OpenAPI document and configuration. Generation is **skipped entirely** when:

- The spec file content has not changed (content-aware, not timestamp-based).
- No MSBuild configuration properties affecting the generator have changed.

This means iterative builds are extremely fast — only the initial parse incurs cost.

---

## Diagnostics reference

The source generator reports structured Roslyn diagnostics with `KIOTA`-prefixed codes. These appear in the IDE Error List and in `dotnet build` output.

| Code | Severity | Description |
|------|----------|-------------|
| `KIOTA001` | Error | Failed to parse the OpenAPI document. Check that the file is valid JSON or YAML. |
| `KIOTA002` | Error | Unsupported OpenAPI version. Only 2.0 (Swagger), 3.0, and 3.1 are supported. |
| `KIOTA003` | Error | Missing required configuration. Ensure `KiotaClientName` metadata is set and non-empty. |
| `KIOTA010` | Warning | Non-fatal warning during OpenAPI parsing (e.g. unresolved `$ref`). |
| `KIOTA020` | Warning | Spec exceeds the recommended size for source generation (~5M characters). Consider the MSBuild task. |
| `KIOTA030` | Info | Generation completed successfully. Reports the number of source files produced. |
| `KIOTA031` | Warning | Generation completed but some types were skipped due to errors. Check for `KIOTA050` entries. |
| `KIOTA040` | Error | Unexpected exception in the generator pipeline. The full error message is included. |
| `KIOTA050` | Warning | Failed to emit source for an individual type. That type is skipped; remaining types are still generated (partial generation). |
| `KIOTA051` | Error | Failed to build the code model (CodeDOM) from the parsed OpenAPI document. No source is emitted for this spec. |

### Error handling behavior

The generator is designed to **never crash the compiler or IDE**:

- **Partial generation**: If a single type fails to emit (`KIOTA050`), the remaining types are still generated. Your project can still compile with the successfully generated types.
- **Fallback comments**: On complete pipeline failure, a `.g.cs` file containing an explanatory comment is emitted so the error is visible in Solution Explorer.
- **Cancellation**: IDE cancellation during editing is handled gracefully without reporting false errors.

---

## Troubleshooting

### Generated types not appearing in IntelliSense

1. **Verify `KiotaClientName` metadata** is set on the `<AdditionalFiles>` item — this is the required opt-in marker.
2. **Check the file extension** — only `.json`, `.yaml`, and `.yml` are supported.
3. **Look for diagnostics** in the Error List. Filter by "KIOTA" to see generator-specific messages.
4. **Restart Visual Studio / reload the project** — source generators are loaded at project open time.
5. **Verify `KiotaGenerator_Enabled`** is not set to `false` in a `Directory.Build.props` or `.csproj`.

### Duplicate type errors (CS0101)

If you previously generated code with the Kiota CLI and also have the source generator active, you will get duplicate type errors. **Delete the hand-generated output folder** (the directory created by `kiota generate --output`) and rebuild.

### IDE slowdowns with large specs

If the IDE becomes sluggish after adding a large OpenAPI spec:

1. Check for `KIOTA020` warnings — this means the spec exceeds the recommended size.
2. Switch to the [MSBuild task approach](xref:Uno.Extensions.Http.HowToKiotaBuildGeneration#option-a--msbuild-task-kiotaopenapiref) for that spec.
3. Use `KiotaIncludePatterns` to reduce the scope of generated endpoints.

### Build succeeds but types don't match expected API

1. Verify you are using the **correct version** of the OpenAPI spec file.
2. Check `KiotaIncludePatterns` / `KiotaExcludePatterns` — paths outside the include patterns are not generated.
3. Look for `KIOTA010` warnings that may indicate parsing issues in the spec.

---

## Comparison with MSBuild task

Both approaches produce identical output. Choose based on your workflow:

| Feature | Source generator | MSBuild task |
|---------|-----------------|--------------|
| IDE IntelliSense without building | **Yes** | No |
| Works in CI without Roslyn | No | **Yes** |
| Large spec support (>5K lines) | May be slow | **Yes** |
| Incremental caching | Content-aware (Roslyn) | File-timestamp based |
| Generated file location | In-memory (Analyzers node) | On disk in `obj/` |
| Extra features (CleanOutput, Serializers, etc.) | No | **Yes** |

You can use **both approaches** in the same project for different specs — for example, the source generator for a small internal API and the MSBuild task for a large external API.

---

## See also

* [Generate Kiota clients at build time](xref:Uno.Extensions.Http.HowToKiotaBuildGeneration) — quick-start for both MSBuild task and source generator
* [Migrate to build-time Kiota generation](xref:Uno.Extensions.Http.HowToKiotaMigration) — step-by-step migration from manual CLI or between approaches
* [Create a Kiota Client](xref:Uno.Extensions.Http.HowToKiota) — initial setup, DI registration, and manual CLI workflow
* [Overview: What is Kiota?](https://learn.microsoft.com/en-us/openapi/kiota/)
* [HTTP overview](xref:Uno.Extensions.Http.Overview)
