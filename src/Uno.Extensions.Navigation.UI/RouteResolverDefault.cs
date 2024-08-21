using System.Reflection;

namespace Uno.Extensions.Navigation;

public class RouteResolverDefault : RouteResolver
{
	public bool ReturnImplicitMapping { get; set; } = true;

	public string[] ViewSuffixes { get; set; } = new[] { "View", "Page", "Control", "Flyout", "Dialog", "Popup" };

	public string[] ViewModelSuffixes { get; set; } = new[] { "Model", "ViewModel", "VM" };

	private IDictionary<string, Type>? loadedTypes;

	public RouteResolverDefault(
		ILogger<RouteResolverDefault> logger,
		IRouteRegistry routes,
		IViewRegistry views
		) : base(logger, routes, views)
	{
	}

	protected override RouteInfo? InternalFindByPath(string? path)
	{
		var map = base.InternalFindByPath(path);
		return map ?? DefaultMapping(path: path).FirstOrDefault();
	}

	protected override RouteInfo[] InternalFindByViewModel(Type? viewModel)
	{
		var map = base.InternalFindByViewModel(viewModel);
		return map.Any() ? map : DefaultMapping(viewModel: viewModel);
	}

	protected override RouteInfo[] InternalFindByView(Type? view)
	{
		var map = base.InternalFindByView(view);
		return map.Any() ? map : DefaultMapping(view: view);
	}


	private RouteInfo[] DefaultMapping(string? path = null, Type? view = null, Type? viewModel = null)
	{
		var routeMap = InternalDefaultMapping(path, view, viewModel);
		if (routeMap is not null)
		{
			// If the default mapping is being created by a mapped route resolver, the un-mapped
			// routemap may already be added to the Mappings table, so remove it.
			Mappings.Remove(route => route.Path == routeMap.Path);
			Mappings.Add(routeMap);
			return new[] { routeMap };
		}
		return Array.Empty<RouteInfo>();
	}

	protected virtual RouteInfo? InternalDefaultMapping(string? path = null, Type? view = null, Type? viewModel = null)
	{
		if (path is null &&
			view is null &&
			viewModel is null)
		{
			return default;
		}

		if (!ReturnImplicitMapping)
		{
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Implicit mapping disabled");
			return default;
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
			if (Logger.IsEnabled(LogLevel.Warning))
				Logger.LogWarningMessage($"Unable to resolve path from types. Path: '{path}', View: '{view?.Name}', ViewModel: '{viewModel?.Name}'");
			return default;
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
												ResultData: viewMap?.ResultData,
												IsDialogViewType: () =>
												{
													return IsDialogViewType(viewFunc?.Invoke());
												});
			Mappings.Add(defaultMapFromViewMap);
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Created default mapping from viewmap - Path '{defaultMapFromViewMap.Path}'");
			return defaultMapFromViewMap;
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

		if (view != null && IsCommonControlName(view.Name))
		{
			if (Logger.IsEnabled(LogLevel.Warning))
				Logger.LogWarningMessage($"Potential conflict detected: The route '{path}' resolved to a common control or class name '{view.Name}'. This could lead to unexpected behavior.");
		}

		if (path is not null &&
			!string.IsNullOrWhiteSpace(path))
		{
			var defaultMap = new RouteInfo(path, View: () => view, ViewModel: viewModel, IsDialogViewType: () =>
			{
				return IsDialogViewType(view);
			});
			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Created default mapping - Path '{defaultMap.Path}'");
			Mappings.Add(defaultMap);
			return defaultMap;
		}

		if (Logger.IsEnabled(LogLevel.Warning)) Logger.LogWarningMessage($"Unable to create default mapping");
		return null;
	}

	private bool IsCommonControlName(string name)
	{
		var commonNames = new List<string> { "Scroll", "List", "Grid", "Tree", "Web", "Navigation", "Content", "User", "Items", "Menu" };
		return commonNames.Contains(name);
	}

	private Type? TypeFromPath(string path, bool allowMatchExact, IEnumerable<string> suffixes, Func<Type, bool>? condition = null)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			if (Logger.IsEnabled(LogLevel.Warning))
				Logger.LogWarningMessage($"Navigation failed: Empty or null path provided.");
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
		if (Logger.IsEnabled(LogLevel.Warning))
			Logger.LogWarningMessage($"Navigation failed: Could not resolve type for path '{path}'.");

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

		var best = path;
		foreach (var item in suffixes)
		{
			var candidate = path.TrimEnd(item, StringComparison.InvariantCultureIgnoreCase);
			if (candidate.Length < best.Length)
			{
				best = candidate;
			}
		}

		return best;
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
