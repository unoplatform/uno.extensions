namespace Uno.Extensions.Navigation.Navigators;

public abstract class ControlNavigator<TControl> : ControlNavigator
	where TControl : class
{
	public virtual TControl? Control { get; }

	protected ControlNavigator(
		ILogger logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		TControl? control)
		: base(logger, dispatcher, region, resolver)
	{
		Control = control;
	}

	protected override Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		if (Control is null)
		{
			return Task.FromResult(false);
		}
		return base.RegionCanNavigate(route, routeMap);
	}

	protected virtual FrameworkElement? CurrentView => default;

	protected abstract Task<string?> Show(string? path, Type? viewType, object? data);

	protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		if (Control is null)
		{
			return default;
		}

		var route = request.Route;
		var mapping = Resolver.FindByPath(route.Base);

		var executedPath = await Show(mapping?.Path ?? route.Base, mapping?.RenderView, route.Data);

		if (executedPath is null)
		{
			return Route.Empty;
		}

		var executedRoute = route with { Base = executedPath, Path = null };

		await InitializeCurrentView(request, executedRoute, mapping);

		return executedRoute;
	}

	protected async Task<object?> InitializeCurrentView(NavigationRequest request, Route route, RouteInfo? mapping, bool refresh = false)
	{
		var view = CurrentView;

		if (view is null)
		{
			return null;
		}

		var navigator = Region.Navigator();

		if (view is FrameView fv)
		{
			await fv.EnsureLoaded();
			navigator = fv.Navigator;
		}


		var services = navigator?.Get<IServiceProvider>();

		if (navigator is null ||
			services is null)
		{
			return null;
		}

		var viewModel = view.DataContext;
		if (refresh ||
			viewModel is null ||
			viewModel.GetType() != mapping?.ViewModel)
		{
			// This will happen if cache mode isn't set to required
			viewModel = await CreateViewModel(services, request, route, mapping);
		}

		await view.InjectServicesAndSetDataContextAsync(services, navigator, viewModel);

		return viewModel;
	}


	protected override string NavigatorToString => (Route?.ToString()) ?? string.Empty;

	protected object? CreateControlFromType(Type typeToCreate)
	{
		try
		{
			var services = this.Get<IServiceProvider>();

			if (typeToCreate is null) return default;
			if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformationMessage($"Creating control of type {typeToCreate.Name}");
			if (typeToCreate == typeof(FrameView)) return new FrameView();
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"Control not FrameView - {typeToCreate.Name}");
			if (services?.GetService(typeToCreate) is { } ctrl) return ctrl;
			if (Logger.IsEnabled(LogLevel.Warning)) Logger.LogWarningMessage($"Type not registered {typeToCreate.Name}, so calling Activator");

			return Activator.CreateInstance(typeToCreate);
		}
		catch
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to create control of type {typeToCreate.Name}");
			return default;
		}
	}
}

public abstract class ControlNavigator : Navigator
{
	public virtual bool CanGoBack => false;

	protected ControlNavigator(
		ILogger logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver)
		: base(logger, dispatcher, region, resolver)
	{
	}

	protected async override Task<NavigationResponse?> CoreNavigateAsync(NavigationRequest request)
	{
		var regionResponse = await ControlCoreNavigateAsync(request);

		if (regionResponse is not null)
		{
			if (!regionResponse.Success)
			{
				return regionResponse;
			}

			request = request with { Route = request.Route.Trim(regionResponse?.Route) };
		}

		var coreResponse = await base.CoreNavigateAsync(request);

		return coreResponse ?? regionResponse;
	}

	private async Task<NavigationResponse?> ControlCoreNavigateAsync(NavigationRequest request)
	{
		var routeMap = Resolver.FindByPath(request.Route.Base);
		if (await RegionCanNavigate(request.Route, routeMap))
		{
			return await Dispatcher.ExecuteAsync(async cancellation =>
				{
					return await ControlNavigateAsync(request);
				});
		}

		return default;
	}

	public virtual void ControlInitialize()
	{
	}

	protected async Task<NavigationResponse?> ControlNavigateAsync(NavigationRequest request)
	{
		var services = Region.Services;
		if (services is null)
		{
			return default;
		}

		var executedRoute = await ExecuteRequestAsync(request);

		UpdateRoute(executedRoute);

		return new NavigationResponse(executedRoute ?? Route.Empty, Navigator: this);
	}

	protected virtual void UpdateRoute(Route? route)
	{
		//var rm = Resolver.Find(route);
		Route = route is not null
			// && !(rm?.IsPrivate ?? false)
			? new Route(Qualifiers.None, route.Base, null, route.Data) : null;
	}

	protected async Task<object?> CreateViewModel(
		IServiceProvider services,
		NavigationRequest request,
		Route route,
		RouteInfo? mapping)
	{
		var navigator = services.GetInstance<INavigator>();
		if (mapping?.ViewModel is not null)
		{
			var parameters = route.Data ?? new Dictionary<string, object>();
			if (parameters.Any() &&
				mapping.FromQuery is not null)
			{
				var data = await mapping.FromQuery(services, parameters);
				if (data is not null)
				{
					parameters[string.Empty] = data;
				}
			}

			// Attempt to use the data object passed with navigation
			var vm = (parameters.TryGetValue(string.Empty, out var navData) &&
						(navData.GetType() == mapping.ViewModel || navData.GetType().IsSubclassOf(mapping.ViewModel))) ? navData : default;

			if (vm is null)
			{
				vm = await Task.Run(async () =>
				{

					// Attempt to create view model using the DI container
					var dataFactor = services.GetRequiredService<NavigationDataProvider>();
					dataFactor.Parameters = route.Data ?? new Dictionary<string, object>();

					services.AddScopedInstance(request);

					var created = services.GetService(mapping!.ViewModel);

					if (created is not null)
					{
						return created;
					}
					// Attempt to create view model using reflection
					try
					{
						var ctr = mapping.ViewModel.GetNavigationConstructor(navigator!, Region.Services!, out var args);
						if (ctr is not null)
						{
							return ctr.Invoke(args);
						}
					}
					catch
					{
						if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformationMessage("ViewModel not included in RouteMap, and unable to instance using Activator instead of ServiceProvider");
					}
					return default;
				});
			}

			if (vm is IInjectable<INavigator> navAware)
			{
				navAware.Inject(navigator!);
			}

			if (vm is IInjectable<IServiceProvider> spAware && Region.Services is not null)
			{
				spAware.Inject(Region.Services);
			}

			return vm;
		}

		return null;
	}

	protected abstract Task<Route?> ExecuteRequestAsync(NavigationRequest request);
}
