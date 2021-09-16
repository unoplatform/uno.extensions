using System;
using System.Collections.Generic;
using System.Linq;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation;

public class NavigationMapping : INavigationMapping
{
    public bool ReturnImplicitMapping { get; set; } = true;

    private IDictionary<string, NavigationMap> Mappings { get; } = new Dictionary<string, NavigationMap>();

    public void Register(NavigationMap map)
    {
        Mappings[map.Path] = map;
    }

    public NavigationMap LookupByPath(string path)
    {
        return Mappings.TryGetValue(path, out var map) ? map : default;
    }

    public NavigationMap LookupByViewModel(Type viewModelType)
    {
        return (Mappings.FirstOrDefault(x => x.Value.ViewModel == viewModelType).Value) ?? DefaultForType(viewModelType, false, true);
    }

    public NavigationMap LookupByView(Type viewType)
    {
        return (Mappings.FirstOrDefault(x => x.Value.View == viewType).Value) ?? DefaultForType(viewType, true, false);
    }

    public NavigationMap LookupByData(Type dataType)
    {
        return Mappings.First(x => x.Value.Data == dataType).Value;
    }

    private NavigationMap DefaultForType(Type type, bool isView, bool isViewModel)
    {
        return ReturnImplicitMapping ? new NavigationMap(type.Name, isView ? type : null, isViewModel ? type : null, null) : null;
    }
}
