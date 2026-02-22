# Project Structure & Repository Layout

## Summary

This document specifies the new projects, build configurations, NuGet packaging, and solution changes required to integrate Kiota code generation into the Uno.Extensions repository.

---

## 1. New Projects

### Phase 1: MSBuild Task

#### `Uno.Extensions.Http.Kiota.Generator.Cli`

A console application that wraps `Microsoft.OpenApi.Kiota.Builder` for MSBuild invocation.

```
src/
  Uno.Extensions.Http.Kiota.Generator.Cli/
    Uno.Extensions.Http.Kiota.Generator.Cli.csproj
    Program.cs
    KiotaGeneratorCommand.cs
    GeneratorConfiguration.cs
```

**`.csproj`**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <!-- Multi-target for broad runtime support -->
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
    <RollForward>Major</RollForward>
    <AssemblyName>kiota-gen</AssemblyName>
    <RootNamespace>Uno.Extensions.Http.Kiota.Generator.Cli</RootNamespace>

    <!-- Packaging: embed in NuGet tools/ folder -->
    <IsPackable>true</IsPackable>
    <PackAsTool>false</PackAsTool>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
    <PublishTrimmed>true</PublishTrimmed>
    <PublishSingleFile>true</PublishSingleFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.OpenApi.Kiota.Builder" />
    <PackageReference Include="System.CommandLine" />
  </ItemGroup>
</Project>
```

#### `Uno.Extensions.Http.Kiota.Generator.Tasks`

An MSBuild task library providing the `<KiotaGenerate>` task and associated `.props`/`.targets` files.

```
src/
  Uno.Extensions.Http.Kiota.Generator.Tasks/
    Uno.Extensions.Http.Kiota.Generator.Tasks.csproj
    KiotaGenerateTask.cs
    buildTransitive/
      Uno.Extensions.Http.Kiota.Generator.props
      Uno.Extensions.Http.Kiota.Generator.targets
```

**`.csproj`**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IsPackable>true</IsPackable>
    <PackageId>Uno.Extensions.Http.Kiota.Generator</PackageId>
    <Description>MSBuild integration for Kiota OpenAPI code generation</Description>
    <DevelopmentDependency>true</DevelopmentDependency>
    <NoPackageAnalysis>true</NoPackageAnalysis>
    <SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Build.Framework" PrivateAssets="All" />
    <PackageReference Include="Microsoft.Build.Utilities.Core" PrivateAssets="All" />
  </ItemGroup>

  <!-- Pack build files -->
  <ItemGroup>
    <None Include="buildTransitive\**\*" Pack="true" PackagePath="buildTransitive" />
  </ItemGroup>

  <!-- Pack CLI tool alongside -->
  <ItemGroup>
    <None Include="..\Uno.Extensions.Http.Kiota.Generator.Cli\bin\$(Configuration)\net8.0\publish\**"
          Pack="true" PackagePath="tools\net8.0\any" />
    <None Include="..\Uno.Extensions.Http.Kiota.Generator.Cli\bin\$(Configuration)\net9.0\publish\**"
          Pack="true" PackagePath="tools\net9.0\any" />
  </ItemGroup>
</Project>
```

### Phase 2: Source Generator

#### `Uno.Extensions.Http.Kiota.SourceGenerator`

A Roslyn incremental source generator, following the existing `Uno.Extensions.Core.Generators` pattern.

```
src/
  Uno.Extensions.Http.Kiota.SourceGenerator/
    Uno.Extensions.Http.Kiota.SourceGenerator.csproj
    KiotaSourceGenerator.cs
    Configuration/
      KiotaGeneratorConfig.cs
      ConfigurationReader.cs
    Parsing/
      OpenApiDocumentParser.cs
    CodeDom/
      CodeElement.cs
      CodeNamespace.cs
      CodeClass.cs
      CodeMethod.cs
      CodeProperty.cs
      CodeEnum.cs
      CodeType.cs
      CodeUnionType.cs
      CodeIntersectionType.cs
      CodeIndexer.cs
      CodeParameter.cs
      KiotaCodeDomBuilder.cs
    Refinement/
      CSharpRefiner.cs
      CSharpConventionService.cs
    Emitter/
      CSharpEmitter.cs
      ClassDeclarationEmitter.cs
      ConstructorEmitter.cs
      PropertyEmitter.cs
      MethodEmitter.cs
      SerializerEmitter.cs
      DeserializerEmitter.cs
      FactoryMethodEmitter.cs
      EnumEmitter.cs
    buildTransitive/
      Uno.Extensions.Http.Kiota.SourceGenerator.props
```

