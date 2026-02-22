# Phase 1: MSBuild Task with Embedded Kiota.Builder

## Summary

Phase 1 delivers Kiota code generation as part of `dotnet build` by embedding a wrapper console application (built on `Microsoft.OpenApi.Kiota.Builder`) inside a NuGet package along with MSBuild targets. This follows the proven [NSwag.ApiDescription.Client](https://github.com/RicoSuter/NSwag/tree/master/src/NSwag.ApiDescription.Client) pattern.

## Architecture

```
NuGet Package: Uno.Extensions.Http.Kiota
├── lib/net9.0/
│   └── Uno.Extensions.Http.Kiota.dll              ← existing runtime library
├── build/
│   ├── Uno.Extensions.Http.Kiota.props             ← MSBuild properties + item definitions
│   └── Uno.Extensions.Http.Kiota.targets           ← MSBuild targets hooking into build
└── tools/
    ├── win-x64/
    │   └── Uno.Extensions.Http.Kiota.Generator.exe ← self-contained generator
    ├── linux-x64/
    │   └── Uno.Extensions.Http.Kiota.Generator     ← self-contained generator
    ├── osx-x64/
    │   └── Uno.Extensions.Http.Kiota.Generator     ← self-contained generator
    └── osx-arm64/
        └── Uno.Extensions.Http.Kiota.Generator     ← self-contained generator
```

## New Project: `Uno.Extensions.Http.Kiota.Generator`

A .NET console application that wraps `KiotaBuilder` from the `Microsoft.OpenApi.Kiota.Builder` NuGet package.

### Responsibilities

1. Accept command-line arguments mapping to `GenerationConfiguration` properties
2. Parse the OpenAPI document from a local file path
3. Generate C# source files to a specified output directory
4. Return exit code 0 on success, non-zero on failure
5. Write validation diagnostics (warnings/errors) to stdout/stderr in MSBuild-parseable format

### Command-Line Interface

```bash
Uno.Extensions.Http.Kiota.Generator \
  --openapi <path-to-openapi.json> \
  --output <output-directory> \
  --class-name <ClientClassName> \
  --namespace <ClientNamespaceName> \
  [--uses-backing-store <true|false>] \
  [--include-additional-data <true|false>] \
  [--exclude-backward-compatible <true|false>] \
  [--type-access-modifier <Public|Internal>] \
  [--include-patterns <glob1;glob2>] \
  [--exclude-patterns <glob1;glob2>] \
  [--serializers <factory1;factory2>] \
  [--deserializers <factory1;factory2>] \
  [--structured-mime-types <mime1;mime2>] \
  [--clean-output <true|false>] \
  [--disable-validation-rules <rule1;rule2>] \
  [--log-level <Information|Warning|Error>]
```

### Core Implementation

```csharp
// Uno.Extensions.Http.Kiota.Generator/Program.cs (conceptual)
public static async Task<int> Main(string[] args)
{
    var options = ParseArguments(args);

    var config = new GenerationConfiguration
    {
        OpenAPIFilePath = options.OpenApiPath,
        OutputPath = options.OutputPath,
        Language = GenerationLanguage.CSharp,
        ClientClassName = options.ClassName,
        ClientNamespaceName = options.Namespace,
        UsesBackingStore = options.UsesBackingStore,
        IncludeAdditionalData = options.IncludeAdditionalData,
        ExcludeBackwardCompatible = options.ExcludeBackwardCompatible,
        // ... map all options
    };

    var logger = CreateMSBuildLogger(); // formats output as MSBuild warnings/errors
    using var httpClient = new HttpClient();
    var builder = new KiotaBuilder(logger, config, httpClient);

    var result = await builder.GenerateClientAsync(CancellationToken.None);
    return result ? 0 : 1;
}
```

### Project File

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <!-- Published as part of Uno.Extensions.Http.Kiota package -->
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.OpenApi.Kiota.Builder" />
    <PackageReference Include="System.CommandLine" />
  </ItemGroup>
</Project>
```

### Build & Publish

The generator is published for each target RID during the Uno.Extensions release process:

```bash
dotnet publish -c Release -r win-x64   --self-contained -o tools/win-x64
dotnet publish -c Release -r linux-x64 --self-contained -o tools/linux-x64
dotnet publish -c Release -r osx-x64   --self-contained -o tools/osx-x64
dotnet publish -c Release -r osx-arm64 --self-contained -o tools/osx-arm64
```

## MSBuild Integration

### Props File: `build/Uno.Extensions.Http.Kiota.props`

Defines the `<KiotaOpenApiReference>` item group and default property values:

```xml
<Project>
  <!-- Default generation properties -->
  <PropertyGroup>
    <KiotaGeneratorEnabled Condition="'$(KiotaGeneratorEnabled)' == ''">true</KiotaGeneratorEnabled>
    <KiotaOutputPath Condition="'$(KiotaOutputPath)' == ''">$(IntermediateOutputPath)KiotaGenerated\</KiotaOutputPath>
    <KiotaDefaultUsesBackingStore Condition="'$(KiotaDefaultUsesBackingStore)' == ''">false</KiotaDefaultUsesBackingStore>
    <KiotaDefaultIncludeAdditionalData Condition="'$(KiotaDefaultIncludeAdditionalData)' == ''">true</KiotaDefaultIncludeAdditionalData>
    <KiotaDefaultExcludeBackwardCompatible Condition="'$(KiotaDefaultExcludeBackwardCompatible)' == ''">false</KiotaDefaultExcludeBackwardCompatible>
    <KiotaDefaultTypeAccessModifier Condition="'$(KiotaDefaultTypeAccessModifier)' == ''">Public</KiotaDefaultTypeAccessModifier>
  </PropertyGroup>

  <!-- Item definition metadata defaults -->
  <ItemDefinitionGroup>
    <KiotaOpenApiReference>
      <ClientClassName>ApiClient</ClientClassName>
      <Namespace>$(RootNamespace).Client</Namespace>
      <UsesBackingStore>$(KiotaDefaultUsesBackingStore)</UsesBackingStore>
      <IncludeAdditionalData>$(KiotaDefaultIncludeAdditionalData)</IncludeAdditionalData>
      <ExcludeBackwardCompatible>$(KiotaDefaultExcludeBackwardCompatible)</ExcludeBackwardCompatible>
      <TypeAccessModifier>$(KiotaDefaultTypeAccessModifier)</TypeAccessModifier>
      <IncludePatterns></IncludePatterns>
      <ExcludePatterns></ExcludePatterns>
    </KiotaOpenApiReference>
  </ItemDefinitionGroup>
</Project>
```

### Targets File: `build/Uno.Extensions.Http.Kiota.targets`

Hooks generation into the build pipeline:

```xml
<Project>
  <PropertyGroup>
    <!-- Resolve generator binary per RID -->
    <_KiotaGeneratorDir>$(MSBuildThisFileDirectory)..\tools\</_KiotaGeneratorDir>
    <_KiotaGeneratorExe Condition="$([MSBuild]::IsOSPlatform('Windows'))">$(_KiotaGeneratorDir)win-x64\Uno.Extensions.Http.Kiota.Generator.exe</_KiotaGeneratorExe>
    <_KiotaGeneratorExe Condition="$([MSBuild]::IsOSPlatform('Linux'))">$(_KiotaGeneratorDir)linux-x64/Uno.Extensions.Http.Kiota.Generator</_KiotaGeneratorExe>
    <_KiotaGeneratorExe Condition="$([MSBuild]::IsOSPlatform('OSX')) AND '$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)' == 'Arm64'">$(_KiotaGeneratorDir)osx-arm64/Uno.Extensions.Http.Kiota.Generator</_KiotaGeneratorExe>
    <_KiotaGeneratorExe Condition="$([MSBuild]::IsOSPlatform('OSX')) AND '$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)' != 'Arm64'">$(_KiotaGeneratorDir)osx-x64/Uno.Extensions.Http.Kiota.Generator</_KiotaGeneratorExe>
  </PropertyGroup>

  <!-- Hook before compilation -->
  <PropertyGroup>
    <CoreCompileDependsOn>_KiotaGenerate;$(CoreCompileDependsOn)</CoreCompileDependsOn>
  </PropertyGroup>

  <!-- Generate target -->
  <Target Name="_KiotaGenerate"
          Condition="'$(KiotaGeneratorEnabled)' == 'true' AND '@(KiotaOpenApiReference)' != ''"
          Inputs="@(KiotaOpenApiReference)"
          Outputs="@(KiotaOpenApiReference -> '$(KiotaOutputPath)%(ClientClassName).stamp')">

    <!-- Ensure output directory -->
    <MakeDir Directories="$(KiotaOutputPath)" />

    <!-- Run generator for each OpenAPI reference -->
    <Exec Command="&quot;$(_KiotaGeneratorExe)&quot; --openapi &quot;%(KiotaOpenApiReference.FullPath)&quot; --output &quot;$(KiotaOutputPath)%(KiotaOpenApiReference.ClientClassName)&quot; --class-name &quot;%(KiotaOpenApiReference.ClientClassName)&quot; --namespace &quot;%(KiotaOpenApiReference.Namespace)&quot; --uses-backing-store %(KiotaOpenApiReference.UsesBackingStore) --include-additional-data %(KiotaOpenApiReference.IncludeAdditionalData) --exclude-backward-compatible %(KiotaOpenApiReference.ExcludeBackwardCompatible) --type-access-modifier %(KiotaOpenApiReference.TypeAccessModifier) --include-patterns &quot;%(KiotaOpenApiReference.IncludePatterns)&quot; --exclude-patterns &quot;%(KiotaOpenApiReference.ExcludePatterns)&quot;"
           ConsoleToMSBuild="true"
           StandardOutputImportance="low">
      <Output TaskParameter="ConsoleOutput" ItemName="_KiotaOutput" />
    </Exec>

    <!-- Create stamp file for incremental build -->
    <Touch Files="$(KiotaOutputPath)%(KiotaOpenApiReference.ClientClassName).stamp" AlwaysCreate="true" />
  </Target>

  <!-- Include generated files in compilation -->
  <Target Name="_KiotaIncludeGenerated"
          BeforeTargets="CoreCompile"
          Condition="'$(KiotaGeneratorEnabled)' == 'true' AND '@(KiotaOpenApiReference)' != ''">
    <ItemGroup>
      <Compile Include="$(KiotaOutputPath)**\*.cs" Link="KiotaGenerated\%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>
  </Target>

  <!-- Clean generated files -->
  <Target Name="_KiotaClean" BeforeTargets="CoreClean">
    <RemoveDir Directories="$(KiotaOutputPath)" />
  </Target>
</Project>
```

## User Experience

### Basic Usage

Add an OpenAPI reference to your `.csproj`:

```xml
<ItemGroup>
  <KiotaOpenApiReference Include="openapi.json"
    ClientClassName="PetStoreClient"
    Namespace="MyApp.PetStore" />
</ItemGroup>
```

Then `dotnet build` automatically generates the client code.

### Multiple APIs

```xml
<ItemGroup>
  <KiotaOpenApiReference Include="apis/petstore.json"
    ClientClassName="PetStoreClient"
    Namespace="MyApp.PetStore" />

  <KiotaOpenApiReference Include="apis/weather.json"
    ClientClassName="WeatherClient"
    Namespace="MyApp.Weather" />
</ItemGroup>
```

### Advanced Configuration

```xml
<ItemGroup>
  <KiotaOpenApiReference Include="openapi.json"
    ClientClassName="GraphClient"
    Namespace="MyApp.Graph"
    UsesBackingStore="true"
    IncludeAdditionalData="true"
    ExcludeBackwardCompatible="true"
    TypeAccessModifier="Internal"
    IncludePatterns="/users/**;/groups/**"
    ExcludePatterns="/admin/**" />
</ItemGroup>
```

### Project-Wide Defaults

```xml
<PropertyGroup>
  <KiotaDefaultUsesBackingStore>true</KiotaDefaultUsesBackingStore>
  <KiotaDefaultExcludeBackwardCompatible>true</KiotaDefaultExcludeBackwardCompatible>
</PropertyGroup>
```

### DI Registration (unchanged)

Generated clients work directly with the existing Uno.Extensions.Http.Kiota registration:

```csharp
services.AddKiotaClient<PetStoreClient>(context, options: new EndpointOptions { Url = "https://petstore.example.com" });
```

or

```csharp
services.AddKiotaClientWithEndpoint<PetStoreClient, PetStoreEndpoint>(context);
```

## Configuration Mapping

| MSBuild Item Metadata | `GenerationConfiguration` Property | Default |
|----------------------|--------------------------------------|---------|
| `ClientClassName` | `ClientClassName` | `"ApiClient"` |
| `Namespace` | `ClientNamespaceName` | `$(RootNamespace).Client` |
| `UsesBackingStore` | `UsesBackingStore` | `false` |
| `IncludeAdditionalData` | `IncludeAdditionalData` | `true` |
| `ExcludeBackwardCompatible` | `ExcludeBackwardCompatible` | `false` |
| `TypeAccessModifier` | `TypeAccessModifier` | `"Public"` |
| `IncludePatterns` | `IncludePatterns` | `""` (all) |
| `ExcludePatterns` | `ExcludePatterns` | `""` (none) |
| `Serializers` | `Serializers` | Kiota defaults |
| `Deserializers` | `Deserializers` | Kiota defaults |
| `StructuredMimeTypes` | `StructuredMimeTypes` | Kiota defaults |
| `DisableValidationRules` | `DisabledValidationRules` | `""` (none) |

## Incremental Build Support

The MSBuild target uses the standard `Inputs`/`Outputs` mechanism:

- **Inputs**: The OpenAPI file (`%(KiotaOpenApiReference.FullPath)`)
- **Outputs**: A stamp file (`$(KiotaOutputPath)%(ClientClassName).stamp`)

The target only re-executes when the input file timestamp is newer than the stamp file. This means:
- First build: generates all code
- Subsequent builds with no spec changes: skips generation entirely
- After modifying the OpenAPI spec: regenerates

## Error Handling

The generator console app formats Kiota diagnostics as MSBuild messages:

| Severity | Format |
|----------|--------|
| Error | `error KIOTA001: {message}` → MSBuild error (breaks build) |
| Warning | `warning KIOTA002: {message}` → MSBuild warning |
| Info | `{message}` → logged at low importance |

The `<Exec>` task has `ConsoleToMSBuild="true"`, which automatically parses these formatted messages and surfaces them in the build output and IDE error list.

## Comparison with NSwag.ApiDescription.Client

| Aspect | NSwag.ApiDescription.Client | Uno.Extensions.Http.Kiota (Phase 1) |
|--------|----------------------------|--------------------------------------|
| **Generator binary** | NSwag console app embedded in NuGet `tools/` | Kiota.Builder wrapper in NuGet `tools/` |
| **Item group** | `<OpenApiReference>` | `<KiotaOpenApiReference>` |
| **Multi-RID** | .NET framework-specific (Net60/Net80) | Self-contained per OS RID |
| **Hook point** | `CoreCompileDependsOn` | `CoreCompileDependsOn` |
| **Output location** | `obj\` folder | `obj\KiotaGenerated\` |
| **Incremental** | Hash-based checking | Timestamp-based `Inputs`/`Outputs` |
| **Runtime deps** | Newtonsoft.Json / NSwag | Microsoft.Kiota.Abstractions + serialization |
| **Configuration** | Item metadata + Options param | Item metadata |
| **IDE integration** | None (build only) | None (build only) |
| **Generated code style** | Single-file, HttpClient-based | Multi-file, RequestBuilder pattern |

## Packaging Strategy

Within the Uno.Extensions repo:

1. `Uno.Extensions.Http.Kiota.Generator` is a separate project under `src/`
2. During release builds, CI publishes the generator for all RIDs
3. The published binaries are included in the `Uno.Extensions.Http.Kiota` NuGet package under `tools/`
4. The `build/*.props` and `build/*.targets` files are included in the NuGet package under `build/`
5. The existing `lib/` content (runtime DI extensions) remains unchanged

This follows the repo's convention where generators are packaged alongside their parent library (see `ToolOfPackage` pattern in `Uno.CrossTargeting.props`).

## Limitations

- **No IDE-time generation**: code is only generated during `dotnet build`, not live in the editor
- **Package size**: self-contained binaries for 4 RIDs adds ~60-100MB to the NuGet package (can be mitigated with framework-dependent publishing if a compatible .NET runtime is guaranteed)
- **Process startup overhead**: launching the generator exe adds a few hundred milliseconds per OpenAPI reference (mitigated by incremental build — only runs when specs change)
- **Kiota.Builder version coupling**: the generator binary pins to a specific `Microsoft.OpenApi.Kiota.Builder` version; updating requires a new Uno.Extensions release
