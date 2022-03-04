namespace Uno.Extensions.Navigation;

public interface IRouteResolver
{
	RouteMap? Parent(RouteMap? routeMap);

	RouteMap? Find(Route? route);

	RouteMap? FindByPath(string? path);

	RouteMap? FindByViewMap(ViewMap viewMap);

	RouteMap? FindByViewModel(Type? viewModelType);

	RouteMap? FindByView(Type? viewType);

	RouteMap? FindByData(Type? dataType);

	RouteMap? FindByResultData(Type? resultDataType);
}
