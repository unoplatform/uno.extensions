using System;

namespace Uno.Extensions.Navigation;

public interface INavigationMapping
{
    void Register(NavigationMap map);

    NavigationMap LookupByPath(string path);

    NavigationMap LookupByViewModel(Type viewModelType);

    NavigationMap LookupByView(Type viewType);

    NavigationMap LookupByData(Type dataType);
}
