namespace Uno.Extensions.Navigation;

public interface IRouteResolver 
{
	RouteInfo? FindByPath(string? path);

	RouteInfo[] FindByViewModel(Type? viewModelType);

	RouteInfo[] FindByView(Type? viewType);

	RouteInfo[] FindByData(Type? dataType);

	RouteInfo[] FindByResultData(Type? resultDataType);
}
