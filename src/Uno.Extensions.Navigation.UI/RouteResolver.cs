namespace Uno.Extensions.Navigation;

public class RouteResolver : IRouteResolver
{
	private RouteInfo? First { get; }
	protected IDictionary<string, RouteInfo> Mappings { get; } = new Dictionary<string, RouteInfo>();

	protected ILogger Logger { get; }

	protected IRouteRegistry RouteMaps { get; }
	protected IViewRegistry ViewMaps { get; }

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
		RouteMaps = routes;
		ViewMaps = views;

		var maps = ResolveViewMaps(routes.Items);

		if (maps is not null)
		{
			// Set the first routemap to be either the first with IsDefault, if
			// if none have IsDefault then just return the first
			First = maps.FirstOrDefault(x => x.IsDefault) ?? maps.FirstOrDefault();
			var dependentRoutes = new List<RouteInfo>();
			maps.Flatten().ForEach(route =>
			{
				Mappings[route.Path] = route;
				if (route.IsDependent)
				{
					dependentRoutes.Add(route);
				}
			});
			dependentRoutes.ForEach(route => route.DependsOnRoute = Mappings[route.DependsOn]);
		}


		var messageDialogRoute = new RouteInfo(
			Path: RouteConstants.MessageDialogUri,
			View: () => typeof(MessageDialog),
			ResultData: typeof(MessageDialog)
		);

		// Make sure the message dialog is the last route to be listed
		Mappings[messageDialogRoute.Path] = messageDialogRoute;
	}

	protected RouteInfo[] ResolveViewMaps(IEnumerable<RouteMap> maps)
	{
		if (!(maps?.Any() ?? false))
		{
			return Array.Empty<RouteInfo>();
		}
		return (
				from drm in maps
				select FromRouteMap(drm)
				).ToArray();
	}

	protected virtual RouteInfo FromRouteMap(RouteMap drm)
	{
		var viewFunc = (drm.View?.View is not null) ?
										() => drm.View.View :
										drm.View?.ViewSelector;
		return AssignParentRouteInfo(new RouteInfo(
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
			IsDialogViewType: () =>
			{
				return IsDialogViewType(viewFunc?.Invoke());
			},
			Nested: ResolveViewMaps(drm.Nested)));
	}

	protected static RouteInfo AssignParentRouteInfo(RouteInfo info)
	{
		foreach (var nestedInfo in info.Nested)
		{
			nestedInfo.Parent = info;
		}
		return info;
	}

	protected static bool IsDialogViewType(Type? viewType = null)
	{
		if(viewType is null)
		{
			return false;;
		}

		return viewType == typeof(MessageDialog) ||
			viewType == typeof(ContentDialog) ||
			viewType.IsSubclassOf(typeof(ContentDialog)) ||
			viewType == typeof(Flyout) ||
			viewType.IsSubclassOf(typeof(Flyout));
	}
	public virtual RouteInfo? FindByPath(string? path)
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

	public virtual RouteInfo[] FindByViewModel(Type? viewModelType)
	{
		return FindRouteByType(viewModelType, map => map.ViewModel);
	}

	public virtual RouteInfo[] FindByView(Type? viewType)
	{
		return FindRouteByType(viewType, map => map.RenderView);
	}

	public RouteInfo[] FindByData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.Data);
	}

	public RouteInfo[] FindByResultData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.ResultData);
	}

	private RouteInfo[] FindRouteByType(Type? typeToFind, Func<RouteInfo, Type?> mapType)
	{
		return FindByInheritedTypes(Mappings, typeToFind, mapType);
	}

	private TMap[] FindByInheritedTypes<TMap>(IDictionary<string, TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
	{
		return mappings.Values.FindByInheritedTypes(typeToFind, mapType);
	}
}
