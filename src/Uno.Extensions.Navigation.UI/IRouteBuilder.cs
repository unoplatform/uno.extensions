using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public interface IRouteBuilder
{
	IRouteBuilder Register(RouteMap route);

	IRouteBuilder Register(ViewMap view);

	IRouteBuilder Register<TData>(ViewMap<TData> route)
		where TData : class;

	IRouteBuilder Register<TData, TResultData>(ViewMap<TData, TResultData> route)
		where TData : class
		where TResultData : class;
}
