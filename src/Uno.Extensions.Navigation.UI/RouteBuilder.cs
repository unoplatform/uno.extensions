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

	public IRouteBuilder Register(ViewMap view)
	{
		Services.AddSingleton(view);
		if (view.ViewModelType is not null)
		{
			Services.AddTransient(view.ViewModelType);
		}

		return this;
	}
}
