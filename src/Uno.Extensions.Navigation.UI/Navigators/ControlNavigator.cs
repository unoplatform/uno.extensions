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
			// ContentControlNavigator/PanelVisiblityNavigator deliberately return
			// null when they wrap a Page-subclass view in a FrameView — the page
			// itself is the navigation target and the wrapper's DataContext is
			// intentionally nulled (see FrameView ctor) to prevent ViewModel
			// inheritance. Returning Route.Empty here is the right thing for the
			// downstream pipeline (Trim keeps the request route intact, so the
			// inner Frame's FrameNavigator receives the page navigation), but we
			// MUST still wait for the FrameView to load — otherwise the inner
			// Frame's NavigationRegion has no chance to register as a child of
			// this region before the parent navigator forwards the request, and
			// the cascade has nothing to dispatch into. Skip InitializeCurrentView
			// itself — it would set DataContext on the FrameView wrapper and
			// override FrameView's null DataContext invariant.
			if (mapping?.RenderView is { } renderView && renderView.IsSubclassOf(typeof(Page))
				&& CurrentView is FrameView fv)
			{
				await fv.EnsureLoaded();
				ClearPendingFailedRequest();
				return Route.Empty;
			}

			// A SelectorNavigator (TabBar / NavigationView) returns null from Show() by
			// design: RegionCanNavigate already verified the item exists, Show() selected
			// it, and the page itself is rendered by the sibling content region. This is a
			// successful delegation, not a missing view — so clear any stale pending slot
			// and return Route.Empty WITHOUT warning and WITHOUT recording a pending HR
			// retry. Recording one here would let the hot-reload retry walk re-issue a
			// phantom selector navigation on every later delta, thrashing the active tab.
			// The route flow is identical to the failure path below —
			// both return Route.Empty, so Trim keeps the request route intact for the
			// sibling/child regions. See spec 004.
			if (IsNullShowResultExpected)
			{
				ClearPendingFailedRequest();
				return Route.Empty;
			}

			// Only warn on the FIRST failure for a given route. Hot-reload's poll
			// pattern fires this branch on every HR delta until the missing type
			// finally loads — that's expected behavior of the pending-retry path,
			// but logging at Warning every time floods the bundle with N repeats
			// of the same diagnostic and drowns out real misconfigurations.
			// Distinguish "first time we've seen this route fail" (real signal —
			// might be a typo or missing RouteMap registration) from "this route
			// is already in the pending-retry queue" (already known, just waiting
			// for HR to deliver the type) by comparing against the pending slot's
			// current route. A bare null check on the pending slot isn't enough
			// because a different failing route would have replaced the slot.
			var sameRouteRetrying = HasPendingFailedRequestFor(route);

			if (!sameRouteRetrying && Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Navigation to '{route.Base}' failed: Show() returned null. No matching view was found or created. Ensure a RouteMap is registered or a Page type named '{route.Base}Page' (or similar suffix) exists in the assembly.");
			}
			else if (sameRouteRetrying && Logger.IsEnabled(LogLevel.Debug))
			{
				Logger.LogDebugMessage($"Navigation to '{route.Base}' failed again: Show() returned null (pending retry — type not yet hot-reloaded in).");
			}

			// Hot-reload may add the missing type after this point. Remember the
			// request so NavigationRouteUpdateHandler can retry it once the
			// resolver has been rebuilt with the newly registered types. The
			// retry is cleared on the success branch below, or replaced when a
			// superseding request fails.
			RememberPendingFailedRequest(request);

			return Route.Empty;
		}

		// If we were in a pending-retry loop for this same route, the success
		// here is the moment the missing type finally landed via hot-reload.
		if (HasPendingFailedRequestFor(route) && Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Navigation to '{route.Base}' recovered after hot-reload (pending retry succeeded).");
		}

		ClearPendingFailedRequest();

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

	/// <summary>
	/// When <c>true</c>, a <c>null</c> result from <see cref="ControlNavigator{TControl}.Show"/>
	/// is a successful delegation rather than a failed view resolution, so it must not be
	/// recorded as a pending hot-reload retry.
	/// </summary>
	/// <remarks>
	/// A <see cref="SelectorNavigator{TControl}"/> (TabBar / NavigationView) verifies the
	/// requested item exists, selects it in
	/// <c>Show()</c>, and intentionally returns <c>null</c> so the route flows to the
	/// sibling content region that renders the page. Classifying that <c>null</c> as a
	/// missing view wrongly arms <see cref="RememberPendingFailedRequest"/>, which the
	/// hot-reload retry walk (<see cref="UI.NavigationRouteUpdateHandler"/>) then re-issues
	/// on every later delta — thrashing the active selection — and logs a misleading
	/// "Show() returned null" warning on every navigation. See spec 004.
	/// </remarks>
	protected virtual bool IsNullShowResultExpected => false;

	// The most recent NavigationRequest whose Show() resolved to null because
	// the target view type could not be created — typically the type doesn't
	// exist yet in the loaded assembly (hot-reload scaffolding).
	// NavigationRouteUpdateHandler walks the live region tree after a C# or
	// XAML hot-reload and re-issues these requests so an initial navigation
	// that fired before the missing type was hot-reloaded in can self-heal
	// without requiring a full app restart. Only accessed on the UI dispatcher
	// thread (ExecuteRequestAsync runs under Dispatcher.ExecuteAsync; the HR
	// retry walk is dispatched via TryEnqueue), so no synchronization needed.
	private NavigationRequest? _pendingFailedRequest;

	internal bool HasPendingFailedRequest => _pendingFailedRequest is not null;

	internal void RememberPendingFailedRequest(NavigationRequest request)
	{
		_pendingFailedRequest = request;
	}

	internal void ClearPendingFailedRequest()
	{
		_pendingFailedRequest = null;
	}

	// True when there is a pending failed request whose Route.Base matches `route`.
	// Lets ExecuteRequestAsync in the derived generic class identify "this is a
	// re-fire of the same HR poll" without reaching into the private pending slot.
	internal bool HasPendingFailedRequestFor(Route route)
		=> string.Equals(_pendingFailedRequest?.Route.Base, route.Base, StringComparison.Ordinal);

	/// <summary>
	/// Re-issues the most recent failed navigation request, if one is pending.
	/// Called by <see cref="UI.NavigationRouteUpdateHandler"/> after a C# or
	/// XAML hot-reload has potentially added the previously-missing type to
	/// the running assembly.
	/// </summary>
	/// <remarks>
	/// We deliberately do NOT clear <see cref="_pendingFailedRequest"/> before
	/// invoking NavigateAsync. If the retry fails again (the type
	/// is still missing on this HR delta), the failure path in
	/// <see cref="ControlNavigator{TControl}.ExecuteRequestAsync"/> needs to
	/// detect "this is the same route we already had pending" via
	/// <see cref="HasPendingFailedRequestFor"/> so it can demote the warning to
	/// Debug. Clearing the slot here would race the retry's own failure path:
	/// by the time ExecuteRequestAsync reads the slot, it would be empty and
	/// the retry's re-failure would log as if it were a brand-new failure,
	/// re-flooding the bundle. On success, the success branch of
	/// <see cref="ControlNavigator{TControl}.ExecuteRequestAsync"/> calls
	/// <see cref="ClearPendingFailedRequest"/>; on failure, the failure branch
	/// re-arms via <see cref="RememberPendingFailedRequest"/> (a self-overwrite
	/// with the same request — net no-op).
	/// </remarks>
	internal Task RetryPendingFailedRequestAsync()
	{
		var pending = _pendingFailedRequest;
		if (pending is null)
		{
			return Task.CompletedTask;
		}

		if (Logger.IsEnabled(LogLevel.Debug))
		{
			Logger.LogDebugMessage($"Retrying pending navigation '{pending.Route.Base}' after hot-reload");
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
