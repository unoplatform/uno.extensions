using Uno.Extensions.Logging;
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class NavigatorFactory : INavigatorFactory
{
	public IDictionary<string, (Type, bool)> Navigators { get; } = new Dictionary<string, (Type, bool)>();

	private ILogger Logger { get; }

	private IRouteResolver RouteResolver { get; }

	public NavigatorFactory(
		ILogger<NavigatorFactory> logger,
		IEnumerable<NavigatorFactoryBuilder> builders,
		IRouteResolver routeResolver)
	{
		Logger = logger;
		RouteResolver = routeResolver;
		builders.ForEach(builder => builder.Configure?.Invoke(this));
	}

	public void RegisterNavigator<TNavigator>(bool requestRegion, params string[] names)
		where TNavigator : INavigator
	{
		names.ForEach(name => Navigators[name] = (typeof(TNavigator), requestRegion));
	}

	public INavigator? CreateService(IRegion region)
	{
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Adding region");

		var services = region.Services;
		var control = region.View;

		if (services is null)
		{
			return default;
		}


		INavigator? navService = null;

		if (control is not null)
		{
			services.GetRequiredService<RegionControlProvider>().RegionControl = control;

			var navigatorType = NavigatorForControl(control);
			navService = services.GetService(navigatorType) as INavigator;
		}

		if (navService is null)
		{
			navService = services.GetRequiredService<Navigator>();
		}

		// Make sure the nav service gets added to the container before initialize
		// is invoked to prevent reentry
		services.AddInstance<INavigator>(navService);

		if (navService is ControlNavigator controlService)
		{
			controlService.ControlInitialize();
		}

		// Retrieve the region container and the navigation service
		return navService;
	}

	private Type NavigatorForControl(FrameworkElement control)
	{
		var navigator = control.GetNavigator() ?? control.GetType().Name;
		if (Navigators.TryGetValue(navigator, out var serviceType))
		{
			return serviceType.Item1;
		}

		var baseTypes = control.GetType().GetBaseTypes().Select(t => t.Name);
		foreach (var baseType in baseTypes)
		{
			if (Navigators.TryGetValue(baseType, out var serviceType2))
			{
				return serviceType2.Item1;
			}

		}

		return typeof(Navigator);
	}

	public INavigator? CreateService(IRegion region, NavigationRequest request)
	{
		if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Adding region");

		if (region.Services is null)
		{
			return null;
		}

		var scope = region.Services.CreateScope();
		var services = scope.ServiceProvider;

		var dialogRegion = new NavigationRegion(services: services);
		services.AddInstance<IRegion>(dialogRegion);

		var mapping = RouteResolver.FindByPath(request.Route.Base);
		var serviceLookupType = mapping?.View;
		if (serviceLookupType is null)
		{
			object? resource = request.RouteResourceView(region);
			serviceLookupType = resource?.GetType();
		}

		if (serviceLookupType is null)
		{
			return null;
		}

		var serviceType = this.FindRequestServiceByType(serviceLookupType);//  ServiceTypes[mapping.View.Name];
		if (serviceType is null)
		{
			if (request.Route.IsDialog())
			{
				serviceType = this.FindRequestServiceByType(typeof(Flyout));
			}

			if (serviceType is null)
			{
				return null;
			}
		}

		var navService = services.GetRequiredService(serviceType) as INavigator;
		if (navService is null)
		{
			return null;
		}

		services.AddInstance<INavigator>(navService);

		return navService;
	}
}
