# Quick Start & Validation Guide: Kiota MSBuild Code Generation

**Feature**: kiota-source-gen | **Date**: 2026-02-21

## Phase 1 Validation: MSBuild Task

### Prerequisites

- .NET 9.0 SDK installed
- Clone of `uno.extensions` repository
- Test OpenAPI spec (e.g., Petstore)

### Step 1: Build the Generator CLI

```bash
cd src/Uno.Extensions.Http.Kiota.Generator.Cli
dotnet publish -c Release -r win-x64 --self-contained -o ../../tools/win-x64
# (repeat for linux-x64, osx-x64, osx-arm64 as needed)
```

### Step 2: Verify CLI Standalone

```bash
./tools/win-x64/kiota-gen.exe \
  --openapi TestData/petstore.json \
  --output ./test-output \
  --class-name PetStoreClient \
  --namespace MyApp.PetStore
```

**Expected**: Exit code 0 + generated `.cs` files in `./test-output/`

### Step 3: Verify MSBuild Integration

Create a test `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Kiota.Abstractions" Version="1.21.0" />
    <PackageReference Include="Microsoft.Kiota.Http.HttpClientLibrary" Version="1.21.0" />
    <PackageReference Include="Microsoft.Kiota.Serialization.Json" Version="1.21.0" />
    <PackageReference Include="Microsoft.Kiota.Serialization.Text" Version="1.21.0" />
    <PackageReference Include="Microsoft.Kiota.Serialization.Form" Version="1.21.0" />
    <PackageReference Include="Microsoft.Kiota.Serialization.Multipart" Version="1.21.0" />
  </ItemGroup>

  <ItemGroup>
    <KiotaOpenApiReference Include="petstore.json"
      ClientClassName="PetStoreClient"
      Namespace="TestApp.PetStore" />
  </ItemGroup>
</Project>
```

```bash
dotnet build
```

**Expected**:
- [ ] Build succeeds with no errors
- [ ] Generated `.cs` files appear in `obj/KiotaGenerated/PetStoreClient/`
- [ ] `PetStoreClient` class extends `BaseRequestBuilder`
- [ ] Navigation properties for API endpoints are present
- [ ] Model classes implement `IParsable`

### Step 4: Verify DI Registration

```csharp
// In a test project:
services.AddKiotaClient<PetStoreClient>(context, options: new EndpointOptions { Url = "https://petstore.example.com" });

// Resolve and verify type
var client = serviceProvider.GetRequiredService<PetStoreClient>();
Assert.IsNotNull(client);
```

**Expected**: Client resolves successfully via DI.

### Step 5: Verify Incremental Build

```bash
dotnet build  # first build — generates code
dotnet build  # second build — should skip generation (check MSBuild output)
# Modify petstore.json timestamp
dotnet build  # third build — should regenerate
```

**Expected**:
- [ ] Second build shows `Skipping target "_KiotaGenerate"` or similar
- [ ] Third build re-runs generation

---

## Phase 2 Validation: Source Generator

### Step 1: Verify Generator Loads in IDE

Create a test `.csproj` referencing the source generator:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Kiota.Abstractions" Version="1.21.0" />
    <!-- other Kiota runtime packages -->
  </ItemGroup>

  <ItemGroup>
    <AdditionalFiles Include="petstore.json"
      KiotaClientName="PetStoreClient"
      KiotaNamespace="TestApp.PetStore" />
  </ItemGroup>

  <!-- Source generator reference -->
  <ItemGroup>
    <ProjectReference Include="..\Uno.Extensions.Http.Kiota.SourceGenerator\Uno.Extensions.Http.Kiota.SourceGenerator.csproj"
      OutputItemType="Analyzer"
      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

**Expected**: IntelliSense shows `PetStoreClient` class after opening the project.

### Step 2: Verify Live Regeneration

1. Open the test project in Visual Studio / VS Code
2. Save the `petstore.json` file (touch timestamp or make a change)
3. Check that IntelliSense updates without a build

**Expected**: Generated types reflect any spec changes within a few seconds.

### Step 3: Verify Build Compilation

```bash
dotnet build
```

**Expected**:
- [ ] Build succeeds
- [ ] No source generator warnings/errors
- [ ] Generated code appears under `Dependencies > Analyzers > Uno.Extensions.Http.Kiota.SourceGenerator`

---

## Parity Validation (Both Phases)

### Golden File Test

1. Generate code using Kiota CLI:
   ```bash
   kiota generate -l CSharp -d petstore.json -c PetStoreClient -n TestApp.PetStore -o ./golden/
   ```

2. Generate using Phase 1 CLI:
   ```bash
   kiota-gen --openapi petstore.json --output ./phase1/ --class-name PetStoreClient --namespace TestApp.PetStore
   ```

3. Generate using Phase 2 (build test project)

4. Diff all three outputs:
   ```bash
   diff -r ./golden/ ./phase1/   # Phase 1 vs Kiota CLI
   diff -r ./golden/ ./phase2/   # Phase 2 vs Kiota CLI (once Phase 2 ships)
   ```

**Expected**: Only acceptable differences (version strings, timestamps).

### Test Specs for Parity

| Spec | Key Features Tested |
|------|-------------------|
| `petstore.json` | Basic CRUD, simple models, path parameters, query parameters |
| `inheritance.json` | `allOf` composition, base class inheritance, discriminators |
| `composed-types.json` | `oneOf` union types, `anyOf` intersection types |
| `enums.json` | Simple enums, flags enums, enum properties |
| `error-responses.json` | Error models extending `ApiException`, error mappings |

---

## Smoke Test Checklist

### Phase 1

- [ ] `dotnet build` generates client from OpenAPI spec with zero manual steps
- [ ] No `dotnet tool install` required
- [ ] Generated code compiles against Kiota runtime 1.21.0
- [ ] `AddKiotaClient<T>()` registration works with generated client
- [ ] Incremental build skips generation when spec unchanged
- [ ] Multiple `<KiotaOpenApiReference>` items supported
- [ ] Error messages from generator surface as MSBuild errors/warnings

### Phase 2

- [ ] IntelliSense shows generated types in IDE
- [ ] Generated types update when OpenAPI spec file is saved
- [ ] Build compiles generated source
- [ ] Generated code matches Phase 1 / Kiota CLI output
- [ ] Multiple `<AdditionalFiles>` with different `KiotaClientName` supported
- [ ] Large spec (10K+ lines) doesn't freeze IDE (incremental caching works)
- [ ] Missing/invalid spec files emit diagnostic warnings, not crashes
