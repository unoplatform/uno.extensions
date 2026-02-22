# Data Model: Kiota MSBuild Code Generation

**Feature**: kiota-source-gen | **Date**: 2026-02-21

## Overview

This document defines the core data models used across both phases. Phase 1 uses Kiota.Builder's existing types directly. Phase 2 re-implements a subset as netstandard2.0-compatible CodeDOM types for the Roslyn source generator.

---

## 1. Configuration Entities

### GeneratorOptions (Phase 1 — CLI input)

| Field | Type | Default | Source |
|-------|------|---------|--------|
| `OpenApiPath` | `string` | *(required)* | `--openapi` CLI arg / `%(KiotaOpenApiReference.FullPath)` |
| `OutputPath` | `string` | *(required)* | `--output` CLI arg / `$(KiotaOutputPath)` |
| `ClassName` | `string` | `"ApiClient"` | `--class-name` / `%(ClientClassName)` |
| `Namespace` | `string` | `$(RootNamespace).Client` | `--namespace` / `%(Namespace)` |
| `UsesBackingStore` | `bool` | `false` | `--uses-backing-store` |
| `IncludeAdditionalData` | `bool` | `true` | `--include-additional-data` |
| `ExcludeBackwardCompatible` | `bool` | `false` | `--exclude-backward-compatible` |
| `TypeAccessModifier` | `string` | `"Public"` | `--type-access-modifier` |
| `IncludePatterns` | `string[]` | `[]` | `--include-patterns` (semicolon-separated) |
| `ExcludePatterns` | `string[]` | `[]` | `--exclude-patterns` (semicolon-separated) |
| `Serializers` | `string[]` | Kiota defaults | `--serializers` |
| `Deserializers` | `string[]` | Kiota defaults | `--deserializers` |
| `StructuredMimeTypes` | `string[]` | `application/json`, etc. | `--structured-mime-types` |
| `CleanOutput` | `bool` | `false` | `--clean-output` |
| `DisableValidationRules` | `string[]` | `[]` | `--disable-validation-rules` |
| `LogLevel` | `LogLevel` | `Information` | `--log-level` |

### KiotaGeneratorConfig (Phase 2 — Source Generator input)

| Field | Type | Default | Source |
|-------|------|---------|--------|
| `ClientClassName` | `string` | `"ApiClient"` | `build_metadata.AdditionalFiles.KiotaClientName` |
| `ClientNamespaceName` | `string` | `"ApiSdk"` | `build_metadata.AdditionalFiles.KiotaNamespace` |
| `UsesBackingStore` | `bool` | `false` | `build_metadata.AdditionalFiles.KiotaUsesBackingStore` or global default |
| `IncludeAdditionalData` | `bool` | `true` | `build_metadata.AdditionalFiles.KiotaIncludeAdditionalData` or global default |
| `ExcludeBackwardCompatible` | `bool` | `false` | `build_metadata.AdditionalFiles.KiotaExcludeBackwardCompatible` or global default |
| `TypeAccessModifier` | `string` | `"Public"` | `build_metadata.AdditionalFiles.KiotaTypeAccessModifier` or global default |
| `IncludePatterns` | `string[]` | `[]` | `build_metadata.AdditionalFiles.KiotaIncludePatterns` |
| `ExcludePatterns` | `string[]` | `[]` | `build_metadata.AdditionalFiles.KiotaExcludePatterns` |

**Equality**: Must implement `IEquatable<KiotaGeneratorConfig>` for Roslyn incremental caching.

---

## 2. CodeDOM Types (Phase 2 — netstandard2.0 re-implementation)

### Class Hierarchy

