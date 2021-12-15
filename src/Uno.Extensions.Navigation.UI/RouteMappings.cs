namespace Uno.Extensions.Navigation;

public class RouteMappings : IRouteResolver //, IViewResolver
{
	protected IList<RouteMap> Mappings { get; } = new List<RouteMap>();
	//protected IDictionary<Type, ViewMap> ViewMappings { get; } = new Dictionary<Type, ViewMap>();

	protected ILogger Logger { get; }

	protected RouteMappings(ILogger logger, IRouteRegistry routes)//, IEnumerable<ViewMap> viewMaps)
	{
		Logger = logger;

		var maps = routes.Routes;

		if (maps is not null)
		{
			Mappings.AddRange(maps.Flatten());
		}
	}

	public RouteMappings(
		ILogger<RouteMappings> logger,
		IRouteRegistry routes
		//, IEnumerable<ViewMap> viewMaps
		) : this((ILogger)logger, routes
			//, viewMaps
			)
	{
	}

	public RouteMap? Find(Route? route) => route is not null ? FindByPath(route.Base) : Mappings.FirstOrDefault();

	public virtual RouteMap? FindByPath(string? path)
	{
		if (
			path is null ||
			string.IsNullOrWhiteSpace(path) ||
			path == Schemes.Parent ||
			path == Schemes.Current)
		{
			return null;
		}

		return Mappings.FirstOrDefault(map => map.Path == path);
	}

	public RouteMap? FindByViewModel(Type? viewModelType)
	{
		return FindRouteByType(viewModelType, map => map.ViewModel);
	}

	public RouteMap? FindByView(Type? viewType)
	{
		return FindRouteByType(viewType, map => map.View);
	}

	public virtual RouteMap? FindByData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.Data);
	}

	public virtual RouteMap? FindByResultData(Type? dataType)
	{
		return FindRouteByType(dataType, map => map.ResultData);
	}

	//private RouteMap? FindRouteByViewMapType(Type? typeToFind, Func<ViewMap, Type?> mapType)
	//{
	//	var viewMap = FindByInheritedTypes(ViewMappings, typeToFind, mapType);
	//	return FindByView(viewMap?.View);
	//}

	private RouteMap? FindRouteByType(Type? typeToFind, Func<RouteMap, Type?> mapType)
	{
		return FindByInheritedTypes(Mappings, typeToFind, mapType);
	}

	//private ViewMap? FindViewByRouteMapType(Type? typeToFind, Func<RouteMap, Type?> mapType)
	//{
	//	var routeMap = FindByInheritedTypes(Mappings, typeToFind, mapType);
	//	return FindViewByPath(routeMap?.Path);
	//}

	//private ViewMap? FindViewByType(Type? typeToFind, Func<ViewMap, Type?> mapType)
	//{
	//	return FindByInheritedTypes(ViewMappings, typeToFind, mapType);
	//}

	private TMap? FindByInheritedTypes<TMap>(IList<TMap> mappings, Type? typeToFind, Func<TMap, Type?> mapType)
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

	//public ViewMap? FindView(Route route) => FindViewByPath(route.Base);
	//public virtual ViewMap? FindViewByPath(string? path)
	//{
	//	if (
	//		path is null ||
	//		string.IsNullOrWhiteSpace(path) ||
	//		path == Schemes.Parent ||
	//		path == Schemes.Current)
	//	{
	//		return null;
	//	}

	//	var routeMap = FindByPath(path);
	//	return FindViewByView(routeMap?.View);
	//}

	//public virtual ViewMap? FindViewByViewModel(Type? viewModelType)
	//{
	//	return FindViewByType(viewModelType, map => map.ViewModel);
	//}
	//public virtual ViewMap? FindViewByView(Type? viewType)
	//{
	//	return FindViewByType(viewType, map => map.View);
	//}

	//public ViewMap? FindViewByData(Type? dataType)
	//{
	//	return FindViewByType(dataType, map => map.Data);
	//}

	//public ViewMap? FindViewByResultData(Type? resultDataType)
	//{
	//	return FindViewByType(resultDataType, map => map.ResultData);
	//}
}
