---
uid: Uno.Extensions.Http.HowToKiotaBuildGeneration
---
# How-To: Generate Kiota clients at build time

> **UnoFeatures:** `HttpKiota` (add to `<UnoFeatures>` in your `.csproj`)

Uno.Extensions ships two **build-time** code-generation paths that turn an OpenAPI description into a strongly-typed Kiota client automatically during `dotnet build` — no `dotnet tool install` or manual CLI invocations required.

| Approach | When to use |
|----------|-------------|
| **MSBuild task** (`KiotaOpenApiReference`) | Recommended for CI/CD pipelines and specs of any size. Runs the full Kiota engine during build. |
| **Source generator** (`AdditionalFiles` with metadata) | Recommended for IDE-time feedback. Produces IntelliSense as you type, and regenerates when the spec file is saved. Best for specs under ~5,000 lines. |

Both approaches produce output identical to the Kiota CLI and are fully compatible with `AddKiotaClient<T>()`.

> [!TIP]
> If you are currently using the manual `kiota generate` workflow, see [Migrate from manual Kiota CLI](#migrate-from-manual-kiota-cli) at the bottom of this page.

---

## Prerequisites

* An Uno Platform project created from the template wizard or `dotnet new unoapp`.
* `HttpKiota` in `<UnoFeatures>` (see [Create a Kiota Client](xref:Uno.Extensions.Http.HowToKiota) for initial setup).
* An OpenAPI specification file (`.json`, `.yaml`, or `.yml`) for your target API.

---

## Option A — MSBuild Task (`KiotaOpenApiReference`)

### 1. Add the OpenAPI spec to your project

Copy your OpenAPI spec file (for example `petstore.json`) into the project directory.

### 2. Declare a `KiotaOpenApiReference`

Add a `<KiotaOpenApiReference>` item in your `.csproj`:

```xml
<ItemGroup>
  <KiotaOpenApiReference Include="petstore.json"
    ClientClassName="PetStoreClient"
    Namespace="MyApp.PetStore" />
</ItemGroup>
```

### 3. Build

```bash
dotnet build
```

**That's it.** Generated `.cs` files appear under `obj/<Config>/<TFM>/KiotaGenerated/PetStoreClient/` and are compiled automatically. The generated `PetStoreClient` class extends `BaseRequestBuilder` and is ready for DI registration.

### Item metadata reference

All metadata has sensible defaults. Only `ClientClassName` is typically needed.

| Metadata | Default | Description |
|----------|---------|-------------|
| `ClientClassName` | `ApiClient` | Name of the root client class. |
| `Namespace` | `$(RootNamespace).Client` | Root namespace for all generated types. |
| `UsesBackingStore` | `false` | Enable `IBackedModel` / `IBackingStore` pattern. |
| `IncludeAdditionalData` | `true` | Generate `AdditionalData` dictionary on model classes. |
| `ExcludeBackwardCompatible` | `false` | Skip deprecated backward-compatibility overloads. |
| `TypeAccessModifier` | `Public` | `Public` or `Internal` for generated types. |
| `IncludePatterns` | *(empty — all paths)* | Semicolon-separated glob patterns to include API paths (e.g. `**/pets/**`). |
| `ExcludePatterns` | *(empty)* | Semicolon-separated glob patterns to exclude API paths. |
| `Serializers` | *(Kiota defaults)* | Semicolon-separated `ISerializationWriterFactory` class names. |
| `Deserializers` | *(Kiota defaults)* | Semicolon-separated `IParseNodeFactory` class names. |
| `StructuredMimeTypes` | *(Kiota defaults)* | Semicolon-separated MIME types the client handles. |
| `CleanOutput` | `false` | Delete output directory before each generation. |
| `DisableValidationRules` | *(empty)* | Semicolon-separated OpenAPI validation rules to suppress. |

### Global properties

These properties apply to **all** `KiotaOpenApiReference` items in a project:

| Property | Default | Description |
|----------|---------|-------------|
| `KiotaGeneratorEnabled` | `true` | Set to `false` to disable generation entirely. |
| `KiotaOutputPath` | `$(IntermediateOutputPath)KiotaGenerated\` | Base directory for generated files. |
| `KiotaDefaultUsesBackingStore` | `false` | Default for `UsesBackingStore` when not set per-item. |
| `KiotaDefaultIncludeAdditionalData` | `true` | Default for `IncludeAdditionalData`. |
| `KiotaDefaultExcludeBackwardCompatible` | `false` | Default for `ExcludeBackwardCompatible`. |
| `KiotaDefaultTypeAccessModifier` | `Public` | Default for `TypeAccessModifier`. |
| `KiotaLogLevel` | `Warning` | Minimum log level for generator diagnostics. |

### Multiple clients

Add multiple `<KiotaOpenApiReference>` items to generate clients for different APIs in the same project:

```xml
<ItemGroup>
  <KiotaOpenApiReference Include="apis/petstore.json"
    ClientClassName="PetStoreClient"
    Namespace="MyApp.PetStore" />

  <KiotaOpenApiReference Include="apis/weather.yaml"
    ClientClassName="WeatherClient"
    Namespace="MyApp.Weather" />
</ItemGroup>
```

### Incremental builds

Generation only runs when the OpenAPI spec file changes. A second `dotnet build` with no spec modifications skips the generation target automatically.

---

## Option B — Source Generator (`AdditionalFiles`)

The source generator runs inside the Roslyn compiler process and provides real-time IntelliSense in Visual Studio and VS Code. Types appear as you type, without running a build.

### 1. Add the OpenAPI spec as an `AdditionalFiles` item

```xml
<ItemGroup>
  <AdditionalFiles Include="petstore.json"
    KiotaClientName="PetStoreClient"
    KiotaNamespace="MyApp.PetStore" />
</ItemGroup>
```

> [!NOTE]
> The presence of `KiotaClientName` metadata is what signals the source generator to process the file. Files without this metadata are ignored.

### 2. Build (or just open in IDE)

* In the IDE, IntelliSense should show the `PetStoreClient` class within seconds of opening the project.
* `dotnet build` compiles the generated source normally.

Generated types appear under **Dependencies > Analyzers > Uno.Extensions.Http.Kiota.SourceGenerator** in Solution Explorer.

### Per-file metadata reference

| Metadata | Default | Description |
|----------|---------|-------------|
| `KiotaClientName` | *(required)* | Name of the root client class. Also acts as the opt-in marker. |
| `KiotaNamespace` | `ApiSdk` | Root namespace for all generated types. |
| `KiotaUsesBackingStore` | `false` | Enable `IBackedModel` / `IBackingStore` pattern. |
| `KiotaIncludeAdditionalData` | `true` | Generate `AdditionalData` dictionary on model classes. |
| `KiotaExcludeBackwardCompatible` | `false` | Skip deprecated backward-compatibility overloads. |
| `KiotaTypeAccessModifier` | `Public` | `Public` or `Internal` for generated types. |
| `KiotaIncludePatterns` | *(empty — all paths)* | Semicolon-separated glob patterns for API path inclusion. |
| `KiotaExcludePatterns` | *(empty)* | Semicolon-separated glob patterns for API path exclusion. |

### Global properties

| Property | Default | Description |
|----------|---------|-------------|
| `KiotaGenerator_Enabled` | `true` | Set to `false` to disable the source generator. |
| `KiotaGenerator_DefaultUsesBackingStore` | `false` | Global default for `KiotaUsesBackingStore`. |
| `KiotaGenerator_DefaultIncludeAdditionalData` | `true` | Global default for `KiotaIncludeAdditionalData`. |
| `KiotaGenerator_DefaultExcludeBackwardCompatible` | `false` | Global default for `KiotaExcludeBackwardCompatible`. |
| `KiotaGenerator_DefaultTypeAccessModifier` | `Public` | Global default for `KiotaTypeAccessModifier`. |

### Performance considerations

The source generator is designed for typical API specs. For very large specs (over ~5,000 lines / 100K characters), the MSBuild task approach (Option A) is recommended to avoid IDE slowdowns. The generator emits a `KIOTA020` warning when a spec exceeds the recommended size threshold.

---

## Register the generated client

Both approaches produce a client class that extends `BaseRequestBuilder`. Register it with Uno.Extensions the same way as a manually generated client:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(hostBuilder =>
        {
            hostBuilder.UseHttp((context, services) =>
            {
                services.AddKiotaClient<PetStoreClient>(
                    context,
                    options: new EndpointOptions
                    {
                        Url = "https://petstore.example.com"
                    });
            });
        });
}
```

Then inject the client into your view model or service:

```csharp
public class PetViewModel
{
    private readonly PetStoreClient _client;

    public PetViewModel(PetStoreClient client) => _client = client;

    public async Task LoadPetsAsync()
    {
        var pets = await _client.Pets.GetAsync();
    }
}
```

---

## Choosing between MSBuild task and source generator

| Consideration | MSBuild task | Source generator |
|---------------|--------------|------------------|
| IDE IntelliSense without building | No | **Yes** |
| Large spec support (>5K lines) | **Yes** | May be slow |
| CI-only generation (no Roslyn needed) | **Yes** | No |
| Incremental caching | File-timestamp based | Roslyn content-aware |
| Generated file visibility | Files on disk in `obj/` | In-memory (Analyzers node) |

**Recommendation**: Use the **source generator** for day-to-day development with small-to-medium specs, and the **MSBuild task** for CI/CD or very large APIs.

---

## Filtering API paths

Both approaches support glob patterns to restrict which API endpoints are included:

```xml
<!-- MSBuild task -->
<KiotaOpenApiReference Include="large-api.json"
  ClientClassName="SubsetClient"
  Namespace="MyApp.Subset"
  IncludePatterns="**/pets/**;**/stores/**"
  ExcludePatterns="**/admin/**" />

<!-- Source generator -->
<AdditionalFiles Include="large-api.json"
  KiotaClientName="SubsetClient"
  KiotaNamespace="MyApp.Subset"
  KiotaIncludePatterns="**/pets/**;**/stores/**"
  KiotaExcludePatterns="**/admin/**" />
```

When `IncludePatterns` is empty, all paths are included by default.

---

## Diagnostics

Both generation approaches emit structured diagnostics with `KIOTA` prefixed codes:

| Code | Severity | Meaning |
|------|----------|---------|
| `KIOTA001` | Error | Failed to parse the OpenAPI document. |
| `KIOTA002` | Error | Unsupported OpenAPI version. |
| `KIOTA003` | Error | Missing required configuration (e.g. no `KiotaClientName`). |
| `KIOTA010` | Warning | Non-fatal parsing warning. |
| `KIOTA020` | Warning | Spec exceeds recommended size for source generation. |
| `KIOTA030` | Info | Generation completed successfully. |
| `KIOTA031` | Warning | Generation completed with some types skipped. |
| `KIOTA040` | Error | Unexpected error in the generator pipeline. |

In the MSBuild task, these appear as standard MSBuild errors/warnings in the Error List. In the source generator, they appear as Roslyn analyzer diagnostics.

---

## Migrate from manual Kiota CLI

If you are currently generating code with `kiota generate` (or `dotnet kiota generate`), see the dedicated [migration guide](xref:Uno.Extensions.Http.HowToKiotaMigration) for detailed, step-by-step instructions covering three migration paths:

- Manual CLI → MSBuild task
- Manual CLI → Source generator
- MSBuild task ↔ Source generator

---

## See also

* [Migrate to build-time Kiota generation](xref:Uno.Extensions.Http.HowToKiotaMigration) — step-by-step migration from manual CLI or between task/generator
* [How-To: Create a Kiota Client](xref:Uno.Extensions.Http.HowToKiota) — initial Kiota setup and DI registration
* [Overview: What is Kiota?](https://learn.microsoft.com/en-us/openapi/kiota/)
* [HTTP overview](xref:Uno.Extensions.Http.Overview)
* [Register HTTP endpoints](xref:Uno.Extensions.Http.HowToHttp)