```
CodeElement (abstract base)
├── CodeNamespace
│   ├── Name: string
│   ├── Classes: List<CodeClass>
│   ├── Enums: List<CodeEnum>
│   └── Namespaces: List<CodeNamespace>
│
├── CodeClass
│   ├── Name: string
│   ├── Kind: CodeClassKind
│   ├── BaseClass: CodeType?
│   ├── Interfaces: List<CodeType>
│   ├── Properties: List<CodeProperty>
│   ├── Methods: List<CodeMethod>
│   ├── InnerClasses: List<CodeClass>
│   └── Description: string?
│
├── CodeMethod
│   ├── Name: string
│   ├── Kind: CodeMethodKind
│   ├── ReturnType: CodeTypeBase
│   ├── Parameters: List<CodeParameter>
│   ├── IsAsync: bool
│   ├── IsStatic: bool
│   ├── AccessModifier: AccessModifier
│   └── Description: string?
│
├── CodeProperty
│   ├── Name: string
│   ├── Kind: CodePropertyKind
│   ├── Type: CodeTypeBase
│   ├── SerializedName: string?
│   ├── IsReadOnly: bool
│   └── Description: string?
│
├── CodeEnum
│   ├── Name: string
│   ├── Options: List<CodeEnumOption>
│   └── IsFlags: bool
│
├── CodeEnumOption
│   ├── Name: string
│   └── SerializedName: string
│
├── CodeType : CodeTypeBase
│   ├── Name: string
│   ├── TypeDefinition: CodeElement?  (resolved reference)
│   ├── IsNullable: bool
│   ├── IsCollection: bool
│   ├── IsExternal: bool
│   └── CollectionKind: CollectionKind
│
├── CodeUnionType : CodeTypeBase    (oneOf)
│   └── Types: List<CodeType>
│
├── CodeIntersectionType : CodeTypeBase  (anyOf)
│   └── Types: List<CodeType>
│
├── CodeIndexer
│   ├── Name: string
│   ├── ReturnType: CodeType
│   ├── ParameterName: string
│   └── PathSegment: string
│
└── CodeParameter
    ├── Name: string
    ├── Type: CodeTypeBase
    ├── Kind: CodeParameterKind
    └── Optional: bool
```

### Enumerations

```
CodeClassKind:
  RequestBuilder | Model | QueryParameters | RequestConfiguration

CodeMethodKind:
  Constructor | RequestExecutor | RequestGenerator | Serializer |
  Deserializer | Factory | Getter | Setter | IndexerAccessor | WithUrl

CodePropertyKind:
  Custom | UrlTemplate | PathParameters | RequestAdapter |
  BackingStore | AdditionalData | QueryParameter | Navigation

CodeParameterKind:
  Path | QueryParameter | Body | RequestConfiguration |
  RawUrl | RequestAdapter | Cancellation

AccessModifier:
  Public | Internal

CollectionKind:
  None | Array | Complex (List<T>)
```

---

## 3. OpenAPI-to-CodeDOM Type Mapping

| OpenAPI Type | Format | C# Type | CodeType.Name | IsNullable |
|-------------|--------|---------|---------------|------------|
| `string` | — | `string` | `string` | Yes |
| `string` | `date-time` | `DateTimeOffset` | `DateTimeOffset` | Yes (struct → nullable) |
| `string` | `date` | `DateOnly` | `DateOnly` | Yes |
| `string` | `time` | `TimeOnly` | `TimeOnly` | Yes |
| `string` | `duration` | `TimeSpan` | `TimeSpan` | Yes |
| `string` | `uuid` | `Guid` | `Guid` | Yes |
| `string` | `binary` | `Stream` | `Stream` | Yes |
| `string` | `byte` | `byte[]` | `byte` | Yes (collection) |
| `integer` | `int32` | `int` | `int` | Yes (nullable) |
| `integer` | `int64` | `long` | `long` | Yes (nullable) |
| `number` | `float` | `float` | `float` | Yes (nullable) |
| `number` | `double` | `double` | `double` | Yes (nullable) |
| `number` | `decimal` | `decimal` | `decimal` | Yes (nullable) |
| `boolean` | — | `bool` | `bool` | Yes (nullable) |
| `array` | — | `List<T>` | inner type | collection |
| `object` | — | nested `CodeClass` | class name | Yes |

---

## 4. Composition Mapping

| OpenAPI Composition | C# Pattern | CodeDOM Representation |
|---------------------|-----------|------------------------|
| `allOf` (single `$ref` + props) | Class inheritance | `CodeClass` with `BaseClass` set |
| `allOf` (multiple `$ref`) | First ref = base, rest merged | `CodeClass` with merged properties |
| `oneOf` | `IComposedTypeWrapper` | `CodeUnionType` → generates wrapper class |
| `anyOf` | `IComposedTypeWrapper` | `CodeIntersectionType` → generates wrapper class |
| `discriminator` | Factory method with switch | `CreateFromDiscriminatorValue` with mapping dict |

---

## 5. MSBuild Item Definitions

### KiotaOpenApiReference (Phase 1)

