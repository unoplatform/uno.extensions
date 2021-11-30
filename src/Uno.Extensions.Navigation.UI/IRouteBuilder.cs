using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public interface IRouteBuilder
{
	IRouteBuilder Register(RouteMap route);
	IRouteBuilder Register<TData>(RouteMap<TData> route)
		where TData : class;
	IRouteBuilder Register<TData, TResultData>(RouteMap<TData, TResultData> route)
		where TData : class
		where TResultData : class;

	IRouteBuilder Register(ViewMap view);
}
