namespace Uno.Extensions.Navigation;

public interface IRouteResolver
{
	RouteInfo? FindByPath(string? path);

	RouteInfo? FindByViewModel(Type? viewModelType, INavigator? navigator);

	RouteInfo? FindByView(Type? viewType, INavigator? navigator);

	RouteInfo? FindByData(Type? dataType, INavigator? navigator);

	RouteInfo? FindByResultData(Type? resultDataType, INavigator? navigator);

	void InsertRoute(RouteInfo routeInfo);
}
