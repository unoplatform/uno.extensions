using System;

namespace Uno.Extensions.Navigation;

public interface IRouteMappings
{
    RouteMap? Find(Route route);

    RouteMap? FindByPath(string? path);

    RouteMap? FindByViewModel(Type? viewModelType);

    RouteMap? FindByView(Type? viewType);

    RouteMap? FindByData(Type? dataType);

    RouteMap? FindByResultData(Type? resultDataType);
}
