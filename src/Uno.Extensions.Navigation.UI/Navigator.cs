namespace Uno.Extensions.Navigation;

public class Navigator : INavigator, IInstance<IServiceProvider>
{
	protected ILogger Logger { get; }

	protected IRegion Region { get; }

	private IRouteUpdater? RouteUpdater => Region.Services?.GetRequiredService<IRouteUpdater>();

	IServiceProvider? IInstance<IServiceProvider>.Instance => Region.Services;

	public Route? Route { get; protected set; }

	protected IRouteResolver Resolver { get; }

	internal IDispatcher Dispatcher { get; }

	public Navigator(
		ILogger<Navigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver)
		: this((ILogger)logger, dispatcher, region, resolver)
	{
	}

	protected Navigator(
		ILogger logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver)
	{
		Region = region;
		Logger = logger;
		Resolver = resolver;
		Dispatcher = dispatcher;
	}

	public async Task<NavigationResponse?> NavigateAsync(NavigationRequest request)
	{
		var regionUpdateId = RouteUpdater?.StartNavigation(Region) ?? Guid.Empty;
		try
		{
			if (request.Source is null)
			{
				if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformationMessage($"Starting Navigation - Navigator: {this.GetType().Name} Request: {request.Route}");
				request = request with { Source = this };
			}


			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($" Navigator: {this.GetType().Name} Request: {request.Route}");

			// Do any initialisation logic that may be
			// defined for the route - allows for
			// routes to be redirected
			request = InitializeRequest(request);

			// Redirect navigation if required
			// eg route that matches a child, should be routed to that child
			// eg route that doesn't match a page for frame nav should be sent to parent
			var redirection = await RedirectNavigateAsync(request);
			if (redirection is not null)
			{
				return await redirection;
			}

			// Append Internal qualifier to avoid requests being sent back to parent
			request = request with { Route = request.Route with { IsInternal = true } };


			// Make sure the view has completely loaded before trying to process the nav request
			// Typically this might happen with the first navigation of the application where the
			// window hasn't been activated yet, so the root region may not have loaded
			await Region.View.EnsureLoaded();

			if (request.Route.IsDialog())
			{
				// Dialogs will load a separate navigation hierarchy
				// so there's no need to route the request to child regions
				return await DialogNavigateAsync(request);
			}
			else
			{
				// Invoke the region specific navigation
				return await RegionNavigateAsync(request);
			}
		}
		finally
		{
			RouteUpdater?.EndNavigation(regionUpdateId);
		}
	}

	private async Task<Task<NavigationResponse?>?> RedirectNavigateAsync(NavigationRequest request)
	{
		if (request.Route.IsInternal)
		{
			return default;
		}


		// Deal with any named children that match the first segment of the request
		// In this case, the request should be trimmed
		var nested = Region.Children.Where(x => !string.IsNullOrWhiteSpace(request.Route.Base) && x.Name == request.Route.Base).ToArray();
		if (nested.Any() && !await ParentCanNavigate(request.Route))
		{
			request = request with { Route = request.Route.Next() };
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Redirecting to children ({nested.Length}) New request: {request.Route}");
			return NavigateChildRegions(nested, request);
		}


		// ./ route request to nested region (named or unnamed)
		if (request.Route.IsNested())
		{
			request = request with { Route = request.Route.TrimQualifier(Qualifiers.Nested) };

			// Now, deal with any unnamed children - send the request without
			// trimming the request
			nested = Region.Children.Where(x => string.IsNullOrWhiteSpace(x.Name) || x.Name == this.Route?.Base).ToArray();
			if (nested.Any())
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Forced redirection to children ({nested.Length}) New request: {request.Route}");
				return NavigateChildRegions(nested, request);
			}

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Forced redirection to children but no matching child regions found");
			return Task.FromResult(default(NavigationResponse?));
		}


