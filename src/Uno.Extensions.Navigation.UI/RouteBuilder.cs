using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;

public class RouteBuilder : IViewRegistry, IRouteRegistry
{
	private IServiceCollection Services { get; }
	public RouteBuilder(IServiceCollection services)
	{
		Services = services;
	}

	public IRouteRegistry Register(RouteMap route)
	{
		Services.AddSingleton(route);
		return this;
	}

	public IViewRegistry Register(ViewMap view)
	{
		Services.AddSingleton(view);
		if (view.ViewModel is not null)
		{
			Services.AddTransient(view.ViewModel);
		}

		return this;
	}

	public IViewRegistry Register<TData>(ViewMap<TData> route)
	where TData : class
	{
		Register((ViewMap)route);

		Services.AddViewModelData<TData>();
		return this;
	}

	public IViewRegistry Register<TData, TResultData>(ViewMap<TData, TResultData> route)
		where TData : class
		where TResultData : class
	{
		Register((ViewMap<TData>)route);

		Services.AddViewModelData<TResultData>();
		return this;
	}
}