**`.csproj`**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <IsPackable>false</IsPackable>
    <ToolOfPackage>Uno.Extensions.Http.Kiota</ToolOfPackage>
    <RootNamespace>Uno.Extensions.Http.Kiota.SourceGenerator</RootNamespace>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Uno.Roslyn" PrivateAssets="All" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="All" />
    <PackageReference Include="Microsoft.OpenApi" PrivateAssets="All" />
    <PackageReference Include="Microsoft.OpenApi.Readers" PrivateAssets="All" />
    <PackageReference Include="DotNet.Glob" PrivateAssets="All" />
  </ItemGroup>

  <!-- Build transitive props for CompilerVisibleProperty -->
  <ItemGroup>
    <None Include="buildTransitive\**\*"
          CopyToOutputDirectory="PreserveNewest"
          Pack="true"
          PackagePath="buildTransitive" />
  </ItemGroup>
</Project>
```

This follows the exact pattern from `Uno.Extensions.Core.Generators`:
- `netstandard2.0` target
- `IsPackable=false` (packed via `ToolOfPackage`)
- `ToolOfPackage=Uno.Extensions.Http.Kiota` (routes output to `analyzers/dotnet/cs` in the host package)
- All dependencies are `PrivateAssets="All"`

---

## 2. Test Projects

### `Uno.Extensions.Http.Kiota.Generator.Tests`

```
src/
  Uno.Extensions.Http.Kiota.Generator.Tests/
    Uno.Extensions.Http.Kiota.Generator.Tests.csproj
    Phase1/
      CliIntegrationTests.cs
      MsBuildTaskTests.cs
    Phase2/
      OpenApiParserTests.cs
      CodeDomBuilderTests.cs
      CSharpEmitterTests.cs
      GeneratorIntegrationTests.cs
    Parity/
      ParityTestBase.cs
      PetstoreParityTests.cs
      InheritanceParityTests.cs
      ComposedTypeParityTests.cs
    TestData/
      petstore.json
      inheritance.json
      composed-types.json
      enums.json
      error-responses.json
    GoldenFiles/
      petstore/
        PetStoreClient.cs
        Pets/
          PetsRequestBuilder.cs
        Models/
          Pet.cs
          Error.cs
```

**`.csproj`**:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="MSTest.TestAdapter" />
    <PackageReference Include="MSTest.TestFramework" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Uno.Extensions.Http.Kiota.SourceGenerator\Uno.Extensions.Http.Kiota.SourceGenerator.csproj" />
    <ProjectReference Include="..\Uno.Extensions.Http.Kiota.Generator.Tasks\Uno.Extensions.Http.Kiota.Generator.Tasks.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include="TestData\**\*" CopyToOutputDirectory="PreserveNewest" />
    <None Include="GoldenFiles\**\*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

---

## 3. Directory.Packages.props Updates

Add new centrally-managed package versions:

```xml
<!-- In src/Directory.Packages.props -->

<!-- Kiota Builder (Phase 1 - used by CLI) -->
<PackageVersion Include="Microsoft.OpenApi.Kiota.Builder" Version="1.30.0" />

<!-- OpenAPI parsing (Phase 2 - netstandard2.0 compatible) -->
<PackageVersion Include="Microsoft.OpenApi" Version="3.3.1" />
<PackageVersion Include="Microsoft.OpenApi.Readers" Version="1.6.28" />
<PackageVersion Include="DotNet.Glob" Version="3.1.3" />

<!-- MSBuild Task authoring -->
<PackageVersion Include="Microsoft.Build.Framework" Version="17.12.6" />
<PackageVersion Include="Microsoft.Build.Utilities.Core" Version="17.12.6" />

<!-- CLI -->
<PackageVersion Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
```

Note: The existing Kiota runtime package references (v1.21.0) in `Directory.Packages.props` remain unchanged.

---

## 4. Solution Registration

### Uno.Extensions.sln

Add new projects under a `Kiota` solution folder:

```
Solution Folders:
  Kiota/
    Uno.Extensions.Http.Kiota                     (existing)
    Uno.Extensions.Http.Kiota.Generator.Cli       (new - Phase 1)
    Uno.Extensions.Http.Kiota.Generator.Tasks     (new - Phase 1)
    Uno.Extensions.Http.Kiota.SourceGenerator     (new - Phase 2)
    Uno.Extensions.Http.Kiota.Generator.Tests     (new)
```

### Solution Filters

Update `Uno.Extensions-packageonly.slnf` if applicable to include the new generator projects.

---

## 5. NuGet Package Layout

### Phase 1 Package: `Uno.Extensions.Http.Kiota.Generator`

```
Uno.Extensions.Http.Kiota.Generator.nupkg
├── buildTransitive/
│   ├── Uno.Extensions.Http.Kiota.Generator.props    ← KiotaOpenApiReference item definition
│   └── Uno.Extensions.Http.Kiota.Generator.targets  ← CoreCompileDependsOn hook
├── tools/
│   ├── net8.0/
│   │   └── any/
│   │       ├── kiota-gen.dll
│   │       ├── kiota-gen.deps.json
│   │       └── ... (dependencies)
│   └── net9.0/
│       └── any/
│           ├── kiota-gen.dll
│           └── ...
└── lib/
    └── netstandard2.0/
        └── Uno.Extensions.Http.Kiota.Generator.Tasks.dll
