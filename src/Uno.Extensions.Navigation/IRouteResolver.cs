namespace Uno.Extensions.Navigation;

public interface IRouteResolver 
{
	InternalRouteMap? Parent(InternalRouteMap? routeMap);

	InternalRouteMap? Find(Route? route);

	InternalRouteMap? FindByPath(string? path);

	InternalRouteMap? FindByViewModel(Type? viewModelType);

	InternalRouteMap? FindByView(Type? viewType);

	InternalRouteMap? FindByData(Type? dataType);

	InternalRouteMap? FindByResultData(Type? resultDataType);
}
