using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public class RouteBuilder : IRouteBuilder
{
	private IServiceCollection Services { get; }
	public RouteBuilder(IServiceCollection services)
	{
		Services = services;
	}

	public IRouteBuilder Register(RouteMap route)
	{
		Services.AddSingleton(route);
		if (route.ViewModel is not null)
		{
			Services.AddTransient(route.ViewModel);
		}
		return this;
	}

	public IRouteBuilder Register<TData>(RouteMap<TData> route)
		where TData : class
	{
		Register((RouteMap)route);

		Services.AddViewModelData<TData>();
		return this;
	}

	public IRouteBuilder Register<TData, TResultData>(RouteMap<TData, TResultData> route)
		where TData : class
		where TResultData : class
	{
		Register((RouteMap<TData>)route);

		Services.AddViewModelData<TResultData>();
		return this;
	}
}
