using System.Collections.Immutable;
using System.Reflection.Metadata;

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

		try
		{
			// Re-invoke the (potentially updated) delegate.
			ctx.RouteBuilder.Invoke(ctx.Views, ctx.Routes);

			// Rebuild the flat lookup tables from the new route data.
			resolver.Rebuild();
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
	}
}
