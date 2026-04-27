using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Uno.Extensions.Navigation.UI.NavigationRouteUpdateHandler))]

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// C# hot-reload handler that re-invokes the <c>viewRouteBuilder</c> delegate
/// registered via <see cref="ServiceCollectionExtensions.AddNavigation"/> and
/// rebuilds <see cref="RouteResolver"/> mappings so that newly added routes
/// become navigable without restarting the app.
/// </summary>
internal static class NavigationRouteUpdateHandler
{
	internal static Action<IViewRegistry, IRouteRegistry>? RouteBuilder { get; set; }
	internal static IViewRegistry? Views { get; set; }
	internal static IRouteRegistry? Routes { get; set; }
	internal static RouteResolver? Resolver { get; set; }

	/// <summary>
	/// Called by the .NET hot-reload infrastructure after updated types have
	/// been applied. Re-invokes the route builder delegate (whose method body
	/// may have been replaced) and rebuilds the resolver's lookup tables.
	/// </summary>
#pragma warning disable IDE0051 // Remove unused private members — called by the runtime via MetadataUpdateHandler
	static void UpdateApplication(Type[]? updatedTypes)
#pragma warning restore IDE0051
	{
		var builder = RouteBuilder;
		var views = Views;
		var routes = Routes;
		var resolver = Resolver;

		if (builder is null || views is null || routes is null || resolver is null)
		{
			return;
		}

		// Clear existing registrations so re-invocation starts fresh.
		if (views is Registry<ViewMap> viewRegistry)
		{
			viewRegistry.Clear();
		}

		if (routes is Registry<RouteMap> routeRegistry)
		{
			routeRegistry.Clear();
		}

		// Re-invoke the (potentially updated) delegate.
		builder.Invoke(views, routes);

		// Rebuild the flat lookup tables from the new route data.
		resolver.Rebuild();
	}
}