		// ../ or ! route request to parent
		if (
			// Note: Disabling parent routing - leaving this code in case parent routing is required
			// request.Route.IsParent() ||
			request.Route.IsDialog()
			)
		{
			if (Region.Parent is not null)
			{
				// Note: Disabling parent routing - leaving this code in case parent routing is required
				// // Only trim ../ since ! will be handled by the root navigator
				// request = request with { Route = request.Route.TrimQualifier(Qualifiers.Parent) };

				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Redirecting to parent for dialog");
				return Region.Parent.NavigateAsync(request);
			}
			else
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: No redirection - at root region to handle dialog navigation request");
				return default;
			}
		}



		// / route request to root (via parent)
		if (request.Route.IsRoot())
		{
			if (Region.Parent?.Parent is null)
			{
				// If parent's Parent is null, then parent is the root
				// so trim the root qualifier.
				request = request with { Route = request.Route.TrimQualifier(Qualifiers.Root) with { IsInternal = true } };
			}

			// If the original request came into the root navigator, then
			// need to redirect request to the same navigator with the
			// root qualifier stripped
			var region = Region.Parent is not null ? Region.Parent : Region;

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Updating request and redirecting for root request.  New request: {request.Route}");
			return region.NavigateAsync(request);
		}

		var rm = Resolver.FindByPath(request.Route.Base);

		// If
		//		route has DependsOn AND
		//		the current route equals the DependsOn value AND
		//		there is an un-named child region
		// Then
		//		route request to child region
		if (!string.IsNullOrWhiteSpace(rm?.DependsOn) &&
			(Region.Ancestors(true).FirstOrDefault(x => x.Item1?.Base == rm!.DependsOn) is { } ancestor) &&
			ancestor.Item2 != Region.Parent)
		{
			var ancestorRegion = ancestor.Item2;
			if (ancestorRegion is not null)
			{
				foreach (var child in ancestorRegion.Children)
				{
					if (child.IsUnnamed(ancestor.Item1) &&
						await child.CanNavigate(request.Route))
					{
						return child.NavigateAsync(request);
					}
				}
			}
		}


		// If the current navigator can handle this route,
		// then simply return without redirecting the request

		// Navigator can handle the request as it's presented
		//		a) route has depends on that matches current route, return true
		//		b) route has depends on that doesn't match current route - if parent can navigate to dependson, return false
		//		c) route has no depends on - if parent can navigate to the route, return false

		if (await CanNavigate(request.Route) &&
			!await ParentCanNavigate(request.Route))
		{
			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: No redirection - Navigator can handle request (and parent cannot)");
			return default;
		}

		// If this is a back/close with no other path, then return
		// as if this navigator can handl it - it can't, so the request
		// will effetively be terminated
		if (request.Route.IsBackOrCloseNavigation())
		{
			if (Region.Parent is not null)
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Redirecting back navigation to parent");
				return Region.Parent.NavigateAsync(request);
			}

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Back navigation being handled by root region");
			return default;
		}


		if (rm is null)
		{
			if (Region.Parent is not null)
			{
				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: No routemap redirecting to parent");
				return Region.Parent.NavigateAsync(request);
			}

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: No routemap to be handled by root region");
			return default;
		}

		if (Region.Parent is not null)
		{
			if (!string.IsNullOrWhiteSpace(rm?.DependsOn))
			{
				var depends = rm?.DependsOn;
				var parent = Region.Parent;
				while (parent is not null)
				{
					if (parent.Navigator()?.Route?.Base == depends)
					{
						if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Depends on matches current route of parent, so redirecting to parent");
						return parent.NavigateAsync(request);
					}
					parent = parent.Parent;
				}

				request = request with { Route = (request.Route with { Base = depends, Path = null }).Append(request.Route) };

				if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Updating request with depends on and invoking navigate on current region. New request: {request.Route}");
				return Region.NavigateAsync(request);
			}

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Redirecting unhandled request to parent");
			return Region.Parent.NavigateAsync(request);
		}
		else
		{
			var routeMaps = new List<RouteInfo> { rm };
			var parent = rm.Parent;
			while (parent is not null)
			{
				routeMaps.Insert(0, parent);
				parent = parent.Parent;
			}
			var route = new Route(Qualifiers.None, Data: request.Route.Data);
			route = BuildFullRoute(route, routeMaps);
			route = route with { Qualifier = Qualifiers.Root };
			request = request with { Route = route };

			if (Logger.IsEnabled(LogLevel.Trace)) Logger.LogTraceMessage($"RedirectNavigateAsync: Building fully qualified route for unhandled request. New request: {request.Route}");
			return Region.NavigateAsync(request);
		}
	}


	private static Route BuildFullRoute(Route route, IEnumerable<RouteInfo> maps)
	{
		foreach (var map in maps)
		{
			if (route.IsEmpty())
			{
				route = route with { Base = map.Path };
			}
			else
			{
				route = route.Append(map.Path);
			}
		}

		return route;
	}


	private NavigationRequest InitializeRequest(NavigationRequest request)
	{
		// If this is an empty request on the root region
		// then look up the default route (ie for startup logic)
		if (Region.Parent is null &&
			request.Route.IsEmpty())
		{
			// Clear any existing route information to make
			// sure the navigation is restarted
			this.Route = Route.Empty;

			// Get the first route map
			var map = Resolver.FindByPath(string.Empty);
			if (map is not null)
			{
				request = request with { Route = request.Route.Append(map.Path) };
			}

			// Append Internal qualifier to avoid requests being sent back to parent
			request = request with { Route = request.Route with { IsInternal = true } };
		}

		var requestMap = Resolver.FindByPath(request.Route.Base);
		if (requestMap?.Init is not null)
		{
			var newRequest = requestMap.Init(request);
			while (!request.SameRouteBase(newRequest))
			{
				request = newRequest;
				requestMap = Resolver.FindByPath(request.Route.Base);
				if (requestMap?.Init is not null)
				{
					newRequest = requestMap.Init(request);
				}
			}
			request = newRequest;
		}

		return request;
	}

	public Task<bool> CanNavigate(Route route)
	{
		if (!route.IsInternal)
		{
			// Only root region should handle dialogs
			if (Region.Parent is not null)
			{
				if (route.IsDialog())
				{
					return Task.FromResult(false);
				}

			}
		}

		var routeMap = Resolver.FindByPath(route.Base);

		var canNav = RegionCanNavigate(route, routeMap);
		return canNav;
	}

	private Task<bool> ParentCanNavigate(Route route)
	{
		if (Region.Parent is null)
		{
			return Task.FromResult(false);
		}

		var parentNavigator = Region.Parent.Navigator();
		if (parentNavigator is not null &&
				(
					parentNavigator.IsComposite() ||
					// TODO: PanelVisibilityNavigator needs to be adapted to inherit from SelectorNavigator, or share an interface
					parentNavigator.GetType() == typeof(PanelVisiblityNavigator) ||
					(
						(parentNavigator.GetType().BaseType?.IsGenericType ?? false) &&
						parentNavigator.GetType().BaseType?.GetGenericTypeDefinition() == typeof(SelectorNavigator<>)
					)
				)
			)
		{
			return parentNavigator.CanNavigate(route);
		}

		return Task.FromResult(false);
	}

	protected virtual bool CanNavigateToDependentRoutes => false;


	protected virtual async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		// Default behaviour for all navigators is that they can't handle back or close requests
		// This is overridden by navigators that can handle close operation
		if (route.IsBackOrCloseNavigation())
		{
			return false;
		}

		// Default behaviour for all navigators is that they can't handle routes that are dependent
		// on another route (ie DependsOn <> "")
		// This is overridden by navigators that can handle close operation
		if ((routeMap?.IsDependent ?? false) &&
			!CanNavigateToDependentRoutes)
		{
			return false;
		}

		// Check type of this navigator - if base class (ie Navigator)
		// then an check children to see if they can navigate to the route
		// This won't cause a cycle as we force IsInternal to true, which will
		// avoid any parent checks in cannavigate
		// This is to support the concept of a composite region
		// whose sole responsibility is to forward navigation requests to all child regions
		if (this.IsComposite())
		{
			var internalRoute = route with { IsInternal = true };
			foreach (var child in Region.Children)
			{
				if (!await child.CanNavigate(internalRoute))
				{
					return false;
				}
			}
		}

		return true;
	}

	private async Task<NavigationResponse?> DialogNavigateAsync(NavigationRequest request)
	{
		var dialogService = Region.Services?.GetService<INavigatorFactory>()?.CreateService(Region, request);

		// The "!" prefix is no longer required
		request = request with { Route = request.Route.TrimQualifier(Qualifiers.Dialog) };

		var dialogResponse = await (dialogService?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse?>(default));

		return dialogResponse;
	}

	private async Task<NavigationResponse?> RegionNavigateAsync(NavigationRequest request)
	{
		var services = Region.Services;
		if (services is null)
		{
			return default;
		}

		var mapping = Resolver.FindByPath(request.Route.Base);
		if (mapping?.ToQuery is not null)
		{
			request = request with { Route = request.Route with { Data = request.Route.Data?.AsParameters(mapping) } };
		}

		// Setup the navigation data (eg parameters to be injected into viewmodel)
		var dataFactor = services.GetRequiredService<NavigationDataProvider>();
		dataFactor.Parameters = (request.Route?.Data) ?? new Dictionary<string, object>();

		var responseFactory = services.GetRequiredService<IResponseNavigatorFactory>();
		// Create ResponseNavigator if result is requested
		var navigator = request.Result is not null ? request.GetResponseNavigator(responseFactory, this) : default;

		if (navigator is null)
		{
			// Since this navigation isn't requesting a response, make sure
			// the current INavigator is this navigator. This will have override
			// any responsenavigator that has been registered and avoid incorrectly
			// sending a response when simply navigating back
			services.AddScopedInstance<INavigator>(this);
		}

		var executedRoute = await CoreNavigateAsync(request);


		if (navigator is not null)
		{
			return navigator.AsResponseWithResult(executedRoute);
		}


		return executedRoute;

	}

	protected virtual async Task<NavigationResponse?> CoreNavigateAsync(NavigationRequest request)
	{
		// Don't propagate the response request further than a named region
		if (!string.IsNullOrWhiteSpace(Region.Name) && request.Result is not null)
		{
			request = request with { Result = null };
		}

		if(Region.Children.Count>0)
		{
			// Force navigators to be created on the UI thread before they're accessed
			var navigators = await Dispatcher.ExecuteAsync(async cancellation =>
			{
				return (from child in Region.Children
						let nav = child.Navigator()
						select nav).ToList();
			});
		}

		if (request.Route.IsEmpty())
		{
			// Check to see if there are any child regions, and if there are
			// whether there are any that don't already have a route
			if (Region.Children.Count == 0 ||
				Region.Children.All(r => !(r.GetRoute()?.IsEmpty() ?? true)))
			{
				return null;
			}

			var dataRoute = Resolver.FindByPath(request.Route.Base);
			if (dataRoute is not null &&
				!Region.Ancestors(true).Any(x=>x.Item1?.Base==dataRoute.Path))
			{
				request = request with { Route = request.Route with { Base = dataRoute.Path } };
			}
			else
			{
				var route = Resolver.FindByPath(this.Route?.Base);
				if (route is not null)
				{
					var defaultRoute = route.Nested?.FirstOrDefault(x => x.IsDefault);
					if (defaultRoute is not null)
					{
						if (Region.Children.FirstOrDefault(x => x.Name == defaultRoute.Path) is { } childRegion &&
							defaultRoute.Nested?.FirstOrDefault(x => x.IsDefault) is { } nestedDefaultRoute)
						{
							request = request with { Route = request.Route.Append(nestedDefaultRoute.Path) };
							return await childRegion.NavigateAsync(request);
						}

						request = request with { Route = request.Route.Append(defaultRoute.Path) };

					}
				}
			}

			if (request.Route.IsEmpty())
			{
				return null;
			}
		}

		if (request.Route.IsBackOrCloseNavigation() && !request.Route.IsClearBackstack())
		{
			return null;
		}

		var children = Region.Children.Where(region =>
										// Unnamed child regions
										string.IsNullOrWhiteSpace(region.Name) ||
										// Regions whose name matches the next route segment
										region.Name == request.Route.Base ||
										// Regions whose name matches the current route
										// eg currently selected tab
										region.Name == Route?.Base
									).ToArray();
		return await NavigateChildRegions(children, request);

	}

	private async Task<NavigationResponse?> NavigateChildRegions(IEnumerable<IRegion>? children, NavigationRequest request)
	{
		if (children is null)
		{
			return default;
		}

		// Append Internal qualifier to avoid requests being sent back to parent
		request = request with { Route = request.Route with { IsInternal = true } };

		var tasks = new List<Task<NavigationResponse?>>();
		foreach (var child in children)
		{
			var nav = child.Navigator();
			if (nav is not null)
			{
				tasks.Add(nav.NavigateAsync(request));
			}
		}

		await Task.WhenAll(tasks);
#pragma warning disable CA1849 // We've already waited all tasks at this point (see Task.WhenAll in line above)
		return tasks.FirstOrDefault(r => r.Result is not null)?.Result;
#pragma warning restore CA1849
	}

	public override string ToString()
	{
		var current = NavigatorToString;
		if (!string.IsNullOrWhiteSpace(current))
		{
			current = $"({current})";
		}
		return $"{this.GetType().Name}{current}";
	}

	protected virtual string NavigatorToString { get; } = string.Empty;
}
