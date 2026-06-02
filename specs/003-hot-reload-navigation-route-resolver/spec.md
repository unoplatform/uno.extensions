# Hot-Reload Navigation Route Resolver Self-Healing

**Status:** Implemented (branch `dev/sb/hr-nav`)
**Affects:** `Uno.Extensions.Navigation.UI`
**Files touched:**

- `src/Uno.Extensions.Navigation.UI/NavigationHostedService.cs`
- `src/Uno.Extensions.Navigation.UI/NavigationRouteUpdateHandler.cs`
- `src/Uno.Extensions.Navigation.UI/NavigationVisibilityUpdateHandler.cs`
- `src/Uno.Extensions.Navigation.UI/Navigators/ControlNavigator.cs`
- `src/Uno.Extensions.Navigation.UI/RouteResolver.cs`

## Problem

After scaffolding a tabbed app via a hot-reload-driven generation pipeline, the content area above the `TabBar` stayed blank forever. The shell rendered, but the `IsDefault` page (for example `HomePage` under the `Home` tab) never appeared. A full app rebuild fixed it; subsequent hot-reload deltas did not.

Every `[MetadataUpdateHandler] UpdateApplication` invocation fired by the CLR aborted at `RebuildRoutes: resolver is null, returning` — so the route resolver was never re-evaluated after hot-reload, and any pending navigation that landed before its target type was loaded had no chance to recover.

### Root cause — silent override of the `IRouteResolver` factory delegate

`ServiceCollectionExtensions.AddNavigation` registers `IRouteResolver` via a factory delegate whose **only** job, beyond returning the resolver, is to side-effect `ctx.Resolver = routeResolver` so the static `NavigationRouteUpdateHandler.UpdateApplication` can find it:

```csharp
.AddSingleton<IRouteResolver>(sp =>
{
    var config = sp.GetRequiredService<NavigationConfiguration>();
    var resolver = (sp.GetRequiredService(config.RouteResolver!) as IRouteResolver)!;
    if (resolver is RouteResolver routeResolver &&
        sp.GetService<NavigationRouteContext>() is { } ctx)
    {
        ctx.Resolver = routeResolver;  // <-- the only assignment of ctx.Resolver
    }
    return resolver;
})
```

`HostBuilderExtensions.UseNavigation` then runs `configureServices` AFTER `AddNavigation` and overrides the registration:

```csharp
services.AddSingleton<IRouteResolver, MappedRouteResolver>();
```

This is a last-registration-wins replacement that bypasses the factory delegate. The factory never runs, `ctx.Resolver` stays `null` forever, and every hot-reload cycle silently no-ops:

- `UpdateApplication CALLED BY CLR with N type(s); _contexts.Count = 1` — the handler IS invoked (so `[MetadataUpdateHandler]` works fine in nested ALCs)
- `RebuildRoutes called (resolver=False, rootRegion=True)` → `RebuildRoutes: resolver is null, returning`
- `ScheduleCascadeForAllContexts: skipping a context (resolver=False, rootRegion=True)`

The symptom looks like "hot-reload doesn't work in the inner ALC", but the CLR mechanism is healthy. The fault is two cooperating registrations in the same library that override each other across two different extension entry points.

### Secondary problem — initial navigation drops when the target type is not yet loaded

In a hot-reload-driven scaffolding flow, the navigation system is brought up before the application's page types have been hot-reloaded into the running assembly. When `INavigator.NavigateRouteAsync` resolves a route whose `RouteMap` references a type that does not yet exist:

1. `ControlNavigator.Show()` returns `null` (no matching view could be created).
2. `ExecuteRequestAsync` logs a warning and returns `Route.Empty`.
3. The navigation request is forgotten.

Even after `NavigationRouteUpdateHandler.UpdateApplication` later rebuilds the route table with the newly added type, the original navigation request is gone — the application sits on an empty region.

## Design

### Fix 1 — Assign `NavigationRouteContext.Resolver` from `NavigationHostedService`

