using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation;

public class RouteMappings : IRouteMappings
{
    public bool ReturnImplicitMapping { get; set; } = true;

    public string[] ViewSuffixes { get; set; } = new[] { "View", "Page", "Control" };

    public string[] ViewModelSuffixes { get; set; } = new[] { "ViewModel", "VM" };

    private IDictionary<string, Type> loadedTypes;

    private IDictionary<string, RouteMap> Mappings { get; } = new Dictionary<string, RouteMap>();

    private ILogger Logger { get; }

    public RouteMappings(ILogger<RouteMappings> logger)
    {
        Logger = logger;
    }

    public void Register(RouteMap map)
    {
        Mappings[map.Path] = map;
    }

    public RouteMap LookupByPath(string path)
    {
        if (path == NavigationConstants.RelativePath.BackPath ||
            path == NavigationConstants.RelativePath.Current ||
            path == NavigationConstants.RelativePath.Nested)
        {
            return null;
        }

        return Mappings.TryGetValue(path, out var map) ? map : DefaultMapping(path: path);
    }

    public RouteMap LookupByViewModel(Type viewModelType)
    {
        return (Mappings.FirstOrDefault(x => x.Value.ViewModel == viewModelType).Value) ?? DefaultMapping(viewModel: viewModelType);
    }

    public RouteMap LookupByView(Type viewType)
    {
        return (Mappings.FirstOrDefault(x => x.Value.View == viewType).Value) ?? DefaultMapping(view: viewType);
    }

    public RouteMap LookupByData(Type dataType)
    {
        return Mappings.First(x => x.Value.Data == dataType).Value;
    }

    private RouteMap DefaultMapping(string path = null, Type view = null, Type viewModel = null)
    {
        if (!ReturnImplicitMapping)
        {
            Logger.LazyLogDebug(() => "Implicit mapping disabled");
            return null;
        }

        Logger.LazyLogWarning(() => $"For better performance (avoid reflection), create mapping for for path '{path}', view '{view?.Name}', view model '{viewModel?.Name}'");

        Logger.LazyLogDebug(() => $"Creating default mapping for path '{path}', view '{view?.Name}', view model '{viewModel?.Name}'");
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

        if (!string.IsNullOrWhiteSpace(path))
        {
            var defaultMap = new RouteMap(path, view, viewModel, null);
            Mappings[path] = defaultMap;
            Logger.LazyLogDebug(() => $"Created default mapping - Path '{defaultMap.Path}', View '{defaultMap.View?.Name}', View Model '{defaultMap.ViewModel?.Name}'");
            return defaultMap;
        }

        Logger.LazyLogDebug(() => $"Unable to create default mapping");
        return null;
    }

    private Type TypeFromPath(string path, bool allowMatchExact, IEnumerable<string> suffixes, Func<Type, bool> condition = null)
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

    private string PathFromTypes(Type view, Type viewModel)
    {
        var path = ViewTypeToPath(view);
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return ViewModelTypeToPath(viewModel);
    }

    private string ViewTypeToPath(Type view)
    {
        return TypeToPath(view, ViewSuffixes);
    }

    private string ViewModelTypeToPath(Type view)
    {
        return TypeToPath(view, ViewModelSuffixes);
    }

    private string TypeToPath(Type view, IEnumerable<string> suffixes)
    {
        var path = view?.Name + string.Empty;
        return TrimSuffices(path, suffixes);
    }

    private string TrimSuffices(string path, IEnumerable<string> suffixes)
    {
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
                               where (!asb.FullName.StartsWith("_") &&
                               !asb.FullName.StartsWith("Microsoft.") &&
                               !asb.FullName.StartsWith("Uno."))
                               from t in asb.GetTypes()
                               where t.IsClass
                               select new { t.Name, Type = t }).ToDictionaryDistinct(x => x.Name, x => x.Type);
            }

            return loadedTypes;
        }
    }
}
