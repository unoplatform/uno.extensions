using System.Reflection;

namespace Uno.Extensions.Navigation;

public class RouteResolverDefault : RouteResolver
{
	public bool ReturnImplicitMapping { get; set; } = true;

	public string[] ViewSuffixes { get; set; } = new[] { "View", "Page", "Control", "Flyout", "Dialog", "Popup" };

	public string[] ViewModelSuffixes { get; set; } = new[] { "ViewModel", "VM" };

	private IDictionary<string, Type>? loadedTypes;

	public RouteResolverDefault(
		ILogger<RouteResolverDefault> logger,
		IRouteRegistry routes,
		IViewRegistry views
		) : base(logger, routes, views)
	{
	}

	public override RouteInfo? FindByPath(string? path)
	{
		var map = base.FindByPath(path);
		return map ?? DefaultMapping(path: path).FirstOrDefault();
	}

	public override RouteInfo[] FindByViewModel(Type? viewModel)
	{
		var map = base.FindByViewModel(viewModel);
		return map.Any() ? map : DefaultMapping(viewModel: viewModel);
	}

	public override RouteInfo[] FindByView(Type? view)
	{
		var map = base.FindByView(view);
		return map.Any() ? map : DefaultMapping(view: view);
	}


	private RouteInfo[] DefaultMapping(string? path = null, Type? view = null, Type? viewModel = null)
	{
		if (!ReturnImplicitMapping)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Implicit mapping disabled");
			return Array.Empty<RouteInfo>();
		}

		// Trim any qualifiers
		path = path.ExtractBase(out _, out _);

		// If no path is provided, attempt to get a path
		// from the view or viewmodel type provided
		if (string.IsNullOrWhiteSpace(path))
		{
			path = PathFromTypes(view, viewModel);
		}

		// If path is still null, we can't build a mapping, so just return
		if (path is null ||
			string.IsNullOrWhiteSpace(path))
		{
			return Array.Empty<RouteInfo>();
		}

		// Attempt to find a viewmap to build the routeinfo from
		var viewMap = ViewMaps.Items.FirstOrDefault(x =>
		{
			var mapView = x.View ?? x.ViewSelector?.Invoke();
			var mapViewModel = x.ViewModel;
			var mapPath = PathFromTypes(mapView, mapViewModel);
			return mapPath == path;
		});
		if (viewMap is not null)
		{
			var viewFunc = (viewMap.View is not null) ?
										() => viewMap.View :
										viewMap.ViewSelector;
			var defaultMapFromViewMap = new RouteInfo(
												Path: path,
												View: viewFunc,
												ViewAttributes: viewMap.ViewAttributes,
												ViewModel: viewMap.ViewModel,
												Data: viewMap.Data?.Data,
												ToQuery: viewMap?.Data?.UntypedToQuery,
												FromQuery: viewMap?.Data?.UntypedFromQuery,
												ResultData: viewMap?.ResultData);
			Mappings[defaultMapFromViewMap.Path] = defaultMapFromViewMap;
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Created default mapping from viewmap - Path '{defaultMapFromViewMap.Path}'");
		}

		if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformationMessage($"For better performance (avoid reflection), create mapping for for path '{path}', view '{view?.Name}', view model '{viewModel?.Name}'");

		if (view is null)
		{
			var trimmedPath = TrimSuffices(path, ViewModelSuffixes);
			view = TypeFromPath(trimmedPath, true, ViewSuffixes, type => type.IsSubclassOf(typeof(FrameworkElement)));
		}

		if (viewModel is null)
		{
			var trimmedPath = TrimSuffices(path, ViewSuffixes);
			viewModel = TypeFromPath(trimmedPath, false, ViewModelSuffixes);
		}

		if (path is not null &&
			!string.IsNullOrWhiteSpace(path))
		{
			var defaultMap = new RouteInfo(path, View: () => view, ViewModel: viewModel);
			Mappings[defaultMap.Path] = defaultMap;
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Created default mapping - Path '{defaultMap.Path}'");
			return new RouteInfo[] { defaultMap };
		}

		if (Logger.IsEnabled(LogLevel.Warning)) Logger.LogWarningMessage($"Unable to create default mapping");
		return Array.Empty<RouteInfo>();
	}

	private Type? TypeFromPath(string path, bool allowMatchExact, IEnumerable<string> suffixes, Func<Type, bool>? condition = null)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return default;
		}

		if (allowMatchExact && LoadedTypes.TryGetValue(path, out var type))
		{
			return type;
		}

		foreach (var suffix in suffixes)
		{
			if (LoadedTypes.TryGetValue($"{path}{suffix}", out type))
			{
				if (condition?.Invoke(type) ?? true)
				{
					return type;
				}
			}
		}

		return null;
	}

	private string PathFromTypes(Type? view, Type? viewModel)
	{
		if (view is null && viewModel is null)
		{
			return string.Empty;
		}

		var path = ViewTypeToPath(view);
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		return ViewModelTypeToPath(viewModel);
	}

	private string ViewTypeToPath(Type? view)
	{
		return TypeToPath(view, ViewSuffixes);
	}

	private string ViewModelTypeToPath(Type? view)
	{
		return TypeToPath(view, ViewModelSuffixes);
	}

	private string TypeToPath(Type? view, IEnumerable<string> suffixes)
	{
		var path = view?.Name + string.Empty;
		return TrimSuffices(path, suffixes);
	}

	private string TrimSuffices(string? path, IEnumerable<string> suffixes)
	{
		if (path is null)
		{
			return string.Empty;
		}

		foreach (var item in suffixes)
		{
			path = path.TrimEnd(item, StringComparison.InvariantCultureIgnoreCase);
		}

		return path;
	}

	public IDictionary<string, Type> LoadedTypes
	{
		get
		{
			if (loadedTypes is null)
			{
				loadedTypes = (from asb in AppDomain.CurrentDomain.GetAssemblies()
							   where (!(asb.FullName ?? string.Empty).StartsWith("_") && !AssemblyExtensions.Excludes.Contains(asb.FullName ?? string.Empty))
							   from t in asb.SafeGetTypes()
							   where t.IsClass
							   select new { t.Name, Type = t }).ToDictionaryDistinct(x => x.Name, x => x.Type);
			}

			return loadedTypes;
		}
	}
}

public static class AssemblyExtensions
{
	public static IList<string> Excludes { get; } = new List<string>();
	public static Type[] SafeGetTypes(this Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch
		{
			if (assembly.FullName is not null)
			{
				Excludes.Add(assembly.FullName);
			}
			return new Type[] { };
		}
	}
}
