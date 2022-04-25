namespace Uno.Extensions.Navigation;

public class RouteResolver : IRouteResolver
{
	private InternalRouteMap? First { get; }
	protected IDictionary<string, InternalRouteMap> Mappings { get; } = new Dictionary<string, InternalRouteMap>();

	protected ILogger Logger { get; }

	public RouteResolver(
		ILogger<RouteResolver> logger,
		IRouteRegistry routes,
		IViewRegistry views
		) : this((ILogger)logger, routes, views)
	{
	}

	protected RouteResolver(ILogger logger, IRouteRegistry routes, IViewRegistry views)
	{
		Logger = logger;

		var maps = ResolveViewMaps(routes.Items, views);

		if (maps is not null)
		{
			// Set the first routemap to be either the first with IsDefault, if
			// if none have IsDefault then just return the first
			First = maps.FirstOrDefault(x => x.IsDefault) ?? maps.FirstOrDefault();
			maps.Flatten().ForEach(route => Mappings[route.Path] = route);
		}


		var messageDialogRoute = new InternalRouteMap(
			Path: RouteConstants.MessageDialogUri,
			View: () => typeof(MessageDialog),
			ResultData: typeof(MessageDialog)
		);

		// Make sure the message dialog is the last route to be listed
		Mappings[messageDialogRoute.Path] = messageDialogRoute;
	}

	private InternalRouteMap[] ResolveViewMaps(IEnumerable<RouteMap> maps, IViewRegistry views)
	{
		if (!(maps?.Any() ?? false))
		{
			return Array.Empty<InternalRouteMap>();
		}
		return (
				from drm in maps
				let viewFunc = (drm.View?.View is not null) ?
										() => drm.View.View :
										drm.View?.DynamicView
				select new InternalRouteMap(
					Path: drm.Path,
					View: viewFunc,
					ViewAttributes: drm.View?.ViewAttributes,
					ViewModel: drm.View?.ViewModel,
					Data: drm.View?.Data?.Data,
					ToQuery: drm.View?.Data?.UntypedToQuery,
					FromQuery: drm.View?.Data?.UntypedFromQuery,
					ResultData: drm.View?.ResultData,
					IsDefault: drm.IsDefault,
					DependsOn: drm.DependsOn,
					Init: drm.Init,
					Nested: ResolveViewMaps(drm.Nested, views))
				).ToArray();
	}



	public InternalRouteMap? Parent(InternalRouteMap? routeMap)
	{
		if (routeMap is null)
		{
			return default;
		}

		return Mappings
			.Where(
				x => x.Value.Nested is not null &&
					x.Value.Nested.Contains(routeMap))
			.Select(x => x.Value)
			.FirstOrDefault();
	}


	public InternalRouteMap? Find(Route? route) =>
		route is not null ?
			FindByPath(route.Base) ??
				(
					(route.Data?.TryGetValue(String.Empty, out var data) ?? false) ?
						FindByData(data.GetType()) :
						default
				) :
			First;

	public virtual InternalRouteMap? FindByPath(string? path)
	{
		if (path is null)
		{
			return null;
		}

		if (path == string.Empty)
		{
			return First;
		}

		path = path.ExtractBase(out var _, out var _);

		return Mappings.TryGetValue(path!, out var map) ? map : default;
	}

	public virtual InternalRouteMap? FindByViewModel(Type? viewModelType)
	{
		return FindRouteByType(viewModelType, map => map.ViewModel);
	}

	public virtual InternalRouteMap? FindByView(Type? viewType)
	{
		return FindRouteByType(viewType, map => map.RenderView);
	}

	public InternalRouteMap? FindByData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.Data);
	}

	public InternalRouteMap? FindByResultData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.ResultData);
	}

	private InternalRouteMap? FindRouteByType(Type? typeToFind, Func<InternalRouteMap, Type?> mapType)
	{
		return FindByInheritedTypes(Mappings, typeToFind, mapType);
	}

	private TMap? FindByInheritedTypes<TMap>(IDictionary<string, TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
	{
		return FindByInheritedTypes(mappings.Values, typeToFind, mapType);
	}

	private TMap? FindByInheritedTypes<TMap>(IEnumerable<TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
	{
		if (typeToFind is null)
		{
			return default;
		}

		// Handle the non-reflection check first
		var map = (from m in mappings
				   where mapType(m) == typeToFind
				   select m)
				   .FirstOrDefault();
		if (map is not null)
		{
			return map;
		}

		return (from baseType in typeToFind.GetBaseTypes()
				from m in mappings
				where mapType(m) == baseType
				select m)
				   .FirstOrDefault();
	}
}