`NavigationHostedService` already runs at startup, has access to both `NavigationRouteContext` and `IRouteResolver` from DI, and was the natural seam for wiring the two without dragging `IRouteResolver` into the factory delegate of `NavigationRouteContext` (or the inverse).

The fix injects `IRouteResolver` into `NavigationHostedService` (made optional for tests that do not configure routing) and, on `StartAsync`, assigns it onto `ctx.Resolver` before calling `Register`:

```csharp
public NavigationHostedService(
    ILogger<NavigationRegion> regionLogger,
    NavigationRouteContext? routeContext = null,
    IRouteResolver? routeResolver = null)
{
    _regionLogger = regionLogger;
    _routeContext = routeContext;
    _routeResolver = routeResolver;
}

public Task StartAsync(CancellationToken cancellationToken)
{
    Region.Logger = _regionLogger;

    if (_routeContext is not null)
    {
        // HostBuilderExtensions.UseNavigation registers IRouteResolver
        // directly with MappedRouteResolver, which overrides the factory
        // delegate in ServiceCollectionExtensions.AddNavigation that used
        // to assign ctx.Resolver. Without this assignment, every HR
        // UpdateApplication invocation bails at "resolver is null" and the
        // route table never rebuilds. Assigning here closes that gap: by
        // the time StartAsync runs, both NavigationRouteContext and
        // IRouteResolver have been resolved from the same scope, so we can
        // link the two without dragging the dependency into the resolver
        // factory.
        if (_routeContext.Resolver is null && _routeResolver is RouteResolver rr)
        {
            _routeContext.Resolver = rr;
        }

        NavigationRouteUpdateHandler.Register(_routeContext);
    }

    _completion.SetResult(true);
    return Task.CompletedTask;
}
```

Both services are singletons in the same container, so they are guaranteed to be the same instances DI will hand out later. The fix is idempotent (`_routeContext.Resolver is null` guard) and degrades cleanly when no `IRouteResolver` is registered (tests, minimal hosts).

### Why not fix it in the DI factory?

`HostBuilderExtensions.UseNavigation` is the documented user-facing extension and its `AddSingleton<IRouteResolver, MappedRouteResolver>()` is intentional — `MappedRouteResolver` extends `RouteResolverDefault` and is required for the ViewModel-to-Route binding path. The factory-delegate version of the registration was a bolt-on for hot-reload that became dead code the moment a `HostBuilderExtensions` consumer registered an override. Moving the assignment into `NavigationHostedService` keeps the resolver-selection contract in `HostBuilderExtensions` while still wiring hot-reload.

### Fix 2 — Pending failed request retry on `ControlNavigator`

`ControlNavigator` now remembers the most recent navigation request whose `Show()` resolved to `null` because the target view type could not be created. `NavigationRouteUpdateHandler` walks the live region tree after a C# or XAML hot-reload and re-issues these requests so an initial navigation that fired before the missing type was hot-reloaded in can self-heal without requiring a full app restart.

```csharp
// New base-class state on ControlNavigator
private NavigationRequest? _pendingFailedRequest;

internal bool HasPendingFailedRequest => _pendingFailedRequest is not null;

protected void RememberPendingFailedRequest(NavigationRequest request)
    => _pendingFailedRequest = request;

protected void ClearPendingFailedRequest()
    => _pendingFailedRequest = null;

internal Task RetryPendingFailedRequestAsync()
{
    var pending = _pendingFailedRequest;
    if (pending is null)
    {
        return Task.CompletedTask;
    }

    _pendingFailedRequest = null;
    return NavigateAsync(pending);
}
```

`ControlNavigator<TControl>.ExecuteRequestAsync` calls `RememberPendingFailedRequest(request)` when `Show()` returns `null`, and `ClearPendingFailedRequest()` on the success branch (so a superseding successful navigation replaces the slot).

