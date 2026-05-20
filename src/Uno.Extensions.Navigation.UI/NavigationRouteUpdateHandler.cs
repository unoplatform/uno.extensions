using System.Collections.Immutable;
using System.Reflection.Metadata;
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.Regions;

[assembly: MetadataUpdateHandler(typeof(Uno.Extensions.Navigation.UI.NavigationRouteUpdateHandler))]

namespace Uno.Extensions.Navigation.UI;

// Diagnostic note: we emit BOTH via Region.Logger (which can be muted by
// log-level filters) AND via System.Diagnostics.Debug.WriteLine (which bubbles
// up through DebugListenerForwarder into the host's structured log so a
// missing Region.Logger or NullLogger fallback does not erase the trace).
internal static class NavRouteHandlerDiag
{
	internal static void Log(string message)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine($"[NavRouteHandler] {message}");
		}
		catch
		{
		}

		var logger = Region.Logger;
		if (logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformationMessage($"[NavRouteHandler] {message}");
		}
	}
}

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

	static NavigationRouteUpdateHandler()
	{
		// Static cctor fires on first access of any member. If you see this in
		// the logs at all, the assembly's type-level wiring is alive in this
		// ALC; if you NEVER see it but you do see ContentControlNavigator logs,
		// then the [MetadataUpdateHandler] attribute is being ignored because
		// the assembly was loaded into a context the HR pipeline doesn't scan.
		NavRouteHandlerDiag.Log("static cctor running (handler class loaded into this ALC)");
	}

	internal static void Register(NavigationRouteContext context)
	{
		ImmutableInterlocked.Update(ref _contexts, l => l.Add(context));
		NavRouteHandlerDiag.Log($"Register called (total contexts now {_contexts.Count})");
	}

	internal static void Unregister(NavigationRouteContext context)
	{
		ImmutableInterlocked.Update(ref _contexts, l => l.Remove(context));
		NavRouteHandlerDiag.Log($"Unregister called (total contexts now {_contexts.Count})");
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
		NavRouteHandlerDiag.Log($"UpdateApplication CALLED BY CLR with {updatedTypes?.Length ?? 0} type(s); _contexts.Count = {_contexts.Count}");

		foreach (var ctx in _contexts)
		{
			RebuildRoutes(ctx);
		}
	}

	private static void RebuildRoutes(NavigationRouteContext ctx)
	{
		NavRouteHandlerDiag.Log($"RebuildRoutes called (resolver={ctx.Resolver is not null}, rootRegion={ctx.RootRegion is not null})");

		var resolver = ctx.Resolver;
		if (resolver is null)
		{
			NavRouteHandlerDiag.Log("RebuildRoutes: resolver is null, returning");
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
			NavRouteHandlerDiag.Log("RebuildRoutes: re-invoking RouteBuilder delegate");
			ctx.RouteBuilder.Invoke(ctx.Views, ctx.Routes);

			// Rebuild the flat lookup tables from the new route data.
			NavRouteHandlerDiag.Log("RebuildRoutes: calling resolver.Rebuild()");
			resolver.Rebuild();
			rebuiltSuccessfully = true;
		}
		catch (Exception ex)
		{
			NavRouteHandlerDiag.Log($"RebuildRoutes: RouteBuilder/Rebuild threw {ex.GetType().Name}: {ex.Message} — restoring previous registry state");
			// The delegate may have been replaced and the old version could
			// throw (e.g. HotReloadException). Restore the previous state
			// so the navigation engine remains functional.
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
			ScheduleCascade(root, resolver);
		}
	}

	/// <summary>
	/// Defers a cascade walk on the dispatcher. Public entry point for callers
	/// outside the resolver-rebuild path (e.g. XAML hot-reload, which adds new
	/// active navigation structure under an already-rooted region tree but does
	/// not invoke this class's <see cref="UpdateApplication"/> hook).
	/// </summary>
	internal static void ScheduleCascadeForAllContexts()
	{
		NavRouteHandlerDiag.Log($"ScheduleCascadeForAllContexts called (_contexts.Count = {_contexts.Count})");
		foreach (var ctx in _contexts)
		{
			if (ctx.Resolver is { } resolver && ctx.RootRegion is { } root)
			{
				ScheduleCascade(root, resolver);
			}
			else
			{
				NavRouteHandlerDiag.Log($"ScheduleCascadeForAllContexts: skipping a context (resolver={ctx.Resolver is not null}, rootRegion={ctx.RootRegion is not null})");
			}
		}
	}

	private static void ScheduleCascade(IRegion root, RouteResolver resolver)
	{
		var dispatcher = root.Services?.GetService<IDispatcher>();
		NavRouteHandlerDiag.Log($"ScheduleCascade called (root='{root.Name ?? string.Empty}', dispatcher={dispatcher is not null}, root.Children={root.Children.Count})");
		if (dispatcher is not null)
		{
			var enqueued = dispatcher.TryEnqueue(() =>
			{
				NavRouteHandlerDiag.Log("ScheduleCascade lambda STARTING on dispatcher");

				CascadeNewDefaultsFromRoot(root, resolver);

				// Re-issue any navigation requests that were dropped earlier because
				// their target view type was not yet present in the assembly. The
				// resolver has now been rebuilt with the latest route table, so a
				// previously-failed mapping may resolve successfully on retry. Walks
				// the live region tree without short-circuiting (unlike the
				// IsDefault cascade above) so every navigator with a pending request
				// is given a chance to recover.
				RetryPendingFailedRequestsFromRoot(root);

				NavRouteHandlerDiag.Log("ScheduleCascade lambda COMPLETED");
			});
			NavRouteHandlerDiag.Log($"ScheduleCascade dispatcher.TryEnqueue returned {enqueued}");
		}
	}

	private static void RetryPendingFailedRequestsFromRoot(IRegion region)
	{
		var navigator = region.Navigator();
		var asControl = navigator as ControlNavigator;
		var hasPending = asControl?.HasPendingFailedRequest ?? false;
		NavRouteHandlerDiag.Log($"RetryWalk visiting '{region.Name ?? string.Empty}' (navigator={navigator?.GetType().Name ?? "<null>"}, isControl={asControl is not null}, hasPending={hasPending}, children={region.Children.Count})");

		if (asControl is { HasPendingFailedRequest: true })
		{
			_ = asControl.RetryPendingFailedRequestAsync();
		}

		foreach (var child in region.Children.ToArray())
		{
			RetryPendingFailedRequestsFromRoot(child);
		}
	}

	private static void CascadeNewDefaultsFromRoot(IRegion region, RouteResolver resolver)
	{
		var navigator = region.Navigator();
		var currentBase = navigator?.Route?.Base;
		var routeInfoForCurrent = (currentBase is { Length: > 0 }) ? resolver.FindByPath(currentBase) : null;
		var nestedCount = routeInfoForCurrent?.Nested?.Length ?? 0;
		var isDefaultNested = routeInfoForCurrent?.Nested?.FirstOrDefault(n => n.IsDefault)?.Path ?? "<none>";
		NavRouteHandlerDiag.Log($"CascadeWalk visiting '{region.Name ?? string.Empty}' (navigator={navigator?.GetType().Name ?? "<null>"}, currentBase='{currentBase ?? string.Empty}', nestedCount={nestedCount}, isDefaultNested='{isDefaultNested}', children={region.Children.Count})");

		if (navigator is not null &&
			!string.IsNullOrEmpty(currentBase) &&
			resolver.FindByPath(currentBase) is { } routeInfo &&
			routeInfo.Nested?.FirstOrDefault(n => n.IsDefault) is { Path.Length: > 0 } defaultNested &&
			!HasActiveDescendantNestedRoute(region, routeInfo.Nested))
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
			NavRouteHandlerDiag.Log($"CascadeWalk MATCH on '{region.Name ?? string.Empty}': dispatching IsDefault '{defaultNested.Path}' to first child '{pageRegion?.Name ?? "<null>"}' (pageNavigator={pageNavigator?.GetType().Name ?? "<null>"})");
			if (pageNavigator is not null)
			{
				var route = new Route(Qualifiers.None, Base: defaultNested.Path);
				var request = new NavigationRequest(pageNavigator, route.AsInternal());
				_ = pageNavigator.NavigateAsync(request);
				return;
			}
		}

		foreach (var child in region.Children.ToArray())
		{
			CascadeNewDefaultsFromRoot(child, resolver);
		}
	}

	/// <summary>
	/// Returns true if any descendant region already has an active route whose Base
	/// matches one of the navigator's nested route paths. Used to decide whether to
	/// cascade an IsDefault nested route: if the user (or a prior cascade) has already
	/// navigated to any sibling nested route, the current selection must be preserved
	/// — re-asserting IsDefault here would clobber it (e.g. snapping a TabBar back
	/// from TabTwo to TabOne after a C# hot-reload).
	/// </summary>
	private static bool HasActiveDescendantNestedRoute(IRegion region, RouteInfo[] nestedRoutes)
	{
		foreach (var child in region.Children)
		{
			var childBase = child.Navigator()?.Route?.Base;
			if (!string.IsNullOrEmpty(childBase) &&
				nestedRoutes.Any(n => string.Equals(n.Path, childBase, StringComparison.Ordinal)))
			{
				return true;
			}

			if (HasActiveDescendantNestedRoute(child, nestedRoutes))
			{
				return true;
			}
		}

		return false;
	}
}
