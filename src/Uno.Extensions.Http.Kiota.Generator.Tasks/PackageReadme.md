# Uno.Extensions.Http.Kiota.Generator

MSBuild integration for [Kiota](https://learn.microsoft.com/openapi/kiota/) OpenAPI code generation. Automatically generates
type-safe C# REST client code from OpenAPI specifications during `dotnet build` — no global tool installation required.

## Quick Start

1. Add the package to your project:

```xml
<PackageReference Include="Uno.Extensions.Http.Kiota.Generator" Version="*" PrivateAssets="All" />
```

2. Add an OpenAPI reference:

```xml
<ItemGroup>
  <KiotaOpenApiReference Include="openapi.json"
    ClientClassName="PetStoreClient"
    Namespace="MyApp.PetStore" />
</ItemGroup>
```

3. Build your project — the client code is generated automatically:

```
dotnet build
```

The generated client code appears in `obj/KiotaGenerated/` and is included in compilation automatically.

## Configuration

Each `<KiotaOpenApiReference>` item supports the following metadata:

| Metadata | Default | Description |
|----------|---------|-------------|
| `ClientClassName` | `ApiClient` | Name of the generated root client class |
| `Namespace` | `$(RootNamespace).Client` | Namespace for generated code |
| `UsesBackingStore` | `false` | Enable backing store for change tracking |
| `IncludeAdditionalData` | `true` | Generate `AdditionalData` dictionary |
| `ExcludeBackwardCompatible` | `false` | Exclude backward-compatible code |
| `TypeAccessModifier` | `Public` | Access modifier for generated types |
| `IncludePatterns` | *(all paths)* | Semicolon-separated glob patterns to include |
| `ExcludePatterns` | *(none)* | Semicolon-separated glob patterns to exclude |
| `CleanOutput` | `false` | Delete output directory before generation |

### Global Properties

| Property | Default | Description |
|----------|---------|-------------|
| `KiotaGeneratorEnabled` | `true` | Enable/disable generation globally |
| `KiotaOutputPath` | `$(IntermediateOutputPath)KiotaGenerated\` | Output directory for generated code |
| `KiotaLogLevel` | `Warning` | CLI log level (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`) |

## Multiple API References

Generate clients for multiple APIs in the same project:

```xml
<ItemGroup>
  <KiotaOpenApiReference Include="specs/petstore.json"
    ClientClassName="PetStoreClient"
    Namespace="MyApp.PetStore" />
  <KiotaOpenApiReference Include="specs/github.json"
    ClientClassName="GitHubClient"
    Namespace="MyApp.GitHub"
    IncludePatterns="/repos/**;/users/**" />
</ItemGroup>
```

## Incremental Builds

Generation only re-runs when the OpenAPI spec file changes. Subsequent builds with an unchanged spec skip generation entirely.

## Requirements

- .NET 8.0+ SDK (for the build-time CLI tool)
- Generated client code requires [Microsoft.Kiota.Abstractions](https://www.nuget.org/packages/Microsoft.Kiota.Abstractions) and serialization packages at runtime

## Documentation

- [Uno.Extensions documentation](https://platform.uno/docs/articles/external/uno.extensions/doc/ExtensionsOverview.html)
- [Kiota documentation](https://learn.microsoft.com/openapi/kiota/)
