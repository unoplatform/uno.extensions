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
		if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Pre-navigation: - {Region.Root().ToString()}");
		var regionUpdateId = RouteUpdater?.StartNavigation(Region) ?? Guid.Empty;
		try
		{
			// Do any initialisation logic that may be
			// defined for the route - allows for
			// routes to be redirected
			request = InitializeRequest(request);

			// Redirect navigation if required
			// eg route that matches a child, should be routed to that child
			// eg route that doesn't match a page for frame nav should be sent to parent
			var redirection = RedirectNavigateAsync(request);
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
			if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Post-navigation: {Region.Root().ToString()}");
			if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Post-navigation (route): {Region.Root().GetRoute()}");
			RouteUpdater?.EndNavigation(regionUpdateId);
		}
	}

	private Task<NavigationResponse?>? RedirectNavigateAsync(NavigationRequest request)
	{
		if (request.Route.IsInternal)
		{
			return default;
		}


		// Deal with any named children that match the first segment of the request
		// In this case, the request should be trimmed
		var nested = Region.Children.Where(x => !string.IsNullOrWhiteSpace(request.Route.Base) && x.Name == request.Route.Base).ToArray();
		if (nested.Any())
		{
			request = request with { Route = request.Route.Next() };
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
				return NavigateChildRegions(nested, request);
			}

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

				return Region.Parent.NavigateAsync(request);
			}
			else
			{
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
			return region.NavigateAsync(request);
		}

		// Exception: If this region is an unnamed child of a composite,
		// send request to parent
		if (!Region.IsNamed() &&
			Region.Parent is not null)
		{
			return Region.Parent.NavigateAsync(request);
		}

		// If the current navigator can handle this route,
		// then simply return without redirecting the request
		if (CanNavigateToRoute(request.Route))
		{
			return default;
		}

		// If this is a back/close with no other path, then return
		// as if this navigator can handl it - it can't, so the request
		// will effetively be terminated
		if (request.Route.IsBackOrCloseNavigation())
		{
			return default;
		}

		var rm = Resolver.FindByPath(request.Route.Base);
		if (rm is null)
		{
			return default;
		}

		if (Region.Parent is not null)
		{
			if (!string.IsNullOrWhiteSpace(rm?.DependsOn))
			{
				request = request with { Route = (request.Route with { Base = rm?.DependsOn, Path = null }).Append(request.Route) };
			}
			return Region.Parent.NavigateAsync(request);
		}
		else
		{
			var routeMaps = new List<InternalRouteMap> { rm };
			var parent = Resolver.Parent(rm);
			while (parent is not null)
			{
				routeMaps.Insert(0, parent);
				parent = Resolver.Parent(parent);
			}
			var route = new Route(Qualifiers.None, Data: request.Route.Data);
			route = BuildFullRoute(route, routeMaps);
			route = route with { Qualifier = Qualifiers.Root };
			request = request with { Route = route };

			return Region.NavigateAsync(request);
		}
	}


	private static Route BuildFullRoute(Route route, IEnumerable<InternalRouteMap> maps)
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
			var map = Resolver.Find(null);
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

	// By default, all navigators can handle all routes
	// except where it's dialog - these should only be
	// handled by the root (ie Region.Parent is null)
	protected virtual bool CanNavigateToRoute(Route route) => (Region.Parent is null || !route.IsDialog()) && !route.IsBackOrCloseNavigation();

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

		var mapping = Resolver.Find(request.Route);
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
		if (request.Route.IsEmpty())
		{
			var dataRoute = Resolver.Find(request.Route);
			if (dataRoute is not null)
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
						request = request with { Route = request.Route.Append(defaultRoute.Path) };

					}
				}
			}

			if (request.Route.IsEmpty())
			{
				return null;
			}
		}

		// Don't propagate the response request further than a named region
		if (!string.IsNullOrWhiteSpace(Region.Name) && request.Result is not null)
		{
			request = request with { Result = null };
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

		var navigators = await Dispatcher.ExecuteAsync(async () =>
		{
			return (from child in children
					let nav = child.Navigator()
					select nav).ToList();
		});

		var tasks = new List<Task<NavigationResponse?>>();
		foreach (var region in navigators)
		{
			tasks.Add(region.NavigateAsync(request));
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
