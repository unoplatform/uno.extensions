using System.Collections.Immutable;
using System.Reflection.Metadata;
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.Regions;

[assembly: MetadataUpdateHandler(typeof(Uno.Extensions.Navigation.UI.NavigationRouteUpdateHandler))]

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Holds the references needed to refresh navigation routes after a C# hot-reload.
/// One instance exists per navigation engine (<see cref="ServiceCollectionExtensions.AddNavigation"/>).
/// Registered/unregistered by <see cref="NavigationHostedService"/> to avoid leaking.
/// </summary>
internal sealed class NavigationRouteContext
{
	public required Action<IViewRegistry, IRouteRegistry> RouteBuilder { get; init; }
	public required IViewRegistry Views { get; init; }
	public required IRouteRegistry Routes { get; init; }
	public RouteResolver? Resolver { get; set; }

	/// <summary>
	/// The root <see cref="IRegion"/> of the live navigator tree, populated by
	/// <see cref="NavigationRegion.InitializeRootRegion"/>. Used by the C# hot-reload
	/// route refresh to walk the live region tree and re-cascade the current route
	/// when newly registered nested IsDefault routes become available.
	/// </summary>
	public IRegion? RootRegion { get; set; }
}

/// <summary>
/// C# hot-reload handler that re-invokes the <c>viewRouteBuilder</c> delegate
/// registered via <see cref="ServiceCollectionExtensions.AddNavigation"/> and
/// rebuilds <see cref="RouteResolver"/> mappings so that newly added routes
/// become navigable without restarting the app.
/// </summary>
internal static class NavigationRouteUpdateHandler
{
	private static ImmutableList<NavigationRouteContext> _contexts = ImmutableList<NavigationRouteContext>.Empty;

	internal static void Register(NavigationRouteContext context)
	{
		ImmutableInterlocked.Update(ref _contexts, l => l.Add(context));
	}

	internal static void Unregister(NavigationRouteContext context)
	{
		ImmutableInterlocked.Update(ref _contexts, l => l.Remove(context));
	}

	/// <summary>
	/// Called by the .NET hot-reload infrastructure after updated types have
	/// been applied. Re-invokes each registered route builder delegate (whose
	/// method body may have been replaced) and rebuilds the resolver's lookup
	/// tables.
	/// </summary>
#pragma warning disable IDE0051 // Remove unused private members — called by the runtime via MetadataUpdateHandler
	static void UpdateApplication(Type[]? updatedTypes)
#pragma warning restore IDE0051
	{
		if (Region.Logger.IsEnabled(LogLevel.Information))
		{
			Region.Logger.LogInformationMessage($"Hot-reload UpdateApplication received {updatedTypes?.Length ?? 0} updated type(s); refreshing {_contexts.Count} navigation context(s)");
		}

		// Snapshot under iteration: _contexts is ImmutableList, but a per-context
		// throw must not abort the loop — one broken context should not deny HR
		// recovery to the others. The .NET HR pipeline also has no resilience to
		// exceptions thrown back from a MetadataUpdateHandler, so swallow at this
		// boundary and log instead.
		foreach (var ctx in _contexts)
		{
			try
			{
				RebuildRoutes(ctx, updatedTypes);
			}
			catch (Exception ex)
			{
				if (Region.Logger.IsEnabled(LogLevel.Error))
				{
					Region.Logger.LogErrorMessage($"Hot-reload RebuildRoutes failed for a navigation context: {ex.GetType().Name}: {ex.Message}. Other contexts will still be processed.");
				}
			}
		}
	}

