using System.Diagnostics.CodeAnalysis;

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

	protected abstract Task<string?> Show(
		string? path,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type? viewType,
		object? data);

	protected override async Task<Route?> ExecuteRequestAsync(NavigationRequest request)
	{
		if (Control is null)
		{
			return default;
		}

		var route = request.Route;
		var mapping = Resolver.FindByPath(route.Base);

		if (Logger.IsEnabled(LogLevel.Debug))
		{
			if (mapping is not null)
				Logger.LogDebugMessage($"Route '{route.Base}' resolved to mapping: Path='{mapping.Path}', View={mapping.RenderView?.Name ?? "(none)"}");
			else
				Logger.LogDebugMessage($"Route '{route.Base}' has no explicit mapping — relying on auto-resolve or panel child lookup");
		}

		var executedPath = await Show(mapping?.Path ?? route.Base, mapping?.RenderView, route.Data);

		if (executedPath is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
				Logger.LogWarningMessage($"Navigation to '{route.Base}' failed: Show() returned null. No matching view was found or created. Ensure a RouteMap is registered or a Page type named '{route.Base}Page' (or similar suffix) exists in the assembly.");

			// Hot-reload may add the missing type after this point. Remember the
			// request so NavigationRouteUpdateHandler can retry it once the
			// resolver has been rebuilt with the newly-registered types. Cleared
			// in the success branch below or when a superseding request arrives.
			RememberPendingFailedRequest(request);

			return Route.Empty;
		}

		ClearPendingFailedRequest();

		if (executedPath.Length == 0)
		{
			// Benign success: Show mounted a wrapper view (e.g. the FrameView
			// that ContentControlNavigator wraps a Page in) whose own navigator
			// owns the actual page route. We deliberately skip the DataContext
			// part of InitializeCurrentView — setting it on the wrapper would
			// propagate the page's ViewModel down to the wrapper's children and
			// break the FrameView contract (which intentionally nulls its
			// DataContext to prevent inheritance). But we MUST still wait for
			// FrameView.EnsureLoaded(): that call drives the inner Frame's
			// activation, which is what populates the wrapper region's child
			// region tree (the Frame's NavigationRegion) so the IsDefault
			// cascade has something to dispatch into. Skipping EnsureLoaded
			// leaves Region.Children empty and the next nav into this region
			// trips the "Region has no children to forward request to" warning.
			if (CurrentView is FrameView fv)
			{
				await fv.EnsureLoaded();
			}
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

	protected object? CreateControlFromType(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type typeToCreate)
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

	// The most recent NavigationRequest whose Show() resolved to null because
	// the target view type could not be created (typically the type doesn't
	// exist yet in the loaded assembly — Studio Live / hot-reload scaffolding
	// scenario). NavigationRouteUpdateHandler walks the live region tree after
	// a C# or XAML hot-reload and re-issues these requests so an initial
	// navigation that fired before the missing type was hot-reloaded in can
	// self-heal without requiring a full app restart. Only accessed on the UI
	// dispatcher thread (ExecuteRequestAsync runs under Dispatcher.ExecuteAsync;
	// the HR retry walk is dispatched via TryEnqueue), so no synchronization
	// is required.
	private NavigationRequest? _pendingFailedRequest;

	internal bool HasPendingFailedRequest => _pendingFailedRequest is not null;

	protected void RememberPendingFailedRequest(NavigationRequest request)
	{
		_pendingFailedRequest = request;
	}

	protected void ClearPendingFailedRequest()
	{
		_pendingFailedRequest = null;
	}

	/// <summary>
	/// Re-issues the most recent failed navigation request, if one is pending.
	/// Called by <see cref="UI.NavigationRouteUpdateHandler"/> after a C# or
	/// XAML hot-reload has potentially added the previously-missing type to
	/// the running assembly. The pending slot is cleared before re-issuing so
	/// a second failure cleanly re-arms it for the next hot-reload cycle.
	/// </summary>
	internal Task RetryPendingFailedRequestAsync()
	{
		var pending = _pendingFailedRequest;
		if (pending is null)
		{
			return Task.CompletedTask;
		}

		_pendingFailedRequest = null;

		if (Logger.IsEnabled(LogLevel.Information))
		{
			Logger.LogInformationMessage($"Retrying pending navigation '{pending.Route.Base}' after hot-reload");
		}

		return NavigateAsync(pending);
	}

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

		// Allow subclasses to adjust the request before forwarding to child regions.
		// This is used by FrameNavigator to restore nested route state (e.g., TabBar selection)
		// when navigating back.
		request = AdjustRequestForChildNavigation(request);

		var coreResponse = await base.CoreNavigateAsync(request);

		return coreResponse ?? regionResponse;
	}

	/// <summary>
	/// Called after the control-specific navigation is done but before the request is forwarded
	/// to child regions. Subclasses can override this to adjust the remaining request route,
	/// for example to restore previously cached nested route state.
	/// </summary>
	protected virtual NavigationRequest AdjustRequestForChildNavigation(NavigationRequest request)
		=> request;

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
					dataFactor.Parameters = parameters;

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
