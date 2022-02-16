namespace Uno.Extensions.Navigation;

public class RouteResolver : IRouteResolver
{
	private RouteMap First { get; }
	protected IDictionary<string, RouteMap> Mappings { get; } = new Dictionary<string, RouteMap>();

	protected ILogger Logger { get; }

	protected RouteResolver(ILogger logger, IRouteRegistry routes)
	{
		Logger = logger;

		var maps = routes.Routes;

		if (maps is not null)
		{
			First = maps.FirstOrDefault();
			maps.Flatten().ForEach(route => Mappings[route.Path] = route);
		}


		var messageDialogRoute = new RouteMap(
			Path: typeof(MessageDialog).Name,
			View: typeof(MessageDialog),
			ResultData: typeof(MessageDialog)
		);

		// Make sure the message dialog is the last route to be listed
		Mappings[messageDialogRoute.Path] = messageDialogRoute;
	}

	public RouteResolver(
		ILogger<RouteResolver> logger,
		IRouteRegistry routes
		) : this((ILogger)logger, routes
			)
	{
	}

	public RouteMap? Find(Route? route) => route is not null ? FindByPath(route.Base) : First;

	public virtual RouteMap? FindByPath(string? path)
	{
		if (
			path is null ||
			string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		path = path.ExtractBase(out var nextQualifier, out var nextPath);

		return Mappings.TryGetValue(path!, out var map) ? map : default;
	}

	public virtual RouteMap? FindByViewModel(Type? viewModelType)
	{
		return FindRouteByType(viewModelType, map => map.ViewModel);
	}

	public virtual RouteMap? FindByView(Type? viewType)
	{
		return FindRouteByType(viewType, map => map.View);
	}

	public RouteMap? FindByData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.Data);
	}

	public RouteMap? FindByResultData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.ResultData);
	}

	private RouteMap? FindRouteByType(Type? typeToFind, Func<RouteMap, Type?> mapType)
	{
		return FindByInheritedTypes(Mappings, typeToFind, mapType);
	}

	private TMap? FindByInheritedTypes<TMap>(IDictionary<string,TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
	{
		if (typeToFind is null)
		{
			return default;
		}

		// Handle the non-reflection check first
		var map = (from m in mappings.Values
				   where mapType(m) == typeToFind
				   select m)
				   .FirstOrDefault();
		if (map is not null)
		{
			return map;
		}

		return (from baseType in typeToFind.GetBaseTypes()
				from m in mappings.Values
				where mapType(m) == baseType
				select m)
				   .FirstOrDefault();
	}
}