	private static void RebuildRoutes(NavigationRouteContext ctx, Type[]? updatedTypes)
	{
		var resolver = ctx.Resolver;
		if (resolver is null)
		{
			// Either the navigation engine was registered without an IRouteResolver
			// (no UseNavigation builder), or the resolver-assignment path was broken
			// (see NavigationHostedService.StartAsync for the historical bug where
			// HostBuilderExtensions.UseNavigation's IRouteResolver override silently
			// bypassed the factory delegate that assigned ctx.Resolver). Warning so
			// future regressions of either condition surface immediately rather than
			// degrading to "HR doesn't update routes" with no log trail.
			if (Region.Logger.IsEnabled(LogLevel.Warning))
			{
				Region.Logger.LogWarningMessage("Hot-reload route rebuild skipped: NavigationRouteContext.Resolver is null. Route table will not be refreshed.");
			}
			return;
		}

		// Snapshot current state so we can restore on failure.
		ViewMap[]? previousViews = null;
		RouteMap[]? previousRoutes = null;

		if (ctx.Views is Registry<ViewMap> viewRegistry)
		{
			previousViews = viewRegistry.Items.ToArray();
			viewRegistry.Clear();
		}

		if (ctx.Routes is Registry<RouteMap> routeRegistry)
		{
			previousRoutes = routeRegistry.Items.ToArray();
			routeRegistry.Clear();
		}

		var rebuiltSuccessfully = false;

		try
		{
			// Re-invoke the (potentially updated) delegate.
			ctx.RouteBuilder.Invoke(ctx.Views, ctx.Routes);

			// Rebuild the flat lookup tables from the new route data.
			resolver.Rebuild();
			rebuiltSuccessfully = true;
		}
		catch (Exception ex)
		{
			// The delegate may have been replaced and the old version could
			// throw (e.g. HotReloadException). Restore the previous state
			// so the navigation engine remains functional. Log at Warning —
			// silently swallowing this is what made the original symptom of
			// "navigation suddenly stopped finding routes after an HR cycle"
			// unattributable in feedback bundles.
			if (Region.Logger.IsEnabled(LogLevel.Warning))
			{
				Region.Logger.LogWarningMessage($"Hot-reload route rebuild threw {ex.GetType().Name}: {ex.Message}. Previous registry state restored; navigation continues with the pre-HR route table.");
			}

			if (previousViews is not null && ctx.Views is Registry<ViewMap> vr)
			{
				vr.Clear();
				vr.Register(previousViews);
			}

			if (previousRoutes is not null && ctx.Routes is Registry<RouteMap> rr)
			{
				rr.Clear();
				rr.Register(previousRoutes);
			}

			resolver.Rebuild();
		}

		// Newly registered nested IsDefault routes don't auto-navigate on their
		// own — the existing navigator tree already finished its initial cascade
		// before the routes existed. Walk the live region tree and, for each
		// navigator whose RouteInfo now exposes an IsDefault nested route that
		// isn't yet populated as a child region's active route, dispatch a
		// navigation request into the matching child region.
		//
		// The walk is deferred onto the dispatcher: UpdateApplication runs
		// synchronously on the UI thread under the .NET hot-reload pipeline,
		// and the host's XAML HR processor may immediately follow with more
		// deltas. Calling NavigateAsync directly here returns a task whose
		// continuations are scheduled on the UI dispatcher — and that
		// dispatcher is starved while the HR pipeline keeps the thread busy.
		// TryEnqueue gives the HR pipeline time to drain before the navigation
		// runs and lets its async continuations make progress.
		if (rebuiltSuccessfully && ctx.RootRegion is { } root)
		{
			// Only cascade if at least one updated type is registered as a view or
			// view-model in the (just-rebuilt) route table.  A XAML-only HR cycle
			// (e.g. a Text change on a page with x:Uid) produces updatedTypes that
			// contain only the page's generated partial class, which is not a
			// navigation-registered type.  Cascading unconditionally on every such
			// cycle re-mounts the page and lets x:Uid overwrite the edited value,
			// making the edit appear to revert.  See studio.live#2293.
			if (ShouldCascadeForUpdatedTypes(updatedTypes, resolver))
			{
				ScheduleCascade(root, resolver);
			}
			else if (Region.Logger.IsEnabled(LogLevel.Debug))
			{
				Region.Logger.LogDebugMessage("Hot-reload cascade skipped: none of the updated types are registered navigation routes.");
			}
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> when the hot-reload update should cascade
	/// through the live region tree.
	/// </summary>
	internal static bool ShouldCascadeForUpdatedTypes(Type[]? updatedTypes, RouteResolver resolver)
		=> updatedTypes is null || HasRouteRegisteredType(updatedTypes, resolver);

	/// <summary>
	/// Returns <see langword="true"/> if at least one type in <paramref name="types"/>
	/// is registered in the resolver as a view or view-model.
	/// </summary>
	private static bool HasRouteRegisteredType(Type[] types, RouteResolver resolver)
	{
		foreach (var t in types)
		{
			if (resolver.FindByView(t, navigator: null) is not null)
			{
				return true;
			}

			// IL2072: t comes from a Type[] whose elements don't carry DynamicallyAccessedMembers
			// annotations, but FindByViewModel uses them only for lookup — no construction or
			// property access is performed. The call is safe for this read-only, type-identity check.
#pragma warning disable IL2072
			if (resolver.FindByViewModel(t, navigator: null) is not null)
#pragma warning restore IL2072
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Defers a cascade walk on the dispatcher. Public entry point for callers
	/// outside the resolver-rebuild path (e.g. XAML hot-reload, which adds new
	/// active navigation structure under an already-rooted region tree but does
	/// not invoke this class's <see cref="UpdateApplication"/> hook).
	/// </summary>
	internal static void ScheduleCascadeForAllContexts()
	{
		foreach (var ctx in _contexts)
		{
			if (ctx.Resolver is { } resolver && ctx.RootRegion is { } root)
			{
				ScheduleCascade(root, resolver);
			}
		}
	}

	/// <summary>
	/// Like <see cref="ScheduleCascadeForAllContexts"/> but skips the cascade
	/// when <paramref name="updatedTypes"/> contains no navigation-registered type.
	/// Pass <see langword="null"/> to unconditionally cascade (same behaviour as
	/// <see cref="ScheduleCascadeForAllContexts"/>).
	/// </summary>
	internal static void ScheduleCascadeForAllContextsIfRouteRelevant(Type[]? updatedTypes)
	{
		foreach (var ctx in _contexts)
		{
			if (ctx.Resolver is { } resolver && ctx.RootRegion is { } root)
			{
				if (ShouldCascadeForUpdatedTypes(updatedTypes, resolver))
				{
					ScheduleCascade(root, resolver);
				}
				else if (Region.Logger.IsEnabled(LogLevel.Debug))
				{
					Region.Logger.LogDebugMessage("Hot-reload visibility-restore cascade skipped: none of the updated types are registered navigation routes.");
				}
			}
		}
	}

	private static void ScheduleCascade(IRegion root, RouteResolver resolver)
	{
		var dispatcher = root.Services?.GetService<IDispatcher>();
		if (dispatcher is null)
		{
			if (Region.Logger.IsEnabled(LogLevel.Warning))
			{
				Region.Logger.LogWarningMessage("Cascade walk skipped: no IDispatcher available on the root region's services. IsDefault routes added by hot-reload will not auto-navigate.");
			}
			return;
		}

		if (Region.Logger.IsEnabled(LogLevel.Debug))
		{
			Region.Logger.LogDebugMessage($"Scheduling cascade walk on dispatcher (root region: '{root.Name ?? string.Empty}', children: {root.Children.Count})");
		}

		dispatcher.TryEnqueue(() =>
		{
			CascadeNewDefaultsFromRoot(root, resolver);

			// Re-issue any navigation requests that were dropped earlier because
			// their target view type was not yet present in the assembly. The
			// resolver has now been rebuilt with the latest route table, so a
			// previously-failed mapping may resolve successfully on retry. Walks
			// the live region tree without short-circuiting (unlike the
			// IsDefault cascade above) so every navigator with a pending request
			// is given a chance to recover.
			RetryPendingFailedRequestsFromRoot(root);
		});
	}

	private static void RetryPendingFailedRequestsFromRoot(IRegion region)
	{
		if (region.Navigator() is ControlNavigator { HasPendingFailedRequest: true } cn)
		{
			SafeFireAndForget(cn.RetryPendingFailedRequestAsync(), "retry pending failed navigation");
		}

		foreach (var child in region.Children.ToArray())
		{
			RetryPendingFailedRequestsFromRoot(child);
		}
	}

	// Awaits a fire-and-forget navigation task on the dispatcher and swallows
	// any throw with a Warning. The HR cascade dispatches navigation work
	// through TryEnqueue, so the caller cannot observe exceptions from the
	// returned Task. An unhandled throw becomes an unobserved TaskException —
	// silent on WASM, a TaskScheduler.UnobservedTaskException on desktop.
	// AGENTS.md §10: every fire-and-forget MUST have a try/catch.
	private static async void SafeFireAndForget(Task task, string operation)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException)
		{
			// Expected when the dispatcher is torn down mid-cascade (e.g. host
			// shutdown immediately after HR delivery). Silent — not an error.
		}
		catch (Exception ex)
		{
			if (Region.Logger.IsEnabled(LogLevel.Warning))
			{
				Region.Logger.LogWarningMessage($"Hot-reload {operation} threw {ex.GetType().Name}: {ex.Message}. Region tree state is unchanged; subsequent HR deltas may recover.");
			}
		}
	}

	private static void CascadeNewDefaultsFromRoot(IRegion region, RouteResolver resolver)
	{
		var navigator = region.Navigator();
		var currentBase = navigator?.Route?.Base;

		if (navigator is not null &&
			!string.IsNullOrEmpty(currentBase) &&
			resolver.FindByPath(currentBase) is { } routeInfo &&
			routeInfo.Nested?.FirstOrDefault(n => n.IsDefault) is { Path.Length: > 0 } defaultNested)
		{
			// Before dispatching, check whether a descendant region already has an
			// active route that matches one of the nested routes — if so, preserve
			// that selection rather than clobbering it back to the IsDefault. The
			// helper returns the matching descendant info so we can log WHICH
			// region with WHICH route caused the short-circuit; without that, a
			// future investigator chasing "why didn't IsDefault cascade fire" has
			// no way to attribute the decision to a specific competing route.
			var conflict = FindActiveDescendantNestedRoute(region, routeInfo.Nested);
			if (conflict is null)
			{
				// Issue navigation on the matched navigator's CHILD region rather
				// than the navigator itself. Targeting the matched navigator (e.g.
				// the Frame) would tear down and rebuild its child region tree
				// (Region.Children.Clear() + ReassignRegionParent) — which races
				// with the freshly-added XAML-HR child controls (TabBar, content
				// grid) on the same page. Going one level deeper lets the page
				// region's composite navigator dispatch the IsDefault path to its
				// own children (e.g. TabBarNavigator + PanelVisiblityNavigator)
				// without disturbing the parent frame.
				var pageRegion = region.Children.FirstOrDefault();
				var pageNavigator = pageRegion?.Navigator();
				if (pageNavigator is not null)
				{
					if (Region.Logger.IsEnabled(LogLevel.Debug))
					{
						Region.Logger.LogDebugMessage($"Cascade dispatching IsDefault '{defaultNested.Path}' to first child '{pageRegion?.Name ?? string.Empty}' of region '{region.Name ?? string.Empty}'");
					}
					var route = new Route(Qualifiers.None, Base: defaultNested.Path);
					var request = new NavigationRequest(pageNavigator, route.AsInternal());
					SafeFireAndForget(pageNavigator.NavigateAsync(request), $"IsDefault cascade to '{defaultNested.Path}'");
					return;
				}
			}
			else if (Region.Logger.IsEnabled(LogLevel.Debug))
			{
				Region.Logger.LogDebugMessage($"Cascade skip on region '{region.Name ?? string.Empty}': IsDefault '{defaultNested.Path}' suppressed because descendant region '{conflict.Value.RegionName}' already has active route '{conflict.Value.ActiveRouteBase}' (matches nested path of '{currentBase}')");
			}
		}

		foreach (var child in region.Children.ToArray())
		{
			CascadeNewDefaultsFromRoot(child, resolver);
		}
	}

	/// <summary>
	/// Walks descendant regions looking for one whose active route matches one of
	/// the supplied nested route paths. Returns the first match (region name +
	/// active route base) or <c>null</c> if none. The caller uses the returned
	/// region name + route to log WHICH descendant caused a cascade-skip decision;
	/// a bool-only result would leave future investigators with no way to attribute
	/// "IsDefault cascade didn't fire" to a specific competing route.
	/// </summary>
	private static (string RegionName, string ActiveRouteBase)? FindActiveDescendantNestedRoute(IRegion region, RouteInfo[] nestedRoutes)
	{
		foreach (var child in region.Children)
		{
			var childBase = child.Navigator()?.Route?.Base;
			if (!string.IsNullOrEmpty(childBase) &&
				nestedRoutes.Any(n => string.Equals(n.Path, childBase, StringComparison.Ordinal)))
			{
				return (child.Name ?? string.Empty, childBase);
			}

			if (FindActiveDescendantNestedRoute(child, nestedRoutes) is { } deeper)
			{
				return deeper;
			}
		}

		return null;
	}
}
