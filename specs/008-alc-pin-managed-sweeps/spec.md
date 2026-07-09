# Process-wide statics pin previewed-app AssemblyLoadContexts (Batch 2 managed sweeps)

**Status:** In progress (branch `dev/nr/wasm-alc-sweeps-ext`)
**Issue:** [#3126](https://github.com/unoplatform/uno.extensions/issues/3126)
**Affects:** `Uno.Extensions.Configuration`, `Uno.Extensions.Core`, `Uno.Extensions.Navigation.UI`, `Uno.Extensions.Reactive`, `Uno.Extensions.Serialization`, `Uno.Extensions.Logging`

## Problem

Several process-wide `static` fields capture the first host's `ILoggerFactory` / `ServiceProvider`
/ `Assembly` / serializer state at first touch and never release or re-scope it. In a **downstream
host that loads previewed apps into their own collectible `AssemblyLoadContext`s** (load app, run,
unload, load the next app into a fresh ALC), these statics keep the *previous* app's object graph
alive for the whole process lifetime, so the collectible ALC can never be collected. One static
additionally serves the **wrong app's configuration** to a later load — a functional bug independent
of the leak.

Standalone (single-app) usage is unaffected: the process only ever hosts one app, so a permanent
static reference is harmless. The defects only manifest when a second app is loaded after the first
is torn down.

## Findings

| # | Static | Project | Kind |
| --- | --- | --- | --- |
| 1 | `LogExtensions._unoLogger` / `_boundToUnoLogger` (+ `Holder<T>.Logger`); ambient factory reset on shutdown | Reactive / Logging | ALC pin (first `ILoggerFactory` + its `ServiceProvider`) |
| 2 | `Region.Logger` | Navigation.UI | ALC pin (last host's logger/provider graph) |
| 3 | `PlatformHelper._appAssembly` | Core | ALC pin (previewed app `Assembly`) |
| 4 | `EmbeddedAppConfigurationFile.AllFiles<T>()` static cache | Configuration | **Functional bug** + ALC pin |
| 5 | `NavigationRouteUpdateHandler` context / `RootRegion` / `Resolver` retention | Navigation.UI | ALC pin on non-graceful teardown |
| 6 | `HotReloadService._latestShadow` | Reactive | ALC pin (previewed-app `Type`s) |
| 7 | `JsonSerializationOptions.DefaultSerializerOptions` fallbacks | Serialization | ALC pin (cached `JsonTypeInfo` for app `Type`s) |

## Out of scope (follow-ups)

`FeedDependencyRegistry` purge, `ApplicationUpdated` weak subscriptions, `SourceContext`
deterministic teardown, `RouteResolverDefault` ALC-scoped scan.

---

## Finding 4 — `EmbeddedAppConfigurationFile.AllFiles<T>()` first-caller-wins cache (functional bug)

**Files touched:**

- `src/Uno.Extensions.Configuration/EmbeddedAppConfigurationFile.cs`
- `src/Uno.Extensions.Configuration.Tests/` *(new MSTest project — red/fix/green + non-regression)*

### Root cause

`AllFiles<TApplicationRoot>()` scans the manifest resources of `typeof(TApplicationRoot).Assembly`
for `appsettings*` files and caches the result in a single process-wide field:

```csharp
private static EmbeddedAppConfigurationFile[]? _appConfigurationFiles;

public static EmbeddedAppConfigurationFile[] AllFiles<TApplicationRoot>() where TApplicationRoot : class
{
    if (_appConfigurationFiles is null)                       // <-- keyed on "have we run once", not on the assembly
    {
        var executingAssembly = typeof(TApplicationRoot).Assembly;
        _appConfigurationFiles = executingAssembly.GetManifestResourceNames() ... .ToArray();
    }
    return _appConfigurationFiles;                            // <-- always the FIRST caller's files
}
```

The cache key is "has this method run at all", not the assembly. After the first call the field is
non-null forever, so every later `AllFiles<TOtherAppRoot>()` returns the **first** app's files and
ignores its own type argument. Two consequences:

1. **Functional bug:** a second app hosted after the first receives the first app's `appsettings.*`
   (or, if the first app had none, an empty set) — cross-app configuration bleed.
2. **ALC pin:** the first `EmbeddedAppConfigurationFile` instances hold a strong `Assembly`
   reference, keeping the first app's collectible ALC alive for the process lifetime.

### Fix

Rekey the cache per-`Assembly` with a `ConditionalWeakTable<Assembly, EmbeddedAppConfigurationFile[]>`.
Each assembly gets its own cached array; the `ConditionalWeakTable` holds the key **weakly**, so once
an app's ALC unloads the entry (and its cached `Assembly`) becomes collectible.

```csharp
private static readonly ConditionalWeakTable<Assembly, EmbeddedAppConfigurationFile[]> _appConfigurationFiles = new();

public static EmbeddedAppConfigurationFile[] AllFiles<TApplicationRoot>() where TApplicationRoot : class
{
    var executingAssembly = typeof(TApplicationRoot).Assembly;
    return _appConfigurationFiles.GetValue(
        executingAssembly,
        static assembly => assembly.GetManifestResourceNames() ... .ToArray());
}
```

`GetValue` remains a cache (repeated calls for the same assembly return the identical array), so the
caching behavior the callers relied on is preserved — only the key is corrected.

### Testing (red/fix/green) — `Uno.Extensions.Configuration.Tests` (new MSTest, net9.0)

The test assembly embeds exactly one `appsettings.json`, so `AllFiles<TypeInThisAssembly>()`
returns one file while `AllFiles<FrameworkType>()` (an assembly with no `appsettings` resource)
must return **none**. This reproduces two apps resolving distinct assemblies.

- `When_SecondAssemblyRequested_Then_DoesNotReturnFirstAssemblyFiles` — prime with the assembly that
  *has* settings, then request a framework type; the second must be empty.
- `When_FirstRequestedIsWithoutSettings_Then_SecondAssemblyStillGetsItsOwnFiles` — reversed order;
  each assembly still resolves its own files (order-independent).
- `When_SameAssemblyRequestedTwice_Then_ReturnsCachedInstance` — the per-assembly cache still returns
  the identical array instance.

**Red (pre-fix):** the first two tests fail — the second assembly wrongly received
`Uno.Extensions.Configuration.Tests.appsettings.json` (the first caller's file). **Green (post-fix):**
all three pass. Verified via `dotnet test src/Uno.Extensions.Configuration.Tests`.

---

## Finding 3 — `PlatformHelper._appAssembly` strong reference pins the app assembly

**Files touched:**

- `src/Uno.Extensions.Core/PlatformHelper.cs`
- `src/Uno.Extensions.Core.Tests/PlatformHelper/Given_PlatformHelper.cs` *(new red/fix/green test)*

### Root cause

`PlatformHelper` cached the app assembly in a strong static field, set either explicitly via
`SetAppAssembly` or lazily via `GetAppAssembly` (`_appAssembly ??= Assembly.GetCallingAssembly()`):

```csharp
private static Assembly? _appAssembly;
public static void SetAppAssembly(Assembly? assembly) => _appAssembly = assembly;
```

A downstream host calls `SetAppAssembly(previewedAppAssembly)` for each previewed app. The strong
reference kept that assembly — and therefore its collectible ALC — alive for the process lifetime,
so no previewed app's ALC could ever be collected.

### Fix

Hold the reference weakly (`WeakReference<Assembly>?`). `SetAppAssembly` wraps the assembly in a
`WeakReference` (or clears the field on `null`); `GetAppAssembly` returns the target if still alive
and otherwise re-derives the fallback (`GetEntryAssembly` / `GetCallingAssembly`) and re-caches it
weakly. Behavior is unchanged while any strong reference to the assembly exists (the host holds one
while the app is loaded); only the *pin* after the host drops its references is removed.

### Testing (red/fix/green) — `Uno.Extensions.Core.Tests` (MSTest, net9.0)

`When_SetAppAssembly_From_CollectibleAlc_Then_AlcIsCollectible` emits a tiny assembly with Roslyn,
loads it into a **collectible** `AssemblyLoadContext`, registers it via `SetAppAssembly`, takes a
`WeakReference` to the ALC, unloads it, and forces GC. The ALC must be collected.

The load/register happens in a `[MethodImpl(NoInlining)]` helper so the ALC and assembly locals do
not linger on the caller's stack.

**Red (pre-fix):** the strong reference pins the assembly, the ALC stays alive, the test fails.
**Green (post-fix):** the weak reference lets the ALC collect. Verified via
`dotnet test src/Uno.Extensions.Core.Tests --filter Given_PlatformHelper`.

---

## Finding 7 — `JsonSerializationOptions.DefaultSerializerOptions` handed out as fallback

**Files touched:**

- `src/Uno.Extensions.Serialization/JsonSerializationOptions.cs`
- `src/Uno.Extensions.Serialization/ServiceCollectionExtensions.cs`
- `src/Uno.Extensions.Serialization/SystemTextJsonSerializer.cs`
- `src/Uno.Extensions.Serialization/Uno.Extensions.Serialization.csproj` *(`InternalsVisibleTo` for tests)*
- `src/Uno.Extensions.Serialization.Tests/Given_JsonSerializationOptions.cs` *(new red/fix/green test)*

### Root cause

`DefaultSerializerOptions` is a process-wide `static` `JsonSerializerOptions`. It is *mostly* used as
a template (copied via `new JsonSerializerOptions(DefaultSerializerOptions)`), but two fallback paths
returned the static **directly**:

- `ServiceCollectionExtensions.GetJsonSerializationOptions` — when no `IOptions<JsonSerializationOptions>`
  is registered.
- `SystemTextJsonSerializer` constructor — final `??` fallback.

`System.Text.Json` caches a `JsonTypeInfo` on the options instance for every type it (de)serializes
via reflection. A serializer using the shared static therefore accumulates a `JsonTypeInfo` for every
app type it touches, and each `JsonTypeInfo` holds a `Type`. For a downstream host that loads
previewed apps into collectible ALCs, that pins the app types — and their ALC — on a process-wide
object for the process lifetime.

### Fix

Keep `DefaultSerializerOptions` strictly as a template and add an internal
`CreateSerializerOptions()` factory that returns a fresh `new JsonSerializerOptions(DefaultSerializerOptions)`.
Both fallback paths now hand out a host-scoped copy, so the per-type cache is confined to the host
and released with it. The already-copying registrations were normalized to the same factory.

### Testing (red/fix/green) — `Uno.Extensions.Serialization.Tests` (MSTest, net9.0)

`InternalsVisibleTo` was added so the tests can observe the internal template/factory.

- `When_CreateSerializerOptions_Then_ReturnsFreshCopyNotTemplate` — the factory returns a distinct
  instance (never the template) and preserves the template's configuration.
- `When_NoOptionsConfigured_Then_FallbackDoesNotReturnTemplate` — the `GetJsonSerializationOptions`
  fallback returns a copy, not the shared static.
- `When_TwoHostsRegisterSerialization_Then_EachGetsIndependentOptions` — two hosts each own a distinct
  options instance and serialization still works off the host-scoped copy.

**Red (pre-fix):** `When_NoOptionsConfigured…` fails — the fallback returned the shared template
by reference (demonstrated by temporarily reverting the single fallback line while keeping the new
factory so the suite still compiles). **Green (post-fix):** all three pass, and the full 20-test
Serialization suite stays green (no regressions). Verified via
`dotnet test src/Uno.Extensions.Serialization.Tests`.

## Remaining (this branch)

Findings 1 (`LogExtensions._unoLogger` / `Holder<T>` + ambient reset), 2 (`Region.Logger`),
5 (`NavigationRouteUpdateHandler` retention), and 6 (`HotReloadService._latestShadow`) are tracked
here and landed as follow-up commits.
