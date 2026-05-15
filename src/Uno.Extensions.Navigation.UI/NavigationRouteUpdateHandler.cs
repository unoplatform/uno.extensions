using System.Collections.Immutable;
using System.Reflection.Metadata;
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
		ImmutableInterlocked.Update(ref _contexts, list => list.Add(context));
	}

	internal static void Unregister(NavigationRouteContext context)
	{
		ImmutableInterlocked.Update(ref _contexts, list => list.Remove(context));
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
		foreach (var ctx in _contexts)
		{
			RebuildRoutes(ctx);
		}
	}

	private static void RebuildRoutes(NavigationRouteContext ctx)
	{
		var resolver = ctx.Resolver;
		if (resolver is null)
		{
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
		catch
		{
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
		foreach (var ctx in _contexts)
		{
			if (ctx.Resolver is { } resolver && ctx.RootRegion is { } root)
			{
				ScheduleCascade(root, resolver);
			}
		}
	}

	private static void ScheduleCascade(IRegion root, RouteResolver resolver)
	{
		var dispatcher = root.Services?.GetService<IDispatcher>();
		if (dispatcher is not null)
		{
			dispatcher.TryEnqueue(() => CascadeNewDefaultsFromRoot(root, resolver));
		}
	}

	private static void CascadeNewDefaultsFromRoot(IRegion region, RouteResolver resolver)
	{
		var navigator = region.Navigator();
		var currentBase = navigator?.Route?.Base;

		if (navigator is not null &&
			!string.IsNullOrEmpty(currentBase) &&
			resolver.FindByPath(currentBase) is { } routeInfo &&
			routeInfo.Nested?.FirstOrDefault(n => n.IsDefault) is { Path.Length: > 0 } defaultNested &&
			!HasActiveDescendantRoute(region, defaultNested.Path))
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

	private static bool HasActiveDescendantRoute(IRegion region, string targetPath)
	{
		foreach (var child in region.Children)
		{
			if (child.Navigator()?.Route?.Base == targetPath)
			{
				return true;
			}

			if (HasActiveDescendantRoute(child, targetPath))
			{
				return true;
			}
		}

		return false;
	}
}
