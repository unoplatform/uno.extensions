namespace Uno.Extensions.Navigation;

internal class NavigatorFactory : INavigatorFactory
{
	public IDictionary<string, (Type, bool)> Navigators { get; } = new Dictionary<string, (Type, bool)>();

	private ILogger Logger { get; }

	private IRouteResolver Resolver { get; }

	public NavigatorFactory(
		ILogger<NavigatorFactory> logger,
		IEnumerable<NavigatorFactoryBuilder> builders,
		IRouteResolver resolver)
	{
		Logger = logger;
		Resolver = resolver;
		builders.ForEach(builder => builder.Configure?.Invoke(this));
	}

	public void RegisterNavigator<TNavigator>(bool requestRegion, params string[] names)
		where TNavigator : INavigator
	{
		names.ForEach(name =>
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Registering {typeof(TNavigator)} for region {name}");
			}

			Navigators[name] = (typeof(TNavigator), requestRegion);

		});
	}

	public INavigator? CreateService(IRegion region)
	{
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Adding region");
		}

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
		services.AddScopedInstance<INavigator>(navService);

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
		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Adding region");
		}

		if (region.Services is null)
		{
			return null;
		}

		var services = region.Services.CreateNavigationScope();

		var dialogRegion = new NavigationRegion(region.Services.GetRequiredService<ILogger<NavigationRegion>>(), services: services);
		services.AddScopedInstance<IRegion>(dialogRegion);

		var mapping = Resolver.FindByPath(request.Route.Base);
		var serviceLookupType = mapping?.RenderView;
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

		services.AddScopedInstance<INavigator>(navService);

		return navService;
	}
}
