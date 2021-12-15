namespace Uno.Extensions.Navigation;

public class RouteResolver : IRouteResolver
{
	protected IList<RouteMap> Mappings { get; } = new List<RouteMap>();

	protected ILogger Logger { get; }

	protected RouteResolver(ILogger logger, IRouteRegistry routes)//, IEnumerable<ViewMap> viewMaps)
	{
		Logger = logger;

		var maps = routes.Routes;

		if (maps is not null)
		{
			Mappings.AddRange(maps.Flatten());
		}
	}

	public RouteResolver(
		ILogger<RouteResolver> logger,
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

	private RouteMap? FindRouteByType(Type? typeToFind, Func<RouteMap, Type?> mapType)
	{
		return FindByInheritedTypes(Mappings, typeToFind, mapType);
	}

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
}
