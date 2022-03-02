using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public class Navigator : INavigator, IInstance<IServiceProvider>
{
	protected ILogger Logger { get; }

	protected IRegion Region { get; }

	private IRouteUpdater? RouteUpdater => Region.Services?.GetRequiredService<IRouteUpdater>();

	IServiceProvider? IInstance<IServiceProvider>.Instance => Region.Services;

	public Route? Route { get; protected set; }

	protected IResolver Resolver { get; }

	public Navigator(ILogger<Navigator> logger, IRegion region, IResolver resolver)
		: this((ILogger)logger, region, resolver)
	{
	}

	protected Navigator(ILogger logger, IRegion region, IResolver resolver)
	{
		Region = region;
		Logger = logger;
		Resolver = resolver;
	}

	public async Task<NavigationResponse?> NavigateAsync(NavigationRequest request)
	{
		if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Pre-navigation: - {Region.Root().ToString()}");
		var regionUpdateId = RouteUpdater?.StartNavigation(Region) ?? Guid.Empty;
		try
		{

			request = InitialiseRequest(request);

			// If this isn't an internal request, then check to
			// see if the request needs to be redirected to a
			// different navigator.
			// eg route that matches a child, should be routed to that child
			// eg route that doesn't match a page for frame nav should be sent to parent
			if (!request.Route.IsInternal)
			{
				var redirection = RedirectRequest(request);
				if (redirection is not null)
				{
					return await redirection;
				}
			}
			
			// Append Internal qualifier to avoid requests being sent back to parent
			request = request with { Route = request.Route with { IsInternal = true } };

			// If this is an empty request on the root region
			// then look up the default route (ie for startup logic)
			if (Region.Parent is null &&
				request.Route.IsEmpty())
			{
				// Clear any existing route information to make
				// sure the navigation is restarted
				this.Route = Route.Empty;

				// Get the first route map
				var map = Resolver.Routes.Find(null);
				if (map is not null)
				{
					request = request with { Route = request.Route.Append(map.Path) };
				}
			}

			// Run dialog requests
			if (request.Route.IsDialog())
			{
				return await DialogNavigateAsync(request);
			}
			else
			{
				// Make sure the view has completely loaded before trying to process the nav request
				// Typically this might happen with the first navigation of the application where the
				// window hasn't been activated yet, so the root region may not have loaded
				await Region.View.EnsureLoaded();

				return await ResponseNavigateAsync(request);
			}
		}
		finally
		{
			if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Post-navigation: {Region.Root().ToString()}");
			if (Logger.IsEnabled(LogLevel.Information)) Logger.LogInformation($"Post-navigation (route): {Region.Root().GetRoute()}");
			RouteUpdater?.EndNavigation(regionUpdateId);
		}
	}

	private Task<NavigationResponse?>? RedirectRequest(NavigationRequest request)
	{
		// Deal with any named children that match the first segment of the request
		// In this case, the request should be trimmed
		var nested = Region.FindChildren(x => !string.IsNullOrWhiteSpace(request.Route.Base) && x.Name == request.Route.Base).ToArray();
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
			nested = Region.FindChildren(x => string.IsNullOrWhiteSpace(x.Name) || x.Name==this.Route?.Base).ToArray();
			if (nested.Any())
			{
				return NavigateChildRegions(nested, request);
			}

			return Task.FromResult(default(NavigationResponse?));
		}


		// ../ or ! route request to parent
		if ((request.Route.IsParent() ||
			request.Route.IsDialog()) &&
			Region.Parent is not null)
		{
			// Only trim ../ since ! will be handled by the root navigator
			request = request with { Route = request.Route.TrimQualifier(Qualifiers.Parent) };

			return Region.Parent.NavigateAsync(request);
		}



		// / route request to root (via parent)
		if (request.Route.IsRoot())
		{
			if (Region.Parent?.Parent is null)
			{
				// If parent's Parent is null, then parent is the root
				// so trim the root qualifier.
				request = request with { Route = request.Route.TrimQualifier(Qualifiers.Root) };
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

		// If the request can't be handled (or redirect), then
		// default to sending the request to the parent
		return Region.Parent?.NavigateAsync(request);
	}

	private NavigationRequest InitialiseRequest(NavigationRequest request)
	{
		var requestMap = Resolver.Routes.FindByPath(request.Route.Base);
		if (requestMap?.Init is not null)
		{
			var newRequest = requestMap.Init(request);
			while (!request.SameRouteBase(newRequest))
			{
				request = newRequest;
				requestMap = Resolver.Routes.FindByPath(request.Route.Base);
				if (requestMap?.Init is not null)
				{
					newRequest = requestMap.Init(request);
				}
			}
			request = newRequest;
		}
		return request;
	}

	// The base navigator can't handle navigating to any routes
	// This doesn't reflect whether there are any parent or child
	// regions that can process this request
	protected virtual bool CanNavigateToRoute(Route route) => false; 

	private async Task<NavigationResponse?> DialogNavigateAsync(NavigationRequest request)
	{
		var dialogService = Region.Services?.GetService<INavigatorFactory>()?.CreateService(Region, request);

		// The "!" prefix is no longer required
		request = request with { Route = request.Route.TrimQualifier(Qualifiers.Dialog) };

		var dialogResponse = await (dialogService?.NavigateAsync(request) ?? Task.FromResult<NavigationResponse?>(default));

		return dialogResponse;
	}

	private async Task<NavigationResponse?> ResponseNavigateAsync(NavigationRequest request)
	{
		var services = Region.Services;
		if (services is null)
		{
			return default;
		}

		var mapping = Resolver.Routes.Find(request.Route);
		if (mapping?.View?.Data?.UntypedToQuery is not null)
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
			services.AddInstance<INavigator>(this);
		}

		if(!string.IsNullOrWhiteSpace(this.Region.Name) &&
			this.Region.Name == request.Route?.Base &&
			this.CanNavigateToRoute(request.Route?.Next()))
		{
			request = request with { Route = request.Route.Next() };
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
			var route = Resolver.Routes.FindByPath(this.Route?.Base);
			if (route is not null)
			{
				var defaultRoute = route.Nested?.FirstOrDefault(x => x.IsDefault);
				if (defaultRoute is not null)
				{
					request = request with { Route = request.Route.Append(defaultRoute.Path) };

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
		if(children is null)
		{
			return default;
		}

		var tasks = new List<Task<NavigationResponse?>>();
		foreach (var region in children)
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
