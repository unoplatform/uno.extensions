﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

public class NavigationMapping : INavigationMapping
{
    public bool ReturnImplicitMapping { get; set; } = true;

    public string[] ViewSuffixes { get; set; } = new[] { "View", "Page", "Control" };

    public string[] ViewModelSuffixes { get; set; } = new[] { "ViewModel", "VM" };

    private IDictionary<string, Type> loadedTypes;

    private IDictionary<string, NavigationMap> Mappings { get; } = new Dictionary<string, NavigationMap>();

    public void Register(NavigationMap map)
    {
        Mappings[map.Path] = map;
    }

    public NavigationMap LookupByPath(string path)
    {
        if (path == NavigationConstants.RelativePath.BackPath ||
            path == NavigationConstants.RelativePath.Current ||
            path == NavigationConstants.RelativePath.Nested)
        {
            return null;
        }

        return Mappings.TryGetValue(path, out var map) ? map : DefaultMapping(path: path);
    }

    public NavigationMap LookupByViewModel(Type viewModelType)
    {
        return (Mappings.FirstOrDefault(x => x.Value.ViewModel == viewModelType).Value) ?? DefaultMapping(viewModel: viewModelType);
    }

    public NavigationMap LookupByView(Type viewType)
    {
        return (Mappings.FirstOrDefault(x => x.Value.View == viewType).Value) ?? DefaultMapping(view: viewType);
    }

    public NavigationMap LookupByData(Type dataType)
    {
        return Mappings.First(x => x.Value.Data == dataType).Value;
    }

    private NavigationMap DefaultMapping(string path = null, Type view = null, Type viewModel = null)
    {
        if (!ReturnImplicitMapping)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            path = PathFromTypes(view, viewModel);
        }

        if (view is null)
        {
            view = TypeFromPath(path, true, ViewSuffixes, type => type.IsSubclassOf(typeof(FrameworkElement)));
        }

        if (viewModel is null)
        {
            viewModel = TypeFromPath(path, false, ViewModelSuffixes);
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            var defaultMap = new NavigationMap(path, view, viewModel, null);
            Mappings[path] = defaultMap;
            return defaultMap;
        }

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
                               from t in asb.GetTypes()
                               where t.IsClass
                               select new { t.Name, Type = t }).ToDictionaryDistinct(x => x.Name, x => x.Type);
            }

            return loadedTypes;
        }
    }

}
