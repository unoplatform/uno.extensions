namespace Uno.Extensions.Navigation;

public class RouteResolver : IRouteResolver
{
	private RouteMap? First { get; }
	protected IDictionary<string, RouteMap> Mappings { get; } = new Dictionary<string, RouteMap>();

	protected ILogger Logger { get; }

	protected RouteResolver(ILogger logger, IRouteRegistry routes, IViewResolver viewResolver)
	{
		Logger = logger;

		var maps = ResolveViewMaps(routes.Items, viewResolver);

		if (maps is not null)
		{
			// Set the first routemap to be either the first with IsDefault, if
			// if none have IsDefault then just return the first
			First = maps.FirstOrDefault(x => x.IsDefault) ?? maps.FirstOrDefault();
			maps.Flatten().ForEach(route => Mappings[route.Path] = route);
		}


		var messageDialogRoute = new RouteMap(
			Path: RouteConstants.MessageDialogUri,
			View: new ViewMap<MessageDialog>(ResultData: typeof(MessageDialog))
		);

		// Make sure the message dialog is the last route to be listed
		Mappings[messageDialogRoute.Path] = messageDialogRoute;
	}

	private RouteMap[] ResolveViewMaps(IEnumerable<RouteMap> maps, IViewResolver viewResolver)
	{
		if(!(maps?.Any()??false))
		{
			return new RouteMap[] {};
		}
		return (from drm in maps
				let rm = drm.DynamicView is not null ? new RouteMap(drm.Path, drm.DynamicView?.Invoke(viewResolver), drm.IsDefault, drm.DependsOn, drm.Init,Nested: ResolveViewMaps(drm.Nested, viewResolver)) : drm
				select rm).ToArray();
	}

	public RouteResolver(
		ILogger<RouteResolver> logger,
		IRouteRegistry routes,
		IViewResolver viewResolver
		) : this((ILogger)logger, routes, viewResolver)
	{
	}

	public RouteMap? Parent(RouteMap? routeMap)
	{
		if(routeMap is null)
		{
			return default;
		}

		return Mappings
			.Where(
				x => x.Value.Nested is not null &&
					x.Value.Nested.Contains(routeMap))
			.Select(x=>x.Value)
			.FirstOrDefault();
	}


	public RouteMap? Find(Route? route) =>
		route is not null ?
			FindByPath(route.Base) ??
				(
					(route.Data?.TryGetValue(String.Empty, out var data) ?? false) ?
						FindByData(data.GetType()) :
						default
				) :
			First;

	public virtual RouteMap? FindByPath(string? path)
	{
		if (path is null)
		{
			return null;
		}

		if(path== string.Empty)
		{
			return First;
		}

		path = path.ExtractBase(out var nextQualifier, out var nextPath);

		return Mappings.TryGetValue(path!, out var map) ? map : default;
	}

	public RouteMap? FindByViewMap(ViewMap viewMap)
	{
		foreach (var rm in Mappings.Values)
		{
			if (rm.View == viewMap)
			{
				return rm;
			}
		}

		return default;
	}

	public virtual RouteMap? FindByViewModel(Type? viewModelType)
	{
		return FindRouteByType(viewModelType, map => map.View?.ViewModel);
	}

	public virtual RouteMap? FindByView(Type? viewType)
	{
		return FindRouteByType(viewType, map => map.View?.RenderView);
	}

	public RouteMap? FindByData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.View?.Data?.Data);
	}

	public RouteMap? FindByResultData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.View?.ResultData);
	}

	private RouteMap? FindRouteByType(Type? typeToFind, Func<RouteMap, Type?> mapType)
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
