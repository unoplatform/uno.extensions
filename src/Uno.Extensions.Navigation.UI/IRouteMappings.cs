using System;

namespace Uno.Extensions.Navigation;

public interface IMappings
{
    RouteMap? Find(Route route);

    RouteMap? FindByPath(string? path);

    RouteMap? FindByViewModel(Type? viewModelType);

    RouteMap? FindByView(Type? viewType);

    RouteMap? FindByData(Type? dataType);

    RouteMap? FindByResultData(Type? resultDataType);

	ViewMap? FindView(Route route);

	ViewMap? FindViewByPath(string? path);

	ViewMap? FindViewByViewModel(Type? viewModelType);

	ViewMap? FindViewByView(Type? viewType);

	ViewMap? FindViewByData(Type? dataType);

	ViewMap? FindViewByResultData(Type? resultDataType);
}