Threading: only accessed on the UI dispatcher thread (`ExecuteRequestAsync` runs under `Dispatcher.ExecuteAsync`; the hot-reload retry walk is dispatched via `TryEnqueue`), so no synchronization is needed.

### Fix 3 — Retry walk on hot-reload cascade

`NavigationRouteUpdateHandler.ScheduleCascade` now performs two walks under the dispatcher after a hot-reload delta:

1. `CascadeNewDefaultsFromRoot(root, resolver)` — the existing pass that dispatches any newly-needed `IsDefault` nested route onto the matching child region (short-circuits on first match).
2. `RetryPendingFailedRequestsFromRoot(root)` — a new pass that walks the live region tree **without** short-circuiting and re-invokes `RetryPendingFailedRequestAsync` on every `ControlNavigator` with a pending request.

```csharp
private static void RetryPendingFailedRequestsFromRoot(IRegion region)
{
    var navigator = region.Navigator();
    if (navigator is ControlNavigator { HasPendingFailedRequest: true } asControl)
    {
        _ = asControl.RetryPendingFailedRequestAsync();
    }

    foreach (var child in region.Children.ToArray())
    {
        RetryPendingFailedRequestsFromRoot(child);
    }
}
```

### Fix 4 — FrameView page-subclass handling

`ContentControlNavigator` / `PanelVisiblityNavigator` deliberately return `null` from `Show()` when they wrap a `Page`-subclass view in a `FrameView` — the page itself is the navigation target and the wrapper's `DataContext` is intentionally nulled (see `FrameView` ctor) to prevent ViewModel inheritance. Without special handling, this collided with Fix 2: the wrapper navigator would record a pending retry for a route that is actually being handled by the inner `FrameNavigator`.

