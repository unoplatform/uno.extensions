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