| Metadata | Type | Default | Maps To |
|----------|------|---------|---------|
| `Include` | path | *(required)* | OpenAPI spec file path |
| `ClientClassName` | string | `"ApiClient"` | `GenerationConfiguration.ClientClassName` |
| `Namespace` | string | `$(RootNamespace).Client` | `GenerationConfiguration.ClientNamespaceName` |
| `UsesBackingStore` | bool | `false` | `GenerationConfiguration.UsesBackingStore` |
| `IncludeAdditionalData` | bool | `true` | `GenerationConfiguration.IncludeAdditionalData` |
| `ExcludeBackwardCompatible` | bool | `false` | `GenerationConfiguration.ExcludeBackwardCompatible` |
| `TypeAccessModifier` | string | `"Public"` | `GenerationConfiguration.TypeAccessModifier` |
| `IncludePatterns` | string | `""` | Semicolon-separated glob patterns |
| `ExcludePatterns` | string | `""` | Semicolon-separated glob patterns |
| `Serializers` | string | Kiota defaults | Semicolon-separated factory class names |
| `Deserializers` | string | Kiota defaults | Semicolon-separated factory class names |
| `StructuredMimeTypes` | string | `application/json`, etc. | Semicolon-separated MIME types |
| `DisableValidationRules` | string | `""` | Semicolon-separated rule names |

### AdditionalFiles with Kiota Metadata (Phase 2)

| Metadata | Type | Maps To |
|----------|------|---------|
| `KiotaClientName` | string | `KiotaGeneratorConfig.ClientClassName` |
| `KiotaNamespace` | string | `KiotaGeneratorConfig.ClientNamespaceName` |
| `KiotaUsesBackingStore` | bool | `KiotaGeneratorConfig.UsesBackingStore` |
| `KiotaIncludeAdditionalData` | bool | `KiotaGeneratorConfig.IncludeAdditionalData` |
| `KiotaExcludeBackwardCompatible` | bool | `KiotaGeneratorConfig.ExcludeBackwardCompatible` |
| `KiotaTypeAccessModifier` | string | `KiotaGeneratorConfig.TypeAccessModifier` |
| `KiotaIncludePatterns` | string | Semicolon-separated glob patterns |
| `KiotaExcludePatterns` | string | Semicolon-separated glob patterns |

---

## 6. Generated Output Entity Relationships

```
ClientRootClass (extends BaseRequestBuilder)
├── registers serializer/deserializer factories
├── sets base URL
└── has navigation properties to →
    SegmentRequestBuilder (extends BaseRequestBuilder)
    ├── UrlTemplate: string
    ├── has navigation properties to child segments →
    ├── has indexers for parameterized segments →
    │   └── ItemRequestBuilder (extends BaseRequestBuilder)
    ├── has executor methods (GetAsync, PostAsync, etc.) →
    │   └── returns ModelClass or void
    ├── has request info builders (ToGetRequestInformation, etc.) →
    │   └── returns RequestInformation
    ├── has WithUrl method →
    │   └── returns self type with raw URL
    └── has inner classes:
        ├── QueryParameters (one per HTTP method with query params)
        └── RequestConfiguration (deprecated, backward compat only)

ModelClass (implements IParsable + optional IAdditionalDataHolder + optional IBackedModel)
├── properties mapped from OpenAPI schema
├── CreateFromDiscriminatorValue (static factory)
├── GetFieldDeserializers (JSON key → setter lambda)
├── Serialize (write properties)
└── may inherit from another ModelClass (allOf)

ErrorModelClass (extends ApiException, implements IParsable)
├── same as ModelClass
└── overrides Message property

EnumType
├── options with [EnumMember] attributes
└── optional [Flags] attribute

ComposedTypeWrapper (implements IComposedTypeWrapper, IParsable)
├── one property per composed member type
├── delegates serialization/deserialization to active member
└── factory method tries each type
```

---

## 7. Entity Validation Rules

| Entity | Rule | Enforced By |
|--------|------|-------------|
| `KiotaOpenApiReference` | `Include` path must exist and be readable | MSBuild Exec task (Phase 1) |
| `KiotaOpenApiReference` | `ClientClassName` must be valid C# identifier | Generator CLI validation |
| `KiotaOpenApiReference` | `Namespace` must be valid C# namespace | Generator CLI validation |
| `AdditionalFiles` (Phase 2) | `KiotaClientName` metadata required for Kiota processing | Source generator filter |
| `AdditionalFiles` (Phase 2) | File extension must be `.json`, `.yaml`, or `.yml` | Source generator filter |
| `KiotaGeneratorConfig` | Must implement `IEquatable<T>` properly for incremental caching | Unit tests |
| CodeDOM types | All `CodeType` references must be resolved before emission | `MapTypeDefinitions()` phase |
| Generated code | Must compile against Kiota runtime 1.21.0 | Parity tests |
