---
uid: Uno.Extensions.Http.HowToKiotaMigration
---
# How-To: Migrate to build-time Kiota code generation

> **UnoFeatures:** `HttpKiota` (add to `<UnoFeatures>` in your `.csproj`)

This guide walks you through migrating an existing Kiota client setup to build-time code generation. Three migration paths are covered:

| Migration path | Section |
|----------------|---------|
| Manual Kiota CLI → MSBuild task | [Migrate from manual CLI to MSBuild task](#migrate-from-manual-cli-to-msbuild-task) |
| Manual Kiota CLI → Source generator | [Migrate from manual CLI to source generator](#migrate-from-manual-cli-to-source-generator) |
| MSBuild task → Source generator | [Migrate from MSBuild task to source generator](#migrate-from-msbuild-task-to-source-generator) |

> [!TIP]
> See [Generate Kiota clients at build time](xref:Uno.Extensions.Http.HowToKiotaBuildGeneration) for a full reference of all MSBuild properties, metadata, and diagnostics.

---

## Prerequisites

* .NET 9.0 SDK or later.
* `HttpKiota` listed in `<UnoFeatures>`. See [Create a Kiota Client](xref:Uno.Extensions.Http.HowToKiota) for initial setup.

---

## Migrate from manual CLI to MSBuild task

Use this path when you currently run `kiota generate` (or `dotnet kiota generate`) from the command line or a script and want `dotnet build` to handle generation automatically.

### Before (manual CLI)

A typical manual workflow looks like this:

```bash
# Install the global tool (once)
dotnet tool install --global Microsoft.OpenApi.Kiota

# Generate the client
kiota generate \
  --openapi ./swagger.json \
  --language CSharp \
  --class-name MyApiClient \
  --namespace-name MyApp.Client.MyApi \
  --output ./MyApp/Client/MyApi
```

The generated `.cs` files live in your source tree and are checked into version control. You re-run the command manually whenever the spec changes.

### Step-by-step migration

#### 1. Delete the hand-generated output folder

Remove the folder that `kiota generate --output` wrote to:

```bash
# Example — adjust the path to match your project
rm -rf ./MyApp/Client/MyApi
```

Also remove these files from version control. Build-time generation writes to `obj/` and does not require checked-in generated code.

#### 2. Copy the OpenAPI spec into your project

If the spec file is not already part of the project, copy it into the project directory:

```bash
cp swagger.json ./MyApp/swagger.json
```

#### 3. Add a KiotaOpenApiReference item

In your `.csproj`, replace the `<Compile>` or `<Content>` reference to the generated folder with a `<KiotaOpenApiReference>`:

```diff
- <ItemGroup>
-   <Compile Include="Client\MyApi\**\*.cs" />
- </ItemGroup>

+ <ItemGroup>
+   <KiotaOpenApiReference Include="swagger.json"
+     ClientClassName="MyApiClient"
+     Namespace="MyApp.Client.MyApi" />
+ </ItemGroup>
```

Map your previous CLI flags to item metadata:

| CLI flag | Item metadata |
|----------|---------------|
| `--class-name` | `ClientClassName` |
| `--namespace-name` | `Namespace` |
| `--include-additional-data` | `IncludeAdditionalData` |
| `--uses-backing-store` | `UsesBackingStore` |
| `--exclude-backward-compatible` | `ExcludeBackwardCompatible` |
| `--type-access-modifier` | `TypeAccessModifier` |
| `--include-path` | `IncludePatterns` (semicolon-separated) |
| `--exclude-path` | `ExcludePatterns` (semicolon-separated) |

#### 4. Build

```bash
dotnet build
```

Generated files appear under `obj/<Config>/<TFM>/KiotaGenerated/MyApiClient/` and are compiled automatically.

#### 5. Verify your DI registration still works

Your existing `AddKiotaClient<T>()` call requires no changes:

```csharp
hostBuilder.UseHttp((context, services) =>
    services.AddKiotaClient<MyApiClient>(
        context,
        options: new EndpointOptions { Url = "https://api.example.com" }
    )
);
```

> [!NOTE]
> The build-time generated output is byte-for-byte compatible with the Kiota CLI. Existing view models, services, and DI registrations require no modifications.

#### 6. (Optional) Remove the global tool

The global Kiota tool is no longer needed for this project:

```bash
dotnet tool uninstall --global Microsoft.OpenApi.Kiota
```

### After (MSBuild task)

Your project now looks like this:

```
MyApp/
├── MyApp.csproj          ← contains <KiotaOpenApiReference>
├── swagger.json          ← OpenAPI spec (checked in)
├── Program.cs
└── ...
```

No generated `.cs` files in the source tree. Running `dotnet build` produces and compiles the client in one step. Incremental builds skip generation when the spec has not changed.

---

## Migrate from manual CLI to source generator

Use this path when you want IDE-time IntelliSense — the generated client types appear in auto-complete as you type, without needing to build first. Best for specs under ~5,000 lines.

### Step-by-step migration

Follow Steps 1 and 2 from the MSBuild task migration above, then:

#### 3. Add an AdditionalFiles item with Kiota metadata

Instead of `<KiotaOpenApiReference>`, add the spec as an `<AdditionalFiles>` item with `KiotaClientName` metadata:

```diff
- <ItemGroup>
-   <Compile Include="Client\MyApi\**\*.cs" />
- </ItemGroup>

+ <ItemGroup>
+   <AdditionalFiles Include="swagger.json"
+     KiotaClientName="MyApiClient"
+     KiotaNamespace="MyApp.Client.MyApi" />
+ </ItemGroup>
```

Map your previous CLI flags to `AdditionalFiles` metadata:

| CLI flag | AdditionalFiles metadata |
|----------|--------------------------|
| `--class-name` | `KiotaClientName` |
| `--namespace-name` | `KiotaNamespace` |
| `--include-additional-data` | `KiotaIncludeAdditionalData` |
| `--uses-backing-store` | `KiotaUsesBackingStore` |
| `--exclude-backward-compatible` | `KiotaExcludeBackwardCompatible` |
| `--type-access-modifier` | `KiotaTypeAccessModifier` |
| `--include-path` | `KiotaIncludePatterns` (semicolon-separated) |
| `--exclude-path` | `KiotaExcludePatterns` (semicolon-separated) |

#### 4. Open the project in your IDE

IntelliSense should show the generated `MyApiClient` type immediately. The generated types appear under **Dependencies > Analyzers > Uno.Extensions.Http.Kiota.SourceGenerator** in Solution Explorer.

#### 5. Build and verify

```bash
dotnet build
```

Your existing `AddKiotaClient<T>()` call and all consuming code require no changes.

---

## Migrate from MSBuild task to source generator

Use this path when you already have a working `<KiotaOpenApiReference>` setup and want to switch to the source generator for IDE-time IntelliSense.

### Before (MSBuild task)

```xml
<ItemGroup>
  <KiotaOpenApiReference Include="swagger.json"
    ClientClassName="MyApiClient"
    Namespace="MyApp.Client.MyApi"
    UsesBackingStore="false"
    IncludeAdditionalData="true" />
</ItemGroup>
```

### Step-by-step migration

#### 1. Replace KiotaOpenApiReference with AdditionalFiles

Update your `.csproj` to use `<AdditionalFiles>` with the equivalent `Kiota*` metadata:

```diff
  <ItemGroup>
-   <KiotaOpenApiReference Include="swagger.json"
-     ClientClassName="MyApiClient"
-     Namespace="MyApp.Client.MyApi"
-     UsesBackingStore="false"
-     IncludeAdditionalData="true" />
+   <AdditionalFiles Include="swagger.json"
+     KiotaClientName="MyApiClient"
+     KiotaNamespace="MyApp.Client.MyApi"
+     KiotaUsesBackingStore="false"
+     KiotaIncludeAdditionalData="true" />
  </ItemGroup>
```

Use this mapping for all metadata:

| `KiotaOpenApiReference` metadata | `AdditionalFiles` metadata |
|----------------------------------|----------------------------|
| `ClientClassName` | `KiotaClientName` |
| `Namespace` | `KiotaNamespace` |
| `UsesBackingStore` | `KiotaUsesBackingStore` |
| `IncludeAdditionalData` | `KiotaIncludeAdditionalData` |
| `ExcludeBackwardCompatible` | `KiotaExcludeBackwardCompatible` |
| `TypeAccessModifier` | `KiotaTypeAccessModifier` |
| `IncludePatterns` | `KiotaIncludePatterns` |
| `ExcludePatterns` | `KiotaExcludePatterns` |

> [!NOTE]
> The `Serializers`, `Deserializers`, `StructuredMimeTypes`, `CleanOutput`, and `DisableValidationRules` metadata items are specific to the MSBuild task and have no source generator equivalents. In most projects these are left at their defaults and can be safely omitted.

#### 2. Remove MSBuild task global properties (if set)

If you set any of these global properties, replace them with their source generator equivalents:

| MSBuild task property | Source generator property |
|-----------------------|--------------------------|
| `KiotaGeneratorEnabled` | `KiotaGenerator_Enabled` |
| `KiotaDefaultUsesBackingStore` | `KiotaGenerator_DefaultUsesBackingStore` |
| `KiotaDefaultIncludeAdditionalData` | `KiotaGenerator_DefaultIncludeAdditionalData` |
| `KiotaDefaultExcludeBackwardCompatible` | `KiotaGenerator_DefaultExcludeBackwardCompatible` |
| `KiotaDefaultTypeAccessModifier` | `KiotaGenerator_DefaultTypeAccessModifier` |

#### 3. Build and verify

```bash
dotnet build
```

Generated types now come from the Roslyn source generator instead of files on disk. IntelliSense reflects changes to the spec without rebuilding.

### When to stay on the MSBuild task

The MSBuild task may be a better fit if:

- **Your OpenAPI spec is large** (>5,000 lines / >100 endpoints). The source generator runs in the IDE hot path and may cause slowdowns.
- **You need CI-only generation** without Roslyn dependency.
- **You use `CleanOutput`, `DisableValidationRules`, or custom serializer registration** that is only available in the MSBuild task.

You can also use both approaches in the same project for different specs — for example, the source generator for a small internal API and the MSBuild task for a large external API.

---

## Troubleshooting

### Generated types not appearing in IntelliSense (source generator)

1. Ensure `KiotaClientName` metadata is set on the `<AdditionalFiles>` item — this is the trigger that tells the source generator to process the file.
2. Restart Visual Studio or reload the project. Source generators are loaded at project open time.
3. Check the **Error List** for `KIOTA*` diagnostic codes. Common issues:
   - `KIOTA001` — the spec file could not be parsed (invalid JSON/YAML).
   - `KIOTA003` — required metadata is missing.

### Build succeeds but no generated files (MSBuild task)

1. Verify `KiotaGeneratorEnabled` is not set to `false` in a `Directory.Build.props`.
2. Check the build output for `_KiotaGenerate` target execution. Enable detailed verbosity with `dotnet build -v detailed`.
3. Ensure the OpenAPI spec file path in `Include` is correct relative to the project directory.

### Type conflicts between CLI-generated and build-generated code

If both hand-generated files and build-generated files exist, you will get `CS0101` duplicate type errors. Delete the hand-generated output folder (the one previously created by `kiota generate --output`) and rebuild.

---

## See also

* [Generate Kiota clients at build time](xref:Uno.Extensions.Http.HowToKiotaBuildGeneration) — full reference for properties, metadata, and diagnostics
* [Create a Kiota Client](xref:Uno.Extensions.Http.HowToKiota) — initial Kiota setup and DI registration
* [HTTP overview](xref:Uno.Extensions.Http.Overview)
