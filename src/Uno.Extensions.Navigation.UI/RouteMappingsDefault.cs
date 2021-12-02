using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
#if !WINUI
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation;

public class RouteMappingsDefault : RouteMappings
{
    public bool ReturnImplicitMapping { get; set; } = true;

    public string[] ViewSuffixes { get; set; } = new[] { "View", "Page", "Control", "Flyout", "Dialog", "Popup" };

    public string[] ViewModelSuffixes { get; set; } = new[] { "ViewModel", "VM" };

    private IDictionary<string, Type>? loadedTypes;

    public RouteMappingsDefault(ILogger<RouteMappingsDefault> logger, IEnumerable<RouteMap> maps, IEnumerable<ViewMap> viewMaps) : base(logger, maps, viewMaps)
    {
    }

    public override RouteMap? FindByPath(string? path)
    {
        var map = base.FindByPath(path);
        return map ?? DefaultMapping(path: path);
    }

	public override ViewMap? FindViewByPath(string? path)
	{
		var map = base.FindViewByPath(path);
		return map ?? DefaultViewMapping(path: path);
	}

	public override ViewMap? FindViewByViewModel(Type? viewModelType)
	{
		var map = base.FindViewByViewModel(viewModelType);
		return map ?? DefaultViewMapping(viewModel: viewModelType);
	}

	public override ViewMap? FindViewByView(Type? viewType)
	{
		var map = base.FindViewByView(viewType);
		return map ?? DefaultViewMapping(view: viewType);
	}


	private RouteMap? DefaultMapping(string? path = null)
    {
        if (!ReturnImplicitMapping)
        {
            Logger.LogDebugMessage("Implicit mapping disabled");
            return null;
        }

        if (path is not null &&
            !string.IsNullOrWhiteSpace(path))
        {
            var defaultMap = new RouteMap(path);
            Mappings[path] = defaultMap;
            Logger.LogDebugMessage($"Created default mapping - Path '{defaultMap.Path}'");
            return defaultMap;
        }

        Logger.LogDebugMessage($"Unable to create default mapping");
        return null;
    }


	private ViewMap? DefaultViewMapping(string? path = null, Type? view = null, Type? viewModel = null)
	{
		if (!ReturnImplicitMapping)
		{
			Logger.LogDebugMessage("Implicit mapping disabled");
			return null;
		}

		Logger.LogWarningMessage($"For better performance (avoid reflection), create mapping for for path '{path}', view '{view?.Name}', view model '{viewModel?.Name}'");

		Logger.LogDebugMessage($"Creating default mapping for path '{path}', view '{view?.Name}', view model '{viewModel?.Name}'");
		if (string.IsNullOrWhiteSpace(path))
		{
			path = PathFromTypes(view, viewModel);
		}

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
			!string.IsNullOrWhiteSpace(path) &&
			view is not null)
		{
			var defaultMap = new ViewMap(view, viewModel);
			ViewMappings[view] = defaultMap;
			Logger.LogDebugMessage($"Created default mapping - Path '{path}', View '{defaultMap.ViewType?.Name}', View Model '{defaultMap.ViewModelType?.Name}'");
			return defaultMap;
		}

		Logger.LogDebugMessage($"Unable to create default mapping");
		return null;
	}

	private Type? TypeFromPath(string path, bool allowMatchExact, IEnumerable<string> suffixes, Func<Type, bool>? condition = null)
    {
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
                               where (!(asb.FullName ?? string.Empty).StartsWith("_"))
                               from t in asb.GetTypes()
                               where t.IsClass
                               select new { t.Name, Type = t }).ToDictionaryDistinct(x => x.Name, x => x.Type);
            }

            return loadedTypes;
        }
    }
}