The fix detects this case explicitly, awaits the inner `FrameView` load, clears any stale pending slot, and returns `Route.Empty` without calling `InitializeCurrentView` (which would set `DataContext` on the wrapper and break `FrameView`'s null-DataContext invariant):

```csharp
if (mapping?.RenderView is { } renderView && renderView.IsSubclassOf(typeof(Page))
    && CurrentView is FrameView fv)
{
    await fv.EnsureLoaded();
    ClearPendingFailedRequest();
    return Route.Empty;
}
```

### Fix 5 — Diagnostic logging across the hot-reload navigation path

Diagnosing the original `ctx.Resolver is null` issue required adding logs at every static entry point of the route-update handler plus a snapshot of the resolver's top-level route count at construction and after `Rebuild()`. Those logs are retained because they make any future regression in this area immediately diagnosable.

A single `NavRouteHandlerDiag.Log(message)` helper emits to both `Region.Logger` (filterable via the standard `ILogger` pipeline) **and** `System.Diagnostics.Debug.WriteLine` (so a missing `Region.Logger` or `NullLogger` fallback does not erase the trace through a downstream consumer's debug listener forwarder).

Coverage:

- `NavigationRouteUpdateHandler`:
  - Static cctor (confirms the handler class loaded into the ALC at all).
  - `Register` / `Unregister` (with current context count).
  - `UpdateApplication` (CLR-invoked entry — confirms `[MetadataUpdateHandler]` is firing).
  - `RebuildRoutes` (resolver / root-region presence, route-builder invocation, `Rebuild()` invocation, exception captures).
  - `ScheduleCascadeForAllContexts` / `ScheduleCascade` / cascade lambda start-complete / `TryEnqueue` return value.
  - `CascadeWalk` and `RetryWalk` visit logs (per-region navigator type, current route, nested counts, match dispatch).
- `NavigationVisibilityUpdateHandler.CaptureState` / `RestoreState` (per-element, with `HadActiveNavigation` flag).
- `NavigationHostedService.StartAsync` / `StopAsync` (presence of dependencies, resolver assignment).
- `RouteResolver` ctor and `Rebuild()` (`First` path, mappings count, top-level routes with nested counts).
- `ControlNavigator.RetryPendingFailedRequestAsync` (`Information`-level log when a retry is dispatched).

The diagnostic helper uses `Information` level — appropriate for a path that fires only on hot-reload deltas (low frequency) and that has historically been opaque to consumers.

### Fix 6 — Warning suppression for repeat-failed routes

`ControlNavigator<TControl>.ExecuteRequestAsync` previously logged a `Warning` every time `Show()` returned `null`. With Fix 2 in place, hot-reload's polling pattern fires this branch on every hot-reload delta until the missing type finally loads — flooding the bundle with N repeats of the same diagnostic and drowning real misconfigurations.

The fix distinguishes "first time we have seen this route fail" (real signal — might be a typo or missing `RouteMap` registration) from "this route is already in the pending-retry queue" (already known, just waiting for hot-reload to deliver the type) by comparing against the pending slot's current route:

```csharp
var sameRouteRetrying = string.Equals(
    _pendingFailedRequest?.Route.Base,
    request.Route.Base,
    StringComparison.Ordinal);

if (!sameRouteRetrying && Logger.IsEnabled(LogLevel.Warning))
{
    Logger.LogWarningMessage($"Navigation to '{route.Base}' failed: ...");
}
else if (sameRouteRetrying && Logger.IsEnabled(LogLevel.Debug))
{
    Logger.LogDebugMessage($"Navigation to '{route.Base}' failed again: Show() returned null (pending retry — type not yet hot-reloaded in).");
}
```

A bare null check on the pending slot is not enough because a different failing route would have replaced the slot — the comparison is on `Route.Base` so transitions between distinct failing routes still produce a fresh `Warning`.

## Verification

The diagnostic technique that proved the root cause: `[NavRouteHandler]`-prefixed `Information` logs at every static entry point (`Register`, `Unregister`, `UpdateApplication`, `RebuildRoutes`, `ScheduleCascade`, `RetryWalk`) plus a `[NavRouteResolver:ctor/Rebuild]` snapshot of the resolver's top-level route count was decisive.

Before that, the symptom looked identical to "`[MetadataUpdateHandler]` doesn't fire in the inner ALC" and multiple iterations were spent trying to bolt on alternative hot-reload triggers via `[ElementMetadataUpdateHandlerAttribute]`. The lesson for future hot-reload diagnostics:

- Log `_contexts.Count` at the moment the handler fires.
- Log every field of every context that gates a code branch.
- Log the construction site of any singleton the handler depends on.

The factory delegate's silent override would have been obvious if the `[NavRouteResolver:ctor]` log had been there from the start (it showed the resolver was constructed exactly once, with the right state, but `ctx.Resolver` stayed `null` in `UpdateApplication` — telling you immediately that the assignment path was disconnected from the construction path).

## How to apply

When you see a "the handler runs but `ctx.X` is null" pattern in a `[MetadataUpdateHandler]` flow, audit every `AddSingleton<TInterface, TImpl>` registration that happens AFTER the factory delegate registered the same interface. DI's last-wins semantics make the factory delegate silently dead code; the resulting handler-side null check is indistinguishable from "hot-reload doesn't fire" in the logs.

When a navigator wraps a delegate target (e.g. `FrameView` wrapping a `Page`), the wrapper's `Show() == null` is **not** the same as "view type missing" — keep an explicit branch that distinguishes "intentionally null because the inner navigator owns it" from "null because we could not resolve the type at all". Conflating the two breaks both hot-reload retry (wrong navigator records the pending request) and `DataContext` inheritance (`InitializeCurrentView` on the wrapper violates `FrameView`'s null-DataContext invariant).

## Source

This spec captures the lesson recorded in a downstream consumer's lessons log under
"Uno.Extensions.Navigation HR — `NavigationRouteContext.Resolver` Is Never Assigned After `UseNavigation`",
along with the surrounding changes shipped on branch `dev/sb/hr-nav` in commit `307a75dff` (plus the
`ControlNavigator.cs` warning-suppression follow-up still pending commit).