```

This package is a `DevelopmentDependency` — it won't flow to consuming projects' transitive dependencies.

### Phase 2: Source Generator in Host Package

The source generator ships inside the existing `Uno.Extensions.Http.Kiota` NuGet package, following the `ToolOfPackage` pattern:

```
Uno.Extensions.Http.Kiota.nupkg
├── lib/
│   └── net9.0/
│       └── Uno.Extensions.Http.Kiota.dll
├── analyzers/
│   └── dotnet/
│       └── cs/
│           ├── Uno.Extensions.Http.Kiota.SourceGenerator.dll
│           ├── Microsoft.OpenApi.dll              ← bundled dependency
│           ├── Microsoft.OpenApi.Readers.dll      ← bundled dependency
│           └── DotNet.Glob.dll                    ← bundled dependency
└── buildTransitive/
    └── Uno.Extensions.Http.Kiota.SourceGenerator.props  ← CompilerVisibleProperty declarations
```

The `ToolOfPackage` mechanism in `Uno.CrossTargeting.props` handles copying the generator output to the correct NuGet cache path under `analyzers/dotnet/cs`.

---

## 6. Build Pipeline Integration

### Build Order Dependencies

```
Uno.Extensions.Http.Kiota.SourceGenerator
  └── (no project dependencies; only NuGet packages)

Uno.Extensions.Http.Kiota.Generator.Cli
  └── (no project dependencies; only NuGet packages)

Uno.Extensions.Http.Kiota.Generator.Tasks
  └── Uno.Extensions.Http.Kiota.Generator.Cli (build output reference, not ProjectReference)

Uno.Extensions.Http.Kiota
  └── Existing project references (Authentication, Configuration, Http, Serialization)
  └── Uno.Extensions.Http.Kiota.SourceGenerator (via ToolOfPackage, not direct reference)

Uno.Extensions.Http.Kiota.Generator.Tests
  └── Uno.Extensions.Http.Kiota.SourceGenerator (ProjectReference)
  └── Uno.Extensions.Http.Kiota.Generator.Tasks (ProjectReference)
```

### CI Pipeline Changes

Add to `build/ci/`:

1. **Build step**: Include new projects in the build matrix
2. **Test step**: Run `Uno.Extensions.Http.Kiota.Generator.Tests`
3. **Package step**: Build and pack the Phase 1 NuGet package
4. **Parity validation step** (optional): Run golden-file parity tests with latest Kiota CLI output

---

## 7. Full Repository Tree (New Files Only)

```
src/
├── Directory.Packages.props                              (MODIFIED)
│
├── Uno.Extensions.Http.Kiota/                            (EXISTING - MODIFIED)
│   ├── Uno.Extensions.Http.Kiota.csproj                  (MODIFIED - add SourceGenerator ref)
│   └── specs/                                            (NEW - this spec suite)
│       ├── 00-overview.md
│       ├── 01-kiota-architecture-analysis.md
│       ├── 02-phase1-msbuild-task.md
│       ├── 03-phase2-source-generator.md
│       ├── 04-generated-code-structure.md
│       ├── 05-project-structure.md
│       └── 06-risk-analysis.md
│
├── Uno.Extensions.Http.Kiota.Generator.Cli/              (NEW - Phase 1)
│   ├── Uno.Extensions.Http.Kiota.Generator.Cli.csproj
│   ├── Program.cs
│   ├── KiotaGeneratorCommand.cs
│   └── GeneratorConfiguration.cs
│
├── Uno.Extensions.Http.Kiota.Generator.Tasks/            (NEW - Phase 1)
│   ├── Uno.Extensions.Http.Kiota.Generator.Tasks.csproj
│   ├── KiotaGenerateTask.cs
│   └── buildTransitive/
│       ├── Uno.Extensions.Http.Kiota.Generator.props
│       └── Uno.Extensions.Http.Kiota.Generator.targets
│
├── Uno.Extensions.Http.Kiota.SourceGenerator/            (NEW - Phase 2)
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
    ├── Phase2/
    ├── Parity/
    ├── TestData/
    └── GoldenFiles/
```

---

## 8. Modifications to Existing Files

### `src/Uno.Extensions.Http.Kiota/Uno.Extensions.Http.Kiota.csproj`

No direct code changes, but the `ToolOfPackage` mechanism in `Uno.CrossTargeting.props` will automatically bundle the source generator output into this project's NuGet package when `Uno.Extensions.Http.Kiota.SourceGenerator` sets `<ToolOfPackage>Uno.Extensions.Http.Kiota</ToolOfPackage>`.

### `Uno.Extensions.sln`

Add all 4 new projects with appropriate solution folder grouping.

### `src/Directory.Packages.props`

Add the new package version entries as described in Section 3.

### Build CI scripts

Add build/test steps for the new projects.
