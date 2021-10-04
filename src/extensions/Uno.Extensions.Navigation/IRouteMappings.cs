using System;

namespace Uno.Extensions.Navigation;

public interface IRouteMappings
{
    void Register(RouteMap map);

    RouteMap LookupByPath(string path);

    RouteMap LookupByViewModel(Type viewModelType);

    RouteMap LookupByView(Type viewType);

    RouteMap LookupByData(Type dataType);
}
